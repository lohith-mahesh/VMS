import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getNotifications, getReceptionDashboard, listVisitorRequests, markNotificationRead, type NotificationItem, type ReceptionDashboardResponse, type VisitorRequestListItem } from '../services/apiClient'
import { userFacingApiError } from '../utils/logger'
import { formatStatus } from '../utils/formatters'

export function PendingActionsPage() {
  const [items, setItems] = useState<VisitorRequestListItem[]>([])
  const [error, setError] = useState('')
  useEffect(() => {
    listVisitorRequests()
      .then(result => setItems(result.items.filter(item => ['VISITOR_FORM_PENDING', 'VISITOR_FORM_SUBMITTED', 'HOST_DPS', 'EC_REVIEW', 'PENDING_DOCUMENTATION', 'EC_RE_REVIEW_REQUIRED'].includes(item.currentStatus))))
      .catch(reason => setError(userFacingApiError(reason, 'Pending actions could not be loaded.')))
  }, [])
  return (
    <Frame title="Pending actions" error={error}>
      {items.length ? <Rows items={items} /> : <p className="text-sm text-[var(--muted)]">No pending actions.</p>}
    </Frame>
  )
}

export function ReceptionPage() {
  const [dashboard, setDashboard] = useState<ReceptionDashboardResponse | null>(null)
  const [query, setQuery] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  const load = () => {
    setLoading(true)
    setError('')
    getReceptionDashboard()
      .then(setDashboard)
      .catch(reason => setError(userFacingApiError(reason, 'Reception dashboard data could not be loaded.')))
      .finally(() => setLoading(false))
  }

  useEffect(() => { void load() }, [])

  const filteredItems = dashboard?.items.filter(item => {
    if (!query.trim()) return true
    const q = query.toLowerCase()
    return item.visitorName.toLowerCase().includes(q) || item.company.toLowerCase().includes(q) || item.requestNumber.toLowerCase().includes(q) || item.batchId?.toLowerCase().includes(q)
  }) ?? []

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--royal-blue)]">Reception Operations</p>
        <h1 className="display mt-2 text-4xl font-bold text-[var(--royal-blue)]">Reception Dashboard</h1>
        <p className="mt-2 text-sm text-[var(--muted)]">Verify visitor identities, issue badges, and manage check-in/check-out.</p>
      </header>

      {error && (
        <div role="alert" className="flex flex-wrap items-center justify-between gap-3 border border-[#e1b5b5] bg-[#fff4f4] p-4 text-sm text-[#9b2c2c]">
          <span>{error}</span>
          <button type="button" onClick={load} className="cursor-pointer font-semibold underline">Retry</button>
        </div>
      )}

      {loading ? (
        <p className="text-sm text-[var(--muted)]">Loading reception dashboard...</p>
      ) : dashboard ? (
        <div className="space-y-6">
          <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Metric label="Today's Visitors" value={dashboard.todaysVisitors} />
            <Metric label="Expected" value={dashboard.expected} />
            <Metric label="Currently Inside" value={dashboard.currentlyInside} />
            <Metric label="Checked Out" value={dashboard.checkedOut} />
            <Metric label="On Hold" value={dashboard.onHold} />
            <Metric label="No Show" value={dashboard.noShow} />
          </section>

          <section className="border border-[var(--silver)] bg-white p-6">
            <div className="mb-5 flex flex-wrap gap-3">
              <input
                aria-label="Search reception visitors"
                className="w-full border border-[var(--silver)] px-3 py-2 text-sm sm:w-80"
                placeholder="Search Batch ID, visitor, company or request"
                value={query}
                onChange={event => setQuery(event.target.value)}
              />
            </div>

            {filteredItems.length ? (
              <div className="overflow-x-auto">
                <table className="min-w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-[var(--silver)] text-xs uppercase tracking-wider text-[var(--muted)]">
                      <th className="p-3">Batch ID</th>
                      <th className="p-3">Request</th>
                      <th className="p-3">Visitor Name</th>
                      <th className="p-3">Company</th>
                      <th className="p-3">Visit Date</th>
                      <th className="p-3">Status</th>
                      <th className="p-3">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[var(--silver)]">
                    {filteredItems.map(item => (
                      <tr key={item.id} className="hover:bg-[var(--surface)]">
                        <td className="p-3 font-bold text-[var(--royal-blue)]">{item.batchId}</td>
                        <td className="p-3 font-medium text-[var(--ink)]">{item.requestNumber}</td>
                        <td className="p-3 font-medium text-[var(--ink)]">{item.visitorName}</td>
                        <td className="p-3 text-[var(--muted)]">{item.company}</td>
                        <td className="p-3 text-[var(--muted)]">{item.visitDate}</td>
                        <td className="p-3">
                          <span className="rounded bg-[#e9eef6] px-2.5 py-1 text-xs font-semibold text-[var(--royal-blue)]">
                            {formatStatus(item.status)}
                          </span>
                        </td>
                        <td className="p-3">
                          <Link
                            to={`/visitor-requests/${item.requestId}`}
                            className="cursor-pointer rounded bg-[var(--royal-blue)] px-3 py-1.5 text-xs font-semibold text-white hover:bg-[var(--rr-primary)]"
                          >
                            Manage Visitor
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="text-sm text-[var(--muted)]">No approved visitors found for reception today.</p>
            )}
          </section>
        </div>
      ) : null}
    </div>
  )
}

export function NotificationsPage() {
  const [items, setItems] = useState<NotificationItem[]>([])
  const [error, setError] = useState('')
  const load = () => getNotifications().then(setItems).catch(reason => setError(userFacingApiError(reason, 'Notifications could not be loaded.')))
  useEffect(() => { void load() }, [])

  const read = async (id: string) => {
    try {
      await markNotificationRead(id)
      setItems(current => current.map(item => item.id === id ? { ...item, isRead: true } : item))
    } catch (reason) {
      setError(userFacingApiError(reason, 'Notification could not be updated.'))
    }
  }

  return (
    <Frame title="Notifications" error={error}>
      {items.length ? (
        <div className="divide-y divide-[var(--silver)]">
          {items.map(item => (
            <div key={item.id} className="flex items-center justify-between p-4">
              <div>
                <p className="text-sm font-semibold text-[var(--ink)]">{item.message}</p>
                <p className="text-xs text-[var(--muted)]">{new Date(item.createdAt).toLocaleString()}</p>
              </div>
              {!item.isRead && (
                <button type="button" onClick={() => void read(item.id)} className="cursor-pointer text-xs font-semibold text-[var(--royal-blue)]">
                  Mark as read
                </button>
              )}
            </div>
          ))}
        </div>
      ) : (
        <p className="text-sm text-[var(--muted)]">No notifications.</p>
      )}
    </Frame>
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

function Frame({ title, error, children }: { title: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--royal-blue)]">Operations</p>
        <h1 className="display mt-2 text-4xl font-bold text-[var(--royal-blue)]">{title}</h1>
      </header>
      {error && <p role="alert" className="border border-[#e1b5b5] bg-[#fff4f4] p-4 text-sm text-[#9b2c2c]">{error}</p>}
      <section className="border border-[var(--silver)] bg-white p-6">{children}</section>
    </div>
  )
}

function Rows({ items }: { items: VisitorRequestListItem[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="min-w-full text-left text-sm">
        <thead>
          <tr className="border-b border-[var(--silver)] text-xs uppercase tracking-wider text-[var(--muted)]">
            <th className="p-3">Batch ID</th>
            <th className="p-3">Request</th>
            <th className="p-3">Visitor Name</th>
            <th className="p-3">Company</th>
            <th className="p-3">Status</th>
            <th className="p-3">Action</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-[var(--silver)]">
          {items.map(item => (
            <tr key={item.id} className="hover:bg-[var(--surface)]">
              <td className="p-3 font-bold text-[var(--royal-blue)]">{item.batchId || '-'}</td>
              <td className="p-3 font-medium text-[var(--ink)]">{item.requestNumber}</td>
              <td className="p-3 text-[var(--ink)]">{item.visitorName}</td>
              <td className="p-3 text-[var(--muted)]">{item.companyName}</td>
              <td className="p-3">
                <span className="rounded bg-[#e9eef6] px-2.5 py-1 text-xs font-semibold text-[var(--royal-blue)]">
                  {formatStatus(item.currentStatus)}
                </span>
              </td>
              <td className="p-3">
                <Link to={`/visitor-requests/${item.id}`} className="font-semibold text-[var(--royal-blue)]">View</Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
