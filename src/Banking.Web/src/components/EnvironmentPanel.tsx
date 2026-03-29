type EnvironmentPanelProps = {
  health: Record<string, string>
}

export function EnvironmentPanel({ health }: EnvironmentPanelProps) {
  return (
    <article className="panel">
      <h2>Environment</h2>
      <div className="health-grid">
        {Object.entries(health).map(([name, value]) => (
          <div key={name} className="health-card">
            <span>{name}</span>
            <strong>{value}</strong>
          </div>
        ))}
      </div>
    </article>
  )
}
