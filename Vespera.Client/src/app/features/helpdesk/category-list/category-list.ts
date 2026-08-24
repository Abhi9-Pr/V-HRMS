import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { HelpdeskService } from '../helpdesk.service';
import { TicketCategoryDto } from '../helpdesk.models';

@Component({
  selector: 'app-category-list',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './category-list.html',
  styleUrl: './category-list.scss',
})
export class CategoryList implements OnInit {
  protected readonly categories = signal<TicketCategoryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = signal(0);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['name', 'departmentId', 'defaultSlaPolicyId'];

  private readonly fb = inject(FormBuilder);

  protected readonly createForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    departmentId: ['', Validators.required],
    defaultSlaPolicyId: [''],
  });

  constructor(private readonly helpdeskService: HelpdeskService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.helpdeskService.getCategories({ page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe((result) => {
      this.categories.set(result.items);
      this.totalCount.set(result.totalCount);
      this.loading.set(false);
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  createCategory(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.helpdeskService
      .createCategory({
        name: value.name,
        departmentId: value.departmentId,
        defaultSlaPolicyId: value.defaultSlaPolicyId || null,
      })
      .subscribe(() => {
        this.createForm.reset({ name: '', departmentId: '', defaultSlaPolicyId: '' });
        this.load();
      });
  }
}
