import { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Card, Loading, PageTitle, Pagination, StatusTag, TextInput } from '../../components/Ui';
import { api, ApiError, download, queryString } from '../../lib/api';
import { formatDate } from '../../lib/format';
import type { Paged, ReportRow, RequestStatus } from '../../lib/types';

export function ReportsPage() {
  const [search, setSearch] = useState('');
  const [site, setSite] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [status, setStatus] = useState<RequestStatus | ''>('');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<Paged<ReportRow>>();
  const [error, setError] = useState('');
  async function exportReport(path: string, fileName: string): Promise<void> {
    setError('');
    try {
      await download(path, fileName);
    } catch (value) {
      setError(value instanceof ApiError ? value.message : 'The report could not be exported.');
    }
  }
  const load = useCallback((signal?: AbortSignal) => {
    setError('');
    const params = { search, siteCode: site, from, to, status, page, pageSize: 50 };
    return api<Paged<ReportRow>>(`/api/reports${queryString(params)}`, { signal }).then(setResult).catch(value => {
      if (value instanceof DOMException && value.name === 'AbortError') return;
      setError(value instanceof ApiError ? value.message : 'Report data could not be loaded.');
    });
  }, [search, site, from, to, status, page]);
  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => void load(controller.signal), 250);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [load]);
  return <>
    <PageTitle title="Visitor reports" description="Review operational history and export the visible data set." actions={<><Button variant="secondary" onClick={() => void exportReport(`/api/reports/export${queryString({ search, siteCode: site, from, to, status })}`, 'rrvms-report.csv')}>Export CSV</Button><Button variant="secondary" onClick={() => void exportReport(`/api/reports/export.xlsx${queryString({ search, siteCode: site, from, to, status })}`, 'rrvms-report.xlsx')}>Export Excel</Button></>} />
    <Card className="filter-card"><div className="filter-grid filter-grid--reports">
      <label><span>Search</span><TextInput value={search} onChange={event => { setSearch(event.target.value); setPage(1); }} placeholder="Request, visitor or company" /></label>
      <label><span>Site</span><TextInput value={site} onChange={event => { setSite(event.target.value); setPage(1); }} placeholder="All sites" /></label>
      <label><span>From</span><TextInput type="date" value={from} onChange={event => { setFrom(event.target.value); setPage(1); }} /></label>
      <label><span>To</span><TextInput type="date" min={from} value={to} onChange={event => { setTo(event.target.value); setPage(1); }} /></label>
      <label><span>Status</span><select value={status} onChange={event => { setStatus(event.target.value as RequestStatus | ''); setPage(1); }}><option value="">All statuses</option><option>Approved</option><option>PartiallyApproved</option><option>Rejected</option><option>Cancelled</option><option>VisitCompleted</option></select></label>
    </div></Card>
    {error && <Alert tone="danger" title="Report unavailable">{error}</Alert>}
    {!result && !error ? <Loading label="Loading report" /> : result && <Card>{result.items.length === 0 ? <p className="muted">No report rows match these filters.</p> : <div className="table-wrap"><table><thead><tr><th>Request</th><th>Visitor</th><th>Visit</th><th>Site</th><th>Host</th><th>Screening</th><th>Reception</th><th>Badge</th></tr></thead><tbody>{result.items.map((row, index) => <tr key={`${row.requestNumber}-${row.visitorName}-${index}`}><td data-label="Request"><strong>{row.requestNumber}</strong><small>{row.personType}</small></td><td data-label="Visitor">{row.visitorName}<small>{row.companyName}</small></td><td data-label="Visit">{formatDate(row.visitStart)} – {formatDate(row.visitEnd)}</td><td data-label="Site">{row.siteCode}</td><td data-label="Host">{row.hostName}<small>{row.hostDepartment}</small></td><td data-label="Screening"><StatusTag value={row.screeningDecision} /></td><td data-label="Reception"><StatusTag value={row.receptionStatus} /></td><td data-label="Badge">{row.badgeId || '—'}<small>{row.badgeType}</small></td></tr>)}</tbody></table></div>}<Pagination page={result.page} pageSize={result.pageSize} total={result.total} onChange={setPage} /></Card>}
  </>;
}
