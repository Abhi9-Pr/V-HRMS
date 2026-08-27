import { Type } from '@angular/core';
import { AnnouncementsBoardWidgetComponent } from './widgets/announcements-board/announcements-board-widget.component';
import { CelebrationsCarouselWidgetComponent } from './widgets/celebrations-carousel/celebrations-carousel-widget.component';
import { CorporateEventsWidgetComponent } from './widgets/corporate-events/corporate-events-widget.component';
import { LeaveBalanceWidgetComponent } from './widgets/leave-balance/leave-balance-widget.component';
import { PayslipQuickLinkWidgetComponent } from './widgets/payslip-quick-link/payslip-quick-link-widget.component';
import { PendingApprovalsWidgetComponent } from './widgets/pending-approvals/pending-approvals-widget.component';
import { ShiftTrackerWidgetComponent } from './widgets/shift-tracker/shift-tracker-widget.component';
import { TodoListWidgetComponent } from './widgets/todo-list/todo-list-widget.component';

export interface WidgetRegistryEntry {
  component: Type<unknown>;
  title: string;
  icon: string;
}

/**
 * The Angular side of the widget framework's OCP contract: adding a widget to the landing
 * dashboard means writing one standalone component and adding one entry here — nothing in
 * dashboard-host.component.ts changes. The key must match the backend
 * `IDashboardWidgetProvider.WidgetKey` exactly (see docs/CONTRIBUTING-slices.md's widget
 * framework notes) — a mismatch just means that widget's envelope has nowhere to render, not a
 * build error, so keep the two lists side by side when adding one.
 */
export const WIDGET_REGISTRY: Record<string, WidgetRegistryEntry> = {
  shiftTracker: { component: ShiftTrackerWidgetComponent, title: 'My shift', icon: 'schedule' },
  announcements: { component: AnnouncementsBoardWidgetComponent, title: 'Announcements', icon: 'campaign' },
  celebrations: { component: CelebrationsCarouselWidgetComponent, title: 'Celebrations', icon: 'cake' },
  corporateEvents: { component: CorporateEventsWidgetComponent, title: 'Upcoming events', icon: 'event' },
  todos: { component: TodoListWidgetComponent, title: 'My to-dos', icon: 'checklist' },
  pendingApprovals: { component: PendingApprovalsWidgetComponent, title: 'Pending approvals', icon: 'fact_check' },
  leaveBalance: { component: LeaveBalanceWidgetComponent, title: 'Leave balance', icon: 'beach_access' },
  payslip: { component: PayslipQuickLinkWidgetComponent, title: 'Latest payslip', icon: 'receipt_long' },
};
