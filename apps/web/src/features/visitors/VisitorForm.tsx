import { useState, type FormEvent } from 'react';
import { Alert, Button, Field, TextInput } from '../../components/Ui';
import { api, ApiError } from '../../lib/api';
import type { RequestDetail, Visitor } from '../../lib/types';
import countries from '../../lib/countries.json';

interface AssetDraft {
  key: string;
  assetType: string;
  description: string;
  serialNumber: string;
}

export function VisitorForm({ request, visitor, onSaved }: { request: RequestDetail; visitor: Visitor; onSaved: (request: RequestDetail) => void }) {
  const [assets, setAssets] = useState<AssetDraft[]>(visitor.assets.map(asset => ({ ...asset, key: asset.id })));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [phoneCountry, setPhoneCountry] = useState(visitor.phoneCountry);
  const phone = countries.find(country => country.name === phoneCountry);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSaving(true);
    setError('');
    try {
      const result = await api<RequestDetail>(`/api/requests/${request.id}/visitors/${visitor.id}`, {
        method: 'PUT',
        body: JSON.stringify({
          firstName: form.get('firstName'), middleName: form.get('middleName'), lastName: form.get('lastName'),
          citizenship: form.get('citizenship'), designation: form.get('designation'), companyName: form.get('companyName'),
          companyAddress: form.get('companyAddress'), officeCity: form.get('officeCity'), officeCountry: form.get('officeCountry'),
          phoneCountry: form.get('phoneCountry'), phoneDialCode: phone?.dial ?? '', telephone: form.get('telephone'),
          email: form.get('email'), idType: form.get('idType'), otherIdType: form.get('otherIdType'),
          assets: assets.map(({ assetType, description, serialNumber }) => ({ assetType, description, serialNumber })),
          rowVersion: request.rowVersion
        })
      });
      onSaved(result);
    } catch (value) {
      setError(value instanceof ApiError ? value.message : 'Visitor details could not be saved.');
    } finally {
      setSaving(false);
    }
  }
  return <form className="visitor-form" onSubmit={event => void submit(event)}>
    {error && <Alert tone="danger" title="Visitor not saved">{error}</Alert>}
    <div className="form-grid form-grid--three">
      <Field label="First name" required><TextInput name="firstName" defaultValue={visitor.firstName} minLength={2} maxLength={100} required /></Field>
      <Field label="Middle name"><TextInput name="middleName" defaultValue={visitor.middleName} maxLength={100} /></Field>
      <Field label="Last name" required><TextInput name="lastName" defaultValue={visitor.lastName} minLength={2} maxLength={100} required /></Field>
      <Field label="Citizenship" required><CountryInput name="citizenship" value={visitor.citizenship} id={`${visitor.id}-citizenship`} /></Field>
      <Field label="Designation" required><TextInput name="designation" defaultValue={visitor.designation} maxLength={200} required /></Field>
      <Field label="Company" required><TextInput name="companyName" defaultValue={visitor.companyName} maxLength={250} required /></Field>
      <Field label="Company address" required><TextInput name="companyAddress" defaultValue={visitor.companyAddress} maxLength={1000} required /></Field>
      <Field label="Office city" required><TextInput name="officeCity" defaultValue={visitor.officeCity} maxLength={120} required /></Field>
      <Field label="Office country" required><CountryInput name="officeCountry" value={visitor.officeCountry} id={`${visitor.id}-office`} /></Field>
      <Field label="Phone country" required><CountryInput name="phoneCountry" value={phoneCountry} id={`${visitor.id}-phone`} onChange={setPhoneCountry} /></Field>
      <Field label="Telephone" hint={phone ? `+${phone.dial} · ${phone.lengths.length === 1 ? phone.lengths[0] : `${Math.min(...phone.lengths)}–${Math.max(...phone.lengths)}`} digits` : 'Select a phone country'} required><TextInput name="telephone" defaultValue={visitor.telephone} inputMode="numeric" pattern="[0-9]+" minLength={phone ? Math.min(...phone.lengths) : undefined} maxLength={phone ? Math.max(...phone.lengths) : 15} required /></Field>
      <Field label="Email"><TextInput name="email" type="email" defaultValue={visitor.email} maxLength={320} /></Field>
      <Field label="Identity document" required><select name="idType" defaultValue={visitor.idType} required><option value="" disabled>Select document</option><option>Driver's License</option><option>Passport</option><option>Voter ID</option><option>Aadhaar Card</option><option>PAN Card</option><option>Other Government Issued ID</option></select></Field>
      <Field label="Other identity document type"><TextInput name="otherIdType" defaultValue={visitor.otherIdType} maxLength={200} /></Field>
    </div>
    <div className="subsection-heading"><div><h3>Declared assets</h3><p>Add laptops, devices or other equipment entering the site.</p></div><Button variant="secondary" type="button" onClick={() => setAssets(value => [...value, { key: crypto.randomUUID(), assetType: '', description: '', serialNumber: '' }])}>Add asset</Button></div>
    {assets.length === 0 ? <p className="muted">No assets declared.</p> : <div className="asset-list">{assets.map((asset, index) => <div className="asset-row" key={asset.key}>
      <Field label={`Asset ${index + 1} type`} required><TextInput value={asset.assetType} onChange={event => updateAsset(index, 'assetType', event.target.value)} maxLength={100} required /></Field>
      <Field label="Description"><TextInput value={asset.description} onChange={event => updateAsset(index, 'description', event.target.value)} maxLength={500} /></Field>
      <Field label="Serial number" required><TextInput value={asset.serialNumber} onChange={event => updateAsset(index, 'serialNumber', event.target.value)} maxLength={200} required /></Field>
      <Button variant="text" type="button" onClick={() => setAssets(value => value.filter((_, itemIndex) => itemIndex !== index))}>Remove</Button>
    </div>)}</div>}
    <div className="form-actions"><Button type="submit" disabled={saving}>{saving ? 'Saving…' : visitor.detailsStatus === 'RevisionRequired' ? 'Submit corrections' : 'Save visitor details'}</Button></div>
  </form>;

  function updateAsset(index: number, field: keyof Omit<AssetDraft, 'key'>, value: string): void {
    setAssets(current => current.map((asset, itemIndex) => itemIndex === index ? { ...asset, [field]: value } : asset));
  }
}

function CountryInput({ name, value, id, onChange }: { name: string; value: string; id: string; onChange?: (value: string) => void }) {
  return <><TextInput name={name} list={id} value={onChange ? value : undefined} defaultValue={onChange ? undefined : value} onChange={onChange ? event => onChange(event.target.value) : undefined} autoComplete="country-name" required /><datalist id={id}>{countries.map(country => <option key={country.name} value={country.name} />)}</datalist></>;
}
