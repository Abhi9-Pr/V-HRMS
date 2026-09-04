import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, inject, signal } from '@angular/core';
import { MatSidenavModule } from '@angular/material/sidenav';
import { RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { BreadcrumbComponent } from './breadcrumb/breadcrumb.component';
import { SidenavComponent } from './sidenav/sidenav.component';
import { TopbarComponent } from './topbar/topbar.component';

const SIDENAV_COLLAPSED_KEY = 'vespera.sidenavCollapsed';

@Component({
  selector: 'vespera-shell',
  standalone: true,
  imports: [MatSidenavModule, RouterOutlet, TopbarComponent, SidenavComponent, BreadcrumbComponent],
  templateUrl: './shell.component.html',
})
export class ShellComponent {
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly isMobile = toSignal(
    this.breakpointObserver.observe(Breakpoints.Handset).pipe(map((result) => result.matches)),
    { initialValue: false },
  );

  // Mobile: the sidenav is an overlay drawer, opened/closed by the topbar's toggle button.
  readonly sidenavOpened = signal(true);

  // Desktop: the sidenav is always visible but can collapse to an icon-only rail — a distinct
  // concept from the mobile drawer's open/closed state, so it gets its own signal rather than
  // overloading sidenavOpened with two different meanings. Persisted like ThemeService's
  // preference: a UI convenience, not security-sensitive, so localStorage is the right store.
  readonly sidenavCollapsed = signal(localStorage.getItem(SIDENAV_COLLAPSED_KEY) === 'true');

  toggleSidenav(): void {
    if (this.isMobile()) {
      this.sidenavOpened.update((opened) => !opened);
    } else {
      this.sidenavCollapsed.update((collapsed) => {
        const next = !collapsed;
        localStorage.setItem(SIDENAV_COLLAPSED_KEY, String(next));
        return next;
      });
    }
  }
}
