import { Component, effect, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationService } from './core/services/notification.service';
import { ThemeService } from './core/services/theme.service';
import { AuthService } from 'vespera-shared';

@Component({
    selector: 'vespera-root',
    imports: [RouterOutlet],
    templateUrl: './app.component.html'
})
export class AppComponent {
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  // ThemeService applies its stored/system theme as a side effect of construction — injecting it
  // here (root component, constructed once at bootstrap) is what makes that actually happen.
  private readonly theme = inject(ThemeService);

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.notifications.start();
      } else {
        this.notifications.stop();
      }
    });
  }
}
