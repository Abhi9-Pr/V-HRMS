import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AssetsService } from '../assets.service';
import { ASSET_CONDITION_RATING_LABELS, AssetConditionRating } from '../assets.models';

@Component({
  selector: 'app-assignment-handover',
  imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './assignment-handover.html',
  styleUrl: './assignment-handover.scss',
})
export class AssignmentHandover {
  protected readonly signatureReference = signal<string | null>(null);
  protected readonly ratingOptions = [
    AssetConditionRating.Excellent,
    AssetConditionRating.Good,
    AssetConditionRating.Fair,
    AssetConditionRating.Poor,
    AssetConditionRating.Damaged,
  ];

  private readonly fb = inject(FormBuilder);
  private readonly assignmentId: string;

  protected readonly conditionReportForm = this.fb.nonNullable.group({
    rating: [AssetConditionRating.Good, Validators.required],
    notes: [''],
  });

  protected readonly returnForm = this.fb.nonNullable.group({
    condition: ['', Validators.required],
  });

  constructor(
    route: ActivatedRoute,
    private readonly assetsService: AssetsService,
    private readonly snackBar: MatSnackBar,
  ) {
    this.assignmentId = route.snapshot.paramMap.get('assignmentId')!;
  }

  onSignatureSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.assetsService.uploadHandoverSignature(this.assignmentId, file).subscribe((result) => {
      this.signatureReference.set(result.reference);
      this.snackBar.open('Handover signature captured.', 'Dismiss', { duration: 4000 });
    });
  }

  recordCondition(): void {
    if (this.conditionReportForm.invalid) {
      this.conditionReportForm.markAllAsTouched();
      return;
    }

    const value = this.conditionReportForm.getRawValue();
    this.assetsService
      .recordCondition(this.assignmentId, { rating: value.rating, notes: value.notes || null })
      .subscribe(() => this.snackBar.open('Condition report recorded.', 'Dismiss', { duration: 4000 }));
  }

  returnAsset(): void {
    if (this.returnForm.invalid) {
      this.returnForm.markAllAsTouched();
      return;
    }

    this.assetsService
      .returnAsset(this.assignmentId, this.returnForm.getRawValue())
      .subscribe(() => this.snackBar.open('Asset returned to stock.', 'Dismiss', { duration: 4000 }));
  }

  protected ratingLabel(rating: AssetConditionRating): string {
    return ASSET_CONDITION_RATING_LABELS[rating];
  }
}
