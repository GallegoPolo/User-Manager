import type { ReactNode } from "react";
import { MutationCache, QueryCache, QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "../features/auth/context/AuthContext";
import { ApiError } from "../shared/api/apiClient";

function endSessionOnUnauthorized(error: Error) {
  if (error instanceof ApiError && error.status === 401) {
    queryClient.setQueryData(["session"], null);
  }
}

const queryClient = new QueryClient({
  queryCache: new QueryCache({ onError: endSessionOnUnauthorized }),
  mutationCache: new MutationCache({ onError: endSessionOnUnauthorized }),
});

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>{children}</AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
