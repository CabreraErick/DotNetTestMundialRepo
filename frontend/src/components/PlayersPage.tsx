// Responsabilidad: filtra, pagina y registra jugadores asociados a equipos existentes.
// Relación: consume /api/players y utiliza /api/teams como catálogo de selección.
"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { apiRequest, createRequestId, toQuery } from "@/lib/api/client";
import { usePagedResource } from "@/hooks/usePagedResource";
import type { CreatedResource, PagedResult, Player, Team } from "@/types/api";
import { EmptyState, ErrorNotice, LoadingState, PageHeader, Pagination } from "@/components/ui";

export function PlayersPage() {
  const [teams, setTeams] = useState<Team[]>([]);
  const [catalogError, setCatalogError] = useState<unknown>(null);
  const [draftSearch, setDraftSearch] = useState("");
  const [search, setSearch] = useState("");
  const [teamFilter, setTeamFilter] = useState("");
  const [activeFilter, setActiveFilter] = useState("");
  const [page, setPage] = useState(1);
  const [teamId, setTeamId] = useState("");
  const [name, setName] = useState("");
  const [jerseyNumber, setJerseyNumber] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [mutationError, setMutationError] = useState<unknown>(null);
  const [notice, setNotice] = useState("");
  const idempotencyKey = useRef<string | null>(null);

  useEffect(() => {
    apiRequest<PagedResult<Team>>("/api/teams?pageNumber=1&pageSize=100&sortBy=name&sortDirection=asc")
      .then((pageResult) => setTeams(pageResult.data))
      .catch(setCatalogError);
  }, []);

  const path = useMemo(() => `/api/players${toQuery({ search, teamId: teamFilter, isActive: activeFilter, pageNumber: page, pageSize: 10, sortBy: "name", sortDirection: "asc" })}`,
    [search, teamFilter, activeFilter, page]);
  const { result, loading, error, reload } = usePagedResource<Player>(path);
  const teamNames = useMemo(() => new Map(teams.map((team) => [team.id, team.name])), [teams]);

  async function savePlayer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setMutationError(null); setNotice("");
    try {
      if (editingId) {
        await apiRequest<Player>(`/api/players/${editingId}`, {
          method: "PATCH",
          body: JSON.stringify({ name, jerseyNumber: Number(jerseyNumber) }),
        });
        setNotice("Jugador actualizado correctamente.");
      } else {
        idempotencyKey.current ??= createRequestId();
        await apiRequest<CreatedResource>("/api/players", {
          method: "POST",
          body: JSON.stringify({ teamId, name, jerseyNumber: Number(jerseyNumber) }),
          idempotencyKey: idempotencyKey.current,
        });
        setNotice("Jugador registrado correctamente.");
      }
      resetForm(); reload();
    } catch (reason) { setMutationError(reason); }
    finally { setSaving(false); }
  }

  function resetKey() { idempotencyKey.current = null; }
  function resetForm() {
    setEditingId(null); setTeamId(""); setName(""); setJerseyNumber(""); resetKey();
  }
  function editPlayer(player: Player) {
    setEditingId(player.id); setTeamId(player.teamId); setName(player.name);
    setJerseyNumber(String(player.jerseyNumber)); setMutationError(null); setNotice(""); resetKey();
  }
  async function togglePlayer(player: Player) {
    setSaving(true); setMutationError(null); setNotice("");
    try {
      await apiRequest<Player>(`/api/players/${player.id}`, {
        method: "PATCH", body: JSON.stringify({ isActive: !player.isActive }),
      });
      setNotice(player.isActive ? "Jugador desactivado correctamente." : "Jugador activado correctamente.");
      if (editingId === player.id) resetForm();
      reload();
    } catch (reason) { setMutationError(reason); }
    finally { setSaving(false); }
  }

  return (
    <>
      <PageHeader eyebrow="Integrantes" title="Jugadores" description="Registra jugadores y consulta su equipo y estado." />
      <div className="splitLayout">
        <section className="card formCard"><h2>{editingId ? "Editar jugador" : "Nuevo jugador"}</h2>
          <form onSubmit={savePlayer}>
            <label>Equipo<select value={teamId} onChange={(e) => { setTeamId(e.target.value); resetKey(); }} required disabled={editingId !== null}><option value="">Selecciona un equipo</option>{teams.map((team) => <option value={team.id} key={team.id}>{team.name}</option>)}</select></label>
            <label>Nombre<input value={name} onChange={(e) => { setName(e.target.value); resetKey(); }} required maxLength={120} /></label>
            <label>Dorsal<input type="number" min="1" max="99" value={jerseyNumber} onChange={(e) => { setJerseyNumber(e.target.value); resetKey(); }} required /></label>
            <ErrorNotice error={catalogError ?? mutationError} />
            {notice && <div className="alert success" role="status">{notice}</div>}
            <button className="button primary" disabled={saving || teams.length === 0}>{saving ? "Guardando…" : editingId ? "Guardar cambios" : "Registrar jugador"}</button>
            {editingId && <button className="button secondary" type="button" onClick={resetForm} disabled={saving}>Cancelar edición</button>}
          </form>
        </section>
        <section className="card dataCard"><div className="sectionHeading"><h2>Integrantes registrados</h2></div>
          <form className="filters" onSubmit={(e) => { e.preventDefault(); setPage(1); setSearch(draftSearch); }}>
            <label>Buscar<input value={draftSearch} onChange={(e) => setDraftSearch(e.target.value)} placeholder="Nombre" /></label>
            <label>Equipo<select value={teamFilter} onChange={(e) => { setPage(1); setTeamFilter(e.target.value); }}><option value="">Todos</option>{teams.map((team) => <option value={team.id} key={team.id}>{team.name}</option>)}</select></label>
            <label>Estado<select value={activeFilter} onChange={(e) => { setPage(1); setActiveFilter(e.target.value); }}><option value="">Todos</option><option value="true">Activo</option><option value="false">Inactivo</option></select></label>
            <button className="button secondary">Aplicar</button>
          </form>
          <ErrorNotice error={error} />
          {loading ? <LoadingState /> : result.data.length === 0 ? <EmptyState /> : <div className="tableScroll"><table><thead><tr><th>Jugador</th><th>Equipo</th><th>Dorsal</th><th>Estado</th><th>Acciones</th></tr></thead><tbody>{result.data.map((player) => <tr key={player.id}><td><strong>{player.name}</strong></td><td>{teamNames.get(player.teamId) ?? player.teamId}</td><td>{player.jerseyNumber}</td><td><span className={`badge ${player.isActive ? "active" : "inactive"}`}>{player.isActive ? "Activo" : "Inactivo"}</span></td><td><div className="rowActions"><button className="button secondary" type="button" onClick={() => editPlayer(player)} disabled={saving}>Editar</button><button className="button secondary" type="button" onClick={() => togglePlayer(player)} disabled={saving}>{player.isActive ? "Desactivar" : "Activar"}</button></div></td></tr>)}</tbody></table></div>}
          <Pagination page={page} totalPages={result.totalPages} totalRecords={result.totalRecords} onChange={setPage} />
        </section>
      </div>
    </>
  );
}
