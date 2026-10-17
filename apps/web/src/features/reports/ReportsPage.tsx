import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useMe } from '../../app/MeContext';
import { Alert, Button, Card, Loading, PageTitle, Pagination, StatusTag, TextInput } from '../../components/Ui';
import { api, ApiError, download, queryString } from '../../lib/api';
import { formatDate } from '../../lib/format';
import type { Paged, ReceptionStatus, ReportRow, RequestStatus, ScreeningDecision, VerificationStatus } from '../../lib/types';

interface ReportResult extends Paged<ReportRow> {
  matchingVisitors: number;
  matchingRequests: number;
  checkedIn: number;
  assetsDeclared: number;
}

const requestStatuses: RequestStatus[] = ['Draft', 'VisitorFormPending', 'VisitorFormSubmitted', 'HostReview', 'PendingEcReview', 'PendingDocumentation', 'DocumentationSubmitted', 'EcReReviewRequired', 'Approved', 'PartiallyApproved', 'Rejected', 'Cancelled', 'VisitProcessCompleted'];
const screeningStatuses: ScreeningDecision[] = ['Pending', 'Approved', 'Rejected', 'NotApplicable'];
const receptionStatuses: ReceptionStatus[] = ['Upcoming', 'VerificationInProgress', 'ReceptionVerification', 'CheckedIn', 'Completed', 'NoShow', 'EntryRejected', 'Cancelled'];
const verificationStatuses: VerificationStatus[] = ['Pending', 'Approved', 'Rejected', 'NotApplicable'];

export function ReportsPage({ history = false }: { history?: boolean }) {
  const me = useMe();
  const [search, setSearch] = useState('');
  const [visitDate, setVisitDate] = useState('');
  const [status, setStatus] = useState<RequestStatus | ''>('');
  const [screening, setScreening] = useState<ScreeningDecision | ''>('');
  const [reception, setReception] = useState<ReceptionStatus | ''>('');
  const [identity, setIdentity] = useState<VerificationStatus | ''>('');
  const [assets, setAssets] = useState('');
  const [site, setSite] = useState('');
  const [personType, setPersonType] = useState('');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<ReportResult>();
  const [error, setError] = useState('');
  const params = { search, from: visitDate, to: visitDate, status, screening, reception, identity, assets, siteCode: site, personType, history };
  async function exportReport(path: string, fileName: string): Promise<void> {
    setError('');
    try { await download(path, fileName); }
    catch (value) { setError(value instanceof ApiError ? value.message : 'The report could not be exported.'); }
  }
  const load = useCallback((signal?: AbortSignal) => {
    setError('');
    return api<ReportResult>(`/api/reports${queryString({ search, from: visitDate, to: visitDate, status, screening, reception, identity, assets, siteCode: site, personType, history, page, pageSize: 50 })}`, { signal })
      .then(setResult).catch(value => {
        if (value instanceof DOMException && value.name === 'AbortError') return;
        setError(value instanceof ApiError ? value.message : 'Report data could not be loaded.');
      });
  }, [search, visitDate, status, screening, reception, identity, assets, site, personType, history, page]);
  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => void load(controller.signal), 250);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [load]);
  function clear() {
    setSearch(''); setVisitDate(''); setStatus(''); setScreening(''); setReception('');
    setIdentity(''); setAssets(''); setSite(''); setPersonType(''); setPage(1);
  }
  const rows = result?.items ?? [];
  return <>
    <PageTitle title={history ? 'Visitor History' : me.role === 'Reception' ? 'Security Reports' : 'Export Control Reports'} description={history ? 'Your completed, approved, rejected, and cancelled visitors.' : 'Visitor-level activity with day-specific filtering.'} actions={<>
      <Button variant="secondary" onClick={() => void exportReport(`/api/reports/export${queryString(params)}`, 'visitor-analytics.csv')}>Export CSV</Button>
      <Button variant="primary" onClick={() => void exportReport(`/api/reports/export.xlsx${queryString(params)}`, 'visitor-analytics.xlsx')}>Export XLSX</Button>
    </>} />
    <Card className="filter-card">
      <div className="filter-grid filter-grid--reports">
        <label><span>Search</span><TextInput value={search} onChange={event => { setSearch(event.target.value); setPage(1); }} placeholder="Visitor, request, host, badge or asset" /></label>
        <label><span>Visit Date</span><TextInput type="date" value={visitDate} onChange={event => { setVisitDate(event.target.value); setPage(1); }} /></label>
        <label><span>Request Status</span><select value={status} onChange={event => { setStatus(event.target.value as RequestStatus | ''); setPage(1); }}><option value="">All</option>{requestStatuses.map(value => <option key={value} value={value}>{label(value)}</option>)}</select></label>
        <label><span>Screening Status</span><select value={screening} onChange={event => { setScreening(event.target.value as ScreeningDecision | ''); setPage(1); }}><option value="">All</option>{screeningStatuses.map(value => <option key={value} value={value}>{label(value)}</option>)}</select></label>
        <label><span>Reception Status</span><select value={reception} onChange={event => { setReception(event.target.value as ReceptionStatus | ''); setPage(1); }}><option value="">All</option>{receptionStatuses.map(value => <option key={value} value={value}>{label(value)}</option>)}</select></label>
        <label><span>Identity Status</span><select value={identity} onChange={event => { setIdentity(event.target.value as VerificationStatus | ''); setPage(1); }}><option value="">All</option>{verificationStatuses.map(value => <option key={value} value={value}>{label(value)}</option>)}</select></label>
        <label><span>Assets</span><select value={assets} onChange={event => { setAssets(event.target.value); setPage(1); }}><option value="">All</option><option value="DECLARED">Declared</option><option value="NONE">None Declared</option><option value="VERIFIED">Verified</option><option value="REJECTED">Rejected</option></select></label>
        <label><span>Site</span><TextInput value={site} onChange={event => { setSite(event.target.value); setPage(1); }} placeholder="All" /></label>
        <label><span>Person Type</span><select value={personType} onChange={event => { setPersonType(event.target.value); setPage(1); }}><option value="">All</option>{['Internal Visitor', 'External Visitor', 'Vendor', 'GTRE'].map(value => <option key={value}>{value}</option>)}</select></label>
      </div>
      <div className="filter-actions"><Button variant="secondary" onClick={clear}>Clear Filters</Button></div>
    </Card>
    {error && <Alert tone="danger" title="Report unavailable">{error}</Alert>}
    {!result && !error ? <Loading label="Loading report" /> : result && <>
      <div className="metric-grid">{[['Matching Visitors', result.matchingVisitors], ['Matching Requests', result.matchingRequests], ['Checked In', result.checkedIn], ['Assets Declared', result.assetsDeclared]].map(([name, value], index) => <Card className={`metric metric--${index + 1}`} key={name}><span>{name}</span><strong>{value}</strong></Card>)}</div>
      <Card><h2>Visitor Index</h2>{rows.length === 0 ? <p className="muted">No matching records. Adjust or clear the filters.</p> : <div className="table-wrap"><table><thead><tr><th>Visitor</th><th>Visit Range</th><th>Site</th><th>Request Number</th><th>Host</th><th>Screening</th><th>Reception</th><th>Assets</th><th>Badge</th></tr></thead><tbody>{rows.map(row => <tr key={`${row.requestId}-${row.visitorId}`}>
        <td data-label="Visitor"><strong>{row.visitorName || 'Visitor Details Pending'}</strong><small>{row.companyName} · {row.personType}</small></td>
        <td data-label="Visit Range">{formatDate(row.visitStart)} – {formatDate(row.visitEnd)}</td>
        <td data-label="Site">{row.siteCode}</td>
        <td data-label="Request"><Link to={`/requests/${row.requestId}`}>{row.requestNumber}</Link><small><StatusTag value={row.requestStatus} /></small></td>
        <td data-label="Host">{row.hostName}<small>{row.hostDepartment}</small></td>
        <td data-label="Screening"><StatusTag value={row.screeningDecision} /></td>
        <td data-label="Reception"><StatusTag value={row.receptionStatus} /></td>
        <td data-label="Assets">{row.assetCount ? row.assetSummary : 'None'}</td>
        <td data-label="Badge">{row.badgeId || '—'}<small>{row.badgeType}</small></td>
      </tr>)}</tbody></table></div>}
        <Pagination page={result.page} pageSize={result.pageSize} total={result.total} onChange={setPage} />
      </Card>
    </>}
  </>;
}

function label(value: string): string { return value.replace(/([a-z])([A-Z])/g, '$1 $2'); }
