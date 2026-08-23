import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatTableModule } from '@angular/material/table';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiError } from '../../../core/http/api-error.model';
import { ConfirmDialogComponent } from '../../../shared/dialogs/confirm-dialog.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LeaveFacade } from '../data/leave.facade';

/** Not built on `vespera-data-table`: the approval-inbox endpoint returns the caller's whole
 * actionable set in one response (no server-side paging), and a manager's pending queue is small
 * enough that a plain table with a selection column is simpler and more honest here than forcing
 * a paginator over data that isn't actually paged. */
@Component({
  selector: 'vespera-approval-inbox',
  standalone: true,
  imports: [MatTableModule, MatCheckboxModule, MatButtonModule, MatChipsModule, ErrorStateComponent, DatePipe],
  templateUrl: './approval-inbox.component.html',
})
export class ApprovalInboxComponent implements OnInit {
  private readonly leaveFacade = inject(LeaveFacade);
  private readonly dialog = inject(MatDialog);

  readonly items = this.leaveFacade.approvalInbox;
  readonly loading = this.leaveFacade.loading;
  readonly error = this.leaveFacade.error;

  readonly displayedColumns = ['select', 'employeeId', 'dates', 'requestedDays', 'reason', 'delegate'];

  private readonly selectedIds = signal<ReadonlySet<string>>(new Set());
  readonly selectedCount = computed(() => this.selectedIds().size);
  readonly bulkError = signal<string | null>(null);
  readonly bulkBusy = signal(false);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.leaveFacade.loadApprovalInbox();
    this.selectedIds.set(new Set());
  }

  isSelected(id: string | undefined): boolean {
    return !!id && this.selectedIds().has(id);
  }

  toggle(id: string | undefined): void {
    if (!id) {
      return;
    }
    const next = new Set(this.selectedIds());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.selectedIds.set(next);
  }

  toggleAll(): void {
    const allIds = (this.items() ?? []).map((item) => item.leaveRequestId).filter((id): id is string => !!id);
    this.selectedIds.set(this.selectedIds().size === allIds.length ? new Set() : new Set(allIds));
  }

  bulkApprove(): void {
    const ids = [...this.selectedIds()];
    if (ids.length === 0) {
      return;
    }

    this.bulkBusy.set(true);
    this.bulkError.set(null);

    forkJoin(ids.map((id) => this.leaveFacade.approve(id).pipe(catchError((apiError: ApiError) => of(apiError))))).subscribe(
      (results) => {
        this.bulkBusy.set(false);
        const failures = results.filter((result): result is ApiError => result !== null);
        if (failures.length > 0) {
          this.bulkError.set(`${failures.length} of ${ids.length} approval(s) failed: ${failures[0].message}`);
        }
        this.reload();
      },
    );
  }

  bulkReject(): void {
    const ids = [...this.selectedIds()];
    if (ids.length === 0) {
      return;
    }

    this.dialog
      .open(ConfirmDialogComponent, {
        data: { title: 'Reject selected requests', message: `Reject ${ids.length} request(s)? Provide a reason below.`, danger: true },
      })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        const reason = window.prompt('Rejection reason') ?? '';
        if (!reason.trim()) {
          return;
        }

        this.bulkBusy.set(true);
        this.bulkError.set(null);

        forkJoin(
          ids.map((id) => this.leaveFacade.reject(id, reason).pipe(catchError((apiError: ApiError) => of(apiError)))),
        ).subscribe((results) => {
          this.bulkBusy.set(false);
          const failures = results.filter((result): result is ApiError => result !== null);
          if (failures.length > 0) {
            this.bulkError.set(`${failures.length} of ${ids.length} rejection(s) failed: ${failures[0].message}`);
          }
          this.reload();
        });
      });
  }
}
