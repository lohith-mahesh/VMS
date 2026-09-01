import axios from 'axios'
import { logger } from '../utils/logger'

const isProd = import.meta.env.PROD
const rawEnvUrl = import.meta.env.VITE_API_BASE_URL

if (isProd && !rawEnvUrl) {
  console.warn('[App config] VITE_API_BASE_URL is not set; using the current deployment origin or localhost during local development.')
}

const configuredBaseUrl = rawEnvUrl
  ? rawEnvUrl
  : isProd
    ? window.location.origin
    : 'http://localhost:5000'

export const apiBaseUrl = configuredBaseUrl ? configuredBaseUrl.replace(/\/+$/, '') : ''

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.request.use((config) => {
  const userId = localStorage.getItem('visitor.mock.session')
  if (userId) config.headers['X-Visitor-Prototype-User'] = userId
  return config
})
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error(
      `[API ERROR]\nURL: ${(error.config?.baseURL ?? '') + (error.config?.url ?? '')}\nMETHOD: ${error.config?.method?.toUpperCase() ?? 'UNKNOWN'}\nSTATUS: ${error.response?.status ?? 'NO_RESPONSE'}\nRESPONSE:`,
      error.response?.data ?? error.message
    )
    logger.error('API request failed', {
      method: error.config?.method,
      baseUrl: error.config?.baseURL,
      url: error.config?.url,
      status: error.response?.status,
      backend: error.response?.data,
      message: error.message,
    })
    return Promise.reject(error)
  }
)

export type HealthResponse = { status: string; service: string; database: string }
export type VisitorRequestListItem = {
  id: string
  requestNumber: string
  batchId?: string
  visitorName: string
  companyName: string
  currentStatus: string
  createdAt: string
  visitDate?: string
  hostName?: string
  dpsStatus?: string
  currentStage?: string
  lastUpdated?: string
}

export type PreviousRequest = {
  id: string
  requestNumber: string
  visitingSite: string
  purpose: string
  currentStatus: string
  createdAt: string
}

export type PreviousVisitDay = {
  id: string
  requestNumber: string
  visitDate: string
  status: string
}

export type DpsRecord = {
  id: string
  performedBy: string
  status: string
  result: string
  notes?: string
  performedAt?: string
}

export type EcReview = {
  id: string
  reviewerId: string
  status: string
  decision: string
  comments: string
  reviewedAt?: string
}

export type CommentItem = {
  id: string
  authorId: string
  type: string
  text: string
  createdAt: string
}

export type InformationRequestItem = {
  id: string
  fields: string
  comment: string
  status: string
  createdAt: string
  respondedAt?: string
  responseSummary?: string
}

export type FormVersionItem = {
  id: string
  version: number
  fullName: string
  citizenship: string
  country: string
  company: string
  designation: string
  idType: string
  otherIdType?: string
  assets: string
  createdAt: string
}

export type AttendanceItem = {
  id: string
  visitDayId?: string
  category: string
  completed: boolean
  markedByUserId?: string
  markedAt?: string
  comments?: string
}

export type VisitorRequestDetail = {
  id: string
  requestNumber: string
  batchId: string
  visitor: {
    id?: string
    fullName: string
    companyName: string
    citizenship: string
    country: string
    designation: string
    email: string
    phone: string
    idType: string
    otherIdType?: string
    visitorType: string
  }
  purpose: string
  areasToVisit?: string
  visitingCompany: string
  visitingCompanyAddressCountry?: string
  visitingSite: string
  visitPurposeType: string
  mainHostName?: string
  escortingHostName?: string
  currentStatus: string
  visitorFormId?: string
  visitorFormIds?: string[]
  visitorForms?: Array<{ id: string; status: string; fullName: string }>
  formVersions?: FormVersionItem[]
  visitDays: Array<{ id: string; visitDate: string; expectedArrivalTime?: string; expectedDepartureTime?: string; status: string }>
  assets: Array<{ id: string; assetType: string; description: string; serialNumber: string; verificationStatus: string }>
  auditHistory: Array<{ id: string; action: string; details: string; createdAt: string }>
  dpsHistory?: DpsRecord[]
  ecReviews?: EcReview[]
  comments?: CommentItem[]
  informationRequests?: InformationRequestItem[]
  previousRequests?: PreviousRequest[]
  previousVisitDays?: PreviousVisitDay[]
  attendance?: AttendanceItem[]
}

export type CreateVisitorRequest = {
  visitorType: 'Internal' | 'External'
  visitingCompany: string
  visitingCompanyAddressCountry: string
  visitingSite: 'Bangalore' | 'Delhi' | ''
  areasToVisit: string
  siteTimezone: 'Asia/Kolkata'
  numberOfVisitors: number
  purpose: string
  visitPurposeType: 'Technical' | 'Non-Technical' | 'Other'
  mainHostId: string
  escortingHostId?: string
  visitDays: Array<{ visitDate: string; expectedArrivalTime?: string; expectedDepartureTime?: string }>
}

export type DashboardResponse = {
  totalRequests: number
  pendingActions: number
  todaysVisits: number
  currentlyInside: number
  upcomingVisits: number
  noShows: number
  pendingEcReviews: number
  pendingDocumentation: number
  recentRequests: VisitorRequestListItem[]
}

export type EcDashboardResponse = {
  pendingEcReviews: number
  pendingDocumentation: number
  dpsFlags: number
  approved: number
  rejected: number
  visitorHistory: number
  attendance: number
  pendingEcReviewsItems: VisitorRequestListItem[]
  pendingDocumentationItems: VisitorRequestListItem[]
  dpsFlagsItems: VisitorRequestListItem[]
}

export type ReceptionDashboardResponse = {
  todaysVisitors: number
  expected: number
  arrived: number
  onHold: number
  currentlyInside: number
  checkedOut: number
  noShow: number
  items: ReceptionVisitor[]
}

export type NotificationItem = { id: string; type: string; message: string; isRead: boolean; createdAt: string }
export type ReceptionVisitor = {
  id: string
  visitDate: string
  status: string
  requestId: string
  requestNumber: string
  batchId: string
  visitorName: string
  company: string
  idType?: string
  otherIdType?: string
  assets?: Array<{ id: string; assetType: string; description: string; serialNumber: string; verificationStatus: string }>
}

export async function getHealth(): Promise<HealthResponse> {
  const response = await apiClient.get<HealthResponse>('/api/health')
  return response.data
}

export async function listVisitorRequests() {
  return (await apiClient.get<{ items: VisitorRequestListItem[]; total: number }>('/api/visitor-requests')).data
}
export async function getDashboard() {
  return (await apiClient.get<DashboardResponse>('/api/dashboard')).data
}
export async function getEcDashboard() {
  return (await apiClient.get<EcDashboardResponse>('/api/ec/dashboard')).data
}
export async function getReceptionDashboard() {
  return (await apiClient.get<ReceptionDashboardResponse>('/api/reception/dashboard')).data
}
export async function getNotifications() {
  return (await apiClient.get<NotificationItem[]>('/api/notifications')).data
}
export async function markNotificationRead(id: string) {
  await apiClient.post(`/api/notifications/${id}/read`)
}
export async function getReceptionVisitors(search?: string) {
  return (await apiClient.get<ReceptionVisitor[]>('/api/reception/visitors', { params: { search } })).data
}
export async function getVisitorRequest(id: string) {
  return (await apiClient.get<VisitorRequestDetail>(`/api/visitor-requests/${id}`)).data
}
export async function createVisitorRequest(input: CreateVisitorRequest) {
  return (await apiClient.post<VisitorRequestDetail>('/api/visitor-requests', input)).data
}
export type WorkflowAction = {
  action: string
  comment?: string
  reason?: string
  visitDayId?: string
  badgeNumber?: string
  badgeColor?: 'Orange' | 'Red'
  idType?: string
  otherIdType?: string
  assetSerials?: string
  dpsPerformer?: string
  dpsResult?: string
  dpsNotes?: string
  newUserId?: string
  identityVerified?: boolean
  assetsVerified?: boolean
  receptionDecision?: 'CHECK_IN' | 'CHECK_OUT'
}
export async function executeVisitorRequestAction(id: string, action: WorkflowAction) {
  return (await apiClient.post<VisitorRequestDetail>(`/api/visitor-requests/${id}/actions`, action)).data
}

export async function ecRequestInformation(id: string, comment: string) {
  return (await apiClient.post<VisitorRequestDetail>(`/api/visitor-requests/${id}/ec/request-information`, { requestedInformation: comment, comment })).data
}

export async function ecApprove(id: string, comment?: string) {
  return (await apiClient.post<VisitorRequestDetail>(`/api/visitor-requests/${id}/ec/approve`, { comment })).data
}

export async function ecReject(id: string, reason: string) {
  return (await apiClient.post<VisitorRequestDetail>(`/api/visitor-requests/${id}/ec/reject`, { reason, comment: reason })).data
}

export async function updateAttendance(requestId: string, input: { visitDayId?: string; category: string; completed: boolean; comments?: string }) {
  return (await apiClient.put<AttendanceItem>(`/api/visitor-requests/${requestId}/attendance`, input)).data
}

export async function getVisitorForm(id: string) {
  return (await apiClient.get<VisitorForm>(`/api/visitor-forms/${id}`)).data
}
export async function submitVisitorForm(id: string, input: SubmitVisitorForm) {
  return (await apiClient.post<VisitorRequestDetail>(`/api/visitor-forms/${id}/submit`, input)).data
}
export async function submitAdditionalVisitorForm(id: string, input: SubmitVisitorForm) {
  return (await apiClient.post<VisitorRequestDetail>(`/api/visitor-forms/${id}/additional-response`, input)).data
}

export type VisitorForm = {
  id: string
  visitorRequestId: string
  requestNumber: string
  status: string
  fullName: string
  citizenship: string
  country: string
  designation: string
  companyName: string
  officeCity: string
  officeCountry: string
  telephone: string
  email: string
  idType: string
  otherIdType?: string
  assets: Array<{ assetType: string; description: string; serialNumber: string }>
}
export type SubmitVisitorForm = Omit<VisitorForm, 'id' | 'visitorRequestId' | 'requestNumber' | 'status'>
export type AnalyticsResponse = {
  totalRequests: number
  byStatus: Record<string, number>
  rows: Array<{ requestNumber: string; visitor: string; company: string; visitDate: string; status: string; createdAt: string }>
}
export async function getAnalytics() {
  return (await apiClient.get<AnalyticsResponse>('/api/analytics')).data
}
export function analyticsExportUrl(format: 'csv' | 'xlsx') {
  return `${apiBaseUrl}/api/analytics/export.${format}`
}
