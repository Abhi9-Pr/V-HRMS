import { AnnouncementAudienceScope, AnnouncementPriority, RsvpResponse, TodoUrgency } from 'vespera-shared';

/** Enums cross the wire as their numeric ordinal (see docs/CONTRIBUTING-frontend.md — no
 * JsonStringEnumConverter is configured API-wide), so every enum gets a label map here, the same
 * pattern as assets.labels.ts / helpdesk.labels.ts. */
export const announcementAudienceScopeLabels: Record<AnnouncementAudienceScope, string> = {
  [AnnouncementAudienceScope._0]: 'All employees',
  [AnnouncementAudienceScope._1]: 'Department',
  [AnnouncementAudienceScope._2]: 'Location',
};

export const announcementPriorityLabels: Record<AnnouncementPriority, string> = {
  [AnnouncementPriority._0]: 'Low',
  [AnnouncementPriority._1]: 'Normal',
  [AnnouncementPriority._2]: 'High',
  [AnnouncementPriority._3]: 'Critical',
};

export const todoUrgencyLabels: Record<TodoUrgency, string> = {
  [TodoUrgency._0]: 'Low',
  [TodoUrgency._1]: 'Medium',
  [TodoUrgency._2]: 'High',
};

export const rsvpResponseLabels: Record<RsvpResponse, string> = {
  [RsvpResponse._0]: 'Yes',
  [RsvpResponse._1]: 'No',
  [RsvpResponse._2]: 'Maybe',
};

/** String-keyed variants for fields the backend already renders as `.ToString()` (e.g.
 * AnnouncementSummaryDto.priority, TodoItemDto.urgency) rather than the raw numeric enum. */
export const priorityLabelByName: Record<string, string> = {
  Low: 'Low',
  Normal: 'Normal',
  High: 'High',
  Critical: 'Critical',
};
export const urgencyLabelByName: Record<string, string> = { Low: 'Low', Medium: 'Medium', High: 'High' };
