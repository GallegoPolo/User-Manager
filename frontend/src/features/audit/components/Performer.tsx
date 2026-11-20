const systemPerformer = "system";
const shortIdLength = 8;

interface Props {
  performedBy: string;
  namesById: ReadonlyMap<string, string>;
}

export function Performer({ performedBy, namesById }: Props) {
  if (performedBy === systemPerformer) return <>Sistema</>;

  const name = namesById.get(performedBy);
  if (name) return <>{name}</>;

  return (
    <span className="mono" title={performedBy}>
      {performedBy.slice(0, shortIdLength)}
    </span>
  );
}
