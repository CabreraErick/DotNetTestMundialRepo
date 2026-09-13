// Responsabilidad: carga páginas remotas y expone estados uniformes de carga, error y recarga.
// Relación: reutiliza el contrato PagedResult de todas las consultas Dapper del backend.
"use client";

import { useCallback, useEffect, useState } from "react";
import { apiRequest } from "@/lib/api/client";
import type { PagedResult } from "@/types/api";

const emptyPage = <T,>(): PagedResult<T> => ({
  data: [], pageNumber: 1, pageSize: 10, totalRecords: 0, totalPages: 0,
});

export function usePagedResource<T>(path: string) {
  const [state, setState] = useState<{
    requestKey: string;
    result: PagedResult<T>;
    error: unknown;
  }>({ requestKey: "", result: emptyPage<T>(), error: null });
  const [revision, setRevision] = useState(0);

  const reload = useCallback(() => setRevision((value) => value + 1), []);
  const requestKey = `${path}#${revision}`;

  useEffect(() => {
    const controller = new AbortController();
    apiRequest<PagedResult<T>>(path, { signal: controller.signal })
      .then((result) => setState({ requestKey, result, error: null }))
      .catch((reason: unknown) => {
        if (!(reason instanceof DOMException && reason.name === "AbortError")) {
          setState({ requestKey, result: emptyPage<T>(), error: reason });
        }
      });
    return () => controller.abort();
  }, [path, requestKey]);

  return {
    result: state.requestKey === requestKey ? state.result : emptyPage<T>(),
    loading: state.requestKey !== requestKey,
    error: state.requestKey === requestKey ? state.error : null,
    reload,
  };
}
