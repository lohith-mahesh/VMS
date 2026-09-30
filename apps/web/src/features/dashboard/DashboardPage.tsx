import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Alert, Card, Loading, PageTitle } from '../../components/Ui';
import { api, ApiError } from '../../lib/api';
import type { Dashboard } from '../../lib/types';
import { useMe } from '../../app/MeContext';
import { RequestTable } from '../requests/RequestTable';

export function DashboardPage() {
  const me = useMe();
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
    ? [['Today’s visitors', dashboard.todayVisitors], ['Checked in', dashboard.checkedIn], ['Upcoming', dashboard.upcoming], ['No-shows', dashboard.noShows]]
    : me.role === 'ExportControl'
      ? [['Total visitors', dashboard.totalVisitors], ['Pending screening', dashboard.pendingScreening], ['Corrections', dashboard.pendingCorrection], ['Approved', dashboard.approved]]
      : [['Total visitors', dashboard.totalVisitors], ['Actions needed', dashboard.pendingActions], ['Approved', dashboard.approved], ['Checked out today', dashboard.checkedOut]];
  return <>
    <PageTitle eyebrow={roleWelcome(me.role)} title={`Welcome, ${me.displayName.split(' ')[0] ?? me.displayName}`} description="Here is the latest visitor activity and the work that needs your attention." actions={me.role === 'HostRequester' && <Link className="button button--primary" to="/requests/new">Create request</Link>} />
    <div className="metric-grid">{metrics.map(([label, value], index) => <Card className={`metric metric--${index + 1}`} key={String(label)}><span>{label}</span><strong>{value}</strong></Card>)}</div>
    <Card><div className="section-heading"><div><p className="eyebrow">Latest activity</p><h2>Recent requests</h2></div><Link to="/requests">View all</Link></div><RequestTable requests={dashboard.recentRequests} /></Card>
  </>;
}

function roleWelcome(role: string): string {
  if (role === 'Reception') return 'Reception overview';
  if (role === 'ExportControl') return 'Screening overview';
  return 'Host overview';
}
