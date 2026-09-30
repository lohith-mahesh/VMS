import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Alert, Breadcrumbs, Button, Card, Field, Loading, Modal, PageTitle, StatusTag, TextInput } from '../../components/Ui';
import { useMe } from '../../app/MeContext';
import { api, ApiError, download } from '../../lib/api';
import { formatBytes, formatDate, formatDateTime, localDateKey, toLocalInput } from '../../lib/format';
import type { RequestDetail, ScreeningDecision, Visitor, VisitorClassification } from '../../lib/types';
import { VisitorForm } from '../visitors/VisitorForm';

export function RequestDetailPage() {
  const { requestId = '' } = useParams();
  const me = useMe();
  const [request, setRequest] = useState<RequestDetail>();
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [modal, setModal] = useState<'reschedule' | 'cancel' | 'review' | 'correction' | null>(null);
  const load = useCallback(() => api<RequestDetail>(`/api/requests/${requestId}`).then(setRequest).catch(value => setError(value instanceof ApiError ? value.message : 'The request could not be loaded.')), [requestId]);
  useEffect(() => { void load(); }, [load]);
  const editable = request && ['Draft', 'VisitorDetailsPending', 'ReadyForScreening', 'PendingCorrection'].includes(request.status);
  const reviewable = request && ['PendingScreening', 'CorrectionSubmitted', 'PendingCorrection'].includes(request.status);
  const canReschedule = request && !['Cancelled', 'VisitCompleted'].includes(request.status) && !request.visitors.some(visitor => visitor.receptionRecords.some(record => record.status === 'CheckedIn' || record.status === 'Completed'));
  if (error && !request) return <Alert tone="danger" title="Request unavailable">{error}</Alert>;
  if (!request) return <Loading label="Loading request" />;

  async function mutate(path: string, body: unknown, success: string): Promise<void> {
    setError('');
    try {
      const result = await api<RequestDetail>(`/api/requests/${requestId}${path}`, { method: 'POST', body: JSON.stringify(body) });
      setRequest(result);
      setNotice(success);
      setModal(null);
      setSelected([]);
    } catch (value) {
      setError(value instanceof ApiError ? value.message : 'The action could not be completed.');
    }
  }

  return <>
    <Breadcrumbs items={[{ label: 'Requests', to: '/requests' }, { label: request.requestNumber }]} />
    <PageTitle eyebrow={request.visitorType} title={request.requestNumber} description={`${formatDateTime(request.visitStart)} – ${formatDateTime(request.visitEnd)}`} actions={<><StatusTag value={request.status} />{me.role === 'HostRequester' && canReschedule && <Button variant="secondary" onClick={() => setModal('reschedule')}>Reschedule</Button>}{me.role === 'HostRequester' && canReschedule && <Button variant="danger" onClick={() => setModal('cancel')}>Cancel request</Button>}</>} />
    {notice && <Alert tone="success" title="Action completed">{notice}</Alert>}
    {error && <Alert tone="danger" title="Action failed">{error}</Alert>}
    {request.status === 'PendingCorrection' && <Alert tone="warning" title="Corrections requested">Open each marked visitor, update the requested information and resubmit it.</Alert>}
    <Card><div className="section-heading"><div><p className="eyebrow">Request summary</p><h2>Visit and host</h2></div></div><dl className="info-grid">
      <Info label="Site" value={request.siteCode} /><Info label="Purpose" value={`${request.purposeType}: ${request.purpose}`} /><Info label="Areas" value={request.areasToVisit || 'Not specified'} /><Info label="Main host" value={`${request.mainHostName} · ${request.hostDepartment || 'Department not specified'}`} /><Info label="Escorting host" value={request.escortingHostName || 'Not specified'} /><Info label="Visitor class" value={request.contractorType} />
    </dl></Card>

    {me.role === 'HostRequester' && request.status === 'PendingCorrection' && <RequestCorrectionForm request={request} onSaved={value => { setRequest(value); setNotice('Request-level corrections saved.'); }} />}

    {me.role === 'ExportControl' && reviewable && <Card className="action-bar"><div><h2>Export Control review</h2><p>Select one or more visitors for a batch decision, or request corrections for one visitor.</p></div><div className="button-set"><Button variant="secondary" disabled={selected.length !== 1} onClick={() => setModal('correction')}>Request correction</Button><Button disabled={selected.length === 0} onClick={() => setModal('review')}>Record decision</Button></div></Card>}

    <div className="visitor-stack">{request.visitors.map(visitor => <details className="visitor-panel" key={visitor.id} open={request.visitors.length === 1 || visitor.detailsStatus === 'RevisionRequired'}>
      <summary><span className="visitor-number">{visitor.sequence}</span><span><strong>{visitor.fullName || `Visitor ${visitor.sequence}`}</strong><small>{visitor.companyName || 'Details not submitted'}</small></span><span className="summary-tags"><StatusTag value={visitor.detailsStatus} /><StatusTag value={visitor.screeningDecision} /></span></summary>
      <div className="visitor-panel__body">
        {me.role === 'ExportControl' && reviewable && visitor.screeningDecision === 'Pending' && visitor.detailsStatus !== 'RevisionRequired' && <label className="selection"><input type="checkbox" checked={selected.includes(visitor.id)} onChange={event => setSelected(value => event.target.checked ? [...value, visitor.id] : value.filter(id => id !== visitor.id))} /> Select for review</label>}
        {me.role === 'HostRequester' && editable ? <VisitorForm request={request} visitor={visitor} onSaved={value => { setRequest(value); setNotice(`Visitor ${visitor.sequence} saved.`); }} /> : <VisitorReadOnly request={request} visitor={visitor} canDownload={me.role === 'ExportControl'} onError={setError} />}
        {me.role === 'HostRequester' && request.visitorType === 'External' && editable && (!visitor.dpsDocument || visitor.detailsStatus === 'RevisionRequired') && <DpsUpload request={request} visitor={visitor} onSaved={setRequest} />}
        {me.role === 'HostRequester' && visitor.dpsDocument && visitor.detailsStatus !== 'RevisionRequired' && <div className="document-row"><div><strong>{visitor.dpsDocument.fileName}</strong><small>{formatBytes(visitor.dpsDocument.size)} · DPS version {visitor.dpsDocument.version} uploaded</small></div><StatusTag value="Stored securely" /></div>}
        {me.role === 'Reception' && <ReceptionPanel request={request} visitor={visitor} onSaved={setRequest} onError={setError} />}
      </div>
    </details>)}</div>

    {me.role === 'HostRequester' && request.visitorType === 'External' && request.status === 'ReadyForScreening' && <div className="sticky-action"><div><strong>Visitor information is complete</strong><span>Submit the request to Export Control for screening.</span></div><Button onClick={() => void mutate('/submit', { rowVersion: request.rowVersion }, 'Request submitted for screening.')}>Submit for screening</Button></div>}

    <Card><details><summary className="plain-summary"><h2>Audit history</h2><span>{request.auditEvents.length} events</span></summary><ol className="timeline">{request.auditEvents.map(event => <li key={event.id}><span className="timeline__dot" /><div><strong>{event.action}</strong><p>{event.details}</p><small>{formatDateTime(event.occurredAt)} · {event.actorRole}</small></div></li>)}</ol></details></Card>

    {modal === 'reschedule' && <RescheduleModal request={request} onClose={() => setModal(null)} onSubmit={(start, end, reason) => mutate('/reschedule', { visitStart: new Date(start).toISOString(), visitEnd: new Date(end).toISOString(), reason, rowVersion: request.rowVersion }, 'Request rescheduled. External visitors require re-screening.')} />}
    {modal === 'cancel' && <ReasonModal title="Cancel request" confirm="Cancel request" onClose={() => setModal(null)} onSubmit={reason => mutate('/cancel', { reason, rowVersion: request.rowVersion }, 'Request cancelled.')} />}
    {modal === 'review' && <ReviewModal count={selected.length} onClose={() => setModal(null)} onSubmit={(decision, classification, comments) => mutate('/review', { visitorIds: selected, decision, classification, comments, rowVersion: request.rowVersion }, 'Screening decision recorded.')} />}
    {modal === 'correction' && selected[0] && <CorrectionModal visitor={request.visitors.find(visitor => visitor.id === selected[0])!} onClose={() => setModal(null)} onSubmit={(fields, instructions) => mutate('/corrections', { visitorId: selected[0], fields, instructions, rowVersion: request.rowVersion }, 'Correction request sent to the host.')} />}
  </>;
}

function Info({ label, value }: { label: string; value: string }) {
  return <div><dt>{label}</dt><dd>{value}</dd></div>;
}

function VisitorReadOnly({ request, visitor, canDownload, onError }: { request: RequestDetail; visitor: Visitor; canDownload: boolean; onError: (message: string) => void }) {
  return <><dl className="info-grid info-grid--visitor"><Info label="Name" value={visitor.fullName || 'Not submitted'} /><Info label="Citizenship" value={visitor.citizenship || '—'} /><Info label="Designation" value={visitor.designation || '—'} /><Info label="Company" value={visitor.companyName || '—'} /><Info label="Telephone" value={`${visitor.phoneDialCode} ${visitor.telephone}`.trim() || '—'} /><Info label="Identity document" value={visitor.otherIdType || visitor.idType || '—'} /><Info label="Classification" value={visitor.classification} /><Info label="Badge type" value={visitor.badgeType} /></dl>
    {visitor.assets.length > 0 && <div className="compact-table"><h3>Declared assets</h3><table><thead><tr><th>Type</th><th>Description</th><th>Serial</th><th>Status</th></tr></thead><tbody>{visitor.assets.map(asset => <tr key={asset.id}><td>{asset.assetType}</td><td>{asset.description}</td><td>{asset.serialNumber}</td><td><StatusTag value={asset.verificationStatus} /></td></tr>)}</tbody></table></div>}
    {visitor.dpsDocument && <div className="document-row"><div><strong>{visitor.dpsDocument.fileName}</strong><small>{formatBytes(visitor.dpsDocument.size)} · Version {visitor.dpsDocument.version} · {formatDateTime(visitor.dpsDocument.uploadedAt)}</small></div>{canDownload && <Button variant="secondary" onClick={() => void download(`/api/requests/${request.id}/visitors/${visitor.id}/dps/${visitor.dpsDocument!.id}`, visitor.dpsDocument!.fileName).catch(value => onError(value instanceof ApiError ? value.message : 'The DPS document could not be downloaded.'))}>Download DPS</Button>}</div>}</>;
}

function DpsUpload({ request, visitor, onSaved }: { request: RequestDetail; visitor: Visitor; onSaved: (request: RequestDetail) => void }) {
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const input = event.currentTarget.elements.namedItem('file') as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const data = new FormData();
    data.set('file', file);
    data.set('rowVersion', request.rowVersion);
    setBusy(true);
    setError('');
    try {
      onSaved(await api<RequestDetail>(`/api/requests/${request.id}/visitors/${visitor.id}/dps`, { method: 'POST', body: data }));
      event.currentTarget.reset();
    } catch (value) {
      setError(value instanceof ApiError ? value.message : 'The DPS file could not be uploaded.');
    } finally {
      setBusy(false);
    }
  }
  const replace = Boolean(visitor.dpsDocument);
  return <form className="upload-panel" onSubmit={event => void submit(event)}><div><h3>{replace ? 'Replace DPS PDF' : 'Upload DPS PDF'}</h3><p>{replace ? 'Replacement is available only while corrections are requested.' : 'PDF only, maximum 10 MB. Reception cannot access this document.'}</p>{visitor.dpsDocument && <small>Current: {visitor.dpsDocument.fileName} · {formatBytes(visitor.dpsDocument.size)}</small>}</div><input name="file" type="file" accept="application/pdf,.pdf" required /><Button type="submit" disabled={busy}>{busy ? 'Uploading…' : replace ? 'Replace PDF' : 'Upload PDF'}</Button>{error && <span className="field__error">{error}</span>}</form>;
}

function ReceptionPanel({ request, visitor, onSaved, onError }: { request: RequestDetail; visitor: Visitor; onSaved: (request: RequestDetail) => void; onError: (error: string) => void }) {
  const [pending, setPending] = useState<{ type: 'verify' | 'reject' | 'check-in'; visitDayId: string }>();
  const [value, setValue] = useState('');
  const today = localDateKey();
  async function action(path: string, payload: Record<string, unknown>) {
    try {
      onSaved(await api<RequestDetail>(`/api/requests/${request.id}/reception/${path}`, { method: 'POST', body: JSON.stringify({ ...payload, visitorId: visitor.id, rowVersion: request.rowVersion }) }));
      setPending(undefined);
      setValue('');
    } catch (value) {
      onError(value instanceof ApiError ? value.message : 'Reception action failed.');
    }
  }
  return <div className="reception-list"><h3>Daily reception processing</h3>{request.visitDays.map(day => {
    const record = visitor.receptionRecords.find(item => item.visitDayId === day.id);
    if (!record) return null;
    return <div className="reception-day" key={day.id}><div><strong>{formatDate(day.date)}</strong><small>{day.expectedArrival.slice(0, 5)} – {day.expectedDeparture.slice(0, 5)}</small></div><StatusTag value={record.status} />{record.arrivalStatus !== 'NotArrived' && <StatusTag value={record.arrivalStatus} />}
      <div className="button-set">
        {record.status === 'Upcoming' && <>{day.date === today && <><Button variant="secondary" onClick={() => setPending({ type: 'verify', visitDayId: day.id })}>Verify</Button><Button variant="danger" onClick={() => setPending({ type: 'reject', visitDayId: day.id })}>Reject entry</Button></>}{day.date <= today && <Button variant="text" onClick={() => void action('no-show', { visitDayId: day.id })}>No-show</Button>}</>}
        {record.status === 'VerificationComplete' && day.date === today && <Button onClick={() => setPending({ type: 'check-in', visitDayId: day.id })}>Check in</Button>}
        {record.status === 'CheckedIn' && <Button onClick={() => void action('check-out', { visitDayId: day.id })}>Check out</Button>}
      </div>
    </div>;
  })}{pending && <Modal title={pending.type === 'verify' ? 'Verify entry' : pending.type === 'reject' ? 'Reject entry' : 'Check in visitor'} onClose={() => { setPending(undefined); setValue(''); }} actions={<><Button variant="text" onClick={() => { setPending(undefined); setValue(''); }}>Back</Button><Button variant={pending.type === 'reject' ? 'danger' : 'primary'} disabled={pending.type !== 'verify' && !value.trim()} onClick={() => void (pending.type === 'check-in' ? action('check-in', { visitDayId: pending.visitDayId, badgeId: value }) : action('verify', { visitDayId: pending.visitDayId, approved: pending.type === 'verify', identityConfirmed: pending.type === 'verify', assetsConfirmed: pending.type === 'verify', remarks: value }))}>{pending.type === 'verify' ? 'Confirm verification' : pending.type === 'reject' ? 'Reject entry' : 'Check in'}</Button></>}><Field label={pending.type === 'check-in' ? 'Badge ID' : pending.type === 'reject' ? 'Rejection reason' : 'Verification remarks'} required={pending.type !== 'verify'}>{pending.type === 'check-in' ? <TextInput value={value} onChange={event => setValue(event.target.value)} maxLength={100} /> : <textarea rows={4} value={value} onChange={event => setValue(event.target.value)} maxLength={2000} />}</Field>{pending.type === 'verify' && <Alert title="Verification confirmation">Identity and all declared assets will be marked as confirmed.</Alert>}</Modal>}</div>;
}

function ReasonModal({ title, confirm, onClose, onSubmit }: { title: string; confirm: string; onClose: () => void; onSubmit: (reason: string) => void }) {
  const [reason, setReason] = useState('');
  return <Modal title={title} onClose={onClose} actions={<><Button variant="text" onClick={onClose}>Back</Button><Button variant="danger" disabled={!reason.trim()} onClick={() => onSubmit(reason)}>{confirm}</Button></>}><Field label="Reason" required><textarea rows={4} value={reason} onChange={event => setReason(event.target.value)} maxLength={2000} /></Field></Modal>;
}

function RescheduleModal({ request, onClose, onSubmit }: { request: RequestDetail; onClose: () => void; onSubmit: (start: string, end: string, reason: string) => void }) {
  const [start, setStart] = useState(toLocalInput(request.visitStart));
  const [end, setEnd] = useState(toLocalInput(request.visitEnd));
  const [reason, setReason] = useState('');
  return <Modal title="Reschedule visit" onClose={onClose} actions={<><Button variant="text" onClick={onClose}>Back</Button><Button disabled={!start || !end || !reason.trim()} onClick={() => onSubmit(start, end, reason)}>Reschedule</Button></>}><div className="form-grid form-grid--two"><Field label="New start" required><TextInput type="datetime-local" value={start} onChange={event => setStart(event.target.value)} /></Field><Field label="New end" required><TextInput type="datetime-local" value={end} onChange={event => setEnd(event.target.value)} /></Field></div><Field label="Reason" required><textarea rows={3} value={reason} onChange={event => setReason(event.target.value)} /></Field><Alert tone="warning" title="Screening will reset">External visitors must be screened again after a schedule change.</Alert></Modal>;
}

function ReviewModal({ count, onClose, onSubmit }: { count: number; onClose: () => void; onSubmit: (decision: ScreeningDecision, classification: VisitorClassification, comments: string) => void }) {
  const [decision, setDecision] = useState<ScreeningDecision>('Approved');
  const [classification, setClassification] = useState<VisitorClassification>('Visitor');
  const [comments, setComments] = useState('');
  return <Modal title={`Review ${count} visitor${count === 1 ? '' : 's'}`} onClose={onClose} actions={<><Button variant="text" onClick={onClose}>Back</Button><Button variant={decision === 'Rejected' ? 'danger' : 'primary'} disabled={decision === 'Rejected' && !comments.trim()} onClick={() => onSubmit(decision, classification, comments)}>Record {decision.toLowerCase()}</Button></>}><div className="form-grid form-grid--two"><Field label="Decision"><select value={decision} onChange={event => setDecision(event.target.value as ScreeningDecision)}><option value="Approved">Approve</option><option value="Rejected">Reject</option></select></Field><Field label="Classification"><select value={classification} onChange={event => setClassification(event.target.value as VisitorClassification)}><option value="Visitor">Visitor</option><option value="Vendor">Vendor</option></select></Field></div><Field label={decision === 'Rejected' ? 'Reason' : 'Comments'} required={decision === 'Rejected'}><textarea rows={4} value={comments} onChange={event => setComments(event.target.value)} maxLength={2000} /></Field></Modal>;
}

function CorrectionModal({ visitor, onClose, onSubmit }: { visitor: Visitor; onClose: () => void; onSubmit: (fields: string[], instructions: string) => void }) {
  const options = useMemo(() => ['siteCode', 'purposeType', 'purpose', 'areasToVisit', 'mainHost', 'escortingHost', 'firstName', 'middleName', 'lastName', 'citizenship', 'designation', 'companyName', 'companyAddress', 'officeCity', 'officeCountry', 'phoneCountry', 'phoneDialCode', 'telephone', 'email', 'idType', 'otherIdType', 'assets', 'dpsDocument'], []);
  const [fields, setFields] = useState<string[]>([]);
  const [instructions, setInstructions] = useState('');
  return <Modal title={`Request corrections for ${visitor.fullName}`} onClose={onClose} actions={<><Button variant="text" onClick={onClose}>Back</Button><Button disabled={fields.length === 0 || !instructions.trim()} onClick={() => onSubmit(fields, instructions)}>Send request</Button></>}><fieldset className="checkbox-grid"><legend>Fields requiring correction</legend>{options.map(field => <label key={field}><input type="checkbox" checked={fields.includes(field)} onChange={event => setFields(value => event.target.checked ? [...value, field] : value.filter(item => item !== field))} /> {field.replace(/([A-Z])/g, ' $1')}</label>)}</fieldset><Field label="Instructions" required><textarea rows={4} value={instructions} onChange={event => setInstructions(event.target.value)} maxLength={2000} /></Field></Modal>;
}

function RequestCorrectionForm({ request, onSaved }: { request: RequestDetail; onSaved: (request: RequestDetail) => void }) {
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSaving(true);
    setError('');
    try {
      onSaved(await api<RequestDetail>(`/api/requests/${request.id}`, {
        method: 'PUT',
        body: JSON.stringify({
          siteCode: form.get('siteCode'), purposeType: form.get('purposeType'), purpose: form.get('purpose'), areasToVisit: form.get('areasToVisit'),
          mainHostObjectId: form.get('mainHostObjectId'), mainHostName: form.get('mainHostName'), hostDepartment: form.get('hostDepartment'),
          escortingHostObjectId: form.get('escortingHostObjectId'), escortingHostName: form.get('escortingHostName'), rowVersion: request.rowVersion
        })
      }));
    } catch (value) {
      setError(value instanceof ApiError ? value.message : 'Request details could not be updated.');
    } finally {
      setSaving(false);
    }
  }
  return <Card><div className="section-heading"><div><p className="eyebrow">Correction workspace</p><h2>Request-level details</h2><p className="muted">Save only when Export Control requested a site, purpose or host change. This resets every screening decision.</p></div></div>{error && <Alert tone="danger" title="Details not saved">{error}</Alert>}<form onSubmit={event => void submit(event)}><div className="form-grid">
    <Field label="Site" required><select name="siteCode" defaultValue={request.siteCode}><option>Bengaluru</option><option>Delhi</option></select></Field>
    <Field label="Purpose type" required><select name="purposeType" defaultValue={request.purposeType}><option value="Technical">Technical</option><option value="NonTechnical">Non-technical</option><option value="Other">Other</option></select></Field>
    <Field label="Purpose" required><textarea name="purpose" defaultValue={request.purpose} rows={3} maxLength={2000} required /></Field>
    <Field label="Areas to visit"><TextInput name="areasToVisit" defaultValue={request.areasToVisit} maxLength={1000} /></Field>
    <Field label="Main host" required><TextInput name="mainHostName" defaultValue={request.mainHostName} maxLength={200} required /></Field>
    <Field label="Main host object ID" required><TextInput name="mainHostObjectId" defaultValue={request.mainHostObjectId} maxLength={64} required /></Field>
    <Field label="Host department"><TextInput name="hostDepartment" defaultValue={request.hostDepartment} maxLength={200} /></Field>
    <Field label="Escorting host"><TextInput name="escortingHostName" defaultValue={request.escortingHostName} maxLength={200} /></Field>
    <Field label="Escorting host object ID"><TextInput name="escortingHostObjectId" defaultValue={request.escortingHostObjectId} maxLength={64} /></Field>
  </div><div className="form-actions"><Button type="submit" disabled={saving}>{saving ? 'Saving…' : 'Save request corrections'}</Button></div></form></Card>;
}
