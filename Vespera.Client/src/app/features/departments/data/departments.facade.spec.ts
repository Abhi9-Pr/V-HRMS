import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { DepartmentsClient } from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { DepartmentsFacade } from './departments.facade';

describe('DepartmentsFacade', () => {
  let client: jest.Mocked<
    Pick<DepartmentsClient, 'departments_List' | 'departments_Create' | 'departments_Update' | 'departments_Delete'>
  >;
  let facade: DepartmentsFacade;

  beforeEach(() => {
    client = {
      departments_List: jest.fn(),
      departments_Create: jest.fn(),
      departments_Update: jest.fn(),
      departments_Delete: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [DepartmentsFacade, { provide: DepartmentsClient, useValue: client }],
    });

    facade = TestBed.inject(DepartmentsFacade);
  });

  it('load() should populate departments/totalCount on success', () => {
    client.departments_List.mockReturnValue(
      of({ items: [{ id: '1', name: 'Engineering', code: 'ENG' }], page: 1, pageSize: 20, totalCount: 1 } as never),
    );

    facade.load({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.loading()).toBe(false);
    expect(facade.error()).toBeNull();
    expect(facade.departments()).toHaveLength(1);
    expect(facade.totalCount()).toBe(1);
    expect(client.departments_List).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('load() should populate error on failure and stop loading', () => {
    const apiError: ApiError = { status: 403, code: 'forbidden', message: 'no access' };
    client.departments_List.mockReturnValue(throwError(() => apiError));

    facade.load({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.loading()).toBe(false);
    expect(facade.error()).toEqual(apiError);
    expect(facade.departments()).toHaveLength(0);
  });

  it('create() should delegate straight to the generated client', (done) => {
    client.departments_Create.mockReturnValue(of({ id: 'new-id' } as never));

    facade.create({ name: 'Sales', code: 'SLS' }).subscribe((result) => {
      expect(result.id).toBe('new-id');
      expect(client.departments_Create).toHaveBeenCalledWith({ name: 'Sales', code: 'SLS' });
      done();
    });
  });

  it('remove() should delegate to the generated client delete method', (done) => {
    client.departments_Delete.mockReturnValue(of(undefined));

    facade.remove('dept-1').subscribe(() => {
      expect(client.departments_Delete).toHaveBeenCalledWith('dept-1');
      done();
    });
  });
});
