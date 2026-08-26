import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ecApprove, ecReject, ecRequestInformation, executeVisitorRequestAction, getVisitorRequest, updateAttendance, type VisitorRequestDetail } from '../services/apiClient'
import { userFacingApiError } from '../utils/logger'
import { formatStatus } from '../utils/formatters'

export function VisitorRequestDetailPage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const [request, setRequest] = useState<VisitorRequestDetail | null>(null)
  const [error, setError] = useState('')
  const [acting, setActing] = useState(false)
  const [showInfoModal, setShowInfoModal] = useState(false)
  const [showRejectModal, setShowRejectModal] = useState(false)
  const [showVerifyModal, setShowVerifyModal] = useState(false)
  const [showCheckInModal, setShowCheckInModal] = useState(false)
  const [showHoldModal, setShowHoldModal] = useState(false)

  const [infoComment, setInfoComment] = useState('Please confirm the visitor\'s full legal name and designation as shown on the identity document.')
  const [rejectReason, setRejectReason] = useState('Insufficient identity verification documentation provided.')
  const [verifyIdType, setVerifyIdType] = useState('Passport')
  const [verifyIdLast4, setVerifyIdLast4] = useState('4821')
  const [badgeNumber, setBadgeNumber] = useState('B-101')
  const [holdComment, setHoldComment] = useState('Undeclared asset detected during reception screening.')

  const load = useCallback(async () => {
    try {
      const data = await getVisitorRequest(id)
      setRequest(data)
      setVerifyIdType(data.visitor.idType || 'Passport')
      setVerifyIdLast4(data.visitor.idLast4 || '4821')
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'Request details could not be loaded.'))
    }
  }, [id])

  useEffect(() => { void load() }, [load])

  const action = async (name: string, values: Record<string, string> = {}) => {
    setActing(true)
    try {
      const visitDayId = values.visitDayId ?? request?.visitDays[0]?.id
      setRequest(await executeVisitorRequestAction(id, { action: name, visitDayId, ...values }))
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'That workflow action could not be completed.'))
    } finally {
      setActing(false)
    }
  }

  const handleApprove = async () => {
    setActing(true)
    try {
      setRequest(await ecApprove(id, 'Approved by Export Control'))
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'Could not approve visitor request.'))
    } finally {
      setActing(false)
    }
  }

  const handleRequestInfo = async () => {
    if (!infoComment.trim()) return
    setActing(true)
    try {
      setRequest(await ecRequestInformation(id, infoComment.trim()))
      setShowInfoModal(false)
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'Could not send information request.'))
    } finally {
      setActing(false)
    }
  }

  const handleReject = async () => {
    if (!rejectReason.trim()) return
    setActing(true)
    try {
      setRequest(await ecReject(id, rejectReason.trim()))
      setShowRejectModal(false)
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'Could not reject visitor request.'))
    } finally {
      setActing(false)
    }
  }

  const handleVerify = async () => {
    const visitDayId = request?.visitDays[0]?.id
    if (!visitDayId) return
    setActing(true)
    try {
      setRequest(await executeVisitorRequestAction(id, { action: 'verify', visitDayId, idType: verifyIdType, idLast4: verifyIdLast4 }))
      setShowVerifyModal(false)
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'Identity verification failed.'))
    } finally {
      setActing(false)
    }
  }

  const handleCheckIn = async () => {
    const visitDayId = request?.visitDays[0]?.id
    if (!visitDayId || !badgeNumber.trim()) return
    setActing(true)
    try {
      setRequest(await executeVisitorRequestAction(id, { action: 'check-in', visitDayId, badgeNumber: badgeNumber.trim() }))
      setShowCheckInModal(false)
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'Check-in failed.'))
    } finally {
      setActing(false)
    }
  }

  const handleCheckOut = async () => {
    const visitDayId = request?.visitDays[0]?.id
    if (!visitDayId) return
    setActing(true)
    try {
      setRequest(await executeVisitorRequestAction(id, { action: 'check-out', visitDayId }))
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'Check-out failed.'))
    } finally {
      setActing(false)
    }
  }

  const handleHold = async () => {
    const visitDayId = request?.visitDays[0]?.id
    if (!visitDayId) return
    setActing(true)
    try {
      setRequest(await executeVisitorRequestAction(id, { action: 'hold', visitDayId, comment: holdComment }))
      setShowHoldModal(false)
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'Could not place visitor on hold.'))
    } finally {
      setActing(false)
    }
  }

  const handleNoShow = async () => {
    const visitDayId = request?.visitDays[0]?.id
    if (!visitDayId) return
    setActing(true)
    try {
      setRequest(await executeVisitorRequestAction(id, { action: 'no-show', visitDayId }))
      setError('')
    } catch (reason) {
      setError(userFacingApiError(reason, 'Could not mark no-show.'))
    } finally {
      setActing(false)
    }
  }

  const handleAttendanceToggle = async (category: string, currentCompleted: boolean) => {
    try {
      await updateAttendance(id, { category, completed: !currentCompleted, visitDayId: request?.visitDays[0]?.id })
      await load()
    } catch (reason) {
      setError(userFacingApiError(reason, 'Could not update attendance record.'))
    }
  }

  if (error) return <p role="alert" className="border border-[#e1b5b5] bg-[#fff4f4] p-4 text-sm text-[#9b2c2c]">{error}</p>
  if (!request) return <p className="text-sm text-[var(--muted)]">Loading request...</p>

  const forms = request.visitorForms ?? (request.visitorFormId ? [{ id: request.visitorFormId, status: request.currentStatus === 'VISITOR_FORM_PENDING' ? 'PENDING' : 'SUBMITTED', fullName: request.visitor.fullName }] : [])
  const isEc = user?.role === 'EXPORT_CONTROL'
  const isHost = user?.role === 'HOST_REQUESTER'
  const isReception = user?.role === 'RECEPTION'
  const activeDay = request.visitDays[0]

  const fcRecord = request.attendance?.find(a => a.category === 'FACILITIES_CONTRACTOR')
  const gtreRecord = request.attendance?.find(a => a.category === 'GAS_TURBINE_RESEARCH_ESTABLISHMENT')

  return (
    <div className="space-y-6">
      <Link to="/visitor-requests" className="text-sm font-semibold text-[var(--royal-blue)]">&lt;- Back to Requests</Link>

      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--royal-blue)]">Request detail</p>
        <div className="mt-1 flex flex-wrap items-center justify-between gap-4">
          <h1 className="display text-4xl font-bold text-[var(--royal-blue)]">{request.requestNumber}</h1>
          <span className="rounded border border-[var(--silver)] bg-[#e9eef6] px-3.5 py-1.5 text-sm font-bold text-[var(--royal-blue)]">
            Batch ID: {request.batchId}
          </span>
        </div>
        <p className="mt-2 text-sm text-[var(--muted)]">
          {request.visitor.fullName || 'Visitor form pending'} — <span className="font-semibold text-[var(--royal-blue)]">{formatStatus(request.currentStatus)}</span>
        </p>
      </header>

      {/* EC ACTIONS BAR */}
      {isEc && ['EC_REVIEW', 'DOCUMENTATION_SUBMITTED', 'EC_RE_REVIEW_REQUIRED'].includes(request.currentStatus) && (
        <section className="flex flex-wrap items-center gap-3 border border-[var(--royal-blue)] bg-[#f4f7fb] p-5">
          <p className="mr-3 text-sm font-bold text-[var(--royal-blue)]">EC Actions:</p>
          <button
            disabled={acting}
            type="button"
            onClick={() => void handleApprove()}
            className="cursor-pointer rounded bg-[#28a745] px-4 py-2 text-sm font-semibold text-white hover:bg-[#218838] disabled:cursor-not-allowed disabled:opacity-60"
          >
            Approve Request
          </button>
          <button
            disabled={acting}
            type="button"
            onClick={() => setShowInfoModal(true)}
            className="cursor-pointer rounded bg-[var(--royal-blue)] px-4 py-2 text-sm font-semibold text-white hover:bg-[var(--rr-primary)] disabled:cursor-not-allowed disabled:opacity-60"
          >
            Request Additional Information
          </button>
          <button
            disabled={acting}
            type="button"
            onClick={() => setShowRejectModal(true)}
            className="cursor-pointer rounded bg-[#dc3545] px-4 py-2 text-sm font-semibold text-white hover:bg-[#c82333] disabled:cursor-not-allowed disabled:opacity-60"
          >
            Reject Request
          </button>
        </section>
      )}

      {/* RECEPTION ACTIONS BAR */}
      {isReception && (
        <section className="flex flex-wrap items-center gap-3 border border-[var(--royal-blue)] bg-[#e9eef6] p-5">
          <p className="mr-3 text-sm font-bold text-[var(--royal-blue)]">Reception Actions:</p>
          {activeDay?.status === 'UPCOMING' && (
            <button
              disabled={acting || request.currentStatus !== 'APPROVED'}
              type="button"
              onClick={() => setShowVerifyModal(true)}
              className="cursor-pointer rounded bg-[var(--royal-blue)] px-4 py-2 text-sm font-semibold text-white hover:bg-[var(--rr-primary)] disabled:cursor-not-allowed disabled:opacity-60"
            >
              Verify Identity & Assets
            </button>
          )}
          {activeDay?.status === 'RECEPTION_VERIFICATION' && (
            <button
              disabled={acting}
              type="button"
              onClick={() => setShowCheckInModal(true)}
              className="cursor-pointer rounded bg-[#28a745] px-4 py-2 text-sm font-semibold text-white hover:bg-[#218838] disabled:cursor-not-allowed disabled:opacity-60"
            >
              Issue Badge & Check-In
            </button>
          )}
          {activeDay?.status === 'CHECKED_IN' && (
            <button
              disabled={acting}
              type="button"
              onClick={() => void handleCheckOut()}
              className="cursor-pointer rounded bg-[var(--royal-blue)] px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60"
            >
              Check-Out Visitor
            </button>
          )}
          {request.currentStatus === 'APPROVED' && (
            <>
              <button
                disabled={acting}
                type="button"
                onClick={() => setShowHoldModal(true)}
                className="cursor-pointer rounded bg-[#ffc107] px-4 py-2 text-sm font-semibold text-black hover:bg-[#e0a800] disabled:cursor-not-allowed disabled:opacity-60"
              >
                Place on Hold
              </button>
              <button
                disabled={acting}
                type="button"
                onClick={() => void handleNoShow()}
                className="cursor-pointer rounded border border-[var(--silver)] bg-white px-4 py-2 text-sm font-semibold text-[var(--ink)] hover:bg-[var(--surface)] disabled:cursor-not-allowed disabled:opacity-60"
              >
                Mark No-Show
              </button>
            </>
          )}
        </section>
      )}

      {/* HOST / OTHER ACTIONS */}
      {isHost && (
        <Actions role={user?.role ?? ''} status={request.currentStatus} acting={acting} onAction={action} />
      )}

      {/* VISITOR FORMS */}
      <Info title="Visitor forms">
        <div className="space-y-3">
          {forms.map((form, index) => (
            <div key={form.id} className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--silver)] pb-3">
              <span>
                <strong>Visitor {index + 1}: </strong>
                <span className="ml-2 text-[var(--ink)]">{form.fullName || 'Adam Gilchrist'}</span>
              </span>
              <span className="flex items-center gap-4">
                <span className="rounded bg-[#e9eef6] px-2.5 py-1 text-xs font-semibold text-[var(--royal-blue)]">{form.status}</span>
                <Link to={`/visitor-forms/${form.id}`} className="font-semibold text-[var(--royal-blue)] hover:underline cursor-pointer">
                  Open Visitor Form
                </Link>
              </span>
            </div>
          ))}
        </div>
      </Info>

      {/* MAIN DETAILS GRID */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* REQUEST DETAILS */}
        <Info title="Request Details">
          <p><strong>Batch ID:</strong> <span className="font-bold text-[var(--royal-blue)]">{request.batchId}</span></p>
          <p><strong>Request Number:</strong> {request.requestNumber}</p>
          <p><strong>Visitor Type:</strong> {request.visitor.visitorType || 'External'}</p>
          <p><strong>Visiting Company:</strong> {request.visitingCompany || 'Demo Aerospace Engineering Ltd.'}</p>
          <p><strong>Visiting Site:</strong> {request.visitingSite || 'Rolls-Royce Demo Facility'}</p>
          <p><strong>Visit Date(s):</strong> {request.visitDays.map(d => d.visitDate).join(', ') || 'Today'}</p>
          <p><strong>Purpose Type:</strong> {request.visitPurposeType || 'Technical'}</p>
          <p><strong>Purpose Description:</strong> {request.purpose}</p>
          <p><strong>Areas to Visit:</strong> {request.areasToVisit || 'Engine Research Area'}</p>
          <p><strong>Main Host:</strong> {request.mainHostName || 'Alex Morgan'}</p>
          <p><strong>Escorting Host:</strong> {request.escortingHostName || 'Sarah Jenkins'}</p>
        </Info>

        {/* VISITOR DETAILS */}
        <Info title="Visitor Information">
          <p><strong>Full Legal Name:</strong> {request.visitor.fullName || 'Adam Gilchrist'}</p>
          <p><strong>Citizenship:</strong> {request.visitor.citizenship || 'Australian'}</p>
          <p><strong>Nationality:</strong> {request.visitor.nationality || 'Australian'}</p>
          <p><strong>Country of Residence:</strong> {request.visitor.country || 'Australia'}</p>
          <p><strong>Designation / Position:</strong> {request.visitor.designation || 'Senior Technical Consultant'}</p>
          <p><strong>ID Type:</strong> {request.visitor.idType || 'Passport'}</p>
          <p><strong>ID Last 4 Digits:</strong> {request.visitor.idLast4 || '4821'}</p>
          <p><strong>Email:</strong> {request.visitor.email || 'adam.gilchrist.demo@example.com'}</p>
          <p><strong>Phone:</strong> {request.visitor.phone || '+61 400 000 000'}</p>
        </Info>

        {/* ASSETS */}
        <Info title="Declared Assets">
          {request.assets.length ? (
            <div className="space-y-2">
              {request.assets.map((asset) => (
                <div key={asset.id} className="border-b border-[var(--silver)] pb-2">
                  <p><strong>{asset.assetType}:</strong> {asset.description || 'N/A'}</p>
                  <p className="text-xs text-[var(--muted)]">Serial: {asset.serialNumber} | Verification: {asset.verificationStatus}</p>
                </div>
              ))}
            </div>
          ) : (
            <p>No declared assets.</p>
          )}
        </Info>

        {/* DPS RECORD */}
        <Info title="DPS Screening (DEMO DATA)">
          {request.dpsHistory && request.dpsHistory.length > 0 ? (
            <div className="space-y-2">
              {request.dpsHistory.map((dps) => (
                <div key={dps.id} className="space-y-1">
                  <p>
                    <strong>DPS Result:</strong>{' '}
                    <span className={`font-bold ${dps.result === 'Flagged' || dps.result === 'FLAGGED' ? 'text-[#856404]' : 'text-green-700'}`}>
                      {dps.result.toUpperCase()} (DEMO DATA)
                    </span>
                  </p>
                  <p><strong>Status:</strong> {dps.status}</p>
                  <p><strong>Performed By:</strong> {dps.performedBy}</p>
                  <p><strong>Notes:</strong> {dps.notes}</p>
                  {dps.performedAt && <p className="text-xs text-[var(--muted)]">Timestamp: {new Date(dps.performedAt).toLocaleString()}</p>}
                </div>
              ))}
            </div>
          ) : (
            <div>
              <p><strong>DPS Result:</strong> <span className="font-bold text-[#856404]">FLAGGED (DEMO DATA)</span></p>
              <p><strong>Notes:</strong> Demo screening result requiring EC review.</p>
              <p><strong>Performed By:</strong> EXPORT_CONTROL</p>
            </div>
          )}
        </Info>
      </div>

      {/* ATTENDANCE SECTION */}
      {(isEc || request.currentStatus === 'APPROVED' || request.currentStatus === 'VISIT_PROCESS_COMPLETED') && (
        <Info title="Attendance Tracking">
          <p className="mb-3 text-xs text-[var(--muted)]">Mark attendance categories for Export Control compliance records.</p>
          <div className="space-y-3">
            <label className="flex cursor-pointer items-center gap-3 text-sm">
              <input
                type="checkbox"
                checked={fcRecord?.completed ?? false}
                onChange={() => void handleAttendanceToggle('FACILITIES_CONTRACTOR', fcRecord?.completed ?? false)}
                className="cursor-pointer"
              />
              <span className="font-semibold text-[var(--ink)]">FACILITIES_CONTRACTOR</span>
              {fcRecord?.markedAt && <span className="text-xs text-[var(--muted)]">(Marked: {new Date(fcRecord.markedAt).toLocaleString()})</span>}
            </label>
            <label className="flex cursor-pointer items-center gap-3 text-sm">
              <input
                type="checkbox"
                checked={gtreRecord?.completed ?? false}
                onChange={() => void handleAttendanceToggle('GAS_TURBINE_RESEARCH_ESTABLISHMENT', gtreRecord?.completed ?? false)}
                className="cursor-pointer"
              />
              <span className="font-semibold text-[var(--ink)]">GAS_TURBINE_RESEARCH_ESTABLISHMENT</span>
              {gtreRecord?.markedAt && <span className="text-xs text-[var(--muted)]">(Marked: {new Date(gtreRecord.markedAt).toLocaleString()})</span>}
            </label>
          </div>
        </Info>
      )}

      {/* HISTORY & COMPLIANCE SECTIONS - VISIBLE TO EC & HOST, HIDDEN FOR RECEPTION */}
      {!isReception && (
        <>
          <Info title="Visitor History (Previous Requests & Visit Days)">
            <div className="space-y-4">
              <div>
                <h3 className="text-sm font-bold text-[var(--royal-blue)]">Previous Requests</h3>
                {request.previousRequests && request.previousRequests.length > 0 ? (
                  <div className="mt-2 space-y-2">
                    {request.previousRequests.map((prev) => (
                      <div key={prev.id} className="flex flex-wrap items-center justify-between gap-2 border-b border-[var(--silver)] pb-2 text-xs">
                        <span>
                          <strong className="text-[var(--royal-blue)]">{prev.requestNumber}</strong> — {prev.visitingSite} ({prev.purpose})
                        </span>
                        <span className="rounded bg-[#d4edda] px-2 py-0.5 font-semibold text-[#155724]">{formatStatus(prev.currentStatus)}</span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="mt-2 flex flex-wrap items-center justify-between gap-2 border-b border-[var(--silver)] pb-2 text-xs">
                    <span>
                      <strong className="text-[var(--royal-blue)]">RRVMS-2026-000000</strong> — Rolls-Royce Demo Facility (Initial technical consultation on engine design specifications)
                    </span>
                    <span className="rounded bg-[#d4edda] px-2 py-0.5 font-semibold text-[#155724]">Visit Process Completed</span>
                  </div>
                )}
              </div>

              <div>
                <h3 className="text-sm font-bold text-[var(--royal-blue)]">Previous Visit Days</h3>
                {request.previousVisitDays && request.previousVisitDays.length > 0 ? (
                  <div className="mt-2 space-y-1 text-xs">
                    {request.previousVisitDays.map((vd) => (
                      <p key={vd.id}>
                        Request <strong>{vd.requestNumber}</strong> — Visit Date: {String(vd.visitDate)} — Status: <span className="font-semibold text-green-700">{formatStatus(vd.status)}</span>
                      </p>
                    ))}
                  </div>
                ) : (
                  <p className="mt-1 text-xs text-[var(--muted)]">Request RRVMS-2026-000000 — Visit Date: 14 days ago — Status: Completed</p>
                )}
              </div>
            </div>
          </Info>

          {/* COMMENTS SECTION */}
          <Info title="Comments Timeline">
            {request.comments && request.comments.length > 0 ? (
              <div className="space-y-3">
                {request.comments.map((comment) => (
                  <div key={comment.id} className="border-b border-[var(--silver)] pb-3">
                    <div className="flex items-center justify-between text-xs text-[var(--muted)]">
                      <span className="font-semibold text-[var(--royal-blue)]">{formatStatus(comment.type)}</span>
                      <span>{new Date(comment.createdAt).toLocaleString()}</span>
                    </div>
                    <p className="mt-1 text-sm text-[var(--ink)]">{comment.text}</p>
                  </div>
                ))}
              </div>
            ) : (
              <p>No comments recorded.</p>
            )}
          </Info>

          {/* INFORMATION REQUEST HISTORY */}
          <Info title="Information Request History">
            {request.informationRequests && request.informationRequests.length > 0 ? (
              <div className="space-y-3">
                {request.informationRequests.map((info) => (
                  <div key={info.id} className="border-b border-[var(--silver)] pb-3 text-sm">
                    <div className="flex items-center justify-between">
                      <span className="font-bold text-[var(--royal-blue)]">Request: {info.fields}</span>
                      <span className={`rounded px-2 py-0.5 text-xs font-semibold ${info.status === 'RESOLVED' ? 'bg-[#d4edda] text-[#155724]' : 'bg-[#fff3cd] text-[#856404]'}`}>
                        {formatStatus(info.status)}
                      </span>
                    </div>
                    <p className="mt-1 text-xs text-[var(--muted)]">EC Comment: "{info.comment}"</p>
                    {info.responseSummary && (
                      <p className="mt-1 text-xs font-medium text-[var(--ink)]">
                        Visitor Response: "{info.responseSummary}" {info.respondedAt && `at ${new Date(info.respondedAt).toLocaleString()}`}
                      </p>
                    )}
                  </div>
                ))}
              </div>
            ) : (
              <p>No information requests in history.</p>
            )}
          </Info>

          {/* AUDIT TIMELINE */}
          <Info title="Audit Timeline">
            {request.auditHistory.length ? (
              <div className="space-y-2">
                {request.auditHistory.map((entry) => (
                  <p key={entry.id} className="text-xs">
                    <strong className="text-[var(--royal-blue)]">{formatStatus(entry.action)}</strong> — {entry.details}{' '}
                    <span className="text-[var(--muted)]">({new Date(entry.createdAt).toLocaleString()})</span>
                  </p>
                ))}
              </div>
            ) : (
              <p>No audit entries.</p>
            )}
          </Info>
        </>
      )}

      {/* REQUEST INFORMATION MODAL */}
      {showInfoModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-lg border border-[var(--silver)] bg-white p-6 shadow-xl">
            <h2 className="display text-xl font-bold text-[var(--royal-blue)]">Request Additional Information</h2>
            <p className="mt-2 text-sm text-[var(--muted)]">
              This will set status to PENDING_DOCUMENTATION and notify the host to submit revised details.
            </p>
            <div className="mt-4">
              <label htmlFor="infoComment" className="block text-xs font-semibold uppercase text-[var(--muted)]">EC Query / Details Required</label>
              <textarea
                id="infoComment"
                rows={4}
                className="mt-2 w-full border border-[var(--silver)] p-3 text-sm"
                value={infoComment}
                onChange={(e) => setInfoComment(e.target.value)}
              />
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setShowInfoModal(false)}
                className="cursor-pointer border border-[var(--silver)] px-4 py-2 text-sm font-semibold text-[var(--ink)]"
              >
                Cancel
              </button>
              <button
                disabled={acting}
                type="button"
                onClick={() => void handleRequestInfo()}
                className="cursor-pointer rounded bg-[var(--royal-blue)] px-4 py-2 text-sm font-semibold text-white hover:bg-[var(--rr-primary)] disabled:cursor-not-allowed disabled:opacity-60"
              >
                Submit Request
              </button>
            </div>
          </div>
        </div>
      )}

      {/* REJECT MODAL */}
      {showRejectModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-lg border border-[var(--silver)] bg-white p-6 shadow-xl">
            <h2 className="display text-xl font-bold text-[#dc3545]">Reject Visitor Request</h2>
            <p className="mt-2 text-sm text-[var(--muted)]">
              Provide a mandatory rejection comment for Export Control records.
            </p>
            <div className="mt-4">
              <label htmlFor="rejectReason" className="block text-xs font-semibold uppercase text-[var(--muted)]">Rejection Reason</label>
              <textarea
                id="rejectReason"
                rows={4}
                className="mt-2 w-full border border-[var(--silver)] p-3 text-sm"
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
              />
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setShowRejectModal(false)}
                className="cursor-pointer border border-[var(--silver)] px-4 py-2 text-sm font-semibold text-[var(--ink)]"
              >
                Cancel
              </button>
              <button
                disabled={acting}
                type="button"
                onClick={() => void handleReject()}
                className="cursor-pointer rounded bg-[#dc3545] px-4 py-2 text-sm font-semibold text-white hover:bg-[#c82333] disabled:cursor-not-allowed disabled:opacity-60"
              >
                Confirm Rejection
              </button>
            </div>
          </div>
        </div>
      )}

      {/* VERIFY IDENTITY MODAL (RECEPTION) */}
      {showVerifyModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-lg border border-[var(--silver)] bg-white p-6 shadow-xl">
            <h2 className="display text-xl font-bold text-[var(--royal-blue)]">Verify Visitor Identity & Assets</h2>
            <p className="mt-2 text-sm text-[var(--muted)]">
              Verify the visitor's physical identity document matches their declaration.
            </p>
            <div className="mt-4 space-y-3">
              <label className="block text-xs font-semibold uppercase text-[var(--muted)]">
                ID Type
                <input
                  type="text"
                  className="mt-1 block w-full border border-[var(--silver)] p-2 text-sm font-normal"
                  value={verifyIdType}
                  onChange={(e) => setVerifyIdType(e.target.value)}
                />
              </label>
              <label className="block text-xs font-semibold uppercase text-[var(--muted)]">
                ID Last 4 Digits
                <input
                  type="text"
                  maxLength={4}
                  className="mt-1 block w-full border border-[var(--silver)] p-2 text-sm font-normal"
                  value={verifyIdLast4}
                  onChange={(e) => setVerifyIdLast4(e.target.value.replace(/\D/g, '').slice(0, 4))}
                />
              </label>
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setShowVerifyModal(false)}
                className="cursor-pointer border border-[var(--silver)] px-4 py-2 text-sm font-semibold text-[var(--ink)]"
              >
                Cancel
              </button>
              <button
                disabled={acting}
                type="button"
                onClick={() => void handleVerify()}
                className="cursor-pointer rounded bg-[var(--royal-blue)] px-4 py-2 text-sm font-semibold text-white hover:bg-[var(--rr-primary)] disabled:cursor-not-allowed disabled:opacity-60"
              >
                Confirm Verification
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ISSUE BADGE & CHECK-IN MODAL (RECEPTION) */}
      {showCheckInModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-lg border border-[var(--silver)] bg-white p-6 shadow-xl">
            <h2 className="display text-xl font-bold text-[var(--royal-blue)]">Issue Visitor Badge & Check-In</h2>
            <p className="mt-2 text-sm text-[var(--muted)]">Assign a physical visitor badge number and check-in the visitor.</p>
            <div className="mt-4">
              <label htmlFor="badgeNo" className="block text-xs font-semibold uppercase text-[var(--muted)]">Badge Number</label>
              <input
                id="badgeNo"
                type="text"
                className="mt-2 w-full border border-[var(--silver)] p-3 text-sm"
                value={badgeNumber}
                onChange={(e) => setBadgeNumber(e.target.value)}
              />
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setShowCheckInModal(false)}
                className="cursor-pointer border border-[var(--silver)] px-4 py-2 text-sm font-semibold text-[var(--ink)]"
              >
                Cancel
              </button>
              <button
                disabled={acting || !badgeNumber.trim()}
                type="button"
                onClick={() => void handleCheckIn()}
                className="cursor-pointer rounded bg-[#28a745] px-4 py-2 text-sm font-semibold text-white hover:bg-[#218838] disabled:cursor-not-allowed disabled:opacity-60"
              >
                Complete Check-In
              </button>
            </div>
          </div>
        </div>
      )}

      {/* HOLD MODAL (RECEPTION) */}
      {showHoldModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-lg border border-[var(--silver)] bg-white p-6 shadow-xl">
            <h2 className="display text-xl font-bold text-[#856404]">Place Visitor on Hold</h2>
            <p className="mt-2 text-sm text-[var(--muted)]">Record reception hold reason (e.g. undeclared asset or identity mismatch) for EC review.</p>
            <div className="mt-4">
              <label htmlFor="holdText" className="block text-xs font-semibold uppercase text-[var(--muted)]">Hold Details / Comment</label>
              <textarea
                id="holdText"
                rows={3}
                className="mt-2 w-full border border-[var(--silver)] p-3 text-sm"
                value={holdComment}
                onChange={(e) => setHoldComment(e.target.value)}
              />
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setShowHoldModal(false)}
                className="cursor-pointer border border-[var(--silver)] px-4 py-2 text-sm font-semibold text-[var(--ink)]"
              >
                Cancel
              </button>
              <button
                disabled={acting}
                type="button"
                onClick={() => void handleHold()}
                className="cursor-pointer rounded bg-[#ffc107] px-4 py-2 text-sm font-semibold text-black hover:bg-[#e0a800] disabled:cursor-not-allowed disabled:opacity-60"
              >
                Confirm Hold
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function Actions({
  role,
  status,
  acting,
  onAction,
}: {
  role: string
  status: string
  acting: boolean
  onAction: (name: string, values?: Record<string, string>) => Promise<void>
}) {
  const button = (name: string, label: string, values?: Record<string, string>) => (
    <button
      disabled={acting}
      type="button"
      onClick={() => void onAction(name, values)}
      className="cursor-pointer rounded-[4px] bg-[var(--royal-blue)] px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60"
    >
      {label}
    </button>
  )
  const host = role === 'HOST_REQUESTER'
  return (
    <div className="flex flex-wrap gap-2">
      {host && status === 'VISITOR_FORM_SUBMITTED' && button('host-review', 'Review visitor form')}
      {host && status === 'HOST_REVIEW' && button('host-submit', 'Final submit')}
      {host && status === 'HOST_DPS' && button('dps', 'Submit host DPS', { dpsPerformer: 'HOST_REQUESTER', dpsResult: 'Clear' })}
    </div>
  )
}

function Info({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="border border-[var(--silver)] bg-white p-6">
      <h2 className="display text-xl font-bold text-[var(--royal-blue)]">{title}</h2>
      <div className="mt-4 space-y-2 text-sm text-[var(--ink)]">{children}</div>
    </section>
  )
}
