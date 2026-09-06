import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { RecruitmentFacade } from '../data/recruitment.facade';
import { TenantResolutionService } from 'vespera-shared';

/**
 * Reached outside the authenticated shell entirely (see app.routes.ts) — no bearer token exists.
 * authInterceptor only attaches X-Tenant-Id when there's no session, from whatever
 * TenantResolutionService has cached; a visitor arriving here fresh has nothing cached, so this
 * resolves the tenant CODE in the URL first (same TenantsClient lookup the login screen already
 * uses, not a second mechanism) and only calls the public jobs endpoint once that's cached.
 */
@Component({
    selector: 'vespera-public-jobs',
    imports: [ErrorStateComponent, LoadingStateComponent],
    templateUrl: './public-jobs.component.html'
})
export class PublicJobsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly tenantResolution = inject(TenantResolutionService);
  private readonly recruitmentFacade = inject(RecruitmentFacade);

  readonly jobs = this.recruitmentFacade.publicJobs;
  readonly loading = this.recruitmentFacade.publicJobsLoading;
  readonly error = this.recruitmentFacade.publicJobsError;

  ngOnInit(): void {
    const tenantCode = this.route.snapshot.paramMap.get('tenantCode') ?? '';

    this.tenantResolution.resolveByCode(tenantCode).subscribe(() => this.recruitmentFacade.loadPublicJobs());
  }
}
