import axios from 'axios'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.request.use((config) => {
  const userId = localStorage.getItem('rrvms.mock.session')
  if (userId) config.headers['X-RRVMS-Prototype-User'] = userId
  return config
})

export type HealthResponse = { status: string; service: string; database: string }
export type VisitorRequestListItem = { id: string; requestNumber: string; visitorName: string; companyName: string; currentStatus: string; createdAt: string }
export type VisitorRequestDetail = { id: string; requestNumber: string; visitor: { fullName: string; companyName: string; citizenship: string; country: string; designation: string; email: string; phone: string; idType: string; idLast4: string; visitorType: string }; purpose: string; visitingCompany: string; visitingSite: string; visitPurposeType: string; currentStatus: string; visitDays: Array<{ id: string; visitDate: string; expectedArrivalTime?: string; expectedDepartureTime?: string; status: string }>; assets: Array<{ id: string; assetType: string; description: string; serialNumber: string; verificationStatus: string }>; auditHistory: Array<{ id: string; action: string; details: string; createdAt: string }> }
export type CreateVisitorRequest = { fullName: string; companyName: string; citizenship: string; country: string; designation: string; email: string; phone: string; idType: string; idLast4: string; visitingCompany: string; visitingSite: string; purpose: string; visitPurposeType: string; visitDays: Array<{ visitDate: string; expectedArrivalTime?: string; expectedDepartureTime?: string }>; assets: Array<{ assetType: string; description: string; serialNumber: string }> }

export async function getHealth(): Promise<HealthResponse> {
  const response = await apiClient.get<HealthResponse>('/api/health')
  return response.data
}

export async function listVisitorRequests() { return (await apiClient.get<{ items: VisitorRequestListItem[]; total: number }>('/api/visitor-requests')).data }
export async function getVisitorRequest(id: string) { return (await apiClient.get<VisitorRequestDetail>(`/api/visitor-requests/${id}`)).data }
export async function createVisitorRequest(input: CreateVisitorRequest) { return (await apiClient.post<VisitorRequestDetail>('/api/visitor-requests', input)).data }
