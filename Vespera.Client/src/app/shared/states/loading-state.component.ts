import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'vespera-loading-state',
  imports: [MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './loading-state.component.html',
})
export class LoadingStateComponent {
  @Input() title = 'Loading…';
}
