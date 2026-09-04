import { NgComponentOutlet } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { DashboardFacade } from '../data/dashboard.facade';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { DashboardCustomizeDialogComponent } from './dashboard-customize-dialog.component';

/**
 * The landing dashboard. One aggregated GET (see DashboardFacade.load) renders every visible
 * widget from its own registry entry (widget-registry.ts) via NgComponentOutlet — a failed widget
 * still gets a card (an error-state message), never a hole where the whole page breaks. Layout
 * (order/visibility/size) is edited in DashboardCustomizeDialogComponent, not by dragging the
 * live grid — see that component's doc comment for why.
 */
import { PageHeaderComponent } from '../../../shared/page-header/page-header.component';

@Component({
  selector: 'vespera-dashboard-host',
  standalone: true,
  imports: [NgComponentOutlet, MatButtonModule, MatIconModule, LoadingStateComponent, ErrorStateComponent, PageHeaderComponent],
  templateUrl: './dashboard-host.component.html',
})
export class DashboardHostComponent implements OnInit {
  private readonly dialog = inject(MatDialog);
  protected readonly facade = inject(DashboardFacade);

  ngOnInit(): void {
    this.facade.load();
  }

  reload(): void {
    this.facade.load();
  }

  sizeSpanClass(size: string | undefined): string {
    switch (size) {
      case 'Large':
        return 'lg:col-span-3 sm:col-span-2';
      case 'Small':
        return 'lg:col-span-1 sm:col-span-1';
      default:
        return 'lg:col-span-2 sm:col-span-2';
    }
  }

  openCustomize(): void {
    this.dialog
      .open(DashboardCustomizeDialogComponent, { width: '560px', data: { layout: this.facade.layout() } })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.facade.load();
        }
      });
  }
}
