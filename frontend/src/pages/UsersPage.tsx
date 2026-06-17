import { useAuth } from '../contexts/AuthContext'

export function UsersPage() {
  const { logout } = useAuth()

  return (
    <main style={{ padding: '2rem' }}>
      <h1>Usuários</h1>
      <p>Você está logado. Em breve: listagem de usuários.</p>
      <button type="button" onClick={logout}>
        Sair
      </button>
    </main>
  )
}