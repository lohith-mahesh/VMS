export type VisitStatus = 'Expected' | 'Arrived' | 'Checked In' | 'Inside' | 'Waiting for Host' | 'Hold' | 'No Show'

export type Visit = { id: string; visitor: string; company: string; host: string; time: string; purpose: string; status: VisitStatus; initials: string }

export const kpis = [
  { label: "Today's Visits", value: '24', detail: '6 arriving in the next hour', tone: 'blue' },
  { label: 'Pending Actions', value: '08', detail: '3 require attention today', tone: 'amber' },
  { label: 'Inside Facility', value: '17', detail: 'Across 4 active locations', tone: 'green' },
  { label: 'Awaiting Approval', value: '05', detail: 'Export control review queue', tone: 'silver' },
] as const

export const visits: Visit[] = [
  { id: 'V-1042', visitor: 'Sarah Mitchell', company: 'Aero Systems Ltd', host: 'James Whitmore', time: '08:30', purpose: 'Design review', status: 'Arrived', initials: 'SM' },
  { id: 'V-1043', visitor: 'Daniel Okafor', company: 'Nexus Engineering', host: 'Priya Shah', time: '09:00', purpose: 'Supplier meeting', status: 'Inside', initials: 'DO' },
  { id: 'V-1044', visitor: 'Elena Rossi', company: 'Meridian Partners', host: 'Thomas Green', time: '09:30', purpose: 'Programme briefing', status: 'Waiting for Host', initials: 'ER' },
  { id: 'V-1045', visitor: 'Michael Chen', company: 'Apex Technologies', host: 'Amelia Wright', time: '10:00', purpose: 'Technical workshop', status: 'Expected', initials: 'MC' },
  { id: 'V-1046', visitor: 'Laura Bennett', company: 'Northstar Aviation', host: 'James Whitmore', time: '10:30', purpose: 'Site orientation', status: 'Hold', initials: 'LB' },
]

export const quickActions = ['Create visitor request', 'Review pending actions', 'View today\'s visits']
