import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { catchError, of } from 'rxjs';
import { AnnouncementAudienceScope, AnnouncementPriority, LocationDto } from '../../../../core/api/generated/api-client';
import { ApiError } from '../../../../core/http/api-error.model';
import { DepartmentsFacade } from '../../../departments/data/departments.facade';
import { announcementAudienceScopeLabels, announcementPriorityLabels } from '../../dashboard.labels';
import { AnnouncementsFacade } from '../../data/announcements.facade';
import { LocationsFacade } from '../../data/locations.facade';

@Component({
  selector: 'vespera-create-announcement-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
  ],
  templateUrl: './create-announcement-dialog.component.html',
})
export class CreateAnnouncementDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly announcementsFacade = inject(AnnouncementsFacade);
  private readonly departmentsFacade = inject(DepartmentsFacade);
  private readonly locationsFacade = inject(LocationsFacade);
  private readonly dialogRef = inject(MatDialogRef<CreateAnnouncementDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly locations = signal<LocationDto[]>([]);
  readonly departments = this.departmentsFacade.departments;

  readonly scopes = Object.values(AnnouncementAudienceScope).filter((v): v is AnnouncementAudienceScope => typeof v === 'number');
  readonly priorities = Object.values(AnnouncementPriority).filter((v): v is AnnouncementPriority => typeof v === 'number');
  readonly scopeLabel = (scope: AnnouncementAudienceScope): string => announcementAudienceScopeLabels[scope];
  readonly priorityLabel = (priority: AnnouncementPriority): string => announcementPriorityLabels[priority];

  readonly form = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(256)]],
    body: ['', [Validators.required]],
    audienceScope: [AnnouncementAudienceScope._0, [Validators.required]],
    targetDepartmentId: [''],
    targetLocationId: [''],
    priority: [AnnouncementPriority._1, [Validators.required]],
    publishAt: [new Date(), [Validators.required]],
    expiresAt: [null as Date | null],
    publishImmediately: [true],
  });

  constructor() {
    this.departmentsFacade.load({ page: 1, pageSize: 100, sortDescending: false });
    this.locationsFacade.list().subscribe((locations) => this.locations.set(locations));
  }

  get isDepartmentScope(): boolean {
    return this.form.controls.audienceScope.value === AnnouncementAudienceScope._1;
  }

  get isLocationScope(): boolean {
    return this.form.controls.audienceScope.value === AnnouncementAudienceScope._2;
  }

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const value = this.form.getRawValue();

    this.announcementsFacade
      .create({
        title: value.title,
        body: value.body,
        audienceScope: value.audienceScope,
        targetDepartmentId: this.isDepartmentScope ? value.targetDepartmentId || undefined : undefined,
        targetLocationId: this.isLocationScope ? value.targetLocationId || undefined : undefined,
        priority: value.priority,
        publishAt: value.publishAt,
        expiresAt: value.expiresAt ?? undefined,
        publishImmediately: value.publishImmediately,
      })
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.saving.set(false);
          return of(null);
        }),
      )
      .subscribe((id) => {
        if (id) {
          this.dialogRef.close(true);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
