import { AfterViewInit, Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { JobRequisitionDto } from '../../../core/api/generated/api-client';
import { Permissions } from '../../../core/authorization/permissions';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { RecruitmentFacade } from '../data/recruitment.facade';
import { JOB_REQUISITION_STATUS_LABELS, REQUISITION_APPROVAL_STATUS_LABELS } from '../recruitment.labels';
import { RequisitionCreateDialogComponent } from './requisition-create-dialog.component';

@Component({
  selector: 'vespera-requisition-list',
  standalone: true,
  imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent, MatIconModule],
  templateUrl: './requisition-list.component.html',
})
export class RequisitionListComponent implements OnInit, AfterViewInit {
  private readonly recruitmentFacade = inject(RecruitmentFacade);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  @ViewChild('actionsCell', { static: true }) actionsCellTemplate!: TemplateRef<{ $implicit: JobRequisitionDto }>;

  readonly Permissions = Permissions;

  readonly requisitions = this.recruitmentFacade.requisitions;
  readonly totalCount = this.recruitmentFacade.requisitionsTotalCount;
  readonly loading = this.recruitmentFacade.requisitionsLoading;
  readonly error = this.recruitmentFacade.requisitionsError;

  columns: DataTableColumn<JobRequisitionDto>[] = [
    { key: 'title', header: 'Title', cell: (row) => row.title ?? '' },
    { key: 'status', header: 'Status', cell: (row) => (row.status !== undefined ? JOB_REQUISITION_STATUS_LABELS[row.status] : '') },
    {
      key: 'approvalStatus',
      header: 'Approval',
      cell: (row) => (row.approvalStatus !== undefined ? REQUISITION_APPROVAL_STATUS_LABELS[row.approvalStatus] : ''),
    },
    { key: 'isPublished', header: 'Published', cell: (row) => (row.isPublished ? 'Yes' : 'No') },
    { key: 'actions', header: '', cell: () => '' },
  ];

  private lastQuery: DataTableQuery = { page: 1, pageSize: 20, sortDescending: false };

  ngOnInit(): void {
    this.reload();
  }

  ngAfterViewInit(): void {
    this.columns = this.columns.map((column) => (column.key === 'actions' ? { ...column, cellTemplate: this.actionsCellTemplate } : column));
  }

  onQueryChange(query: DataTableQuery): void {
    this.lastQuery = query;
    this.reload();
  }

  reload(): void {
    this.recruitmentFacade.loadRequisitions(this.lastQuery);
  }

  openRequisition(requisition: JobRequisitionDto): void {
    if (requisition.id) {
      void this.router.navigate(['/recruitment', requisition.id]);
    }
  }

  openCreateDialog(): void {
    this.dialog
      .open(RequisitionCreateDialogComponent)
      .afterClosed()
      .subscribe((requisitionId: string | null) => {
        if (requisitionId) {
          void this.router.navigate(['/recruitment', requisitionId]);
        }
      });
  }
}
