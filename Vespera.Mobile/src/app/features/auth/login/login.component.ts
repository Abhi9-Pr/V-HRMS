import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of, switchMap, tap } from 'rxjs';
import { ApiError, AuthService, TenantResolutionService } from 'vespera-shared';

/** Mobile shell's own login screen — reuses AuthService/TenantResolutionService from
 * vespera-shared exactly as Vespera.Client does (docs/CONTRIBUTING-frontend.md), just with
 * mobile-appropriate markup instead of Angular Material. */
@Component({
  selector: 'vespera-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly tenantResolution = inject(TenantResolutionService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly needsTotp = signal(false);
  readonly sessionTimedOut = this.route.snapshot.queryParamMap.get('reason') === 'session-timeout';

  readonly form = this.formBuilder.nonNullable.group({
    tenantCode: [this.tenantResolution.getCachedTenantCode() ?? '', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    totpCode: [''],
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const { tenantCode, email, password, totpCode } = this.form.getRawValue();

    this.tenantResolution
      .resolveByCode(tenantCode)
      .pipe(
        switchMap(() => this.authService.login(email, password, totpCode || undefined)),
        tap(() => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
          void this.router.navigateByUrl(returnUrl);
        }),
        catchError((apiError: ApiError) => {
          this.needsTotp.set(apiError.code === 'auth.totp_required');
          this.error.set(apiError.message);
          return of(null);
        }),
      )
      .subscribe(() => this.submitting.set(false));
  }
}
