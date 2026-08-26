import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { catchError, of } from 'rxjs';
import { InvestmentDeclarationQueueItemDto, InvestmentDeclarationReviewClient } from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { EmptyStateComponent } from '../../../shared/states/empty-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';

/** Finance's review queue: every Submitted declaration with at least one still-Pending line.
 * Approving/rejecting a specific line index is deliberately blunt (no per-line detail view here
 * yet) — reviewing the last Pending line auto-verifies the declaration server-side, so this screen
 * doesn't need its own "verify" action. */
@Component({
  selector: 'vespera-investment-declaration-review',
  standalone: true,
  imports: [MatTableModule, MatButtonModule, MatFormFieldModule, MatInputModule, LoadingStateComponent, EmptyStateComponent],
  templateUrl: './investment-declaration-review.component.html',
})
export class InvestmentDeclarationReviewComponent implements OnInit {
  private readonly client = inject(InvestmentDeclarationReviewClient);

  readonly items = signal<InvestmentDeclarationQueueItemDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly displayedColumns = ['employeeId', 'financialYear', 'lineCount', 'pendingLineCount', 'actions'];

  ngOnInit(): void {
    this.reload();
  }

  private reload(): void {
    this.loading.set(true);
    this.client
      .getReviewQueue(1, 50, undefined, undefined)
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.loading.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        this.items.set(result?.items ?? []);
        this.loading.set(false);
      });
  }

  review(declarationId: string, lineIndex: number, approved: boolean, comment: string | null): void {
    this.client
      .reviewLine(declarationId, lineIndex, { approved, comment: comment ?? undefined })
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          return of(null);
        }),
      )
      .subscribe(() => this.reload());
  }
}
