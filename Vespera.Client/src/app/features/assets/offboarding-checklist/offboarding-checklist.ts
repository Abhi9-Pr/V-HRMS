import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AssetsService } from '../assets.service';
import { OffboardingChecklistDto } from '../assets.models';

@Component({
  selector: 'app-offboarding-checklist',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatListModule, MatProgressSpinnerModule],
  templateUrl: './offboarding-checklist.html',
  styleUrl: './offboarding-checklist.scss',
})
export class OffboardingChecklist {
  protected readonly checklist = signal<OffboardingChecklistDto | null>(null);
  protected readonly loading = signal(false);
  protected readonly notFound = signal(false);

  private readonly fb = inject(FormBuilder);

  protected readonly employeeIdForm = this.fb.nonNullable.group({
    employeeId: ['', Validators.required],
  });

  constructor(private readonly assetsService: AssetsService) {}

  load(): void {
    if (this.employeeIdForm.invalid) {
      this.employeeIdForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.notFound.set(false);
    this.assetsService.getChecklistForEmployee(this.employeeIdForm.getRawValue().employeeId).subscribe({
      next: (checklist) => {
        this.checklist.set(checklist);
        this.loading.set(false);
      },
      error: () => {
        this.checklist.set(null);
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }

  completeItem(index: number): void {
    const checklist = this.checklist();
    if (!checklist) {
      return;
    }

    this.assetsService.completeChecklistItem(checklist.id, index).subscribe(() => this.load());
  }
}
