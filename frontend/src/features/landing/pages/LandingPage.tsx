import { useState } from "react";
import { Link, useLocation, useSearchParams } from "react-router-dom";
import type { ReturnState } from "../../auth/components/ProtectedRoute";
import { useAuth } from "../../auth/context/useAuth";
import { AuditExampleFeed } from "../components/AuditExampleFeed";
import { ContactOrbit } from "../components/ContactOrbit";
import "../landing.css";

const agendaPath = "/contacts";

function MainAction() {
  const { isAuthenticated, isLoading, login } = useAuth();
  const returnState = useLocation().state as ReturnState | null;
  const afterLogin = returnState?.from ?? agendaPath;

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
    <button type="button" className="button-primary landing-cta" onClick={() => login(afterLogin)}>
      Entrar na minha agenda
    </button>
  );
}

function FailedLoginWarning() {
  const { isAuthenticated } = useAuth();
  const [searchParams] = useSearchParams();

  if (isAuthenticated || searchParams.get("login") !== "falhou") return null;

  return (
    <p role="alert" className="notice notice--warn">
      Esse login não pôde ser concluído. Se ele já foi feito em outra aba, use aquela aba; senão, entre de novo.
    </p>
  );
}

function MotionToggle({ paused, onToggle }: { paused: boolean; onToggle: () => void }) {
  return (
    <button type="button" className="landing-motion-toggle" onClick={onToggle}>
      {paused ? "Retomar animações" : "Pausar animações"}
    </button>
  );
}

export function LandingPage() {
  const [paused, setPaused] = useState(false);

  return (
    <div className={paused ? "landing landing--paused" : "landing"}>
      <header className="landing-header">
        <div className="landing-brand">
          <span className="sidebar-brand-mark" aria-hidden="true">
            U
          </span>
          <strong>User Manager</strong>
        </div>
        <MotionToggle paused={paused} onToggle={() => setPaused((value) => !value)} />
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
            <FailedLoginWarning />
            <div className="landing-actions">
              <MainAction />
              <span className="landing-hint">login seguro pelo Keycloak</span>
            </div>
          </div>
          <ContactOrbit />
        </section>

        <section className="landing-cards" aria-label="Como funciona">
          <AuditExampleFeed />
          <article className="landing-card">
            <h2>Um login</h2>
            <p>E-mail e senha ou uma conta social. Contas só se juntam quando você prova que é dona.</p>
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
