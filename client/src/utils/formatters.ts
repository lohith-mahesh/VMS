export function formatStatus(status?: string): string {
  if (!status) return ''
  const map: Record<string, string> = {
    DRAFT: 'Draft',
    VISITOR_FORM_PENDING: 'Visitor Form Pending',
    VISITOR_FORM_SUBMITTED: 'Visitor Form Submitted',
    HOST_REVIEW: 'Host Review',
    HOST_DPS: 'Host DPS',
    EC_DPS: 'EC DPS',
    EC_REVIEW: 'EC Review',
    PENDING_DOCUMENTATION: 'Pending Documentation',
    DOCUMENTATION_SUBMITTED: 'Documentation Submitted',
    EC_RE_REVIEW_REQUIRED: 'EC Re-Review Required',
    APPROVED: 'Approved',
    REJECTED: 'Rejected',
    RECEPTION_VERIFICATION: 'Reception Verification',
    RECEPTION_HOLD: 'Reception Hold',
    CHECKED_IN: 'Checked In',
    CHECKED_OUT: 'Checked Out',
    COMPLETED: 'Completed',
    NO_SHOW: 'No Show',
    ENTRY_REJECTED: 'Entry Rejected',
    CANCELLED_PERSONNEL_CHANGE: 'Cancelled (Personnel Change)',
    VISIT_PROCESS_COMPLETED: 'Visit Process Completed',
    UPCOMING: 'Upcoming',
    BATCH_ID: 'Batch ID',
  }
  if (map[status]) return map[status]
  return status.replace(/_/g, ' ').toLowerCase().replace(/\b\w/g, (char) => char.toUpperCase())
}

export function formatRole(role?: string): string {
  if (!role) return ''
  const map: Record<string, string> = {
    HOST_REQUESTER: 'Host / Requester',
    EXPORT_CONTROL: 'Export Control',
    RECEPTION: 'Reception',
  }
  if (map[role]) return map[role]
  return role.replace(/_/g, ' ').toLowerCase().replace(/\b\w/g, (char) => char.toUpperCase())
}

