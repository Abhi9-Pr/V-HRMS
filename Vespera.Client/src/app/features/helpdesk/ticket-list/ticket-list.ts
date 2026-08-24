import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { HelpdeskService } from '../helpdesk.service';
import {
  TICKET_PRIORITY_LABELS,
  TICKET_PRIORITY_OPTIONS,
  TICKET_STATUS_LABELS,
  TicketPriority,
  TicketStatus,
  TicketSummaryDto,
} from '../helpdesk.models';

@Component({
  selector: 'app-ticket-list',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './ticket-list.html',
  styleUrl: './ticket-list.scss',
})
export class TicketList implements OnInit {
  protected readonly tickets = signal<TicketSummaryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = signal(0);
  protected readonly loading = signal(false);
  protected readonly showCreateForm = signal(false);
  protected readonly displayedColumns = ['subject', 'priority', 'status', 'dueAt', 'assignedTo'];
  protected readonly priorityOptions = TICKET_PRIORITY_OPTIONS;

  private readonly fb = inject(FormBuilder);

  protected readonly createForm = this.fb.nonNullable.group({
    categoryId: ['', Validators.required],
    subject: ['', Validators.required],
    description: ['', Validators.required],
    priority: [TicketPriority.Medium, Validators.required],
  });

  constructor(
    private readonly helpdeskService: HelpdeskService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.helpdeskService.getTickets({ page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe((result) => {
      this.tickets.set(result.items);
      this.totalCount.set(result.totalCount);
      this.loading.set(false);
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  toggleCreateForm(): void {
    this.showCreateForm.set(!this.showCreateForm());
  }

  submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.helpdeskService.raiseTicket(this.createForm.getRawValue()).subscribe((result) => {
      this.showCreateForm.set(false);
      this.createForm.reset({ priority: TicketPriority.Medium });
      this.router.navigate(['/helpdesk/tickets', result.id]);
    });
  }

  openTicket(id: string): void {
    this.router.navigate(['/helpdesk/tickets', id]);
  }

  protected priorityLabel(priority: TicketPriority): string {
    return TICKET_PRIORITY_LABELS[priority];
  }

  protected statusLabel(status: TicketStatus): string {
    return TICKET_STATUS_LABELS[status];
  }
}
