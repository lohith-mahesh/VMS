import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listVisitorRequests, type VisitorRequestListItem } from '../services/apiClient'
import { useAuth } from '../auth/useAuth'

export function DashboardPage() {
  const { user } = useAuth()
  const [requests, setRequests] = useState<VisitorRequestListItem[]>([])
  const [total, setTotal] = useState(0)
  const [error, setError] = useState('')
  useEffect(() => { listVisitorRequests().then((result) => { setRequests(result.items); setTotal(result.total) }).catch(() => setError('Dashboard data could not be loaded.')) }, [])
  return <div className="space-y-7"><header><p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--royal-blue)]">RRVMS workspace</p><h1 className="display mt-2 text-4xl font-bold text-[var(--royal-blue)]">Good morning, {user?.name.split(' ')[0]}</h1><p className="mt-2 text-sm text-[var(--muted)]">Live visitor request activity from the API.</p></header>{error && <p role="alert" className="border border-[#e1b5b5] bg-[#fff4f4] p-4 text-sm text-[#9b2c2c]">{error}</p>}<section className="grid gap-4 sm:grid-cols-2"><div className="border border-[var(--silver)] bg-white p-6"><p className="text-xs uppercase tracking-wide text-[var(--muted)]">Total requests</p><p className="mt-3 text-3xl font-semibold text-[var(--royal-blue)]">{total}</p></div><div className="border border-[var(--silver)] bg-white p-6"><p className="text-xs uppercase tracking-wide text-[var(--muted)]">Loaded activity</p><p className="mt-3 text-3xl font-semibold text-[var(--royal-blue)]">{requests.length}</p></div></section><section className="border border-[var(--silver)] bg-white p-6"><div className="flex flex-wrap items-center justify-between gap-3"><h2 className="display text-xl font-bold text-[var(--royal-blue)]">Recent requests</h2><Link to="/visitor-requests" className="text-sm font-semibold text-[var(--royal-blue)]">View all</Link></div>{requests.length === 0 ? <p className="mt-5 text-sm text-[var(--muted)]">No visitor activity yet.</p> : <div className="mt-4 divide-y divide-[var(--silver)]">{requests.slice(0, 5).map((request) => <Link key={request.id} to={`/visitor-requests/${request.id}`} className="flex flex-wrap justify-between gap-2 py-3 text-sm"><span><strong className="text-[var(--royal-blue)]">{request.requestNumber}</strong><span className="ml-3 text-[var(--ink)]">{request.visitorName}</span></span><span className="text-[var(--muted)]">{request.currentStatus}</span></Link>)}</div>}</section></div>
}
