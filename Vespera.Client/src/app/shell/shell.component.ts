import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatSidenavModule } from '@angular/material/sidenav';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { BreadcrumbComponent } from './breadcrumb/breadcrumb.component';
import { SidenavComponent } from './sidenav/sidenav.component';
import { TopbarComponent } from './topbar/topbar.component';

@Component({
    selector: 'vespera-shell',
    imports: [MatSidenavModule, RouterOutlet, TopbarComponent, SidenavComponent, BreadcrumbComponent],
    templateUrl: './shell.component.html'
})
export class ShellComponent {
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly router = inject(Router);

  readonly isMobile = toSignal(
    this.breakpointObserver.observe(Breakpoints.Handset).pipe(map((result) => result.matches)),
    { initialValue: false },
  );

  readonly sidenavOpened = signal(false);

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        if (this.isMobile()) {
          this.sidenavOpened.set(false);
        }
      });
  }

  toggleSidenav(): void {
    this.sidenavOpened.update((opened) => !opened);
  }

  onSidenavNavigate(): void {
    if (this.isMobile()) {
      this.sidenavOpened.set(false);
    }
  }

  onSidenavClosed(): void {
    this.sidenavOpened.set(false);
  }
}
