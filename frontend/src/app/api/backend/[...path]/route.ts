// Responsabilidad: reenvía al backend .NET las solicitudes realizadas desde el mismo origen del frontend.
// Relación: desacopla navegador y API, conserva idempotencia/correlación y evita requerir CORS.
import "server-only";

interface ProxyContext { params: Promise<{ path: string[] }>; }

const requestHeaders = ["accept", "content-type", "idempotency-key", "x-correlation-id"];
const responseHeaders = ["content-type", "location", "x-correlation-id"];

async function proxy(request: Request, context: ProxyContext): Promise<Response> {
  const apiBaseUrl = process.env.API_BASE_URL?.replace(/\/$/, "");
  if (!apiBaseUrl) {
    return Response.json(
      { code: "Frontend.ApiNotConfigured", message: "API_BASE_URL no está configurada." },
      { status: 500 },
    );
  }

  const { path } = await context.params;
  const incomingUrl = new URL(request.url);
  const targetUrl = `${apiBaseUrl}/${path.map(encodeURIComponent).join("/")}${incomingUrl.search}`;
  const headers = new Headers();
  requestHeaders.forEach((name) => {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  });

  try {
    const body = request.method === "GET" || request.method === "HEAD" ? undefined : await request.arrayBuffer();
    const upstream = await fetch(targetUrl, {
      method: request.method,
      headers,
      body,
      cache: "no-store",
      redirect: "manual",
    });
    const outgoingHeaders = new Headers();
    responseHeaders.forEach((name) => {
      const value = upstream.headers.get(name);
      if (value) outgoingHeaders.set(name, value);
    });
    return new Response(upstream.body, {
      status: upstream.status,
      statusText: upstream.statusText,
      headers: outgoingHeaders,
    });
  } catch {
    return Response.json(
      { code: "Frontend.ApiUnavailable", message: "La API .NET no está disponible." },
      { status: 502 },
    );
  }
}

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const PATCH = proxy;
export const DELETE = proxy;
