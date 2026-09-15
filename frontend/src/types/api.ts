// Responsabilidad: representa en TypeScript los contratos JSON publicados por la API .NET.
// Relación: evita que páginas y componentes dependan de estructuras de respuesta implícitas.
export interface PagedResult<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
}

export interface ApiErrorBody {
  code?: string;
  message?: string;
  title?: string;
  detail?: string;
  type?: string;
  errors?: Record<string, string[]>;
}

export interface Team { id: string; name: string; shortName: string; }

export interface Player {
  id: string;
  teamId: string;
  name: string;
  jerseyNumber: number;
  isActive: boolean;
}

export type MatchStatus = "Scheduled" | "Played" | "Cancelled";

export interface Match {
  id: string;
  homeTeamId: string;
  homeTeamName: string;
  awayTeamId: string;
  awayTeamName: string;
  scheduledAt: string;
  status: MatchStatus;
  homeScore: number | null;
  awayScore: number | null;
  goalCount: number;
}

export interface Goal {
  id: string;
  matchId: string;
  playerId: string;
  playerName: string;
  teamId: string;
  teamName: string;
  minute: number;
}

export interface MatchGoalsPage extends PagedResult<Goal> {
  homeGoals: number;
  awayGoals: number;
}

export interface Standing {
  position: number;
  teamId: string;
  teamName: string;
  shortName: string;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
}

export interface Scorer {
  position: number;
  playerId: string;
  playerName: string;
  teamId: string;
  teamName: string;
  goals: number;
}

export interface CreatedResource { id: string; }
