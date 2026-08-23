import { Component, type ErrorInfo, type ReactNode } from 'react'
import { logger } from '../../utils/logger'

type Props = { children: ReactNode }
type State = { hasError: boolean }

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State { return { hasError: true } }

  componentDidCatch(error: Error, info: ErrorInfo) { logger.error('Unexpected application error', { error, componentStack: info.componentStack }) }

  render() {
    if (!this.state.hasError) return this.props.children
    return <main className="flex min-h-screen items-center justify-center bg-[var(--surface)] px-6"><section className="w-full max-w-lg border border-[var(--silver)] bg-white p-8 text-center shadow-[0_2px_8px_rgba(20,32,48,0.06)]"><h1 className="display text-3xl font-bold text-[var(--royal-blue)]">Something went wrong</h1><p className="mt-3 text-sm text-[var(--muted)]">An unexpected application error occurred.</p><button type="button" onClick={() => { this.setState({ hasError: false }); window.location.reload() }} className="mt-6 rounded-[4px] bg-[var(--royal-blue)] px-4 py-2.5 text-sm font-semibold text-white">Try again</button></section></main>
  }
}