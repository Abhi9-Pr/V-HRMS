import { Injectable, computed, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenStorageService } from '../auth/token-storage.service';
import {
  AnnouncementPublishedPayload,
  ApprovalsCountChangedPayload,
  NotificationMessage,
  PunchStateChangedPayload,
} from './notification.model';

/** Connects to NotificationHub once the user is authenticated (call start() after login/session
 * restore, stop() on logout — the shell owns that lifecycle). The access token is read fresh on
 * every (re)connect attempt via accessTokenFactory, since SignalR reconnects can happen well
 * after the token used at initial connect has rotated. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly tokenStorage = inject(TokenStorageService);

  private connection: signalR.HubConnection | null = null;
  private readonly messages$ = new Subject<NotificationMessage>();
  private readonly unreadCountSignal = signal(0);

  private readonly punchStateChanged$ = new Subject<PunchStateChangedPayload>();
  private readonly approvalsCountChanged$ = new Subject<ApprovalsCountChangedPayload>();
  private readonly announcementPublished$ = new Subject<AnnouncementPublishedPayload>();

  readonly notifications = this.messages$.asObservable();
  readonly unreadCount = computed(() => this.unreadCountSignal());

  /** The landing dashboard's live updates — see notification.model.ts. */
  readonly dashboardPunchStateChanged = this.punchStateChanged$.asObservable();
  readonly dashboardApprovalsCountChanged = this.approvalsCountChanged$.asObservable();
  readonly dashboardAnnouncementPublished = this.announcementPublished$.asObservable();

  start(): void {
    if (this.connection) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.notificationsHubUrl, {
        accessTokenFactory: () => this.tokenStorage.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('notification', (message: NotificationMessage) => {
      this.messages$.next(message);
      this.unreadCountSignal.update((count) => count + 1);
    });

    this.connection.on('dashboard:punchStateChanged', (payload: PunchStateChangedPayload) => {
      this.punchStateChanged$.next(payload);
    });
    this.connection.on('dashboard:approvalsCountChanged', (payload: ApprovalsCountChangedPayload) => {
      this.approvalsCountChanged$.next(payload);
    });
    this.connection.on('dashboard:announcementPublished', (payload: AnnouncementPublishedPayload) => {
      this.announcementPublished$.next(payload);
    });

    this.connection.start().catch(() => {
      // Best-effort: the shell still works without live notifications (e.g. hub unreachable).
    });
  }

  stop(): void {
    void this.connection?.stop();
    this.connection = null;
    this.unreadCountSignal.set(0);
  }

  markAllRead(): void {
    this.unreadCountSignal.set(0);
  }
}
