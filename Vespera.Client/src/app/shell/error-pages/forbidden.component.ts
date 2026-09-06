import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { ErrorStateComponent } from '../../shared/states/error-state.component';

@Component({
  selector: 'vespera-forbidden',
  imports: [ErrorStateComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<vespera-error-state
    icon="block"
    title="You don't have access to this page"
    description="Ask an administrator to grant the permission this page requires."
    actionLabel="Go back"
    (action)="goBack()"
  />`,
})
export class ForbiddenComponent {
  private readonly router = inject(Router);

  goBack(): void {
    void this.router.navigateByUrl('/');
  }
}
