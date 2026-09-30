import { Link } from 'react-router-dom';
import { formatDate, formatDateTime } from '../../lib/format';
import type { RequestSummary } from '../../lib/types';
import { EmptyState, StatusTag } from '../../components/Ui';

export function RequestTable({ requests, emptyMessage = 'No requests match the current view.' }: { requests: RequestSummary[]; emptyMessage?: string }) {
  if (requests.length === 0) return <EmptyState title="No requests" message={emptyMessage} />;
  return <div className="table-wrap"><table>
    <thead><tr><th>Request</th><th>Visit</th><th>Site</th><th>Host</th><th>Visitors</th><th>Status</th><th>Updated</th><th><span className="visually-hidden">Open</span></th></tr></thead>
    <tbody>{requests.map(request => <tr key={request.id}>
      <td data-label="Request"><strong>{request.requestNumber}</strong><small>{request.visitorType}</small></td>
      <td data-label="Visit">{formatDate(request.visitStart)}{formatDate(request.visitStart) !== formatDate(request.visitEnd) && <> – {formatDate(request.visitEnd)}</>}</td>
      <td data-label="Site">{request.siteCode}</td>
      <td data-label="Host">{request.mainHostName}<small>{request.hostDepartment}</small></td>
      <td data-label="Visitors">{request.submittedVisitorCount}/{request.visitorCount}</td>
      <td data-label="Status"><StatusTag value={request.status} /></td>
      <td data-label="Updated">{formatDateTime(request.updatedAt)}</td>
      <td><Link className="table-link" to={`/requests/${request.id}`} aria-label={`Open ${request.requestNumber}`}>Open</Link></td>
    </tr>)}</tbody>
  </table></div>;
}
