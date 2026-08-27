import { Component, Input, computed, signal } from '@angular/core';
import { PendingApprovalsWidgetDto } from '../dashboard-widget-payloads.model';

@Component({
  selector: 'vespera-pending-approvals-widget',
  standalone: true,
  templateUrl: './pending-approvals-widget.component.html',
})
export class PendingApprovalsWidgetComponent {
  private readonly dataSignal = signal<PendingApprovalsWidgetDto>({ totalCount: 0, countBySubjectType: {} });

  @Input({ required: true })
  set data(value: PendingApprovalsWidgetDto) {
    this.dataSignal.set(value ?? { totalCount: 0, countBySubjectType: {} });
  }

  readonly totalCount = computed(() => this.dataSignal().totalCount);
  readonly breakdown = computed(() => Object.entries(this.dataSignal().countBySubjectType ?? {}));
}
