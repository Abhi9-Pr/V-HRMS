import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RecruitmentService } from '../recruitment.service';
import { PublicJobDto } from '../recruitment.models';

// The public careers page — no auth session exists here at all, so the tenant id is read from
// the route rather than AuthService's signal (see RecruitmentService.getPublicJobs). A real
// deployment would give each employer's careers page its own URL carrying this id.
@Component({
  selector: 'app-public-jobs',
  imports: [MatCardModule, MatProgressSpinnerModule],
  templateUrl: './public-jobs.html',
  styleUrl: './public-jobs.scss',
})
export class PublicJobs implements OnInit {
  protected readonly jobs = signal<PublicJobDto[]>([]);
  protected readonly loading = signal(false);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly recruitment: RecruitmentService,
  ) {}

  ngOnInit(): void {
    const tenantId = this.route.snapshot.paramMap.get('tenantId')!;
    this.loading.set(true);
    this.recruitment.getPublicJobs(tenantId).subscribe((jobs) => {
      this.jobs.set(jobs);
      this.loading.set(false);
    });
  }
}
