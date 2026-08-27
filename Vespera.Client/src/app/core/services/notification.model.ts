export interface NotificationMessage {
  type: string;
  title: string;
  body?: string;
  occurredAt: string;
}

/** The landing dashboard's structured (non-toast) push messages — see
 * `DashboardRealtimeBroadcaster` on the API side. Sent over the same NotificationHub connection
 * as `NotificationMessage`, under their own event names (`dashboard:punchStateChanged`, etc.). */
export interface PunchStateChangedPayload {
  employeeId: string;
  status: string;
  lastPunchType: string | null;
  lastPunchAt: string | null;
}

export interface ApprovalsCountChangedPayload {
  pendingCount: number;
}

export interface AnnouncementPublishedPayload {
  announcementId: string;
  title: string;
  priority: string;
  isPinned: boolean;
  publishAt: string;
}
