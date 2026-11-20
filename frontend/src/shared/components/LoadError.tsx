import { ApiError } from "../api/apiClient";

interface Props {
  error: unknown;
  what: string;
}

export function LoadError({ error, what }: Props) {
  const forbidden = error instanceof ApiError && error.status === 403;

  return (
    <p role="alert" className="page-error">
      {forbidden ? "Você não tem acesso a esta página." : `Não foi possível carregar ${what}. Tente de novo.`}
    </p>
  );
}
