// Responsabilidad: presenta el acceso inicial a las funciones principales del torneo.
// Relación: orienta al usuario hacia los módulos conectados con los recursos REST del backend.
import Link from "next/link";

const modules = [
  { href: "/equipos", title: "Equipos", text: "Consulta y registra participantes." },
  { href: "/jugadores", title: "Jugadores", text: "Administra integrantes por equipo." },
  { href: "/partidos", title: "Partidos", text: "Programa encuentros, goles y resultados." },
  { href: "/posiciones", title: "Posiciones", text: "Revisa la clasificación automática." },
  { href: "/goleadores", title: "Goleadores", text: "Consulta a los máximos anotadores." },
] as const;

export default function Home() {
  return (
    <>
      <section className="hero">
        <p className="eyebrow">Sistema Gestor de Fútbol</p>
        <h1>Control completo del Mundialito Corporativo</h1>
        <p>
          Gestiona el torneo desde una interfaz sencilla, conectada de forma desacoplada
          con la API construida bajo Clean Architecture y CQRS.
        </p>
        <Link className="button primary" href="/partidos">Ver calendario</Link>
      </section>
      <section aria-labelledby="modules-title">
        <div className="sectionHeading">
          <div>
            <p className="eyebrow">Módulos</p>
            <h2 id="modules-title">Operación del torneo</h2>
          </div>
        </div>
        <div className="cardGrid">
          {modules.map((module) => (
            <Link className="card moduleCard" href={module.href} key={module.href}>
              <h3>{module.title}</h3>
              <p>{module.text}</p>
              <span>Acceder →</span>
            </Link>
          ))}
        </div>
      </section>
    </>
  );
}
