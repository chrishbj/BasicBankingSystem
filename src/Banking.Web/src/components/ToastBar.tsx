type ToastBarProps = {
  text: string
  tone: 'success' | 'danger' | 'info'
  onDismiss: () => void
}

export function ToastBar({ text, tone, onDismiss }: ToastBarProps) {
  if (!text) {
    return null
  }

  return (
    <div className={`toast-bar toast-bar-${tone}`} role="status" aria-live="polite">
      <span>{text}</span>
      <button type="button" className="toast-dismiss" onClick={onDismiss} aria-label="Dismiss notification">
        ×
      </button>
    </div>
  )
}
