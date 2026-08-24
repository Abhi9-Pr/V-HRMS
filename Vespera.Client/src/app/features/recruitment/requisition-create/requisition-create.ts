import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { RecruitmentService } from '../recruitment.service';

@Component({
  selector: 'app-requisition-create',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './requisition-create.html',
  styleUrl: './requisition-create.scss',
})
export class RequisitionCreate {
  private readonly fb = inject(FormBuilder);

  protected readonly form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    departmentId: ['', Validators.required],
    openingsCount: [1, [Validators.required, Validators.min(1)]],
  });

  constructor(
    private readonly recruitment: RecruitmentService,
    private readonly router: Router,
  ) {}

  create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.recruitment.createRequisition(value).subscribe((result) => {
      void this.router.navigate(['/recruitment/requisitions', result.id]);
    });
  }
}
