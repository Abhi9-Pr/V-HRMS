import { Component, inject, output } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router } from '@angular/router';
import { NotificationService } from '../../core/services/notification.service';
import { ThemeService } from '../../core/services/theme.service';
import { AuthService, TenantResolutionService } from 'vespera-shared';

import { GlobalSearchBarComponent } from '../global-search/global-search-bar.component';

@Component({
  selector: 'vespera-topbar',
  standalone: true,
  imports: [MatToolbarModule, MatIconModule, MatButtonModule, MatMenuModule, MatBadgeModule, GlobalSearchBarComponent],
  templateUrl: './topbar.component.html',
})
export class TopbarComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly tenantResolution = inject(TenantResolutionService);

  readonly theme = inject(ThemeService);
  readonly notifications = inject(NotificationService);

  readonly menuToggle = output<void>();

  readonly session = this.auth.session;
  readonly tenantName = this.tenantResolution.getCachedTenantName();

  toggleTheme(): void {
    this.theme.toggle();
  }

  logout(): void {
    this.auth.logout().subscribe(() => void this.router.navigateByUrl('/auth/login'));
  }
}
