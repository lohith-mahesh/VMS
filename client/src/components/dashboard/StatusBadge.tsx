import type { VisitStatus } from '../../data/mockData'

export function StatusBadge({ status }: { status: VisitStatus }) { const tone = status === 'Inside' || status === 'Checked In' ? 'bg-[#e4f0e9] text-[#286844]' : status === 'Hold' || status === 'Waiting for Host' ? 'bg-[#f7eedb] text-[#85611d]' : status === 'No Show' ? 'bg-[#f5e4e2] text-[#94433d]' : 'bg-[#e7edf7] text-[var(--royal-blue)]'; return <span className={`inline-block whitespace-nowrap rounded-[4px] px-2.5 py-1 text-[10px] font-semibold uppercase tracking-wide ${tone}`}>{status}</span> }
