import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Alert, Card, Loading, PageTitle, StatusTag, TextInput } from '../../components/Ui';
import { api, ApiError } from '../../lib/api';
import type { Dashboard } from '../../lib/types';
import { useMe } from '../../app/MeContext';
import { RequestTable } from '../requests/RequestTable';

export function DashboardPage() {
  const me = useMe();
  const [parameters] = useSearchParams();
  const [receptionSearch, setReceptionSearch] = useState('');
  const [dashboard, setDashboard] = useState<Dashboard>();
  const [error, setError] = useState('');
  useEffect(() => {
    const controller = new AbortController();
    api<Dashboard>('/api/dashboard', { signal: controller.signal }).then(setDashboard).catch(value => {
      if (value instanceof DOMException && value.name === 'AbortError') return;
      setError(value instanceof ApiError ? value.message : 'Dashboard data could not be loaded.');
    });
    return () => controller.abort();
  }, []);
  if (error) return <Alert tone="danger" title="Dashboard unavailable">{error}</Alert>;
  if (!dashboard) return <Loading label="Loading dashboard" />;
  const metrics = me.role === 'Reception'
    ? [['Today’s Visitors', dashboard.todayVisitors, '/?view=today'], ['Expected', dashboard.upcoming, '/?view=expected'], ['Currently Inside', dashboard.checkedIn, '/?view=inside'], ['Checked Out', dashboard.checkedOut, '/?view=checked-out'], ['Rejected', dashboard.receptionRejections, '/?view=rejected'], ['No-Shows', dashboard.noShows, '/?view=no-show']]
    : me.role === 'ExportControl'
      ? [['Pending Export Control Review', dashboard.pendingEcReview, '/requests?view=pending-ec'], ['Pending Documentation', dashboard.pendingDocumentation, '/requests?view=pending-documentation'], ['Reception Rejections', dashboard.receptionRejections, '/requests?view=reception-rejected'], ['Approved', dashboard.approved, '/requests?view=approved'], ['Rejected', dashboard.rejected, '/requests?view=rejected'], ['Visitor History', dashboard.visitorHistory, '/history']]
      : [['Total Visitors', dashboard.totalVisitors, '/requests'], ['Pending Actions', dashboard.pendingActions, '/requests?view=pending'], ['Approved', dashboard.approved, '/requests?view=approved'], ['Today’s Visits', dashboard.todayVisitors, '/requests?view=today'], ['Currently Inside', dashboard.checkedIn, '/requests?view=checked-in'], ['Checked Out', dashboard.checkedOut, '/requests?view=checked-out'], ['Pending Documentation', dashboard.pendingDocumentation, '/requests?view=pending-documentation'], ['No-Shows', dashboard.noShows, '/requests?view=no-show']];
  return <>
    <PageTitle title={roleWelcome(me.role)} actions={me.role === 'HostRequester' ? <Link className="button button--primary" to="/requests/new">Create Visitor Request</Link> : me.role === 'Reception' ? <Link className="button button--primary" to="/reports">Open Reports</Link> : null} />
    <div className="metric-grid">{metrics.map(([label, value, path], index) => <Link className={`card metric metric--${index + 1}`} key={String(label)} to={String(path)}><span>{label}</span><strong>{value}</strong></Link>)}</div>
    {me.role === 'ExportControl' ? <>
      <Card><div className="section-heading"><h2>Pending Export Control Reviews</h2><Link to="/requests?view=pending-ec">View All</Link></div><RequestTable requests={dashboard.pendingReviewRequests} /></Card>
      <Card><div className="section-heading"><h2>Pending Documentation</h2><Link to="/requests?view=pending-documentation">View All</Link></div><RequestTable requests={dashboard.pendingDocumentationRequests} /></Card>
    </> : me.role === 'Reception'
      ? <ReceptionIndex items={dashboard.receptionItems} view={parameters.get('view') ?? ''} search={receptionSearch} onSearch={setReceptionSearch} />
      : <Card><div className="section-heading"><h2>Recent Requests</h2><Link to="/requests">View All</Link></div><RequestTable requests={dashboard.recentRequests} /></Card>}
  </>;
}

function ReceptionIndex({ items, view, search, onSearch }: { items: Dashboard['receptionItems']; view: string; search: string; onSearch: (value: string) => void }) {
  const views: Record<string, (item: Dashboard['receptionItems'][number]) => boolean> = {
    today: item => item.scheduledToday,
    expected: item => ['Upcoming', 'VerificationInProgress', 'ReceptionVerification'].includes(item.status),
    inside: item => item.status === 'CheckedIn',
    'checked-out': item => item.status === 'Completed',
    rejected: item => item.status === 'EntryRejected',
    'no-show': item => item.status === 'NoShow'
  };
  const query = search.trim().toLowerCase();
  const filtered = items.filter(item => (!views[view] || views[view](item))
    && (!query || [item.requestNumber, item.visitorName, item.companyName, item.hostName, item.escortName, item.badgeId, item.assetSummary, item.status]
      .some(value => value.toLowerCase().includes(query))));
  return <Card><div className="section-heading"><h2>Visitor Index</h2>{view && <Link to="/">Clear View</Link>}</div>
    <label className="reception-search"><span>Search visitors</span><TextInput value={search} onChange={event => onSearch(event.target.value)} placeholder="Name, company, request, host, badge or asset" /></label>
    {filtered.length === 0 ? <p className="muted">No matching visitors.</p> : <div className="table-wrap"><table><thead><tr><th>Request Number</th><th>Visitor</th><th>Company</th><th>Host and Escort</th><th>Visit Date</th><th>Person Type</th><th>Identity</th><th>Assets</th><th>Badge</th><th>Status</th><th>Details</th></tr></thead><tbody>{filtered.map(item => <tr key={`${item.visitorId}-${item.visitDayId}`}>
      <td data-label="Request">{item.requestNumber}</td>
      <td data-label="Visitor"><strong>{item.visitorName || 'Visitor Details Pending'}</strong></td>
      <td data-label="Company">{item.companyName || '—'}</td>
      <td data-label="Host">{item.hostName}<small>{item.escortName}</small></td>
      <td data-label="Visit Date">{item.visitDate}</td>
      <td data-label="Person Type">{item.personType}</td>
      <td data-label="Identity"><StatusTag value={item.identityStatus} /></td>
      <td data-label="Assets">{item.assetSummary || 'None'}<small><StatusTag value={item.assetsStatus} /></small></td>
      <td data-label="Badge">{item.badgeId || '—'}<small>{item.badgeType}</small></td>
      <td data-label="Status"><StatusTag value={item.status} /></td>
      <td data-label="Details"><Link to={`/requests/${item.requestId}?visitor=${item.visitorId}&day=${item.visitDayId}`}>View</Link></td>
    </tr>)}</tbody></table></div>}
  </Card>;
}

function roleWelcome(role: string): string {
  if (role === 'Reception') return 'Security/Reception';
  if (role === 'ExportControl') return 'Export Control';
  return 'Host/Requester';
}
