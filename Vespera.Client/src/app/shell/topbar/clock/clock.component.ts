import { Component } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { interval, map, startWith } from 'rxjs';

@Component({
  selector: 'vespera-clock',
  standalone: true,
  template: `<span class="text-sm text-text-muted tabular-nums">{{ now() }}</span>`,
})
export class ClockComponent {
  // toSignal tears the interval subscription down on destroy automatically — no manual
  // OnDestroy/DestroyRef cleanup needed. startWith fires an immediate value so the clock doesn't
  // show blank for the first second while waiting for interval's first tick.
  readonly now = toSignal(
    interval(1000).pipe(
      startWith(0),
      map(() => new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })),
    ),
    { initialValue: '' },
  );
}
