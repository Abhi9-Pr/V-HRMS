import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HelpdeskService } from '../helpdesk.service';
import {
  TICKET_PRIORITY_LABELS,
  TICKET_STATUS_LABELS,
  TicketCommentDto,
  TicketDto,
  TicketPriority,
  TicketStatus,
} from '../helpdesk.models';

interface ThreadedComment {
  comment: TicketCommentDto;
  depth: number;
}

@Component({
  selector: 'app-ticket-detail',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './ticket-detail.html',
  styleUrl: './ticket-detail.scss',
})
export class TicketDetail implements OnInit {
  protected readonly ticket = signal<TicketDto | null>(null);
  protected readonly loading = signal(false);
  protected readonly pendingAttachmentReference = signal<string | null>(null);
  protected readonly TicketStatus = TicketStatus;

  private readonly fb = inject(FormBuilder);
  private readonly ticketId: string;

  protected readonly assignForm = this.fb.nonNullable.group({
    employeeId: ['', Validators.required],
  });

  protected readonly replyForm = this.fb.nonNullable.group({
    body: ['', Validators.required],
    isInternal: [false],
    parentCommentId: this.fb.control<string | null>(null),
  });

  protected readonly satisfactionForm = this.fb.nonNullable.group({
    rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
  });

  constructor(
    route: ActivatedRoute,
    private readonly helpdeskService: HelpdeskService,
    private readonly snackBar: MatSnackBar,
  ) {
    this.ticketId = route.snapshot.paramMap.get('id')!;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.helpdeskService.getTicketById(this.ticketId).subscribe((ticket) => {
      this.ticket.set(ticket);
      this.loading.set(false);
    });
  }

  // Mirrors the guard conditions in Vespera.Domain.Helpdesk.Ticket exactly.
  canAssign(status: TicketStatus): boolean {
    return status !== TicketStatus.Resolved && status !== TicketStatus.Closed;
  }

  canResolve(status: TicketStatus): boolean {
    return status !== TicketStatus.Resolved && status !== TicketStatus.Closed;
  }

  canClose(status: TicketStatus): boolean {
    return status === TicketStatus.Resolved;
  }

  canRate(status: TicketStatus): boolean {
    return status === TicketStatus.Closed;
  }

  assign(): void {
    if (this.assignForm.invalid) {
      this.assignForm.markAllAsTouched();
      return;
    }

    this.helpdeskService.assignTicket(this.ticketId, this.assignForm.getRawValue()).subscribe(() => this.load());
  }

  resolve(): void {
    this.helpdeskService.resolveTicket(this.ticketId).subscribe(() => this.load());
  }

  close(): void {
    this.helpdeskService.closeTicket(this.ticketId).subscribe(() => this.load());
  }

  rateSatisfaction(): void {
    if (this.satisfactionForm.invalid) {
      this.satisfactionForm.markAllAsTouched();
      return;
    }

    this.helpdeskService.rateSatisfaction(this.ticketId, this.satisfactionForm.getRawValue()).subscribe(() => this.load());
  }

  onAttachmentSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.helpdeskService.uploadAttachment(this.ticketId, file).subscribe((result) => {
      this.pendingAttachmentReference.set(result.reference);
      this.snackBar.open('Attachment uploaded — it will be attached to your next reply.', 'Dismiss', { duration: 4000 });
    });
  }

  replyTo(commentId: string | null): void {
    this.replyForm.patchValue({ parentCommentId: commentId });
  }

  submitReply(): void {
    if (this.replyForm.invalid) {
      this.replyForm.markAllAsTouched();
      return;
    }

    const value = this.replyForm.getRawValue();
    const attachmentReference = this.pendingAttachmentReference();

    this.helpdeskService
      .addComment(this.ticketId, {
        body: value.body,
        isInternal: value.isInternal,
        parentCommentId: value.parentCommentId,
        attachmentReferences: attachmentReference ? [attachmentReference] : null,
      })
      .subscribe(() => {
        this.replyForm.reset({ body: '', isInternal: false, parentCommentId: null });
        this.pendingAttachmentReference.set(null);
        this.load();
      });
  }

  // Builds a depth-first, parent-then-children ordering from the flat ParentCommentId shape the
  // API returns (see TicketDto's doc comment — deliberately not pre-nested server-side).
  threadedComments(comments: TicketCommentDto[]): ThreadedComment[] {
    const byParent = new Map<string | null, TicketCommentDto[]>();
    for (const comment of comments) {
      const key = comment.parentCommentId;
      const siblings = byParent.get(key) ?? [];
      siblings.push(comment);
      byParent.set(key, siblings);
    }
    for (const siblings of byParent.values()) {
      siblings.sort((a, b) => a.createdAt.localeCompare(b.createdAt));
    }

    const result: ThreadedComment[] = [];
    const visit = (parentId: string | null, depth: number) => {
      for (const comment of byParent.get(parentId) ?? []) {
        result.push({ comment, depth });
        visit(comment.id, depth + 1);
      }
    };
    visit(null, 0);
    return result;
  }

  protected priorityLabel(priority: TicketPriority): string {
    return TICKET_PRIORITY_LABELS[priority];
  }

  protected statusLabel(status: TicketStatus): string {
    return TICKET_STATUS_LABELS[status];
  }
}
