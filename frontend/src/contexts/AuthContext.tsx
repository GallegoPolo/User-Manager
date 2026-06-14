import {
    createContext,
    useCallback,
    useContext,
    useMemo,
    useState,
    type ReactNode,
  } from 'react'
  import { login as loginRequest } from '../services/authService'
  import type { LoginRequest } from '../types/auth'
  
  interface AuthContextValue {
    token: string | null
    isAuthenticated: boolean
    login: (credentials: LoginRequest) => Promise<void>
    logout: () => void
  }
  
  const AuthContext = createContext<AuthContextValue | null>(null)
  
  export function AuthProvider({ children }: { children: ReactNode }) {
    const [token, setToken] = useState<string | null>(null)
  
    const login = useCallback(async (credentials: LoginRequest) => {
      const response = await loginRequest(credentials)
      setToken(response.accessToken)
    }, [])
  
    const logout = useCallback(() => {
      setToken(null)
    }, [])
  
    const value = useMemo(
      () => ({
        token,
        isAuthenticated: token !== null,
        login,
        logout,
      }),
      [token, login, logout],
    )
  
    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
  }
  
  export function useAuth() {
    const context = useContext(AuthContext)
  
    if (!context) {
      throw new Error('useAuth deve ser usado dentro de AuthProvider')
    }
  
    return context
  }