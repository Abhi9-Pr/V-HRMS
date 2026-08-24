import { Component, OnInit, signal } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { AssetsService } from '../assets.service';
import { UnusedSeatsReportRowDto } from '../assets.models';

@Component({
  selector: 'app-unused-seats-report',
  imports: [MatProgressSpinnerModule, MatTableModule],
  templateUrl: './unused-seats-report.html',
  styleUrl: './unused-seats-report.scss',
})
export class UnusedSeatsReport implements OnInit {
  protected readonly rows = signal<UnusedSeatsReportRowDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['productName', 'seatCount', 'seatsUsed', 'unusedSeats', 'expiresAt'];

  constructor(private readonly assetsService: AssetsService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.assetsService.getUnusedSeatsReport().subscribe((rows) => {
      this.rows.set(rows);
      this.loading.set(false);
    });
  }
}
