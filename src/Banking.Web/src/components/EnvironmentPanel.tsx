type EnvironmentPanelProps = {
  health: Record<string, string>
}

const services = ['customer', 'account', 'deposit', 'audit'] as const

const serviceLinks: Record<string, string> = {
  customer: '/customer-api/swagger/index.html',
  account: '/account-api/swagger/index.html',
  deposit: '/deposit-api/swagger/index.html',
  audit: '/audit-api/swagger/index.html',
}

export function EnvironmentPanel({ health }: EnvironmentPanelProps) {
  return (
    <article className="panel compact-panel">
      <div className="panel-head compact-panel-head">
        <div>
          <p className="eyebrow">Environment</p>
          <h2>Services</h2>
        </div>
        <a className="ghost-button" href="/gateway-api/api/v1/system/docs" target="_blank" rel="noreferrer">
          Docs
        </a>
      </div>
      <div className="service-health-list">
        {services.map((name) => {
          const value = health[name] ?? 'Checking'
          const isHealthy = value === 'Healthy'

          return (
          <div key={name} className="service-health-item">
            <span className={isHealthy ? 'service-dot service-dot-up' : 'service-dot service-dot-down'} aria-hidden="true" />
            <span className="service-health-name">{name}</span>
            <a className="service-health-link" href={serviceLinks[name] ?? '#'} target="_blank" rel="noreferrer">
              ...
            </a>
          </div>
          )
        })}
      </div>
    </article>
  )
}
