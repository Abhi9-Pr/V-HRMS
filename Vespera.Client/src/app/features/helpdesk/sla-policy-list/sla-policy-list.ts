import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { HelpdeskService } from '../helpdesk.service';
import { SlaPolicyDto } from '../helpdesk.models';

@Component({
  selector: 'app-sla-policy-list',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './sla-policy-list.html',
  styleUrl: './sla-policy-list.scss',
})
export class SlaPolicyList implements OnInit {
  protected readonly policies = signal<SlaPolicyDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = signal(0);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['name', 'responseTime', 'resolutionTime', 'businessHoursStart', 'businessHoursEnd'];

  private readonly fb = inject(FormBuilder);

  // Business-hours inputs are plain <input type="time"> controls (Material has no native time
  // picker) bound to "HH:mm" strings, padded to the "HH:mm:ss" shape System.Text.Json's default
  // TimeOnly converter expects on submit.
  protected readonly createForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    responseTimeHours: [4, [Validators.required, Validators.min(1)]],
    resolutionTimeHours: [24, [Validators.required, Validators.min(1)]],
    businessHoursStart: ['09:00', Validators.required],
    businessHoursEnd: ['18:00', Validators.required],
  });

  constructor(private readonly helpdeskService: HelpdeskService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.helpdeskService.getSlaPolicies({ page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe((result) => {
      this.policies.set(result.items);
      this.totalCount.set(result.totalCount);
      this.loading.set(false);
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  createPolicy(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.helpdeskService
      .createSlaPolicy({
        name: value.name,
        responseTimeHours: value.responseTimeHours,
        resolutionTimeHours: value.resolutionTimeHours,
        businessHoursStart: toTimeOnly(value.businessHoursStart),
        businessHoursEnd: toTimeOnly(value.businessHoursEnd),
      })
      .subscribe(() => {
        this.createForm.reset({
          name: '',
          responseTimeHours: 4,
          resolutionTimeHours: 24,
          businessHoursStart: '09:00',
          businessHoursEnd: '18:00',
        });
        this.load();
      });
  }
}

function toTimeOnly(hhmm: string): string {
  return hhmm.length === 5 ? `${hhmm}:00` : hhmm;
}
