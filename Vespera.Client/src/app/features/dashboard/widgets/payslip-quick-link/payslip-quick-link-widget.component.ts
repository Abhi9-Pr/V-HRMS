import { Component, Input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CurrencyDisplayPipe } from '../../../../shared/pipes/currency-display.pipe';
import { TimezoneDatePipe } from '../../../../shared/pipes/timezone-date.pipe';
import { MyLatestPayslipDto } from '../dashboard-widget-payloads.model';

@Component({
  selector: 'vespera-payslip-quick-link-widget',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, CurrencyDisplayPipe, TimezoneDatePipe],
  templateUrl: './payslip-quick-link-widget.component.html',
})
export class PayslipQuickLinkWidgetComponent {
  private readonly dataSignal = signal<MyLatestPayslipDto | null>(null);
  readonly payslip = this.dataSignal.asReadonly();

  @Input({ required: true })
  set data(value: MyLatestPayslipDto | null) {
    this.dataSignal.set(value ?? null);
  }

  download(): void {
    const url = this.payslip()?.downloadUrl;
    if (url) {
      window.open(url, '_blank', 'noopener');
    }
  }
}
