import { Component, OnInit, inject } from '@angular/core';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { EmptyStateComponent } from '../../../shared/states/empty-state.component';
import { TimezoneDatePipe } from '../../../shared/pipes/timezone-date.pipe';
import { AssetsFacade } from '../data/assets.facade';

@Component({
  selector: 'vespera-unused-seats-report',
  standalone: true,
  imports: [ErrorStateComponent, LoadingStateComponent, EmptyStateComponent, TimezoneDatePipe],
  templateUrl: './unused-seats-report.component.html',
})
export class UnusedSeatsReportComponent implements OnInit {
  private readonly assetsFacade = inject(AssetsFacade);

  readonly rows = this.assetsFacade.unusedSeatsReport;
  readonly loading = this.assetsFacade.unusedSeatsReportLoading;
  readonly error = this.assetsFacade.unusedSeatsReportError;

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.assetsFacade.loadUnusedSeatsReport();
  }
}
