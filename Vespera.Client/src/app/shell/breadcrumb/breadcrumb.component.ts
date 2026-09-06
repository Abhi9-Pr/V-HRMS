import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, ActivatedRouteSnapshot, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, startWith } from 'rxjs';

interface Crumb {
  label: string;
  url: string;
}

/** Derives from each activated route's `data['breadcrumb']` — a route with no breadcrumb entry
 * contributes nothing, so leaf routes that don't opt in stay invisible here. */
@Component({
  selector: 'vespera-breadcrumb',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './breadcrumb.component.html',
})
export class BreadcrumbComponent {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly crumbs = signal<Crumb[]>([]);

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        startWith(null),
      )
      .subscribe(() => this.crumbs.set(this.buildCrumbs()));
  }

  private buildCrumbs(): Crumb[] {
    const crumbs: Crumb[] = [];
    let route: ActivatedRouteSnapshot | null = this.route.snapshot.root;
    let url = '';

    while (route) {
      url += route.url.map((segment) => `/${segment.path}`).join('');
      const label = route.data['breadcrumb'] as string | undefined;
      if (label) {
        crumbs.push({ label, url });
      }

      route = route.firstChild;
    }

    return crumbs;
  }
}
