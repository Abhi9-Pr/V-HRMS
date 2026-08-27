import { Component, Input, computed, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TimezoneDatePipe } from '../../../../shared/pipes/timezone-date.pipe';
import { CelebrationSummaryDto } from '../dashboard-widget-payloads.model';

/**
 * Follows the WAI-ARIA APG carousel pattern: the viewport is `aria-roledescription="carousel"`
 * with an `aria-live="polite"` region announcing the current slide, each slide is
 * `aria-roledescription="slide"` labelled "N of M", and Previous/Next are real buttons (not just
 * clickable divs) so the whole thing is keyboard- and screen-reader-operable, not mouse-only.
 */
@Component({
  selector: 'vespera-celebrations-carousel-widget',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, TimezoneDatePipe],
  templateUrl: './celebrations-carousel-widget.component.html',
})
export class CelebrationsCarouselWidgetComponent {
  private readonly itemsSignal = signal<CelebrationSummaryDto[]>([]);
  readonly items = this.itemsSignal.asReadonly();

  @Input({ required: true })
  set data(value: CelebrationSummaryDto[]) {
    this.itemsSignal.set(value ?? []);
    if (this.indexSignal() >= (value?.length ?? 0)) {
      this.indexSignal.set(0);
    }
  }

  private readonly indexSignal = signal(0);
  readonly index = this.indexSignal.asReadonly();

  readonly current = computed(() => this.items()[this.index()] ?? null);
  readonly slideLabel = computed(() => `${this.index() + 1} of ${this.items().length}`);

  previous(): void {
    const count = this.items().length;
    if (count === 0) {
      return;
    }
    this.indexSignal.update((i) => (i - 1 + count) % count);
  }

  next(): void {
    const count = this.items().length;
    if (count === 0) {
      return;
    }
    this.indexSignal.update((i) => (i + 1) % count);
  }

  icon(celebrationType: string): string {
    return celebrationType === 'Birthday' ? 'cake' : 'military_tech';
  }
}
