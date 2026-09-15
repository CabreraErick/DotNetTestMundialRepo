<!-- Responsabilidad: documenta instalación, configuración y comprobación del frontend. -->
<!-- Relación: explica cómo Next.js se integra con la API .NET sin acoplar el navegador a su URL. -->
# Frontend del Mundialito Corporativo

Aplicación Next.js con App Router, TypeScript, HTML semántico, CSS y `fetch` nativo. Cubre equipos, jugadores, calendario, goles, resultados, posiciones, goleadores, filtros, paginación y manejo de errores.

## Requisitos

- Node.js 22.13 o superior.
- pnpm 11.19.0.
- API disponible en `http://localhost:5164` o en la dirección indicada por `API_BASE_URL`.

## Ejecución desde PowerShell

~~~powershell
cd frontend
Copy-Item .env.example .env.local
pnpm install
pnpm dev
~~~

Abra `http://localhost:3000`. `.env.local` es configuración local y permanece fuera del repositorio.

## Verificación

~~~powershell
pnpm lint
pnpm build
~~~

## Integración HTTP

El navegador solicita rutas del mismo origen bajo `/api/backend`. El Route Handler dinámico obtiene `API_BASE_URL` en el servidor Next.js y reenvía método, cuerpo, query string, `Idempotency-Key` y `X-Correlation-ID`. Así la API no necesita CORS para el frontend y su dirección no se incorpora al JavaScript público.
