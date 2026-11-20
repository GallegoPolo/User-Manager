const orbitingContacts = [
  { initials: "BL", className: "orbit-contact orbit-contact--top" },
  { initials: "CM", className: "orbit-contact orbit-contact--left" },
  { initials: "DR", className: "orbit-contact orbit-contact--right" },
  { initials: "+3", className: "orbit-contact orbit-contact--more" },
];

export function ContactOrbit() {
  return (
    <div className="orbit" aria-hidden="true">
      <div className="orbit-glow" />
      <div className="orbit-core">
        sua
        <br />
        agenda
      </div>
      <div className="orbit-ring">
        {orbitingContacts.map((contact) => (
          <span key={contact.initials} className={contact.className}>
            {contact.initials}
          </span>
        ))}
      </div>
    </div>
  );
}
