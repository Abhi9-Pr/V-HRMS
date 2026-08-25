/** No feature sends anything over the notifications hub yet (see AGENTS.md — no HR features
 * this phase), so this shape is a reasonable placeholder a later feature's hub messages should
 * match, not a contract proven against a real sender. */
export interface NotificationMessage {
  type: string;
  title: string;
  body?: string;
  occurredAt: string;
}
