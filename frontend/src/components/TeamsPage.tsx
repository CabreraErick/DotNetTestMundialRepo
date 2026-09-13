// Responsabilidad: permite consultar, filtrar, ordenar, paginar y registrar equipos.
// Relación: consume el REST de equipos y respeta idempotencia en POST.
"use client";

import { FormEvent, useMemo, useRef, useState } from "react";
import { apiRequest, createRequestId, toQuery } from "@/lib/api/client";
import { usePagedResource } from "@/hooks/usePagedResource";
import type { CreatedResource, Team } from "@/types/api";
import { EmptyState, ErrorNotice, LoadingState, PageHeader, Pagination } from "@/components/ui";

export function TeamsPage() {
  const [draftSearch, setDraftSearch] = useState("");
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState("name");
  const [sortDirection, setSortDirection] = useState("asc");
  const [page, setPage] = useState(1);
  const [name, setName] = useState("");
  const [shortName, setShortName] = useState("");
  const [saving, setSaving] = useState(false);
  const [mutationError, setMutationError] = useState<unknown>(null);
  const [notice, setNotice] = useState("");
  const idempotencyKey = useRef<string | null>(null);

  const path = useMemo(() => `/api/teams${toQuery({ search, pageNumber: page, pageSize: 10, sortBy, sortDirection })}`,
    [search, page, sortBy, sortDirection]);
  const { result, loading, error, reload } = usePagedResource<Team>(path);

  async function createTeam(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true); setMutationError(null); setNotice("");
    idempotencyKey.current ??= createRequestId();
    try {
      await apiRequest<CreatedResource>("/api/teams", {
        method: "POST",
        body: JSON.stringify({ name, shortName }),
        idempotencyKey: idempotencyKey.current,
      });
      setName(""); setShortName(""); idempotencyKey.current = null;
      setNotice("Equipo registrado correctamente."); reload();
    } catch (reason) { setMutationError(reason); }
    finally { setSaving(false); }
  }

  function changeForm(value: string, setter: (next: string) => void) {
    setter(value); idempotencyKey.current = null;
  }

  return (
    <>
      <PageHeader eyebrow="Participantes" title="Equipos" description="Consulta y registra los equipos del torneo." />
      <div className="splitLayout">
        <section className="card formCard" aria-labelledby="new-team-title">
          <h2 id="new-team-title">Nuevo equipo</h2>
          <form onSubmit={createTeam}>
            <label>Nombre<input value={name} onChange={(e) => changeForm(e.target.value, setName)} required maxLength={100} /></label>
            <label>Abreviatura<input value={shortName} onChange={(e) => changeForm(e.target.value.toUpperCase(), setShortName)} required maxLength={10} /></label>
            <ErrorNotice error={mutationError} />
            {notice && <div className="alert success" role="status">{notice}</div>}
            <button className="button primary" disabled={saving}>{saving ? "Guardando…" : "Registrar equipo"}</button>
          </form>
        </section>
        <section className="card dataCard" aria-labelledby="team-list-title">
          <div className="sectionHeading"><h2 id="team-list-title">Equipos participantes</h2></div>
          <form className="filters" onSubmit={(e) => { e.preventDefault(); setPage(1); setSearch(draftSearch); }}>
            <label>Buscar<input value={draftSearch} onChange={(e) => setDraftSearch(e.target.value)} placeholder="Nombre o abreviatura" /></label>
            <label>Ordenar<select value={sortBy} onChange={(e) => { setPage(1); setSortBy(e.target.value); }}><option value="name">Nombre</option><option value="shortName">Abreviatura</option><option value="id">Identificador</option></select></label>
            <label>Dirección<select value={sortDirection} onChange={(e) => { setPage(1); setSortDirection(e.target.value); }}><option value="asc">Ascendente</option><option value="desc">Descendente</option></select></label>
            <button className="button secondary">Aplicar</button>
          </form>
          <ErrorNotice error={error} />
          {loading ? <LoadingState /> : result.data.length === 0 ? <EmptyState /> : (
            <div className="tableScroll"><table><thead><tr><th>Equipo</th><th>Abreviatura</th></tr></thead><tbody>{result.data.map((team) => <tr key={team.id}><td><strong>{team.name}</strong></td><td>{team.shortName}</td></tr>)}</tbody></table></div>
          )}
          <Pagination page={page} totalPages={result.totalPages} totalRecords={result.totalRecords} onChange={setPage} />
        </section>
      </div>
    </>
  );
}
