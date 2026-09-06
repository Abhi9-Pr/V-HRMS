import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { EmptyStateComponent } from '../../shared/states/empty-state.component';

@Component({
  selector: 'vespera-not-found',
  imports: [EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<vespera-empty-state
    icon="search_off"
    title="Page not found"
    description="The page you're looking for doesn't exist or has moved."
    actionLabel="Go home"
    (action)="goHome()"
  />`,
})
export class NotFoundComponent {
  private readonly router = inject(Router);

  goHome(): void {
    void this.router.navigateByUrl('/');
  }
}
