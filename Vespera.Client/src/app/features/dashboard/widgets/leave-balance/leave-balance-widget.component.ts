import { Component, Input, signal, ChangeDetectionStrategy } from '@angular/core';
import { LeaveBalanceWidgetDto } from '../dashboard-widget-payloads.model';

@Component({
  selector: 'vespera-leave-balance-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './leave-balance-widget.component.html',
})
export class LeaveBalanceWidgetComponent {
  private readonly dataSignal = signal<LeaveBalanceWidgetDto>({ balances: [] });
  readonly widgetData = this.dataSignal.asReadonly();

  @Input({ required: true })
  set data(value: LeaveBalanceWidgetDto) {
    this.dataSignal.set(value ?? { balances: [] });
  }
}
