let currentToken: string | null = null;

export function setStoredToken(token: string | null) {
  currentToken = token;
}

export function getStoredToken(): string | null {
  return currentToken;
}
