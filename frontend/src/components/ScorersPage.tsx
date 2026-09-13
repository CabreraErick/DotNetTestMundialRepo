// Responsabilidad: visualiza goleadores con búsqueda, filtro por equipo y paginación.
// Relación: consume /api/scorers y el catálogo de /api/teams sin recalcular estadísticas.
"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiRequest, toQuery } from "@/lib/api/client";
import { usePagedResource } from "@/hooks/usePagedResource";
import type { PagedResult, Scorer, Team } from "@/types/api";
import { EmptyState, ErrorNotice, LoadingState, PageHeader, Pagination } from "@/components/ui";

export function ScorersPage() {
  const [teams, setTeams] = useState<Team[]>([]); const [catalogError, setCatalogError] = useState<unknown>(null);
  const [draftSearch, setDraftSearch] = useState(""); const [search, setSearch] = useState(""); const [teamId, setTeamId] = useState(""); const [page, setPage] = useState(1);
  useEffect(() => { apiRequest<PagedResult<Team>>("/api/teams?pageNumber=1&pageSize=100&sortBy=name&sortDirection=asc").then((result) => setTeams(result.data)).catch(setCatalogError); }, []);
  const path = useMemo(() => `/api/scorers${toQuery({ search, teamId, pageNumber: page, pageSize: 10, sortBy: "goals", sortDirection: "desc" })}`, [search, teamId, page]);
  const { result, loading, error } = usePagedResource<Scorer>(path);
  function apply(event: FormEvent) { event.preventDefault(); setPage(1); setSearch(draftSearch); }
  return <><PageHeader eyebrow="Estadísticas" title="Goleadores" description="Goles oficiales contabilizados en partidos finalizados." /><section className="card dataCard"><form className="filters" onSubmit={apply}><label>Buscar<input value={draftSearch} onChange={(e) => setDraftSearch(e.target.value)} placeholder="Jugador o equipo" /></label><label>Equipo<select value={teamId} onChange={(e) => { setPage(1); setTeamId(e.target.value); }}><option value="">Todos</option>{teams.map((team) => <option value={team.id} key={team.id}>{team.name}</option>)}</select></label><button className="button secondary">Aplicar</button></form><ErrorNotice error={catalogError ?? error} />{loading ? <LoadingState /> : result.data.length === 0 ? <EmptyState /> : <div className="tableScroll"><table><thead><tr><th>Pos.</th><th>Jugador</th><th>Equipo</th><th>Goles</th></tr></thead><tbody>{result.data.map((row) => <tr key={row.playerId}><td><strong>{row.position}</strong></td><td><strong>{row.playerName}</strong></td><td>{row.teamName}</td><td><span className="goalCount">{row.goals}</span></td></tr>)}</tbody></table></div>}<Pagination page={page} totalPages={result.totalPages} totalRecords={result.totalRecords} onChange={setPage} /></section></>;
}
