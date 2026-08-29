import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { DepartmentsFacade } from '../data/departments.facade';
import { DepartmentListComponent } from './department-list.component';
import { AuthService } from 'vespera-shared';

describe('DepartmentListComponent', () => {
  let fixture: ComponentFixture<DepartmentListComponent>;
  let facade: jest.Mocked<
    Pick<DepartmentsFacade, 'load' | 'remove' | 'departments' | 'totalCount' | 'loading' | 'error'>
  >;
  let dialog: jest.Mocked<Pick<MatDialog, 'open'>>;

  beforeEach(async () => {
    facade = {
      load: jest.fn(),
      remove: jest.fn().mockReturnValue(of(undefined)),
      departments: signal([]),
      totalCount: signal(0),
      loading: signal(false),
      error: signal(null),
    } as never;

    dialog = { open: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [DepartmentListComponent],
      providers: [
        { provide: DepartmentsFacade, useValue: facade },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: { permissions: signal(['Departments.Read', 'Departments.Manage']) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DepartmentListComponent);
  });

  it('should load departments on init', () => {
    fixture.detectChanges();

    expect(facade.load).toHaveBeenCalledWith({ page: 1, pageSize: 20, sortDescending: false });
  });

  it('openCreateDialog should open the form dialog in create mode and reload on save', () => {
    dialog.open.mockReturnValue({ afterClosed: () => of(true) } as never);
    fixture.detectChanges();
    facade.load.mockClear();

    fixture.componentInstance.openCreateDialog();

    expect(dialog.open).toHaveBeenCalled();
    const [, config] = dialog.open.mock.calls[0];
    expect((config as { data: { mode: string } }).data.mode).toBe('create');
    expect(facade.load).toHaveBeenCalled();
  });

  it('confirmDelete should call remove() and reload when the confirm dialog resolves true', () => {
    dialog.open.mockReturnValue({ afterClosed: () => of(true) } as never);
    fixture.detectChanges();
    facade.load.mockClear();

    fixture.componentInstance.confirmDelete({ id: 'dept-1', name: 'Engineering', code: 'ENG' });

    expect(facade.remove).toHaveBeenCalledWith('dept-1');
    expect(facade.load).toHaveBeenCalled();
  });

  it('confirmDelete should not call remove() when the confirm dialog is dismissed', () => {
    dialog.open.mockReturnValue({ afterClosed: () => of(false) } as never);
    fixture.detectChanges();

    fixture.componentInstance.confirmDelete({ id: 'dept-1', name: 'Engineering', code: 'ENG' });

    expect(facade.remove).not.toHaveBeenCalled();
  });
});
