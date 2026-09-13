// Responsabilidad: registra goles y cierra el resultado de un partido programado.
// Relación: coordina detalle, jugadores, /goals y /result respetando reglas del dominio.
"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { apiRequest, createRequestId } from "@/lib/api/client";
import type { CreatedResource, Goal, Match, PagedResult, Player } from "@/types/api";
import { EmptyState, ErrorNotice, LoadingState, PageHeader, StatusBadge } from "@/components/ui";

export function MatchDetailPage({ matchId }: { matchId: string }) {
  const [match, setMatch] = useState<Match | null>(null);
  const [goals, setGoals] = useState<Goal[]>([]);
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
    try {
      const [matchResult, goalResult] = await Promise.all([
        apiRequest<Match>(`/api/matches/${matchId}`),
        apiRequest<Goal[]>(`/api/matches/${matchId}/goals`),
      ]);
      setMatch(matchResult); setGoals(goalResult);
      const playerPages = await Promise.all([matchResult.homeTeamId, matchResult.awayTeamId].map((teamId) =>
        apiRequest<PagedResult<Player>>(`/api/players?teamId=${teamId}&isActive=true&pageNumber=1&pageSize=100&sortBy=name&sortDirection=asc`)));
      setPlayers(playerPages.flatMap((page) => page.data));
      const homeGoals = goalResult.filter((goal) => goal.teamId === matchResult.homeTeamId).length;
      const awayGoals = goalResult.filter((goal) => goal.teamId === matchResult.awayTeamId).length;
      setHomeScore(String(matchResult.homeScore ?? homeGoals));
      setAwayScore(String(matchResult.awayScore ?? awayGoals));
      setError(null);
    } catch (reason) { setError(reason); }
    finally { setLoading(false); }
  }, [matchId]);

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
          {goals.length === 0 ? <EmptyState message="Aún no hay goles registrados." /> : <div className="tableScroll"><table><thead><tr><th>Minuto</th><th>Jugador</th><th>Equipo</th></tr></thead><tbody>{goals.map((goal) => <tr key={goal.id}><td>{goal.minute}&apos;</td><td><strong>{goal.playerName}</strong></td><td>{goal.teamName}</td></tr>)}</tbody></table></div>}
        </section>
        <div className="formStack">
          <section className="card formCard"><h2>Registrar gol</h2><form onSubmit={createGoal}><label>Goleador<select value={playerId} onChange={(e) => { setPlayerId(e.target.value); goalKey.current = null; }} required disabled={match.status !== "Scheduled"}><option value="">Selecciona un jugador</option>{players.map((player) => <option value={player.id} key={player.id}>{player.name} · #{player.jerseyNumber} · {playerTeamName(player)}</option>)}</select></label><label>Minuto<input type="number" min="1" max="120" value={minute} onChange={(e) => { setMinute(e.target.value); goalKey.current = null; }} required disabled={match.status !== "Scheduled"} /></label><button className="button primary" disabled={saving || match.status !== "Scheduled"}>Registrar gol</button></form></section>
          <section className="card formCard"><h2>Resultado final</h2><p className="helper">El marcador debe coincidir con los goles registrados por cada equipo.</p><form onSubmit={registerResult}><div className="scoreInputs"><label>{match.homeTeamName}<input type="number" min="0" value={homeScore} onChange={(e) => setHomeScore(e.target.value)} required disabled={match.status !== "Scheduled"} /></label><label>{match.awayTeamName}<input type="number" min="0" value={awayScore} onChange={(e) => setAwayScore(e.target.value)} required disabled={match.status !== "Scheduled"} /></label></div><button className="button primary" disabled={saving || match.status !== "Scheduled"}>Cerrar partido</button></form></section>
        </div>
      </div>
    </>
  );
}
