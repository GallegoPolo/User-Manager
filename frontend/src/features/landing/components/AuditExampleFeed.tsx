const exampleEvents = [
  { type: "contact.created", who: "ana", when: "agora" },
  { type: "contact.updated", who: "ana", when: "4 s" },
  { type: "user.created", who: "sistema", when: "12 s" },
  { type: "contact.deleted", who: "otto", when: "31 s" },
];

function EventRows({ hidden }: { hidden?: boolean }) {
  return exampleEvents.map((event) => (
    <li key={`${event.type}-${event.when}`} className="feed-row" aria-hidden={hidden}>
      <span className="feed-type">{event.type}</span>
      <span className="feed-meta">
        {event.who} · {event.when}
      </span>
    </li>
  ));
}

export function AuditExampleFeed() {
  return (
    <section className="landing-card feed" aria-labelledby="audit-example-title">
      <h2 id="audit-example-title" className="feed-title">
        <span className="feed-dot" aria-hidden="true" />
        Exemplo de auditoria
      </h2>
      <div className="feed-window">
        <ul className="feed-track">
          <EventRows />
          <EventRows hidden />
        </ul>
      </div>
    </section>
  );
}
