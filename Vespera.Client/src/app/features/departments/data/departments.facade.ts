import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateDepartmentRequest,
  CreateDepartmentResponse,
  DepartmentDto,
  DepartmentsClient,
  UpdateDepartmentRequest,
} from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { DataTableQuery } from '../../../shared/data-table/data-table.model';

/**
 * Wraps the generated DepartmentsClient behind signals + plain methods — feature components
 * never import the generated client directly. This is the pattern every later feature copies;
 * see docs/CONTRIBUTING-frontend.md.
 */
@Injectable({ providedIn: 'root' })
export class DepartmentsFacade {
  private readonly client = inject(DepartmentsClient);

  private readonly departmentsSignal = signal<DepartmentDto[]>([]);
  private readonly totalCountSignal = signal(0);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);

  readonly departments = this.departmentsSignal.asReadonly();
  readonly totalCount = this.totalCountSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  load(query: DataTableQuery): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    this.client.list(query.page, query.pageSize, query.sortBy, query.sortDescending).subscribe({
      next: (result) => {
        this.departmentsSignal.set(result.items ?? []);
        this.totalCountSignal.set(result.totalCount ?? 0);
        this.loadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.errorSignal.set(apiError);
        this.loadingSignal.set(false);
      },
    });
  }

  create(request: CreateDepartmentRequest): Observable<CreateDepartmentResponse> {
    return this.client.create(request);
  }

  update(id: string, request: UpdateDepartmentRequest): Observable<void> {
    return this.client.update(id, request);
  }

  remove(id: string): Observable<void> {
    return this.client.delete(id);
  }
}
