import { Link } from "react-router-dom";
import { useAuth } from "../../auth/context/useAuth";
import { AuditExampleFeed } from "../components/AuditExampleFeed";
import { ContactOrbit } from "../components/ContactOrbit";
import "../landing.css";

const agendaPath = "/me";

function MainAction() {
  const { isAuthenticated, isLoading, login } = useAuth();

  if (isLoading) {
    return <span className="landing-action-placeholder" />;
  }

  if (isAuthenticated) {
    return (
      <Link to={agendaPath} className="button button-primary landing-cta">
        Abrir minha agenda
      </Link>
    );
  }

  return (
    <button type="button" className="button-primary landing-cta" onClick={() => login(agendaPath)}>
      Entrar na minha agenda
    </button>
  );
}

export function LandingPage() {
  return (
    <div className="landing">
      <header className="landing-header">
        <div className="landing-brand">
          <span className="sidebar-brand-mark" aria-hidden="true">
            U
          </span>
          <strong>User Manager</strong>
        </div>
      </header>

      <main className="landing-main">
        <section className="landing-hero">
          <div className="landing-copy">
            <h1>
              Cada contato no seu lugar.
              <span className="landing-accent">Cada mudança registrada.</span>
            </h1>
            <p className="landing-lead">
              Uma agenda pessoal com login único, sessão segura e auditoria que chega mesmo quando algo
              falha no caminho.
            </p>
            <div className="landing-actions">
              <MainAction />
              <span className="landing-hint">com e-mail, GitHub ou Google</span>
            </div>
          </div>
          <ContactOrbit />
        </section>

        <section className="landing-cards" aria-label="Como funciona">
          <AuditExampleFeed />
          <article className="landing-card">
            <h2>Um login</h2>
            <p>E-mail e senha, GitHub ou Google. Contas só se juntam quando você prova que é dona.</p>
          </article>
          <article className="landing-card">
            <h2>Nada se perde</h2>
            <p>Outbox na mesma transação e retry no RabbitMQ: o registro chega mesmo se um serviço cair.</p>
          </article>
        </section>
      </main>
    </div>
  );
}
