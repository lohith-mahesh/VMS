export type UserRole = 'HostRequester' | 'ExportControl' | 'Reception';
export type VisitorType = 'Internal' | 'External';
export type ContractorType = 'NormalVisitor' | 'FacilitiesContractor' | 'GtreContractor';
export type VisitPurposeType = 'Technical' | 'NonTechnical' | 'Others';
export type RequestStatus = 'Draft' | 'VisitorFormPending' | 'VisitorFormSubmitted' | 'HostReview' | 'PendingEcReview' | 'PendingDocumentation' | 'DocumentationSubmitted' | 'EcReReviewRequired' | 'Approved' | 'PartiallyApproved' | 'Rejected' | 'Cancelled' | 'VisitProcessCompleted';
export type ScreeningDecision = 'Pending' | 'Approved' | 'Rejected' | 'NotApplicable';
export type VisitorClassification = 'None' | 'Vendor' | 'Visitor';
export type ReceptionStatus = 'Upcoming' | 'VerificationInProgress' | 'ReceptionVerification' | 'CheckedIn' | 'Completed' | 'NoShow' | 'EntryRejected' | 'Cancelled';
export type VerificationStatus = 'Pending' | 'Approved' | 'Rejected' | 'NotApplicable';
export type ArrivalStatus = 'NotArrived' | 'Early' | 'OnTime' | 'Late';

export interface Me {
  objectId: string;
  displayName: string;
  email: string;
  role: UserRole;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface RequestSummary {
  id: string;
  requestNumber: string;
  status: RequestStatus;
  visitorType: VisitorType;
  siteCode: string;
  mainHostName: string;
  hostDepartment: string;
  visitStart: string;
  visitEnd: string;
  visitorCount: number;
  submittedVisitorCount: number;
  updatedAt: string;
  rowVersion: string;
  visitorNames: string[];
  companyNames: string[];
}

export interface Asset {
  id: string;
  assetType: string;
  description: string;
  serialNumber: string;
  verificationStatus: VerificationStatus;
}

export interface DpsDocument {
  id: string;
  fileName: string;
  size: number;
  version: number;
  uploadedAt: string;
  uploadedBy: string;
}

export interface ReceptionRecord {
  id: string;
  visitDayId: string;
  status: ReceptionStatus;
  identityStatus: VerificationStatus;
  assetsStatus: VerificationStatus;
  arrivalStatus: ArrivalStatus;
  badgeId: string;
  badgeType: string;
  verificationRemarks: string;
  actualArrival?: string;
  actualDeparture?: string;
}

export interface Visitor {
  id: string;
  sequence: number;
  detailsStatus: 'Draft' | 'Submitted' | 'RevisionRequired';
  fullName: string;
  firstName: string;
  middleName: string;
  lastName: string;
  citizenship: string;
  designation: string;
  companyName: string;
  companyAddress: string;
  officeCity: string;
  officeCountry: string;
  phoneCountry: string;
  phoneDialCode: string;
  telephone: string;
  email: string;
  idType: string;
  otherIdType: string;
  screeningDecision: ScreeningDecision;
  classification: VisitorClassification;
  screeningReason: string;
  badgeType: string;
  assets: Asset[];
  dpsDocument?: DpsDocument;
  receptionRecords: ReceptionRecord[];
}

export interface VisitDay {
  id: string;
  date: string;
  expectedArrival: string;
  expectedDeparture: string;
}

export interface AuditEvent {
  id: string;
  action: string;
  actorId: string;
  actorRole: string;
  beforeJson: string;
  afterJson: string;
  details: string;
  occurredAt: string;
}

export interface InformationRequest {
  id: string;
  visitorId: string;
  fieldsJson: string;
  instructions: string;
  changesJson: string;
  status: 'Pending' | 'Resolved';
  createdAt: string;
  resolvedAt?: string;
}

export interface RequestDetail {
  id: string;
  requestNumber: string;
  requesterObjectId: string;
  status: RequestStatus;
  visitorType: VisitorType;
  contractorType: ContractorType;
  siteCode: string;
  purposeType: VisitPurposeType;
  purpose: string;
  areasToVisit: string;
  mainHostObjectId: string;
  mainHostName: string;
  hostDepartment: string;
  escortingHostObjectId: string;
  escortingHostName: string;
  visitStart: string;
  visitEnd: string;
  cancellationReason: string;
  visitors: Visitor[];
  visitDays: VisitDay[];
  auditEvents: AuditEvent[];
  informationRequests: InformationRequest[];
  screeningReviews: Array<{ id: string; visitorId: string; decision: ScreeningDecision; classification: VisitorClassification; comments: string; reviewerId: string; reviewedAt: string }>;
  scheduleChanges: Array<{ id: string; previousStart: string; previousEnd: string; newStart: string; newEnd: string; reason: string; changedBy: string; changedAt: string }>;
  rowVersion: string;
}

export interface Dashboard {
  totalVisitors: number;
  pendingActions: number;
  todayVisitors: number;
  checkedIn: number;
  upcoming: number;
  noShows: number;
  pendingEcReview: number;
  pendingDocumentation: number;
  approved: number;
  checkedOut: number;
  recentRequests: RequestSummary[];
  rejected: number;
  receptionRejections: number;
  visitorHistory: number;
  pendingReviewRequests: RequestSummary[];
  pendingDocumentationRequests: RequestSummary[];
  receptionItems: ReceptionIndexRow[];
}

export interface ReceptionIndexRow {
  requestId: string;
  visitorId: string;
  visitDayId: string;
  requestNumber: string;
  visitorName: string;
  companyName: string;
  hostName: string;
  escortName: string;
  personType: string;
  visitDate: string;
  scheduledToday: boolean;
  status: ReceptionStatus;
  screeningDecision: ScreeningDecision;
  identityStatus: VerificationStatus;
  assetsStatus: VerificationStatus;
  badgeId: string;
  badgeType: string;
  assetSummary: string;
}

export interface ReportRow {
  requestId: string;
  visitorId: string;
  visitDate: string;
  assetCount: number;
  requestNumber: string;
  visitorName: string;
  companyName: string;
  siteCode: string;
  hostName: string;
  hostDepartment: string;
  personType: string;
  visitStart: string;
  visitEnd: string;
  requestStatus: RequestStatus;
  screeningDecision: ScreeningDecision;
  receptionStatus: ReceptionStatus;
  identityStatus: VerificationStatus;
  assetsStatus: VerificationStatus;
  assetSummary: string;
  badgeType: string;
  badgeId: string;
  verificationRemarks: string;
}
