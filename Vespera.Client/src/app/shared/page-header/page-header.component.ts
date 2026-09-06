import { Component, Input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'vespera-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './page-header.component.html',
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle?: string;
}
