import type { ReactNode } from 'react'

export function PageContainer({ children }: { children: ReactNode }) { return <main className="min-w-0 flex-1 bg-[var(--surface)] p-6 md:p-8">{children}</main> }
