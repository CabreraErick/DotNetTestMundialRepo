// Responsabilidad: ofrece piezas visuales reutilizables para estados, títulos y paginación.
// Relación: mantiene consistencia entre todas las consultas paginadas del backend.
import type { ReactNode } from "react";

export function PageHeader({ eyebrow, title, description, actions }: {
  eyebrow: string; title: string; description: string; actions?: ReactNode;
}) {
  return (
    <header className="pageHeader">
      <div><p className="eyebrow">{eyebrow}</p><h1>{title}</h1><p>{description}</p></div>
      {actions && <div className="pageActions">{actions}</div>}
    </header>
  );
}

export function ErrorNotice({ error }: { error: unknown }) {
  if (!error) return null;
  const message = error instanceof Error ? error.message : "Ocurrió un error inesperado.";
  return <div className="alert error" role="alert"><strong>No se pudo completar la operación.</strong><span>{message}</span></div>;
}

export function LoadingState() { return <div className="state" role="status">Cargando información…</div>; }

export function EmptyState({ message = "No se encontraron registros." }: { message?: string }) {
  return <div className="state">{message}</div>;
}

export function Pagination({ page, totalPages, totalRecords, onChange }: {
  page: number; totalPages: number; totalRecords: number; onChange: (page: number) => void;
}) {
  return (
    <div className="pagination">
      <span>{totalRecords} registro{totalRecords === 1 ? "" : "s"}</span>
      <div>
        <button className="button secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>Anterior</button>
        <span>Página {page} de {Math.max(totalPages, 1)}</span>
        <button className="button secondary" disabled={totalPages === 0 || page >= totalPages} onClick={() => onChange(page + 1)}>Siguiente</button>
      </div>
    </div>
  );
}

export function StatusBadge({ status }: { status: string }) {
  const labels: Record<string, string> = { Scheduled: "Programado", Played: "Finalizado", Cancelled: "Cancelado" };
  return <span className={`badge ${status.toLowerCase()}`}>{labels[status] ?? status}</span>;
}
