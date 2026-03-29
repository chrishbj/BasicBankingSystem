type EnvironmentPanelProps = {
  health: Record<string, string>
}

const serviceLinks: Record<string, string> = {
  customer: '/customer-api/swagger',
  account: '/account-api/swagger',
  deposit: '/deposit-api/swagger',
  audit: '/audit-api/swagger',
}

export function EnvironmentPanel({ health }: EnvironmentPanelProps) {
  return (
    <article className="panel compact-panel">
      <div className="panel-head compact-panel-head">
        <div>
          <p className="eyebrow">Environment</p>
          <h2>Services</h2>
        </div>
      </div>
      <div className="service-health-list">
        {Object.entries(health).map(([name, value]) => (
          <div key={name} className="service-health-item">
            <span className={value === 'Healthy' ? 'service-dot service-dot-up' : 'service-dot service-dot-down'} aria-hidden="true" />
            <span className="service-health-name">{name}</span>
            <a className="service-health-link" href={serviceLinks[name] ?? '#'} target="_blank" rel="noreferrer">
              ...
            </a>
          </div>
        ))}
      </div>
    </article>
  )
}
