import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Alert, Breadcrumbs, Button, Card, Field, PageTitle, TextInput } from '../../components/Ui';
import { useMe } from '../../app/MeContext';
import { api, ApiError } from '../../lib/api';
import type { ContractorType, RequestDetail, VisitPurposeType, VisitorType } from '../../lib/types';
import { EmployeePicker } from '../../components/EmployeePicker';
import { toLocalInput } from '../../lib/format';

export function CreateRequestPage() {
  const me = useMe();
  const navigate = useNavigate();
  const [visitorType, setVisitorType] = useState<VisitorType>('External');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSubmitting(true);
    setError('');
    setErrors({});
    try {
      const result = await api<RequestDetail>('/api/requests', {
        method: 'POST',
        body: JSON.stringify({
          visitorType,
          contractorType: visitorType === 'Internal' ? 'NormalVisitor' : form.get('contractorType') as ContractorType,
          siteCode: form.get('siteCode'),
          purposeType: form.get('purposeType') as VisitPurposeType,
          purpose: form.get('purpose'),
          areasToVisit: form.get('areasToVisit'),
          mainHostObjectId: form.get('mainHostObjectId'),
          mainHostName: form.get('mainHostName'),
          hostDepartment: form.get('hostDepartment'),
          escortingHostObjectId: form.get('escortingHostObjectId'),
          escortingHostName: form.get('escortingHostName'),
          numberOfVisitors: Number(form.get('numberOfVisitors')),
          visitStart: new Date(String(form.get('visitStart'))).toISOString(),
          visitEnd: new Date(String(form.get('visitEnd'))).toISOString()
        })
      });
      navigate(`/requests/${result.id}`);
    } catch (value) {
      if (value instanceof ApiError) {
        setError(value.message);
        setErrors(value.errors);
      } else {
        setError('The request could not be created.');
      }
    } finally {
      setSubmitting(false);
    }
  }
  const minimum = toLocalInput(new Date(Date.now() + 5 * 60_000).toISOString());
  return <>
    <Breadcrumbs items={[{ label: 'Requests', to: '/requests' }, { label: 'New request' }]} />
    <PageTitle title="Create visitor request" description="Enter the visit, host and schedule information. Visitor details are completed after this request is saved." />
    <Alert title="Privacy and handling">Enter only information required to arrange and process the visit. DPS documents are restricted to Export Control.</Alert>
    {error && <Alert tone="danger" title="Request not saved">{error}</Alert>}
    <form onSubmit={event => void submit(event)}>
      <Card><div className="section-heading"><div><p className="eyebrow">Step 1 of 2</p><h2>Visit details</h2></div></div>
        <div className="form-grid">
          <Field label="Visitor type" required><select value={visitorType} onChange={event => setVisitorType(event.target.value as VisitorType)}><option value="External">External</option><option value="Internal">Internal</option></select></Field>
          <Field label="Site / facility" error={errors.siteCode?.[0]} required><select name="siteCode" defaultValue="" required><option value="" disabled>Select a site</option><option>Bengaluru</option><option>Delhi</option></select></Field>
          <Field label="Number of visitors" error={errors.numberOfVisitors?.[0]} required><TextInput name="numberOfVisitors" type="number" min="1" max="20" defaultValue="1" required /></Field>
          <Field label="Purpose of visit" required><select name="purposeType" defaultValue="Technical"><option value="Technical">Technical</option><option value="NonTechnical">Non-technical</option><option value="Other">Other</option></select></Field>
          {visitorType === 'External' && <Field label="External visitor classification" required><select name="contractorType" defaultValue="NormalVisitor"><option value="NormalVisitor">Normal visitor</option><option value="FacilitiesContractor">Facilities contractor</option><option value="GtreContractor">Gas Turbine Research Establishment contractor</option></select></Field>}
          <Field label="Areas to visit"><TextInput name="areasToVisit" maxLength={1000} /></Field>
          <Field label="Brief description of visit" error={errors.purpose?.[0]} required><textarea name="purpose" rows={4} maxLength={2000} required /></Field>
        </div>
      </Card>
      <Card><div className="section-heading"><div><p className="eyebrow">Host</p><h2>Host details</h2></div></div>
        <div className="form-grid">
          <EmployeePicker label="Main host" namePrefix="mainHost" defaultName={me.displayName} defaultObjectId={me.objectId} required />
          <EmployeePicker label="Escorting host" namePrefix="escortingHost" />
        </div>
      </Card>
      <Card><div className="section-heading"><div><p className="eyebrow">Schedule</p><h2>Visit schedule</h2></div><span className="tag tag--neutral">Configured business timezone</span></div>
        <div className="form-grid form-grid--two">
          <Field label="Start" required><TextInput name="visitStart" type="datetime-local" min={minimum} required /></Field>
          <Field label="End" required><TextInput name="visitEnd" type="datetime-local" min={minimum} required /></Field>
        </div>
      </Card>
      <div className="form-actions"><Button variant="secondary" type="button" onClick={() => navigate('/requests')}>Cancel</Button><Button type="submit" disabled={submitting}>{submitting ? 'Creating…' : 'Create request'}</Button></div>
    </form>
  </>;
}
