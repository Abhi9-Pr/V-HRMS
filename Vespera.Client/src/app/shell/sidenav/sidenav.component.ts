import { Component, computed, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { hasPermission } from '../../core/auth/permission.util';
import { NAV_TREE } from '../../core/navigation/nav-tree';

@Component({
  selector: 'vespera-sidenav',
  standalone: true,
  imports: [MatListModule, MatIconModule, RouterLink, RouterLinkActive],
  templateUrl: './sidenav.component.html',
})
export class SidenavComponent {
  private readonly auth = inject(AuthService);

  readonly items = computed(() => NAV_TREE.filter((item) => hasPermission(this.auth.permissions(), item.permissions)));
}
