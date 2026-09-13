// Responsabilidad: visualiza y ordena estadísticas oficiales de equipos.
// Relación: consume exclusivamente la proyección calculada por Dapper en /api/standings.
"use client";

import { FormEvent, useMemo, useState } from "react";
import { toQuery } from "@/lib/api/client";
import { usePagedResource } from "@/hooks/usePagedResource";
import type { Standing } from "@/types/api";
import { EmptyState, ErrorNotice, LoadingState, PageHeader, Pagination } from "@/components/ui";

export function StandingsPage() {
  const [draftSearch, setDraftSearch] = useState(""); const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState("points"); const [sortDirection, setSortDirection] = useState("desc"); const [page, setPage] = useState(1);
  const path = useMemo(() => `/api/standings${toQuery({ search, pageNumber: page, pageSize: 10, sortBy, sortDirection })}`, [search, page, sortBy, sortDirection]);
  const { result, loading, error } = usePagedResource<Standing>(path);
  function apply(event: FormEvent) { event.preventDefault(); setPage(1); setSearch(draftSearch); }
  return <><PageHeader eyebrow="Clasificación" title="Tabla de posiciones" description="Puntos y rendimiento calculados con partidos finalizados." /><section className="card dataCard"><form className="filters" onSubmit={apply}><label>Buscar<input value={draftSearch} onChange={(e) => setDraftSearch(e.target.value)} placeholder="Equipo" /></label><label>Ordenar<select value={sortBy} onChange={(e) => { setPage(1); setSortBy(e.target.value); }}><option value="points">Puntos</option><option value="goalDifference">Diferencia</option><option value="goalsFor">Goles a favor</option><option value="played">Jugados</option><option value="won">Ganados</option><option value="teamName">Equipo</option></select></label><label>Dirección<select value={sortDirection} onChange={(e) => { setPage(1); setSortDirection(e.target.value); }}><option value="desc">Descendente</option><option value="asc">Ascendente</option></select></label><button className="button secondary">Aplicar</button></form><ErrorNotice error={error} />{loading ? <LoadingState /> : result.data.length === 0 ? <EmptyState /> : <div className="tableScroll"><table><thead><tr><th>Pos.</th><th>Equipo</th><th>PJ</th><th>G</th><th>E</th><th>P</th><th>GF</th><th>GC</th><th>DG</th><th>Pts.</th></tr></thead><tbody>{result.data.map((row) => <tr key={row.teamId}><td><strong>{row.position}</strong></td><td><strong>{row.teamName}</strong><small className="tableSubtext">{row.shortName}</small></td><td>{row.played}</td><td>{row.won}</td><td>{row.drawn}</td><td>{row.lost}</td><td>{row.goalsFor}</td><td>{row.goalsAgainst}</td><td>{row.goalDifference}</td><td><strong>{row.points}</strong></td></tr>)}</tbody></table></div>}<Pagination page={page} totalPages={result.totalPages} totalRecords={result.totalRecords} onChange={setPage} /></section></>;
}
