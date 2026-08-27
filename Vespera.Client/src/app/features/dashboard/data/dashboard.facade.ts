import { Injectable, computed, inject, signal } from '@angular/core';
import {
  DashboardClient,
  DashboardWidgetEnvelopeDto,
  DashboardWidgetPreferenceDto,
  SaveDashboardLayoutRequest,
  WidgetPreferenceInput,
} from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { NotificationService } from '../../../core/services/notification.service';
import { PunchStateChangedPayload, ApprovalsCountChangedPayload, AnnouncementPublishedPayload } from '../../../core/services/notification.model';
import { WIDGET_REGISTRY } from '../widget-registry';
import { ShiftTrackerWidgetDto, PendingApprovalsWidgetDto } from '../widgets/dashboard-widget-payloads.model';
import { AnnouncementSummaryDto } from '../../../core/api/generated/api-client';

/**
 * Wraps the generated DashboardClient behind signals, same shape as every other facade (see
 * docs/CONTRIBUTING-frontend.md) — plus live patching from NotificationService's dashboard
 * observables, so a punch/approval/announcement update lands without a re-fetch of the whole
 * aggregate call.
 */
@Injectable({ providedIn: 'root' })
export class DashboardFacade {
  private readonly client = inject(DashboardClient);
  private readonly notifications = inject(NotificationService);

  private readonly layoutSignal = signal<DashboardWidgetPreferenceDto[]>([]);
  private readonly widgetsSignal = signal<DashboardWidgetEnvelopeDto[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);
  private liveSubscribed = false;

  readonly layout = this.layoutSignal.asReadonly();
  readonly widgets = this.widgetsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  /** Widgets in the caller's saved order, visible ones only, joined with their registry entry —
   * what the dashboard host actually iterates to render. */
  readonly renderableWidgets = computed(() => {
    const byKey = new Map(this.widgetsSignal().map((widget) => [widget.widgetKey, widget]));
    return this.layoutSignal()
      .filter((preference) => preference.isVisible && byKey.has(preference.widgetKey))
      .map((preference) => ({ preference, envelope: byKey.get(preference.widgetKey)!, registry: WIDGET_REGISTRY[preference.widgetKey!] }))
      .filter((entry) => !!entry.registry);
  });

  load(): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    this.subscribeToLiveUpdatesOnce();

    this.client.dashboard_Get().subscribe({
      next: (response) => {
        this.layoutSignal.set(response.layout ?? []);
        this.widgetsSignal.set(response.widgets ?? []);
        this.loadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.errorSignal.set(apiError);
        this.loadingSignal.set(false);
      },
    });
  }

  saveLayout(widgets: WidgetPreferenceInput[]) {
    this.layoutSignal.set(widgets.map((w) => ({ widgetKey: w.widgetKey, sortOrder: w.sortOrder, isVisible: w.isVisible, size: w.size })));
    return this.client.dashboard_SaveLayout({ widgets } as SaveDashboardLayoutRequest);
  }

  private subscribeToLiveUpdatesOnce(): void {
    if (this.liveSubscribed) {
      return;
    }
    this.liveSubscribed = true;

    this.notifications.dashboardPunchStateChanged.subscribe((payload) => this.patchShiftTracker(payload));
    this.notifications.dashboardApprovalsCountChanged.subscribe((payload) => this.patchPendingApprovals(payload));
    this.notifications.dashboardAnnouncementPublished.subscribe((payload) => this.prependAnnouncement(payload));
  }

  private patchShiftTracker(payload: PunchStateChangedPayload): void {
    this.updateWidgetData<ShiftTrackerWidgetDto>('shiftTracker', (current) => ({
      ...current,
      punchStatus: payload.status as ShiftTrackerWidgetDto['punchStatus'],
      firstIn: payload.status === 'In' ? (current.firstIn ?? payload.lastPunchAt) : current.firstIn,
      lastOut: payload.status === 'Out' ? payload.lastPunchAt : current.lastOut,
    }));
  }

  private patchPendingApprovals(payload: ApprovalsCountChangedPayload): void {
    this.updateWidgetData<PendingApprovalsWidgetDto>('pendingApprovals', (current) => ({
      ...current,
      totalCount: payload.pendingCount,
    }));
  }

  private prependAnnouncement(payload: AnnouncementPublishedPayload): void {
    const newItem: AnnouncementSummaryDto = {
      id: payload.announcementId,
      title: payload.title,
      body: '',
      priority: payload.priority,
      isPinned: payload.isPinned,
      publishAt: new Date(payload.publishAt),
      isAcknowledged: false,
    };

    this.updateWidgetData<AnnouncementSummaryDto[]>('announcements', (current) => [newItem, ...(current ?? [])]);
  }

  private updateWidgetData<T>(widgetKey: string, update: (current: T) => T): void {
    this.widgetsSignal.update((widgets) =>
      widgets.map((widget) =>
        widget.widgetKey === widgetKey && widget.success ? { ...widget, data: update(widget.data as T) } : widget,
      ),
    );
  }
}
