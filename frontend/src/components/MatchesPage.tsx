// Responsabilidad: permite filtrar, paginar, programar y cancelar partidos del torneo.
// Relación: consume /api/matches y usa /api/teams como catálogo de participantes.
"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { apiRequest, createRequestId, toQuery } from "@/lib/api/client";
import { usePagedResource } from "@/hooks/usePagedResource";
import type { CreatedResource, Match, PagedResult, Team } from "@/types/api";
import { EmptyState, ErrorNotice, LoadingState, PageHeader, Pagination, StatusBadge } from "@/components/ui";

function formatDate(value: string) {
  return new Intl.DateTimeFormat("es-SV", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function MatchesPage() {
  const [teams, setTeams] = useState<Team[]>([]);
  const [catalogError, setCatalogError] = useState<unknown>(null);
  const [teamFilter, setTeamFilter] = useState("");
  const [status, setStatus] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const [homeTeamId, setHomeTeamId] = useState("");
  const [awayTeamId, setAwayTeamId] = useState("");
  const [scheduledAt, setScheduledAt] = useState("");
  const [saving, setSaving] = useState(false);
  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [mutationError, setMutationError] = useState<unknown>(null);
  const [cancelError, setCancelError] = useState<unknown>(null);
  const [notice, setNotice] = useState("");
  const idempotencyKey = useRef<string | null>(null);

  useEffect(() => {
    apiRequest<PagedResult<Team>>("/api/teams?pageNumber=1&pageSize=100&sortBy=name&sortDirection=asc")
      .then((result) => setTeams(result.data)).catch(setCatalogError);
  }, []);

  const path = useMemo(() => `/api/matches${toQuery({ teamId: teamFilter, status, from, to, pageNumber: page, pageSize: 10, sortBy: "scheduledAt", sortDirection: "asc" })}`,
    [teamFilter, status, from, to, page]);
  const { result, loading, error, reload } = usePagedResource<Match>(path);

  async function createMatch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setMutationError(null); setNotice("");
    idempotencyKey.current ??= createRequestId();
    try {
      await apiRequest<CreatedResource>("/api/matches", {
        method: "POST",
        body: JSON.stringify({ homeTeamId, awayTeamId, scheduledAt }),
        idempotencyKey: idempotencyKey.current,
      });
      setHomeTeamId(""); setAwayTeamId(""); setScheduledAt(""); idempotencyKey.current = null;
      setNotice("Partido programado correctamente."); reload();
    } catch (reason) { setMutationError(reason); }
    finally { setSaving(false); }
  }

  function resetKey() { idempotencyKey.current = null; }

  async function cancelMatch(match: Match) {
    const confirmed = window.confirm(
      `¿Cancelar el partido ${match.homeTeamName} vs. ${match.awayTeamName}?`,
    );
    if (!confirmed) return;

    setCancellingId(match.id); setCancelError(null); setNotice("");
    try {
      await apiRequest<void>(`/api/matches/${match.id}`, { method: "DELETE" });
      setNotice("Partido cancelado correctamente."); reload();
    } catch (reason) { setCancelError(reason); }
    finally { setCancellingId(null); }
  }

  return (
    <>
      <PageHeader eyebrow="Calendario" title="Partidos" description="Programa encuentros y consulta su estado, marcador y goles." />
      {notice && <div className="alert success" role="status">{notice}</div>}
      <div className="splitLayout">
        <section className="card formCard"><h2>Programar partido</h2>
          <form onSubmit={createMatch}>
            <label>Equipo local<select value={homeTeamId} onChange={(e) => { setHomeTeamId(e.target.value); resetKey(); }} required><option value="">Selecciona un equipo</option>{teams.map((team) => <option value={team.id} key={team.id}>{team.name}</option>)}</select></label>
            <label>Equipo visitante<select value={awayTeamId} onChange={(e) => { setAwayTeamId(e.target.value); resetKey(); }} required><option value="">Selecciona un equipo</option>{teams.filter((team) => team.id !== homeTeamId).map((team) => <option value={team.id} key={team.id}>{team.name}</option>)}</select></label>
            <label>Fecha y hora<input type="datetime-local" value={scheduledAt} onChange={(e) => { setScheduledAt(e.target.value); resetKey(); }} required /></label>
            <ErrorNotice error={catalogError ?? mutationError} />
            <button className="button primary" disabled={saving || teams.length < 2}>{saving ? "Guardando…" : "Programar partido"}</button>
          </form>
        </section>
        <section className="card dataCard"><div className="sectionHeading"><h2>Calendario</h2></div>
          <div className="filters">
            <label>Equipo<select value={teamFilter} onChange={(e) => { setPage(1); setTeamFilter(e.target.value); }}><option value="">Todos</option>{teams.map((team) => <option value={team.id} key={team.id}>{team.name}</option>)}</select></label>
            <label>Estado<select value={status} onChange={(e) => { setPage(1); setStatus(e.target.value); }}><option value="">Todos</option><option value="Scheduled">Programado</option><option value="Played">Finalizado</option><option value="Cancelled">Cancelado</option></select></label>
            <label>Desde<input type="date" value={from} onChange={(e) => { setPage(1); setFrom(e.target.value); }} /></label>
            <label>Hasta<input type="date" value={to} onChange={(e) => { setPage(1); setTo(e.target.value); }} /></label>
          </div>
          <ErrorNotice error={cancelError ?? error} />
          {loading ? <LoadingState /> : result.data.length === 0 ? <EmptyState /> :
            <div className="tableScroll">
              <table>
                <thead>
                  <tr>
                    <th>Partido</th>
                    <th>Fecha</th>
                    <th>Estado</th>
                    <th>Marcador</th>
                    <th>Acciones</th>
                  </tr>
                </thead>
                <tbody>{result.data.map((match) =>
                  <tr key={match.id}>
                    <td><strong>{match.homeTeamName}</strong> vs. <strong>{match.awayTeamName}</strong></td>
                    <td>{formatDate(match.scheduledAt)}</td>
                    <td><StatusBadge status={match.status} /></td>
                    <td>{match.homeScore === null ? "—" : `${match.homeScore} - ${match.awayScore}`}</td>
                    <td>
                      <div className="rowActions">
                        <Link className="textLink" href={`/partidos/${match.id}`}>Detalle</Link>
                        {match.status === "Scheduled" && <button className="button danger" type="button" onClick={() => cancelMatch(match)} disabled={cancellingId !== null}>{cancellingId === match.id ? "Cancelando…" : "Cancelar"}</button>}
                      </div>
                    </td>
                  </tr>)}
                </tbody>
              </table>
            </div>}
          <Pagination page={page} totalPages={result.totalPages} totalRecords={result.totalRecords} onChange={setPage} />
        </section>
      </div>
    </>
  );
}
