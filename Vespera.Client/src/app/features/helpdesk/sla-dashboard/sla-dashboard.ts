import { DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { HelpdeskService } from '../helpdesk.service';
import { SlaComplianceReportDto } from '../helpdesk.models';

@Component({
  selector: 'app-sla-dashboard',
  imports: [DecimalPipe, MatCardModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './sla-dashboard.html',
  styleUrl: './sla-dashboard.scss',
})
export class SlaDashboard implements OnInit {
  protected readonly report = signal<SlaComplianceReportDto | null>(null);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['categoryName', 'total', 'breached', 'compliancePercentage'];

  constructor(private readonly helpdeskService: HelpdeskService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.helpdeskService.getSlaComplianceReport().subscribe((report) => {
      this.report.set(report);
      this.loading.set(false);
    });
  }
}
