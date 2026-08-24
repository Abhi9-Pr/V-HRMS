import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { HelpdeskService } from '../helpdesk.service';
import { PublicHolidayDto } from '../helpdesk.models';

@Component({
  selector: 'app-public-holiday-list',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatNativeDateModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './public-holiday-list.html',
  styleUrl: './public-holiday-list.scss',
})
export class PublicHolidayList implements OnInit {
  protected readonly holidays = signal<PublicHolidayDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = signal(0);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['date', 'name'];

  private readonly fb = inject(FormBuilder);

  protected readonly createForm = this.fb.nonNullable.group({
    date: [new Date(), Validators.required],
    name: ['', Validators.required],
  });

  constructor(private readonly helpdeskService: HelpdeskService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.helpdeskService.getHolidays({ page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe((result) => {
      this.holidays.set(result.items);
      this.totalCount.set(result.totalCount);
      this.loading.set(false);
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  createHoliday(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.helpdeskService.createHoliday({ date: toDateOnly(value.date), name: value.name }).subscribe(() => {
      this.createForm.reset({ date: new Date(), name: '' });
      this.load();
    });
  }
}

function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
