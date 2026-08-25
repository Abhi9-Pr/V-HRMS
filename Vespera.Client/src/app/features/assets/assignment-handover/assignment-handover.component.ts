import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { AssetConditionRating } from '../../../core/api/generated/api-client';
import { Permissions } from '../../../core/authorization/permissions';
import { ApiError } from '../../../core/http/api-error.model';
import { FileUploaderComponent } from '../../../shared/file-uploader/file-uploader.component';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { AssetsFacade } from '../data/assets.facade';
import { ASSET_CONDITION_RATING_LABELS } from '../assets.labels';

@Component({
  selector: 'vespera-assignment-handover',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, FileUploaderComponent, FormErrorComponent, PermissionButtonComponent],
  templateUrl: './assignment-handover.component.html',
})
export class AssignmentHandoverComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private readonly assetsFacade = inject(AssetsFacade);

  readonly Permissions = Permissions;
  readonly conditionRatings = Object.values(AssetConditionRating).filter((value): value is AssetConditionRating => typeof value === 'number');
  readonly conditionRatingLabel = (rating: AssetConditionRating): string => ASSET_CONDITION_RATING_LABELS[rating];

  readonly uploading = signal(false);
  readonly uploadError = signal<string | null>(null);
  readonly signatureReference = signal<string | null>(null);

  readonly recordingCondition = signal(false);
  readonly conditionError = signal<string | null>(null);
  readonly conditionRecorded = signal(false);

  readonly returning = signal(false);
  readonly returnError = signal<string | null>(null);
  readonly returned = signal(false);

  private assignmentId = '';
  assetId = '';

  readonly conditionForm = this.formBuilder.nonNullable.group({
    rating: [AssetConditionRating._1, [Validators.required]],
    notes: [''],
  });

  readonly returnForm = this.formBuilder.nonNullable.group({
    condition: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.assetId = this.route.snapshot.paramMap.get('id') ?? '';
    this.assignmentId = this.route.snapshot.paramMap.get('assignmentId') ?? '';
  }

  onFilesSelected(files: File[]): void {
    const file = files[0];
    if (!file) {
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);

    this.assetsFacade
      .uploadHandoverSignature(this.assignmentId, file)
      .pipe(
        catchError((apiError: ApiError) => {
          this.uploadError.set(apiError.message);
          this.uploading.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.uploading.set(false);
        this.signatureReference.set(result.reference ?? null);
      });
  }

  recordCondition(): void {
    if (this.conditionForm.invalid || this.recordingCondition()) {
      this.conditionForm.markAllAsTouched();
      return;
    }

    this.recordingCondition.set(true);
    this.conditionError.set(null);

    this.assetsFacade
      .recordCondition(this.assignmentId, this.conditionForm.getRawValue())
      .pipe(
        catchError((apiError: ApiError) => {
          this.conditionError.set(apiError.message);
          this.recordingCondition.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.recordingCondition.set(false);
        this.conditionRecorded.set(true);
      });
  }

  returnAsset(): void {
    if (this.returnForm.invalid || this.returning()) {
      this.returnForm.markAllAsTouched();
      return;
    }

    this.returning.set(true);
    this.returnError.set(null);

    this.assetsFacade
      .returnAsset(this.assignmentId, this.returnForm.getRawValue())
      .pipe(
        catchError((apiError: ApiError) => {
          this.returnError.set(apiError.message);
          this.returning.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.returning.set(false);
        this.returned.set(true);
      });
  }
}
