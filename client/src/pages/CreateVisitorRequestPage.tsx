import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { createVisitorRequest, type CreateVisitorRequest } from '../services/apiClient'
import { userFacingApiError } from '../utils/logger'

const siteOptions = ['Bangalore', 'Delhi'] as const
const todayIso = new Date().toISOString().slice(0, 10)

const initialForm: CreateVisitorRequest = {
  visitorType: 'External',
  visitingCompany: '',
  visitingCompanyAddressCountry: '',
  visitingSite: 'Bangalore',
  areasToVisit: '',
  siteTimezone: 'Asia/Kolkata',
  numberOfVisitors: 1,
  purpose: '',
  visitPurposeType: 'Technical',
  mainHostId: '',
  escortingHostId: '',
  visitDays: [{ visitDate: todayIso, expectedArrivalTime: '', expectedDepartureTime: '' }],
}

export function CreateVisitorRequestPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ ...initialForm, mainHostId: user?.id ?? '' })
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  const update = <K extends keyof CreateVisitorRequest>(field: K, value: CreateVisitorRequest[K]) =>
    setForm((current) => ({ ...current, [field]: value }))

  const validate = () => {
    if (!form.visitingCompany.trim()) return 'Visiting company is required.'
    if (!form.visitingCompanyAddressCountry.trim()) return 'Address and country of the visiting company is required.'
    if (!form.visitingSite) return 'Please select the site.'
    if (!form.purpose.trim()) return 'Purpose of visit is required.'
    if (!form.areasToVisit.trim()) return 'Areas to be visited is required.'
    if (!form.mainHostId.trim()) return 'Main host is required.'
    if (form.visitDays.length === 0) return 'At least one visit date is required.'
    for (const day of form.visitDays) {
      if (!day.visitDate) return 'Every visit date is required.'
      if (day.visitDate < todayIso) return 'Past dates are not allowed. Please select today or a future date.'
    }
    return ''
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError('')
    const validationMessage = validate()
    if (validationMessage) {
      setError(validationMessage)
      return
    }

    setSaving(true)
    try {
      const result = await createVisitorRequest({
        ...form,
        visitingCompany: form.visitingCompany.trim(),
        visitingCompanyAddressCountry: form.visitingCompanyAddressCountry.trim(),
        visitingSite: form.visitingSite as 'Bangalore' | 'Delhi',
        siteTimezone: 'Asia/Kolkata',
      })
      navigate(`/visitor-requests/${result.id}`)
    } catch (reason) {
      setError(userFacingApiError(reason, 'The request could not be saved.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={submit} className="mx-auto max-w-4xl space-y-6">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--royal-blue)]">Host workspace</p>
        <h1 className="display mt-2 text-4xl font-bold text-[var(--royal-blue)]">Create visitor request</h1>
        <p className="mt-2 text-sm text-[var(--muted)]">Basic visit details only. Each visitor will complete an individual form after submission.</p>
      </header>

      {error && <p role="alert" className="border border-[#e1b5b5] bg-[#fff4f4] p-4 text-sm text-[#9b2c2c]">{error}</p>}

      <section className="grid gap-4 border border-[var(--silver)] bg-white p-6 sm:grid-cols-2">
        <Select label="Visitor type" value={form.visitorType} onChange={(value) => update('visitorType', value as CreateVisitorRequest['visitorType'])} options={['External', 'Internal']} />
        <Field label="Visiting company" value={form.visitingCompany} onChange={(value) => update('visitingCompany', value)} required />
        <Field label="Address & country of visiting company" value={form.visitingCompanyAddressCountry} onChange={(value) => update('visitingCompanyAddressCountry', value)} required />
        <Select label="Site" value={form.visitingSite} onChange={(value) => update('visitingSite', value as CreateVisitorRequest['visitingSite'])} options={siteOptions} />
        <Field label="Site timezone" value={form.siteTimezone} onChange={() => undefined} readOnly />
        <Field label="Number of visitors" type="number" min="1" value={String(form.numberOfVisitors)} onChange={(value) => update('numberOfVisitors', Number(value))} required />
        <Select label="Purpose type" value={form.visitPurposeType} onChange={(value) => update('visitPurposeType', value as CreateVisitorRequest['visitPurposeType'])} options={['Technical', 'Non-Technical', 'Other']} />
        <Field label="Purpose of visit" value={form.purpose} onChange={(value) => update('purpose', value)} required />
        <Field label="Areas to be visited" value={form.areasToVisit} onChange={(value) => update('areasToVisit', value)} required />
        <Field label="Main host" value={form.mainHostId} onChange={(value) => update('mainHostId', value)} required />
        <Field label="Escorting host (optional)" value={form.escortingHostId ?? ''} onChange={(value) => update('escortingHostId', value)} />
      </section>

      <section className="border border-[var(--silver)] bg-white p-6">
        <h2 className="display text-xl font-bold text-[var(--royal-blue)]">Visit dates</h2>
        <p className="mt-2 text-sm text-[var(--muted)]">Only today or future dates are allowed in IST.</p>

        {form.visitDays.map((day, index) => (
          <div key={`${day.visitDate}-${index}`} className="mt-4 grid gap-4 sm:grid-cols-3">
            <Field
              label="Date"
              type="date"
              value={day.visitDate}
              min={todayIso}
              onChange={(value) => setForm((current) => ({
                ...current,
                visitDays: current.visitDays.map((item, itemIndex) => itemIndex === index ? { ...item, visitDate: value } : item),
              }))}
              required
            />
            <Field
              label="Arrival"
              type="time"
              value={day.expectedArrivalTime ?? ''}
              onChange={(value) => setForm((current) => ({
                ...current,
                visitDays: current.visitDays.map((item, itemIndex) => itemIndex === index ? { ...item, expectedArrivalTime: value } : item),
              }))}
            />
            <Field
              label="Departure"
              type="time"
              value={day.expectedDepartureTime ?? ''}
              onChange={(value) => setForm((current) => ({
                ...current,
                visitDays: current.visitDays.map((item, itemIndex) => itemIndex === index ? { ...item, expectedDepartureTime: value } : item),
              }))}
            />
          </div>
        ))}

        <button
          type="button"
          className="mt-4 text-sm font-semibold text-[var(--royal-blue)]"
          onClick={() => setForm((current) => ({
            ...current,
            visitDays: [...current.visitDays, { visitDate: todayIso, expectedArrivalTime: '', expectedDepartureTime: '' }],
          }))}
        >
          + Add another date
        </button>
      </section>

      <button disabled={saving} className="rounded-[4px] bg-[var(--royal-blue)] px-5 py-3 text-sm font-semibold text-white disabled:opacity-60">
        {saving ? 'Creating...' : 'Create visitor request'}
      </button>
    </form>
  )
}

function Field({
  label,
  value,
  onChange,
  type = 'text',
  required = false,
  readOnly = false,
  min,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  type?: string
  required?: boolean
  readOnly?: boolean
  min?: string
}) {
  return (
    <label className="text-sm font-semibold text-[var(--ink)]">
      {label}
      <input
        required={required}
        readOnly={readOnly}
        type={type}
        min={min}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="mt-2 block w-full border border-[var(--silver)] px-3 py-2.5 font-normal"
      />
    </label>
  )
}

function Select({ label, value, onChange, options }: { label: string; value: string; onChange: (value: string) => void; options: readonly string[] }) {
  return (
    <label className="text-sm font-semibold text-[var(--ink)]">
      {label}
      <select
        className="mt-2 block w-full border border-[var(--silver)] bg-white px-3 py-2.5 font-normal"
        value={value}
        onChange={(event) => onChange(event.target.value)}
      >
        {options.map((option) => (
          <option key={option} value={option}>{option}</option>
        ))}
      </select>
    </label>
  )
}
