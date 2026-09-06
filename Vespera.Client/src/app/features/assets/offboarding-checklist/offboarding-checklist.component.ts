import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { AssetsFacade } from '../data/assets.facade';
import { ApiError, Permissions } from 'vespera-shared';

@Component({
    selector: 'vespera-offboarding-checklist',
    imports: [PermissionButtonComponent, ErrorStateComponent, LoadingStateComponent],
    templateUrl: './offboarding-checklist.component.html'
})
export class OffboardingChecklistComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly assetsFacade = inject(AssetsFacade);

  readonly Permissions = Permissions;

  readonly checklist = this.assetsFacade.checklist;
  readonly loading = this.assetsFacade.checklistLoading;
  readonly error = this.assetsFacade.checklistError;

  readonly completingIndex = signal<number | null>(null);
  readonly completeError = signal<string | null>(null);

  private employeeId = '';

  ngOnInit(): void {
    this.employeeId = this.route.snapshot.paramMap.get('employeeId') ?? '';
    this.reload();
  }

  reload(): void {
    this.assetsFacade.loadOffboardingChecklist(this.employeeId);
  }

  completeItem(itemIndex: number): void {
    const checklistId = this.checklist()?.id;
    if (!checklistId || this.completingIndex() !== null) {
      return;
    }

    this.completingIndex.set(itemIndex);
    this.completeError.set(null);

    this.assetsFacade
      .completeChecklistItem(checklistId, itemIndex)
      .pipe(
        catchError((apiError: ApiError) => {
          this.completeError.set(apiError.message);
          this.completingIndex.set(null);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.completingIndex.set(null);
        this.reload();
      });
  }
}
