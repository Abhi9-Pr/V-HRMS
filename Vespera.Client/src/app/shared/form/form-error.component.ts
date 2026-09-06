import { Component, DestroyRef, Input, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl } from '@angular/forms';
import { VALIDATION_MESSAGES } from './validation-messages';

/** `<vespera-form-error [control]="form.controls.email" />` — shows the first active validation
 * error for a control, only once it's been touched (so a blank required field isn't flagged
 * before the user has had a chance to fill it in). */
@Component({
  selector: 'vespera-form-error',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './form-error.component.html',
})
export class FormErrorComponent {
  private readonly destroyRef = inject(DestroyRef);

  private readonly controlSignal = signal<AbstractControl | null>(null);
  private readonly tick = signal(0);

  @Input({ required: true }) set control(control: AbstractControl) {
    this.controlSignal.set(control);
    // AbstractControl.events (Angular 14+) fires for touched/pristine/status/value changes alike
    // — statusChanges/valueChanges alone would miss a plain markAsTouched() with no value change,
    // which is exactly what a blur or a failed submit does.
    control.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.tick.update((value) => value + 1));
  }

  @Input() override: Record<string, string> = {};

  readonly message = computed(() => {
    this.tick();
    const control = this.controlSignal();

    if (!control || !control.touched || !control.errors) {
      return null;
    }

    const [firstKey] = Object.keys(control.errors);
    return this.override[firstKey] ?? VALIDATION_MESSAGES[firstKey] ?? 'This value is invalid.';
  });
}
