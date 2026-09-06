import { Component, Input, output, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'vespera-error-state',
  imports: [MatIconModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './error-state.component.html',
})
export class ErrorStateComponent {
  @Input() icon = 'error_outline';
  @Input() title = 'Something went wrong';
  @Input() description?: string;
  @Input() actionLabel = 'Retry';

  readonly action = output<void>();
}
