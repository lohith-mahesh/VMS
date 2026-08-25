import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { countries } from '../data/countries'
import { getVisitorForm, submitVisitorForm, type SubmitVisitorForm, type VisitorForm } from '../services/apiClient'
import { userFacingApiError } from '../utils/logger'

const blankSubmission: SubmitVisitorForm = {
  fullName: '',
  citizenship: '',
  nationality: '',
  country: '',
  designation: '',
  companyName: '',
  officeCity: '',
  officeCountry: '',
  telephone: '',
  email: '',
  idType: 'Passport',
  idLast4: '',
  passportNumber: '',
  visaNumber: '',
  governmentIdNumber: '',
  assets: [],
}

export function VisitorFormPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const [formInfo, setFormInfo] = useState<VisitorForm | null>(null)
  const [form, setForm] = useState<SubmitVisitorForm>(blankSubmission)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let mounted = true
    setLoading(true)
    setError('')
    getVisitorForm(id)
      .then((result) => {
        if (!mounted) return
        setFormInfo(result)
        setForm({
          fullName: result.fullName,
          citizenship: result.citizenship,
          nationality: result.nationality,
          country: result.country,
          designation: result.designation,
          companyName: result.companyName,
          officeCity: result.officeCity,
          officeCountry: result.officeCountry,
          telephone: result.telephone,
          email: result.email,
          idType: result.idType || 'Passport',
          idLast4: result.idLast4,
          passportNumber: '',
          visaNumber: '',
          governmentIdNumber: '',
          assets: result.assets,
        })
      })
      .catch((reason) => mounted && setError(userFacingApiError(reason, 'Visitor form was not found.')))
      .finally(() => mounted && setLoading(false))
    return () => { mounted = false }
  }, [id])

  const update = <K extends keyof SubmitVisitorForm>(field: K, value: SubmitVisitorForm[K]) => setForm(current => ({ ...current, [field]: value }))
  const updateAsset = (index: number, field: keyof SubmitVisitorForm['assets'][number], value: string) => setForm(current => ({ ...current, assets: current.assets.map((asset, assetIndex) => assetIndex === index ? { ...asset, [field]: value } : asset) }))
  const addAsset = () => setForm(current => ({ ...current, assets: [...current.assets, { assetType: '', description: '', serialNumber: '' }] }))
  const removeAsset = (index: number) => setForm(current => ({ ...current, assets: current.assets.filter((_, assetIndex) => assetIndex !== index) }))

  const validate = () => {
    const errors: Record<string, string> = {}
    if (!form.fullName.trim()) errors.fullName = 'Full legal name is required.'
    if (!form.country) errors.country = 'Country is required.'
    if (!form.citizenship) errors.citizenship = 'Citizenship is required.'
    if (!form.nationality) errors.nationality = 'Nationality is required.'
    if (!form.officeCountry) errors.officeCountry = 'Office country is required.'
    if (!/^\d{4}$/.test(form.idLast4)) errors.idLast4 = 'ID last 4 must contain exactly four digits.'
    setFieldErrors(errors)
    return Object.keys(errors).length === 0
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError('')
    if (!validate()) return
    setSaving(true)
    try {
      const updatedRequest = await submitVisitorForm(id, form)
      navigate(`/visitor-requests/${updatedRequest.id}`)
    } catch (reason) {
      setError(userFacingApiError(reason, 'The visitor form could not be submitted.'))
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p className="text-sm text-[var(--muted)]">Loading visitor form...</p>
  if (error && !formInfo) return <p role="alert" className="border border-[#e1b5b5] bg-[#fff4f4] p-4 text-sm text-[#9b2c2c]">{error}</p>

  return <form onSubmit={submit} className="mx-auto max-w-4xl space-y-6"><header><p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--royal-blue)]">RRVMS visitor form</p><h1 className="display mt-2 text-4xl font-bold text-[var(--royal-blue)]">Visitor information</h1>{formInfo && <p className="mt-2 text-sm text-[var(--muted)]">{formInfo.requestNumber} - {formInfo.status}</p>}</header>{error && <p role="alert" className="border border-[#e1b5b5] bg-[#fff4f4] p-4 text-sm text-[#9b2c2c]">{error}</p>}<section className="grid gap-4 border border-[var(--silver)] bg-white p-6 sm:grid-cols-2"><Field label="Full legal name" error={fieldErrors.fullName} value={form.fullName} onChange={value => update('fullName', value)} required /><Select label="Citizenship" error={fieldErrors.citizenship} value={form.citizenship} onChange={value => update('citizenship', value)} /><Select label="Nationality" error={fieldErrors.nationality} value={form.nationality} onChange={value => update('nationality', value)} /><Select label="Country of visitor" error={fieldErrors.country} value={form.country} onChange={value => update('country', value)} /><Field label="Designation / position held" value={form.designation} onChange={value => update('designation', value)} /><Field label="Visiting company" value={form.companyName} onChange={value => update('companyName', value)} required /><Field label="Office city" value={form.officeCity} onChange={value => update('officeCity', value)} /><Select label="Office country" error={fieldErrors.officeCountry} value={form.officeCountry} onChange={value => update('officeCountry', value)} /><Field label="Phone" value={form.telephone} onChange={value => update('telephone', value)} required /><Field label="Email" type="email" value={form.email} onChange={value => update('email', value)} required /></section><section className="grid gap-4 border border-[var(--silver)] bg-white p-6 sm:grid-cols-2"><Select label="ID type" value={form.idType} onChange={value => update('idType', value)} options={['Passport', 'Visa', 'Government ID', 'Other valid ID proof']} /><Field label="ID last 4 digits" error={fieldErrors.idLast4} value={form.idLast4} onChange={value => update('idLast4', value.replace(/\D/g, '').slice(0, 4))} required /><Field label="Passport number" value={form.passportNumber} onChange={value => update('passportNumber', value)} /><Field label="Visa number" value={form.visaNumber} onChange={value => update('visaNumber', value)} /><Field label="Government ID number" value={form.governmentIdNumber} onChange={value => update('governmentIdNumber', value)} /></section><section className="border border-[var(--silver)] bg-white p-6"><h2 className="display text-xl font-bold text-[var(--royal-blue)]">Declared assets</h2>{form.assets.map((asset, index) => <div key={index} className="mt-4 grid gap-4 sm:grid-cols-[1fr_1fr_1fr_auto]"><Field label="Asset type" value={asset.assetType} onChange={value => updateAsset(index, 'assetType', value)} required /><Field label="Description" value={asset.description} onChange={value => updateAsset(index, 'description', value)} /><Field label="Serial number" value={asset.serialNumber} onChange={value => updateAsset(index, 'serialNumber', value)} /><button type="button" onClick={() => removeAsset(index)} className="self-end border border-[var(--silver)] px-3 py-2 text-sm font-semibold text-[var(--royal-blue)]">Remove</button></div>)}<button type="button" className="mt-4 text-sm font-semibold text-[var(--royal-blue)]" onClick={addAsset}>+ Add asset</button></section><div className="flex flex-wrap gap-3"><button disabled={saving || formInfo?.status === 'SUBMITTED'} className="rounded-[4px] bg-[var(--royal-blue)] px-5 py-3 text-sm font-semibold text-white disabled:opacity-60">{saving ? 'Submitting...' : formInfo?.status === 'SUBMITTED' ? 'Already submitted' : 'Submit visitor form'}</button>{formInfo && <Link to={`/visitor-requests/${formInfo.visitorRequestId}`} className="border border-[var(--royal-blue)] px-5 py-3 text-sm font-semibold text-[var(--royal-blue)]">Back to request</Link>}</div></form>
}

function Field({ label, value, onChange, type = 'text', required = false, error }: { label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean; error?: string }) { return <label className="text-sm font-semibold text-[var(--ink)]">{label}<input required={required} type={type} value={value} onChange={event => onChange(event.target.value)} className="mt-2 block w-full border border-[var(--silver)] px-3 py-2.5 font-normal" />{error && <span className="mt-1 block text-xs font-normal text-[#9b2c2c]">{error}</span>}</label> }
function Select({ label, value, onChange, options = countries as unknown as string[], error }: { label: string; value: string; onChange: (value: string) => void; options?: string[]; error?: string }) { return <label className="text-sm font-semibold text-[var(--ink)]">{label}<select required className="mt-2 block w-full border border-[var(--silver)] bg-white px-3 py-2.5 font-normal" value={value} onChange={event => onChange(event.target.value)}><option value="">Select...</option>{options.map(option => <option key={option}>{option}</option>)}</select>{error && <span className="mt-1 block text-xs font-normal text-[#9b2c2c]">{error}</span>}</label> }
