import { Component, Input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'vespera-empty-state',
    imports: [MatIconModule, MatButtonModule],
    templateUrl: './empty-state.component.html'
})
export class EmptyStateComponent {
  @Input() icon = 'inbox';
  @Input() title = 'Nothing here yet';
  @Input() description?: string;
  @Input() actionLabel?: string;

  readonly action = output<void>();
}
