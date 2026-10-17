import { Link } from 'react-router-dom';
import { formatDate, formatDateTime } from '../../lib/format';
import type { RequestSummary } from '../../lib/types';
import { EmptyState, StatusTag } from '../../components/Ui';

export function RequestTable({ requests, emptyMessage = 'No requests match the current view.' }: { requests: RequestSummary[]; emptyMessage?: string }) {
  if (requests.length === 0) return <EmptyState title="No requests" message={emptyMessage} />;
  return <div className="table-wrap"><table>
    <thead><tr><th>Request</th><th>Visitors</th><th>Visit Range</th><th>Host</th><th>Status</th><th>Details</th></tr></thead>
    <tbody>{requests.map(request => <tr key={request.id}>
      <td data-label="Request"><strong>{request.requestNumber}</strong><small>{request.siteCode} · {request.visitorType}</small></td>
      <td data-label="Visitors">{request.visitorCount === 1 ? <><strong>{request.visitorNames?.[0] || 'Visitor Details Pending'}</strong><small>{request.companyNames?.[0] || 'Company Details Pending'}</small></> : <details><summary>{request.visitorCount} Visitors · {request.submittedVisitorCount} of {request.visitorCount} Forms Saved</summary><ol>{request.visitorNames?.map((name, index) => <li key={index}>{name || 'Visitor Details Pending'} · {request.companyNames?.[index] || 'Company Details Pending'}</li>)}</ol></details>}</td>
      <td data-label="Visit Range">{formatDate(request.visitStart)}{formatDate(request.visitStart) !== formatDate(request.visitEnd) && <> – {formatDate(request.visitEnd)}</>}</td>
      <td data-label="Host">{request.mainHostName}<small>{request.hostDepartment}</small></td>
      <td data-label="Status"><StatusTag value={request.status} /></td>
      <td data-label="Details"><Link className="table-link" to={`/requests/${request.id}`} aria-label={`Open ${request.requestNumber}`}>View Details</Link><small>{formatDateTime(request.updatedAt)}</small></td>
    </tr>)}</tbody>
  </table></div>;
}
