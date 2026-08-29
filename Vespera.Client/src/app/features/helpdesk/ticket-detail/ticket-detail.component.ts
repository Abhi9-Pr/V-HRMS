import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { FileUploaderComponent } from '../../../shared/file-uploader/file-uploader.component';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { TimezoneDatePipe } from '../../../shared/pipes/timezone-date.pipe';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { TICKET_PRIORITY_LABELS, TICKET_STATUS_LABELS } from '../helpdesk.labels';
import { ApiError, Permissions, TicketCommentDto, TicketDto, TicketPriority, TicketStatus } from 'vespera-shared';

interface ThreadedComment {
  comment: TicketCommentDto;
  depth: number;
}

/**
 * Status-gated action buttons mirror Vespera.Domain/Helpdesk/Ticket.cs's own guard conditions
 * exactly, same discipline as recruitment's requisition-detail / assets' recovery-dashboard:
 * Open/InProgress/OnHold -> Assign or Resolve; Resolved -> Close; Closed -> Rate satisfaction.
 */
@Component({
  selector: 'vespera-ticket-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    FileUploaderComponent,
    FormErrorComponent,
    PermissionButtonComponent,
    TimezoneDatePipe,
    ErrorStateComponent,
    LoadingStateComponent,
  ],
  templateUrl: './ticket-detail.component.html',
})
export class TicketDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private readonly helpdeskFacade = inject(HelpdeskFacade);

  readonly Permissions = Permissions;
  readonly TicketStatus = TicketStatus;
  readonly priorityLabel = (priority: TicketPriority): string => TICKET_PRIORITY_LABELS[priority];
  readonly statusLabel = (status: TicketStatus): string => TICKET_STATUS_LABELS[status];

  readonly ticket = this.helpdeskFacade.ticket;
  readonly loading = this.helpdeskFacade.ticketLoading;
  readonly error = this.helpdeskFacade.ticketError;

  readonly threadedComments = computed<ThreadedComment[]>(() =>
    this.buildThreadedComments(this.ticket()?.comments ?? []),
  );

  readonly replyingToId = signal<string | null>(null);
  readonly pendingAttachmentReference = signal<string | null>(null);
  readonly uploading = signal(false);
  readonly uploadError = signal<string | null>(null);
  readonly postingComment = signal(false);
  readonly commentError = signal<string | null>(null);

  readonly assignForm = this.formBuilder.nonNullable.group({
    employeeId: ['', [Validators.required]],
  });

  readonly commentForm = this.formBuilder.nonNullable.group({
    body: ['', [Validators.required]],
    isInternal: [false],
  });

  readonly ratingForm = this.formBuilder.nonNullable.group({
    rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
  });

  readonly assigning = signal(false);
  readonly assignError = signal<string | null>(null);

  readonly actionInFlight = signal(false);
  readonly actionError = signal<string | null>(null);

  private ticketId = '';

  ngOnInit(): void {
    this.ticketId = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.helpdeskFacade.loadTicketById(this.ticketId);
  }

  canAssign(ticket: TicketDto): boolean {
    return ticket.status !== TicketStatus._3 && ticket.status !== TicketStatus._4;
  }

  canResolve(ticket: TicketDto): boolean {
    return ticket.status !== TicketStatus._3 && ticket.status !== TicketStatus._4;
  }

  canClose(ticket: TicketDto): boolean {
    return ticket.status === TicketStatus._3;
  }

  canRate(ticket: TicketDto): boolean {
    return ticket.status === TicketStatus._4 && ticket.satisfactionRating === undefined;
  }

  assign(): void {
    if (this.assignForm.invalid || this.assigning()) {
      this.assignForm.markAllAsTouched();
      return;
    }

    this.assigning.set(true);
    this.assignError.set(null);

    this.helpdeskFacade
      .assignTicket(this.ticketId, this.assignForm.getRawValue())
      .pipe(
        catchError((apiError: ApiError) => {
          this.assignError.set(apiError.message);
          this.assigning.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.assigning.set(false);
        this.assignForm.reset({ employeeId: '' });
        this.reload();
      });
  }

  resolve(): void {
    this.runAction(this.helpdeskFacade.resolveTicket(this.ticketId));
  }

  close(): void {
    this.runAction(this.helpdeskFacade.closeTicket(this.ticketId));
  }

  rate(): void {
    if (this.ratingForm.invalid || this.actionInFlight()) {
      this.ratingForm.markAllAsTouched();
      return;
    }

    this.runAction(this.helpdeskFacade.rateSatisfaction(this.ticketId, this.ratingForm.getRawValue()));
  }

  private runAction(action$: ReturnType<HelpdeskFacade['resolveTicket']>): void {
    this.actionInFlight.set(true);
    this.actionError.set(null);

    action$
      .pipe(
        catchError((apiError: ApiError) => {
          this.actionError.set(apiError.message);
          this.actionInFlight.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.actionInFlight.set(false);
        this.reload();
      });
  }

  startReply(commentId: string): void {
    this.replyingToId.set(commentId);
  }

  cancelReply(): void {
    this.replyingToId.set(null);
  }

  onFilesSelected(files: File[]): void {
    const file = files[0];
    if (!file) {
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);

    this.helpdeskFacade
      .uploadAttachment(this.ticketId, file)
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
        this.pendingAttachmentReference.set(result.reference ?? null);
      });
  }

  postComment(): void {
    if (this.commentForm.invalid || this.postingComment()) {
      this.commentForm.markAllAsTouched();
      return;
    }

    this.postingComment.set(true);
    this.commentError.set(null);

    const { body, isInternal } = this.commentForm.getRawValue();
    const attachmentReference = this.pendingAttachmentReference();

    this.helpdeskFacade
      .addComment(this.ticketId, {
        body,
        isInternal,
        parentCommentId: this.replyingToId() ?? undefined,
        attachmentReferences: attachmentReference ? [attachmentReference] : undefined,
      })
      .pipe(
        catchError((apiError: ApiError) => {
          this.commentError.set(apiError.message);
          this.postingComment.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.postingComment.set(false);
        this.commentForm.reset({ body: '', isInternal: false });
        this.replyingToId.set(null);
        this.pendingAttachmentReference.set(null);
        this.reload();
      });
  }

  private buildThreadedComments(comments: TicketCommentDto[]): ThreadedComment[] {
    const byParent = new Map<string | undefined, TicketCommentDto[]>();
    for (const comment of comments) {
      const key = comment.parentCommentId ?? undefined;
      const siblings = byParent.get(key) ?? [];
      siblings.push(comment);
      byParent.set(key, siblings);
    }

    const result: ThreadedComment[] = [];
    const visit = (parentId: string | undefined, depth: number): void => {
      for (const comment of byParent.get(parentId) ?? []) {
        result.push({ comment, depth });
        visit(comment.id, depth + 1);
      }
    };
    visit(undefined, 0);
    return result;
  }
}
