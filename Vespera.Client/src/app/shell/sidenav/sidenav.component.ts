import { Component, computed, inject, output, ChangeDetectionStrategy } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NAV_TREE } from '../../core/navigation/nav-tree';
import { NavItem } from '../../core/navigation/nav-item.model';
import { AuthService, hasPermission } from 'vespera-shared';

interface NavSection {
  label: string;
  items: NavItem[];
}

const SECTION_BY_PREFIX: [string, string][] = [
  ['/dashboard', 'Overview'],
  ['/departments', 'People'],
  ['/expenses', 'Expenses'],
  ['/assets', 'Assets'],
  ['/recruitment', 'Recruitment'],
  ['/helpdesk', 'Helpdesk'],
  ['/leave', 'Leave'],
  ['/payroll', 'Payroll'],
];

function sectionForPath(path: string): string {
  const match = SECTION_BY_PREFIX.find(([prefix]) => path.startsWith(prefix));
  return match?.[1] ?? 'Other';
}

@Component({
  selector: 'vespera-sidenav',
  imports: [MatListModule, MatIconModule, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './sidenav.component.html',
})
export class SidenavComponent {
  private readonly auth = inject(AuthService);

  readonly navigate = output<void>();

  readonly sections = computed<NavSection[]>(() => {
    const visible = NAV_TREE.filter((item) => hasPermission(this.auth.permissions(), item.permissions));
    const grouped = new Map<string, NavItem[]>();

    for (const item of visible) {
      const section = sectionForPath(item.path);
      const items = grouped.get(section) ?? [];
      items.push(item);
      grouped.set(section, items);
    }

    const order = [...SECTION_BY_PREFIX.map(([, label]) => label), 'Other'];
    return order.filter((label) => grouped.has(label)).map((label) => ({ label, items: grouped.get(label)! }));
  });
}
