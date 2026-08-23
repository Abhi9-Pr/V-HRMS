import { Injectable, computed, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenStorageService } from '../auth/token-storage.service';
import { NotificationMessage } from './notification.model';

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

  readonly notifications = this.messages$.asObservable();
  readonly unreadCount = computed(() => this.unreadCountSignal());

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
