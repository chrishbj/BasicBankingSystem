type SectionStatusProps = {
  text: string
}

export function SectionStatus({ text }: SectionStatusProps) {
  return <p className="section-status">{text}</p>
}
