// Responsabilidad: centraliza solicitudes del navegador, errores, correlación e idempotencia.
// Relación: todos los módulos llaman al proxy Next.js con un contrato HTTP uniforme.
import type { ApiErrorBody } from "@/types/api";

export class ApiClientError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string,
    public readonly correlationId?: string,
  ) {
    super(message);
    this.name = "ApiClientError";
  }
}

export interface ApiRequestOptions extends RequestInit { idempotencyKey?: string; }

export function createRequestId(): string { return crypto.randomUUID(); }

function errorMessage(body: ApiErrorBody | null, status: number): string {
  if (body?.message) return body.message;
  if (body?.detail) return body.detail;
  if (body?.title) return body.title;
  if (body?.errors) return Object.values(body.errors).flat().join(" ");
  return `La solicitud no pudo completarse (HTTP ${status}).`;
}

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");
  headers.set("X-Correlation-ID", createRequestId());
  if (options.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  if (options.idempotencyKey) headers.set("Idempotency-Key", options.idempotencyKey);

  let response: Response;
  try {
    response = await fetch(`/api/backend${path}`, { ...options, headers, cache: "no-store" });
  } catch {
    throw new ApiClientError(
      "No fue posible conectar con el servidor. Confirma que API y frontend estén activos.",
      0,
    );
  }

  const correlationId = response.headers.get("X-Correlation-ID") ?? undefined;
  if (!response.ok) {
    let body: ApiErrorBody | null = null;
    try { body = (await response.json()) as ApiErrorBody; } catch { /* Respuesta sin JSON. */ }
    throw new ApiClientError(errorMessage(body, response.status), response.status, body?.code, correlationId);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export function toQuery(params: Record<string, string | number | boolean | null | undefined>): string {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") query.set(key, String(value));
  });
  const serialized = query.toString();
  return serialized ? `?${serialized}` : "";
}
