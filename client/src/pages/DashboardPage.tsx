import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getDashboard, type DashboardResponse } from '../services/apiClient'
import { useAuth } from '../auth/useAuth'
import { userFacingApiError } from '../utils/logger'

export function DashboardPage() {
  const { user } = useAuth()
  const [dashboard, setDashboard] = useState<DashboardResponse | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const load = () => { setLoading(true); setError(''); getDashboard().then(setDashboard).catch((reason) => setError(userFacingApiError(reason, 'Dashboard data could not be loaded.'))).finally(() => setLoading(false)) }
  useEffect(() => { const run = async () => { await load() }; void run() }, [])
  return <div className="space-y-7"><header><p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--royal-blue)]">RRVMS workspace</p><h1 className="display mt-2 text-4xl font-bold text-[var(--royal-blue)]">Good morning, {user?.name.split(' ')[0]}</h1><p className="mt-2 text-sm text-[var(--muted)]">Live visitor request activity from the API.</p></header>{error && <div role="alert" className="flex flex-wrap items-center justify-between gap-3 border border-[#e1b5b5] bg-[#fff4f4] p-4 text-sm text-[#9b2c2c]"><span>{error}</span><button type="button" onClick={load} className="font-semibold underline">Retry</button></div>}{loading ? <p className="text-sm text-[var(--muted)]">Loading dashboard...</p> : dashboard && <><section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4"><Metric label="Total requests" value={dashboard.totalRequests} /><Metric label="Pending actions" value={dashboard.pendingActions} /><Metric label="Today's visits" value={dashboard.todaysVisits} /><Metric label="Currently inside" value={dashboard.currentlyInside} /></section><section className="border border-[var(--silver)] bg-white p-6"><div className="flex flex-wrap items-center justify-between gap-3"><h2 className="display text-xl font-bold text-[var(--royal-blue)]">Recent requests</h2><Link to="/visitor-requests" className="text-sm font-semibold text-[var(--royal-blue)]">View all</Link></div>{dashboard.recentRequests.length === 0 ? <p className="mt-5 text-sm text-[var(--muted)]">No visitor activity yet.</p> : <div className="mt-4 divide-y divide-[var(--silver)]">{dashboard.recentRequests.map((request) => <Link key={request.id} to={`/visitor-requests/${request.id}`} className="flex flex-wrap justify-between gap-2 py-3 text-sm"><span><strong className="text-[var(--royal-blue)]">{request.requestNumber}</strong><span className="ml-3 text-[var(--ink)]">{request.visitorName}</span></span><span className="text-[var(--muted)]">{request.currentStatus}</span></Link>)}</div>}</section></>}</div>
}

function Metric({ label, value }: { label: string; value: number }) { return <div className="border border-[var(--silver)] bg-white p-6"><p className="text-xs uppercase tracking-wide text-[var(--muted)]">{label}</p><p className="mt-3 text-3xl font-semibold text-[var(--royal-blue)]">{value}</p></div> }
