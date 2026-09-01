import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getDashboard, getEcDashboard, type DashboardResponse, type EcDashboardResponse } from '../services/apiClient'
import { useAuth } from '../auth/useAuth'
import { userFacingApiError } from '../utils/logger'
import { formatStatus } from '../utils/formatters'

export function DashboardPage() {
  const { user } = useAuth()
  const isEc = user?.role === 'EXPORT_CONTROL'
  const [dashboard, setDashboard] = useState<DashboardResponse | null>(null)
  const [ecDashboard, setEcDashboard] = useState<EcDashboardResponse | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  const load = () => {
    setLoading(true)
    setError('')
    if (isEc) {
      getEcDashboard()
        .then(setEcDashboard)
        .catch((reason) => setError(userFacingApiError(reason, 'EC Dashboard data could not be loaded.')))
        .finally(() => setLoading(false))
    } else {
      getDashboard()
        .then(setDashboard)
        .catch((reason) => setError(userFacingApiError(reason, 'Dashboard data could not be loaded.')))
        .finally(() => setLoading(false))
    }
  }

  useEffect(() => {
    load()
  }, [user?.role])

  return (
    <div className="space-y-7">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--royal-blue)]">Visitor management workspace</p>
        <h1 className="display mt-2 text-4xl font-bold text-[var(--royal-blue)]">Good morning, {user?.name.split(' ')[0]}</h1>
        <p className="mt-2 text-sm text-[var(--muted)]">Live visitor request activity from the API.</p>
      </header>

      {error && (
        <div role="alert" className="flex flex-wrap items-center justify-between gap-3 border border-[#e1b5b5] bg-[#fff4f4] p-4 text-sm text-[#9b2c2c]">
          <span>{error}</span>
          <button type="button" onClick={load} className="cursor-pointer font-semibold underline">Retry</button>
        </div>
      )}

      {loading ? (
        <p className="text-sm text-[var(--muted)]">Loading dashboard...</p>
      ) : isEc && ecDashboard ? (
        <div className="space-y-6">
          <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Metric label="Pending EC Reviews" value={ecDashboard.pendingEcReviews} />
            <Metric label="Pending Documentation" value={ecDashboard.pendingDocumentation} />
            <Metric label="DPS Flags" value={ecDashboard.dpsFlags} />
            <Metric label="Approved" value={ecDashboard.approved} />
            <Metric label="Rejected" value={ecDashboard.rejected} />
            <Metric label="Visitor History" value={ecDashboard.visitorHistory} />
            <Metric label="Attendance" value={ecDashboard.attendance} />
          </section>

          <section className="border border-[var(--silver)] bg-white p-6">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--silver)] pb-4">
              <h2 className="display text-xl font-bold text-[var(--royal-blue)]">Pending EC Reviews</h2>
              <span className="text-xs font-semibold uppercase tracking-wider text-[var(--muted)]">Requires Export Control action</span>
            </div>

            {ecDashboard.pendingEcReviewsItems.length === 0 ? (
              <p className="mt-5 text-sm text-[var(--muted)]">No requests currently pending Export Control review.</p>
            ) : (
              <div className="mt-4 overflow-x-auto">
                <table className="min-w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-[var(--silver)] text-xs uppercase tracking-wider text-[var(--muted)]">
                      <th className="p-3">Batch ID</th>
                      <th className="p-3">Request Number</th>
                      <th className="p-3">Visitor Full Name</th>
                      <th className="p-3">Company</th>
                      <th className="p-3">Visit Date</th>
                      <th className="p-3">DPS Status</th>
                      <th className="p-3">Current Stage</th>
                      <th className="p-3">Status</th>
                      <th className="p-3">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[var(--silver)]">
                    {ecDashboard.pendingEcReviewsItems.map((item) => (
                      <tr key={item.id} className="hover:bg-[var(--surface)]">
                        <td className="p-3 font-bold text-[var(--royal-blue)]">{item.batchId || '-'}</td>
                        <td className="p-3 font-semibold text-[var(--royal-blue)]">{item.requestNumber}</td>
                        <td className="p-3 font-medium text-[var(--ink)]">{item.visitorName || 'Adam Gilchrist'}</td>
                        <td className="p-3 text-[var(--muted)]">{item.companyName}</td>
                        <td className="p-3 text-[var(--muted)]">{item.visitDate ? String(item.visitDate) : 'Today'}</td>
                        <td className="p-3">
                          <span className="inline-block rounded bg-[#fff3cd] px-2 py-0.5 text-xs font-semibold text-[#856404]">
                            {item.dpsStatus || 'FLAGGED'} (DEMO DATA)
                          </span>
                        </td>
                        <td className="p-3 text-xs text-[var(--muted)]">{item.currentStage || 'Export Control Review'}</td>
                        <td className="p-3 text-xs font-semibold text-[var(--royal-blue)]">{formatStatus(item.currentStatus)}</td>
                        <td className="p-3">
                          <Link
                            to={`/visitor-requests/${item.id}`}
                            className="cursor-pointer rounded bg-[var(--royal-blue)] px-3 py-1.5 text-xs font-semibold text-white hover:bg-[var(--rr-primary)]"
                          >
                            Review Details
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      ) : dashboard ? (
        <div className="space-y-6">
          <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Metric label="Total Requests" value={dashboard.totalRequests} />
            <Metric label="Pending Actions" value={dashboard.pendingActions} />
            <Metric label="Today's Visits" value={dashboard.todaysVisits} />
            <Metric label="Currently Inside" value={dashboard.currentlyInside} />
            <Metric label="Upcoming Visits" value={dashboard.upcomingVisits} />
            <Metric label="No Shows" value={dashboard.noShows} />
            <Metric label="Pending EC Reviews" value={dashboard.pendingEcReviews} />
            <Metric label="Pending Documentation" value={dashboard.pendingDocumentation} />
          </section>

          <section className="border border-[var(--silver)] bg-white p-6">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--silver)] pb-4">
              <h2 className="display text-xl font-bold text-[var(--royal-blue)]">Recent Requests</h2>
              <Link to="/visitor-requests" className="text-xs font-semibold uppercase tracking-wider text-[var(--royal-blue)] hover:underline">
                View all -&gt;
              </Link>
            </div>

            {dashboard.recentRequests.length === 0 ? (
              <p className="mt-5 text-sm text-[var(--muted)]">No recent visitor requests found.</p>
            ) : (
              <div className="mt-4 overflow-x-auto">
                <table className="min-w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-[var(--silver)] text-xs uppercase tracking-wider text-[var(--muted)]">
                      <th className="p-3">Batch ID</th>
                      <th className="p-3">Request Number</th>
                      <th className="p-3">Visitor Name</th>
                      <th className="p-3">Company</th>
                      <th className="p-3">Status</th>
                      <th className="p-3">Created</th>
                      <th className="p-3">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[var(--silver)]">
                    {dashboard.recentRequests.map((item) => (
                      <tr key={item.id} className="hover:bg-[var(--surface)]">
                        <td className="p-3 font-bold text-[var(--royal-blue)]">{item.batchId || '-'}</td>
                        <td className="p-3 font-semibold text-[var(--royal-blue)]">{item.requestNumber}</td>
                        <td className="p-3 font-medium text-[var(--ink)]">{item.visitorName || 'Adam Gilchrist'}</td>
                        <td className="p-3 text-[var(--muted)]">{item.companyName}</td>
                        <td className="p-3">
                          <span className="rounded bg-[#e9eef6] px-2.5 py-1 text-xs font-semibold text-[var(--royal-blue)]">
                            {formatStatus(item.currentStatus)}
                          </span>
                        </td>
                        <td className="p-3 text-xs text-[var(--muted)]">{new Date(item.createdAt).toLocaleDateString()}</td>
                        <td className="p-3">
                          <Link to={`/visitor-requests/${item.id}`} className="font-semibold text-[var(--royal-blue)] hover:underline cursor-pointer">
                            Open
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      ) : null}
    </div>
  )
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="border border-[var(--silver)] bg-white p-4">
      <p className="text-xs font-semibold uppercase text-[var(--muted)]">{label}</p>
      <p className="display mt-2 text-3xl font-bold text-[var(--royal-blue)]">{value}</p>
    </div>
  )
}
