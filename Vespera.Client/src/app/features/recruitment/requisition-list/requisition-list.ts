import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { RecruitmentService } from '../recruitment.service';
import {
  JOB_REQUISITION_STATUS_LABELS,
  JobRequisitionDto,
  REQUISITION_APPROVAL_STATUS_LABELS,
  JobRequisitionStatus,
  RequisitionApprovalStatus,
} from '../recruitment.models';

@Component({
  selector: 'app-requisition-list',
  imports: [MatButtonModule, MatPaginatorModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './requisition-list.html',
  styleUrl: './requisition-list.scss',
})
export class RequisitionList implements OnInit {
  protected readonly requisitions = signal<JobRequisitionDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = signal(0);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['title', 'departmentId', 'status', 'approvalStatus', 'isPublished'];

  constructor(
    private readonly recruitment: RecruitmentService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.recruitment.getRequisitions({ page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe((result) => {
      this.requisitions.set(result.items);
      this.totalCount.set(result.totalCount);
      this.loading.set(false);
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  newRequisition(): void {
    void this.router.navigate(['/recruitment/requisitions/new']);
  }

  openRequisition(requisition: JobRequisitionDto): void {
    void this.router.navigate(['/recruitment/requisitions', requisition.id]);
  }

  protected statusLabel(status: JobRequisitionStatus): string {
    return JOB_REQUISITION_STATUS_LABELS[status];
  }

  protected approvalStatusLabel(status: RequisitionApprovalStatus): string {
    return REQUISITION_APPROVAL_STATUS_LABELS[status];
  }
}
