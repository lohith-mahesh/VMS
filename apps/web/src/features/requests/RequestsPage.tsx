import { useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Alert, Card, Loading, PageTitle, Pagination, TextInput } from '../../components/Ui';
import { api, ApiError, queryString } from '../../lib/api';
import type { Paged, RequestStatus, RequestSummary } from '../../lib/types';
import { useMe } from '../../app/MeContext';
import { RequestTable } from './RequestTable';

export function RequestsPage({ mode = 'all' }: { mode?: 'all' | 'history' | 'pending' }) {
  const me = useMe();
  const [parameters] = useSearchParams();
  const [search, setSearch] = useState(parameters.get('search') ?? '');
  const [status, setStatus] = useState<RequestStatus | ''>('');
  const [site, setSite] = useState('');
  const [visitDate, setVisitDate] = useState('');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<Paged<RequestSummary>>();
  const [error, setError] = useState('');
  useEffect(() => { setSearch(parameters.get('search') ?? ''); setPage(1); }, [parameters]);
  const load = useCallback((signal?: AbortSignal) => {
    setError('');
    return api<Paged<RequestSummary>>(`/api/requests${queryString({ search, status, siteCode: site, visitDate, view: mode === 'all' ? parameters.get('view') : mode, page, pageSize: 25 })}`, { signal })
      .then(setResult)
      .catch(value => {
        if (value instanceof DOMException && value.name === 'AbortError') return;
        setError(value instanceof ApiError ? value.message : 'Requests could not be loaded.');
      });
  }, [page, search, site, status, visitDate, mode, parameters]);
  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => void load(controller.signal), 250);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [load]);
  const title = mode === 'history' ? 'Visitor History' : mode === 'pending' ? 'Pending Actions' : me.role === 'Reception' ? 'Visitor Requests' : me.role === 'ExportControl' ? 'Visitor Requests' : 'Visitor Requests';
  return <>
    <PageTitle title={title} description="Search, filter and open visitor requests." actions={me.role === 'HostRequester' && mode === 'all' && <Link className="button button--primary" to="/requests/new">Create Visitor Request</Link>} />
    <Card className="filter-card"><div className="filter-grid">
      <label><span>Search</span><TextInput value={search} onChange={event => { setSearch(event.target.value); setPage(1); }} placeholder="Request, visitor, host or company" /></label>
      <label><span>Status</span><select value={status} onChange={event => { setStatus(event.target.value as RequestStatus | ''); setPage(1); }}><option value="">All statuses</option>{statuses.map(value => <option key={value}>{value}</option>)}</select></label>
      <label><span>Site</span><TextInput value={site} onChange={event => { setSite(event.target.value); setPage(1); }} placeholder="All sites" /></label>
      <label><span>Visit date</span><TextInput type="date" value={visitDate} onChange={event => { setVisitDate(event.target.value); setPage(1); }} /></label>
    </div></Card>
    {error && <Alert tone="danger" title="Unable to load requests">{error}</Alert>}
    {!result && !error ? <Loading label="Loading requests" /> : result && <Card><RequestTable requests={result.items} /><Pagination page={result.page} pageSize={result.pageSize} total={result.total} onChange={setPage} /></Card>}
  </>;
}

const statuses: RequestStatus[] = ['Draft', 'VisitorFormPending', 'VisitorFormSubmitted', 'HostReview', 'PendingEcReview', 'PendingDocumentation', 'DocumentationSubmitted', 'EcReReviewRequired', 'Approved', 'PartiallyApproved', 'Rejected', 'Cancelled', 'VisitProcessCompleted'];
