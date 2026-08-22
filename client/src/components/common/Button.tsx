import type { ButtonHTMLAttributes, ReactNode } from 'react'

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & { children: ReactNode; variant?: 'primary' | 'secondary' | 'quiet' }

export function Button({ children, variant = 'primary', className = '', ...props }: ButtonProps) {
  const styles = { primary: 'bg-[var(--royal-blue)] text-white hover:bg-[#102e62]', secondary: 'border border-[var(--silver)] bg-white text-[var(--royal-blue)] hover:bg-[var(--surface)]', quiet: 'text-[var(--muted)] hover:text-[var(--royal-blue)]' }
  return <button className={`rounded-[4px] px-4 py-2 text-sm font-semibold transition-colors ${styles[variant]} ${className}`} {...props}>{children}</button>
}
