import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { DashboardClient } from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { NotificationService } from '../../../core/services/notification.service';
import {
  AnnouncementPublishedPayload,
  ApprovalsCountChangedPayload,
  PunchStateChangedPayload,
} from '../../../core/services/notification.model';
import { DashboardFacade } from './dashboard.facade';

describe('DashboardFacade', () => {
  let client: jest.Mocked<Pick<DashboardClient, 'dashboard_Get' | 'dashboard_SaveLayout'>>;
  let notifications: {
    dashboardPunchStateChanged: Subject<PunchStateChangedPayload>;
    dashboardApprovalsCountChanged: Subject<ApprovalsCountChangedPayload>;
    dashboardAnnouncementPublished: Subject<AnnouncementPublishedPayload>;
  };
  let facade: DashboardFacade;

  beforeEach(() => {
    client = { dashboard_Get: jest.fn(), dashboard_SaveLayout: jest.fn() };
    notifications = {
      dashboardPunchStateChanged: new Subject(),
      dashboardApprovalsCountChanged: new Subject(),
      dashboardAnnouncementPublished: new Subject(),
    };

    TestBed.configureTestingModule({
      providers: [
        DashboardFacade,
        { provide: DashboardClient, useValue: client },
        { provide: NotificationService, useValue: notifications },
      ],
    });

    facade = TestBed.inject(DashboardFacade);
  });

  it('load() should populate layout and widgets on success', () => {
    client.dashboard_Get.mockReturnValue(
      of({
        layout: [{ widgetKey: 'shiftTracker', sortOrder: 0, isVisible: true, size: 'Medium' }],
        widgets: [{ widgetKey: 'shiftTracker', sortOrder: 0, size: 'Medium', success: true, data: { punchStatus: 'In' } }],
      } as never),
    );

    facade.load();

    expect(facade.loading()).toBe(false);
    expect(facade.layout()).toHaveLength(1);
    expect(facade.widgets()).toHaveLength(1);
  });

  it('load() should populate error on failure', () => {
    const apiError: ApiError = { status: 500, code: 'error', message: 'boom' };
    client.dashboard_Get.mockReturnValue(throwError(() => apiError));

    facade.load();

    expect(facade.error()).toEqual(apiError);
    expect(facade.loading()).toBe(false);
  });

  it('renderableWidgets() should exclude hidden widgets and unknown registry keys', () => {
    client.dashboard_Get.mockReturnValue(
      of({
        layout: [
          { widgetKey: 'shiftTracker', sortOrder: 0, isVisible: true, size: 'Medium' },
          { widgetKey: 'todos', sortOrder: 1, isVisible: false, size: 'Small' },
          { widgetKey: 'notRegistered', sortOrder: 2, isVisible: true, size: 'Small' },
        ],
        widgets: [
          { widgetKey: 'shiftTracker', sortOrder: 0, size: 'Medium', success: true, data: {} },
        ],
      } as never),
    );

    facade.load();

    const renderable = facade.renderableWidgets();
    expect(renderable).toHaveLength(1);
    expect(renderable[0].preference.widgetKey).toBe('shiftTracker');
  });

  it('a live punch-state update should patch the shiftTracker widget in place', () => {
    client.dashboard_Get.mockReturnValue(
      of({
        layout: [{ widgetKey: 'shiftTracker', sortOrder: 0, isVisible: true, size: 'Medium' }],
        widgets: [
          { widgetKey: 'shiftTracker', sortOrder: 0, size: 'Medium', success: true, data: { punchStatus: 'NotStarted', firstIn: null, lastOut: null } },
        ],
      } as never),
    );
    facade.load();

    notifications.dashboardPunchStateChanged.next({ employeeId: 'e1', status: 'In', lastPunchType: 'In', lastPunchAt: '2026-01-01T09:00:00Z' });

    const widget = facade.widgets().find((w) => w.widgetKey === 'shiftTracker')!;
    expect((widget.data as { punchStatus: string }).punchStatus).toBe('In');
  });

  it('a live approvals-count update should patch the pendingApprovals widget in place', () => {
    client.dashboard_Get.mockReturnValue(
      of({
        layout: [{ widgetKey: 'pendingApprovals', sortOrder: 0, isVisible: true, size: 'Small' }],
        widgets: [{ widgetKey: 'pendingApprovals', sortOrder: 0, size: 'Small', success: true, data: { totalCount: 2, countBySubjectType: {} } }],
      } as never),
    );
    facade.load();

    notifications.dashboardApprovalsCountChanged.next({ pendingCount: 5 });

    const widget = facade.widgets().find((w) => w.widgetKey === 'pendingApprovals')!;
    expect((widget.data as { totalCount: number }).totalCount).toBe(5);
  });

  it('saveLayout() should update the local layout signal and delegate to the client', (done) => {
    client.dashboard_SaveLayout.mockReturnValue(of(undefined));

    facade.saveLayout([{ widgetKey: 'shiftTracker', sortOrder: 0, isVisible: true, size: 'Medium' }]).subscribe(() => {
      expect(facade.layout()).toHaveLength(1);
      expect(client.dashboard_SaveLayout).toHaveBeenCalled();
      done();
    });
  });
});
