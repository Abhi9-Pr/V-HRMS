import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { EmptyStateComponent } from '../../../shared/states/empty-state.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { HelpdeskFacade } from '../data/helpdesk.facade';

@Component({
  selector: 'vespera-sla-dashboard',
  imports: [ErrorStateComponent, LoadingStateComponent, EmptyStateComponent, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './sla-dashboard.component.html',
})
export class SlaDashboardComponent implements OnInit {
  private readonly helpdeskFacade = inject(HelpdeskFacade);

  readonly report = this.helpdeskFacade.slaReport;
  readonly loading = this.helpdeskFacade.slaReportLoading;
  readonly error = this.helpdeskFacade.slaReportError;

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.helpdeskFacade.loadSlaComplianceReport();
  }
}
