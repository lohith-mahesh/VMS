import { quickActions } from '../../data/mockData'
import { Button } from '../common/Button'

export function QuickActions() { return <section className="border border-[var(--silver)] bg-white p-5"><h2 className="display text-xl font-bold text-[var(--royal-blue)]">Quick actions</h2><p className="mt-1 text-xs text-[var(--muted)]">Common tasks for your workspace</p><div className="mt-5 space-y-2">{quickActions.map((action, index) => <Button key={action} variant={index === 0 ? 'primary' : 'secondary'} className="flex w-full items-center justify-between text-left">{action}<span aria-hidden="true">→</span></Button>)}</div></section> }
