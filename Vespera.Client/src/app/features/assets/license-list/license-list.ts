import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { AssetsService } from '../assets.service';
import { SoftwareLicenseDto } from '../assets.models';

@Component({
  selector: 'app-license-list',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './license-list.html',
  styleUrl: './license-list.scss',
})
export class LicenseList implements OnInit {
  protected readonly licenses = signal<SoftwareLicenseDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = signal(0);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['productName', 'seatCount', 'seatsUsed', 'expiresAt', 'actions'];

  private readonly fb = inject(FormBuilder);

  protected readonly createForm = this.fb.nonNullable.group({
    productName: ['', Validators.required],
    seatCount: [1, [Validators.required, Validators.min(1)]],
    expiresAt: [null as Date | null],
  });

  protected readonly allocateForm = this.fb.nonNullable.group({
    licenseId: ['', Validators.required],
    employeeId: ['', Validators.required],
  });

  protected readonly releaseForm = this.fb.nonNullable.group({
    allocationId: ['', Validators.required],
  });

  constructor(
    private readonly assetsService: AssetsService,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.assetsService.getLicenses({ page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe((result) => {
      this.licenses.set(result.items);
      this.totalCount.set(result.totalCount);
      this.loading.set(false);
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  createLicense(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.assetsService
      .createLicense({
        productName: value.productName,
        seatCount: value.seatCount,
        expiresAt: value.expiresAt ? toDateOnly(value.expiresAt) : null,
      })
      .subscribe(() => {
        this.createForm.reset({ productName: '', seatCount: 1, expiresAt: null });
        this.load();
      });
  }

  allocateSeat(licenseId: string): void {
    this.allocateForm.patchValue({ licenseId });
    if (this.allocateForm.invalid) {
      this.allocateForm.markAllAsTouched();
      return;
    }

    const value = this.allocateForm.getRawValue();
    this.assetsService.allocateSeat(value.licenseId, { employeeId: value.employeeId }).subscribe(() => {
      this.allocateForm.reset({ licenseId: '', employeeId: '' });
      this.load();
      this.snackBar.open('Seat allocated.', 'Dismiss', { duration: 4000 });
    });
  }

  releaseSeat(): void {
    if (this.releaseForm.invalid) {
      this.releaseForm.markAllAsTouched();
      return;
    }

    this.assetsService.releaseSeat(this.releaseForm.getRawValue().allocationId).subscribe(() => {
      this.releaseForm.reset({ allocationId: '' });
      this.load();
      this.snackBar.open('Seat released.', 'Dismiss', { duration: 4000 });
    });
  }
}

function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
