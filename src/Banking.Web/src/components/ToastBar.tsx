type ToastBarProps = {
  text: string
}

export function ToastBar({ text }: ToastBarProps) {
  if (!text) {
    return null
  }

  return <div className="toast-bar">{text}</div>
}
