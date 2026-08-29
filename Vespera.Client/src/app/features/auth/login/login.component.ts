import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { catchError, of, switchMap, tap } from 'rxjs';
import { ApiError, AuthService, TenantResolutionService } from 'vespera-shared';

@Component({
  selector: 'vespera-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './login.component.html',
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
