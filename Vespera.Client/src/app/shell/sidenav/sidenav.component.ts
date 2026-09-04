import { Component, computed, inject, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NAV_TREE } from '../../core/navigation/nav-tree';
import { AuthService, hasPermission } from 'vespera-shared';

@Component({
  selector: 'vespera-sidenav',
  standalone: true,
  imports: [MatListModule, MatIconModule, MatTooltipModule, RouterLink, RouterLinkActive],
  templateUrl: './sidenav.component.html',
})
export class SidenavComponent {
  private readonly auth = inject(AuthService);

  // Icon-only rail mode — the label moves into a hover tooltip instead of disappearing outright,
  // so the item is still discoverable without expanding the sidenav.
  readonly collapsed = input(false);

  readonly items = computed(() => NAV_TREE.filter((item) => hasPermission(this.auth.permissions(), item.permissions)));
}
