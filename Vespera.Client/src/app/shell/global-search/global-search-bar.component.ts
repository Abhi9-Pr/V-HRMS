import { Component, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

/** Placeholder per the brief — no search backend exists yet. Captures the input and renders an
 * empty-state panel so the shell's layout is final; wiring a real search index is a later-phase
 * decision. */
@Component({
  selector: 'vespera-global-search-bar',
  imports: [FormsModule, MatIconModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './global-search-bar.component.html',
})
export class GlobalSearchBarComponent {
  query = '';
}
