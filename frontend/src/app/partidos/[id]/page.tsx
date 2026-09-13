// Responsabilidad: publica el detalle de un partido identificado por su GUID.
// Relación: entrega el identificador al flujo de goles y resultado final.
import { MatchDetailPage } from "@/components/MatchDetailPage";

export default async function Page({ params }: PageProps<"/partidos/[id]">) {
  const { id } = await params;
  return <MatchDetailPage matchId={id} />;
}
