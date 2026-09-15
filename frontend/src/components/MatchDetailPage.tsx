// Responsabilidad: registra goles y cierra el resultado de un partido programado.
// Relación: coordina detalle, jugadores, /goals y /result respetando reglas del dominio.
"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { apiRequest, createRequestId, toQuery } from "@/lib/api/client";
import type { CreatedResource, MatchGoalsPage, Match, PagedResult, Player } from "@/types/api";
import { EmptyState, ErrorNotice, LoadingState, PageHeader, Pagination, StatusBadge } from "@/components/ui";

export function MatchDetailPage({ matchId }: { matchId: string }) {
  const [match, setMatch] = useState<Match | null>(null);
  const [goalPage, setGoalPage] = useState<MatchGoalsPage>({ data: [], pageNumber: 1, pageSize: 10, totalRecords: 0, totalPages: 0, homeGoals: 0, awayGoals: 0 });
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [draftSearch, setDraftSearch] = useState("");
  const [teamFilter, setTeamFilter] = useState("");
  const [sortDirection, setSortDirection] = useState("asc");
  const loadSequence = useRef(0);
  const [players, setPlayers] = useState<Player[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [mutationError, setMutationError] = useState<unknown>(null);
  const [notice, setNotice] = useState("");
  const [playerId, setPlayerId] = useState("");
  const [minute, setMinute] = useState("");
  const [homeScore, setHomeScore] = useState("");
  const [awayScore, setAwayScore] = useState("");
  const [saving, setSaving] = useState(false);
  const goalKey = useRef<string | null>(null);

  const load = useCallback(async () => {
    const sequence = ++loadSequence.current;
    try {
      const [matchResult, goalResult] = await Promise.all([
        apiRequest<Match>(`/api/matches/${matchId}`),
        apiRequest<MatchGoalsPage>(`/api/matches/${matchId}/goals${toQuery({ pageNumber: page, pageSize: 10, search, teamId: teamFilter, sortBy: "minute", sortDirection })}`),
      ]);
      if (sequence !== loadSequence.current) return;
      setMatch(matchResult); setGoalPage(goalResult);
      const playerPages = await Promise.all([matchResult.homeTeamId, matchResult.awayTeamId].map((teamId) =>
        apiRequest<PagedResult<Player>>(`/api/players?teamId=${teamId}&isActive=true&pageNumber=1&pageSize=100&sortBy=name&sortDirection=asc`)));
      if (sequence !== loadSequence.current) return;
      setPlayers(playerPages.flatMap((page) => page.data));
      setHomeScore(String(matchResult.homeScore ?? goalResult.homeGoals));
      setAwayScore(String(matchResult.awayScore ?? goalResult.awayGoals));
      setError(null);
    } catch (reason) { if (sequence === loadSequence.current) setError(reason); }
    finally { if (sequence === loadSequence.current) setLoading(false); }
  }, [matchId, page, search, teamFilter, sortDirection]);

  useEffect(() => {
    const timerId = window.setTimeout(() => { void load(); }, 0);
    return () => window.clearTimeout(timerId);
  }, [load]);
  function playerTeamName(player: Player) {
    return player.teamId === match?.homeTeamId ? match.homeTeamName : match?.awayTeamName;
  }

  async function createGoal(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setMutationError(null); setNotice("");
    goalKey.current ??= createRequestId();
    try {
      await apiRequest<CreatedResource>(`/api/matches/${matchId}/goals`, {
        method: "POST", body: JSON.stringify({ playerId, minute: Number(minute) }), idempotencyKey: goalKey.current,
      });
      setPlayerId(""); setMinute(""); goalKey.current = null;
      setNotice("Gol registrado correctamente."); await load();
    } catch (reason) { setMutationError(reason); }
    finally { setSaving(false); }
  }

  async function registerResult(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setMutationError(null); setNotice("");
    try {
      await apiRequest(`/api/matches/${matchId}/result`, {
        method: "PUT", body: JSON.stringify({ homeScore: Number(homeScore), awayScore: Number(awayScore) }),
      });
      setNotice("Resultado registrado. Las estadísticas oficiales fueron actualizadas."); await load();
    } catch (reason) { setMutationError(reason); }
    finally { setSaving(false); }
  }

  if (loading) return <><PageHeader eyebrow="Partido" title="Detalle" description="Cargando información del encuentro." /><LoadingState /></>;
  if (!match) return <><PageHeader eyebrow="Partido" title="Detalle" description="No fue posible recuperar el encuentro." /><ErrorNotice error={error} /></>;

  return (
    <>
      <PageHeader eyebrow="Partido" title={`${match.homeTeamName} vs. ${match.awayTeamName}`} description={new Date(match.scheduledAt).toLocaleString("es-SV")} actions={<><StatusBadge status={match.status} /><Link className="button secondary" href="/partidos">Volver</Link></>} />
      <ErrorNotice error={mutationError ?? error} />
      {notice && <div className="alert success" role="status">{notice}</div>}
      <div className="detailGrid">
        <section className="card dataCard"><div className="sectionHeading"><h2>Goles registrados</h2><strong className="score">{homeScore || 0} - {awayScore || 0}</strong></div>
          <form className="filters" onSubmit={(event) => { event.preventDefault(); setPage(1); setSearch(draftSearch); }}>
            <label>Buscar<input value={draftSearch} onChange={(event) => setDraftSearch(event.target.value)} placeholder="Jugador o equipo" /></label>
            <label>Equipo<select value={teamFilter} onChange={(event) => { setPage(1); setTeamFilter(event.target.value); }}><option value="">Ambos</option><option value={match.homeTeamId}>{match.homeTeamName}</option><option value={match.awayTeamId}>{match.awayTeamName}</option></select></label>
            <label>Minuto<select value={sortDirection} onChange={(event) => { setPage(1); setSortDirection(event.target.value); }}><option value="asc">Ascendente</option><option value="desc">Descendente</option></select></label>
            <button className="button secondary">Aplicar</button>
          </form>
          {goalPage.data.length === 0 ? <EmptyState message="No hay goles para estos filtros." /> : <div className="tableScroll"><table><thead><tr><th>Minuto</th><th>Jugador</th><th>Equipo</th></tr></thead><tbody>{goalPage.data.map((goal) => <tr key={goal.id}><td>{goal.minute}&apos;</td><td><strong>{goal.playerName}</strong></td><td>{goal.teamName}</td></tr>)}</tbody></table></div>}
          <Pagination page={page} totalPages={goalPage.totalPages} totalRecords={goalPage.totalRecords} onChange={setPage} />
        </section>
        <div className="formStack">
          <section className="card formCard"><h2>Registrar gol</h2><form onSubmit={createGoal}><label>Goleador<select value={playerId} onChange={(e) => { setPlayerId(e.target.value); goalKey.current = null; }} required disabled={match.status !== "Scheduled"}><option value="">Selecciona un jugador</option>{players.map((player) => <option value={player.id} key={player.id}>{player.name} · #{player.jerseyNumber} · {playerTeamName(player)}</option>)}</select></label><label>Minuto<input type="number" min="1" max="120" value={minute} onChange={(e) => { setMinute(e.target.value); goalKey.current = null; }} required disabled={match.status !== "Scheduled"} /></label><button className="button primary" disabled={saving || match.status !== "Scheduled"}>Registrar gol</button></form></section>
          <section className="card formCard"><h2>Resultado final</h2><p className="helper">El marcador debe coincidir con los goles registrados por cada equipo.</p><form onSubmit={registerResult}><div className="scoreInputs"><label>{match.homeTeamName}<input type="number" min="0" value={homeScore} onChange={(e) => setHomeScore(e.target.value)} required disabled={match.status !== "Scheduled"} /></label><label>{match.awayTeamName}<input type="number" min="0" value={awayScore} onChange={(e) => setAwayScore(e.target.value)} required disabled={match.status !== "Scheduled"} /></label></div><button className="button primary" disabled={saving || match.status !== "Scheduled"}>Cerrar partido</button></form></section>
        </div>
      </div>
    </>
  );
}
