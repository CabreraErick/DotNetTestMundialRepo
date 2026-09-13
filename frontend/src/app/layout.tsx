// Responsabilidad: define metadatos, navegación y estructura visual común del frontend.
// Relación: envuelve todas las pantallas que consumen los casos de uso expuestos por la API .NET.
import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";

export const metadata: Metadata = {
  title: "Mundialito Corporativo",
  description: "Gestión de equipos, jugadores, partidos y estadísticas del torneo.",
};

const navigation = [
  ["/", "Inicio"],
  ["/equipos", "Equipos"],
  ["/jugadores", "Jugadores"],
  ["/partidos", "Partidos"],
  ["/posiciones", "Posiciones"],
  ["/goleadores", "Goleadores"],
] as const;

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es">
      <body>
        <header className="siteHeader">
          <div className="shell headerContent">
            <Link className="brand" href="/">
              <span className="brandMark" aria-hidden="true">MC</span>
              <span>
                <strong>Sistema Mundialito</strong>
                <small>Gestión del torneo</small>
              </span>
            </Link>
            <nav aria-label="Navegación principal">
              {navigation.map(([href, label]) => (
                <Link key={href} href={href}>{label}</Link>
              ))}
            </nav>
          </div>
        </header>
        <main className="shell pageContent">{children}</main>
        <footer className="siteFooter">
          <div className="shell">Informática Atlantida El Salvador - Frontend Next.js · API .NET 8 · SQL Server</div>
        </footer>
      </body>
    </html>
  );
}
