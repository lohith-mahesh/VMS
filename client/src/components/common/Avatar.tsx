type AvatarProps = { initials: string; size?: 'sm' | 'md' }

export function Avatar({ initials, size = 'md' }: AvatarProps) {
  return <span className={`inline-flex shrink-0 items-center justify-center rounded-full bg-[#dce4f0] font-semibold text-[var(--royal-blue)] ${size === 'sm' ? 'h-7 w-7 text-[10px]' : 'h-9 w-9 text-xs'}`}>{initials}</span>
}
