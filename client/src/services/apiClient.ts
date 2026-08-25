import axios from 'axios'
import { logger } from '../utils/logger'

// VITE_API_BASE_URL is baked in at build time (Vercel env var). Falls back to the
// local dev API. Trailing slashes are trimmed so misconfigured values stay valid.
const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'
export const apiBaseUrl = configuredBaseUrl.replace(/\/+$/, '')

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.request.use((config) => {
  const userId = localStorage.getItem('rrvms.mock.session')
  if (userId) config.headers['X-RRVMS-Prototype-User'] = userId
  return config
})
apiClient.interceptors.response.use((response) => response, (error) => { logger.error('API request failed', { method: error.config?.method, url: error.config?.url, status: error.response?.status, message: error.message }); return Promise.reject(error) })

export type HealthResponse = { status: string; service: string; database: string }
export type VisitorRequestListItem = { id: string; requestNumber: string; visitorName: string; companyName: string; currentStatus: string; createdAt: string }
export type VisitorRequestDetail = { id: string; requestNumber: string; visitor: { fullName: string; companyName: string; citizenship: string; country: string; designation: string; email: string; phone: string; idType: string; idLast4: string; visitorType: string }; purpose: string; visitingCompany: string; visitingSite: string; visitPurposeType: string; currentStatus: string; visitorFormId?: string; formVersions?: Array<{ id: string; version: number; fullName: string; citizenship: string; country: string; company: string; designation: string; idType: string; idLast4: string; assets: string; createdAt: string }>; visitDays: Array<{ id: string; visitDate: string; expectedArrivalTime?: string; expectedDepartureTime?: string; status: string }>; assets: Array<{ id: string; assetType: string; description: string; serialNumber: string; verificationStatus: string }>; auditHistory: Array<{ id: string; action: string; details: string; createdAt: string }> }
export type CreateVisitorRequest = { visitorType: 'Internal' | 'External'; visitingCompany: string; visitingSite: string; areasToVisit: string; siteTimezone: string; numberOfVisitors: number; purpose: string; visitPurposeType: 'Technical' | 'Non-Technical' | 'Other'; mainHostId: string; escortingHostId?: string; visitDays: Array<{ visitDate: string; expectedArrivalTime?: string; expectedDepartureTime?: string }> }
export type DashboardResponse = { totalRequests: number; pendingActions: number; todaysVisits: number; currentlyInside: number; upcomingVisits: number; noShows: number; pendingEcReviews: number; pendingDocumentation: number; recentRequests: VisitorRequestListItem[] }
export type NotificationItem = { id: string; type: string; message: string; isRead: boolean; createdAt: string }
export type ReceptionVisitor = { id: string; visitDate: string; status: string; requestId: string; requestNumber: string; visitorName: string; company: string }

export async function getHealth(): Promise<HealthResponse> {
  const response = await apiClient.get<HealthResponse>('/api/health')
  return response.data
}

export async function listVisitorRequests() { return (await apiClient.get<{ items: VisitorRequestListItem[]; total: number }>('/api/visitor-requests')).data }
export async function getDashboard() { return (await apiClient.get<DashboardResponse>('/api/dashboard')).data }
export async function getNotifications() { return (await apiClient.get<NotificationItem[]>('/api/notifications')).data }
export async function markNotificationRead(id: string) { await apiClient.post(`/api/notifications/${id}/read`) }
export async function getReceptionVisitors(search?: string) { return (await apiClient.get<ReceptionVisitor[]>('/api/reception/visitors', { params: { search } })).data }
export async function getVisitorRequest(id: string) { return (await apiClient.get<VisitorRequestDetail>(`/api/visitor-requests/${id}`)).data }
export async function createVisitorRequest(input: CreateVisitorRequest) { return (await apiClient.post<VisitorRequestDetail>('/api/visitor-requests', input)).data }
export type WorkflowAction = { action: string; comment?: string; reason?: string; visitDayId?: string; badgeNumber?: string; idLast4?: string; idType?: string; assetSerials?: string; dpsPerformer?: string; dpsResult?: string; dpsNotes?: string; newUserId?: string }
export async function executeVisitorRequestAction(id: string, action: WorkflowAction) { return (await apiClient.post<VisitorRequestDetail>(`/api/visitor-requests/${id}/actions`, action)).data }
export async function getVisitorForm(id: string) { return (await apiClient.get<VisitorForm>(`/api/visitor-forms/${id}`)).data }
export async function submitVisitorForm(id: string, input: VisitorForm) { await apiClient.post(`/api/visitor-forms/${id}/submit`, input) }
export async function submitAdditionalVisitorForm(id: string, input: VisitorForm) { await apiClient.post(`/api/visitor-forms/${id}/additional-response`, input) }
export type VisitorForm = { fullName: string; citizenship: string; country: string; designation: string; companyName: string; officeCity: string; officeCountry: string; telephone: string; email: string; idType: string; idLast4: string; assets: Array<{ assetType: string; description: string; serialNumber: string }> }
export type AnalyticsResponse = { totalRequests: number; byStatus: Record<string, number>; rows: Array<{ requestNumber: string; visitor: string; company: string; visitDate: string; status: string; createdAt: string }> }
export async function getAnalytics() { return (await apiClient.get<AnalyticsResponse>('/api/analytics')).data }
export function analyticsExportUrl(format: 'csv' | 'xlsx') { return `${apiBaseUrl}/api/analytics/export.${format}` }
