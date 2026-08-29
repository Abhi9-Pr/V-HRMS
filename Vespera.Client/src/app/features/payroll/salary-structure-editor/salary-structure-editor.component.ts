import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { catchError, of } from 'rxjs';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { ApiError, SalaryStructuresClient } from 'vespera-shared';

type FormulaKind = 'FixedAmount' | 'PercentageOfComponent' | 'SumOfComponents' | 'RemainderOfCtc';

interface EditableLine {
  componentId: string;
  formulaKind: FormulaKind;
  fixedAmount: number;
  referenceComponentId: string;
  percent: number;
  sumComponentIds: string;
}

function newLine(): EditableLine {
  return {
    componentId: '',
    formulaKind: 'FixedAmount',
    fixedAmount: 0,
    referenceComponentId: '',
    percent: 0,
    sumComponentIds: '',
  };
}

/**
 * A component + formula editor, not a visual graph — one row per line, a dropdown for which
 * closed-algebra formula kind it uses (see SalaryComponentFormula on the backend), and only the
 * fields that formula kind actually needs. Lines are a plain signal array rather than a nested
 * Angular FormArray, since each row's visible fields already change per formula kind — simpler to
 * own that logic directly than fight a dynamically-shaped reactive form group.
 */
@Component({
  selector: 'vespera-salary-structure-editor',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
    FormErrorComponent,
  ],
  templateUrl: './salary-structure-editor.component.html',
})
export class SalaryStructureEditorComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly client = inject(SalaryStructuresClient);

  readonly formulaKinds: FormulaKind[] = ['FixedAmount', 'PercentageOfComponent', 'SumOfComponents', 'RemainderOfCtc'];

  readonly form = this.formBuilder.nonNullable.group({
    employeeId: ['', Validators.required],
    monthlyCtc: [0, [Validators.required, Validators.min(0)]],
    validFrom: [new Date(), Validators.required],
  });

  readonly lines = signal<EditableLine[]>([newLine()]);

  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly createdId = signal<string | null>(null);

  addLine(): void {
    this.lines.update((lines) => [...lines, newLine()]);
  }

  removeLine(index: number): void {
    this.lines.update((lines) => lines.filter((_, i) => i !== index));
  }

  updateLine(index: number, patch: Partial<EditableLine>): void {
    this.lines.update((lines) => lines.map((line, i) => (i === index ? { ...line, ...patch } : line)));
  }

  submit(): void {
    if (this.form.invalid || this.lines().length === 0 || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const { employeeId, monthlyCtc, validFrom } = this.form.getRawValue();
    const lineRequests = this.lines().map((line) => ({
      componentId: line.componentId,
      formulaKind: line.formulaKind,
      fixedAmount: line.formulaKind === 'FixedAmount' ? line.fixedAmount : undefined,
      referenceComponentId: line.formulaKind === 'PercentageOfComponent' ? line.referenceComponentId : undefined,
      percent: line.formulaKind === 'PercentageOfComponent' ? line.percent : undefined,
      sumComponentIds:
        line.formulaKind === 'SumOfComponents'
          ? line.sumComponentIds
              .split(',')
              .map((id) => id.trim())
              .filter(Boolean)
          : undefined,
    }));

    // Named create8 by the generated client: "Create" collides with several other controllers'
    // own action names — see departments.facade.ts's list2 for the same NSwag numbering.
    this.client
      .create8({ employeeId, monthlyCtc, lines: lineRequests, validFrom, validTo: undefined })
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.submitting.set(false);
          return of(null);
        }),
      )
      .subscribe((id) => {
        if (id) {
          this.createdId.set(id);
          this.submitting.set(false);
        }
      });
  }
}
