import { Component, Input, OnInit, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface DateRange {
  start: Date | null;
  end: Date | null;
}

/** Thin wrapper over Angular Material's date range picker — a plain `{start, end}` in, the same
 * shape out via (rangeChange), so callers never touch Material's own FormGroup API directly. */
@Component({
    selector: 'vespera-date-range-picker',
    imports: [ReactiveFormsModule, MatDatepickerModule, MatFormFieldModule, MatInputModule],
    templateUrl: './date-range-picker.component.html'
})
export class DateRangePickerComponent implements OnInit {
  @Input() label = 'Date range';
  @Input() initialRange: DateRange = { start: null, end: null };

  readonly rangeChange = output<DateRange>();

  readonly range = new FormGroup({
    start: new FormControl<Date | null>(null),
    end: new FormControl<Date | null>(null),
  });

  ngOnInit(): void {
    this.range.setValue(this.initialRange, { emitEvent: false });
    this.range.valueChanges.subscribe((value) => this.rangeChange.emit({ start: value.start ?? null, end: value.end ?? null }));
  }
}
