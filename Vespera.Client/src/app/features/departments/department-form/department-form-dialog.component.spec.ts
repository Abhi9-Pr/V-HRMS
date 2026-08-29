import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { DepartmentsFacade } from '../data/departments.facade';
import { DepartmentFormDialogComponent } from './department-form-dialog.component';
import { DepartmentFormDialogData } from './department-form-dialog.model';
import { ApiError } from 'vespera-shared';

describe('DepartmentFormDialogComponent', () => {
  let fixture: ComponentFixture<DepartmentFormDialogComponent>;
  let facade: jest.Mocked<Pick<DepartmentsFacade, 'create' | 'update'>>;
  let dialogRef: jest.Mocked<Pick<MatDialogRef<DepartmentFormDialogComponent>, 'close'>>;

  function setup(data: DepartmentFormDialogData) {
    dialogRef = { close: jest.fn() };

    TestBed.configureTestingModule({
      imports: [DepartmentFormDialogComponent],
      providers: [
        provideNoopAnimations(),
        { provide: DepartmentsFacade, useValue: facade },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    });

    fixture = TestBed.createComponent(DepartmentFormDialogComponent);
    fixture.detectChanges();
  }

  it('create mode should call facade.create() with the form values and close(true) on success', () => {
    facade = { create: jest.fn().mockReturnValue(of({ id: 'new-id' })), update: jest.fn() };
    setup({ mode: 'create', availableParents: [] });

    fixture.componentInstance.form.setValue({ name: 'Engineering', code: 'ENG', parentDepartmentId: null });
    fixture.componentInstance.submit();

    expect(facade.create).toHaveBeenCalledWith({ name: 'Engineering', code: 'ENG', parentDepartmentId: undefined });
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('edit mode should call facade.update() and not touch the code field', () => {
    const department = { id: 'dept-1', name: 'Engineering', code: 'ENG', parentDepartmentId: undefined };
    facade = { create: jest.fn(), update: jest.fn().mockReturnValue(of(undefined)) };
    setup({ mode: 'edit', department, availableParents: [department] });

    fixture.componentInstance.form.controls.name.setValue('Platform Engineering');
    fixture.componentInstance.submit();

    expect(facade.update).toHaveBeenCalledWith('dept-1', {
      name: 'Platform Engineering',
      parentDepartmentId: undefined,
    });
    expect(fixture.componentInstance.form.controls.code.disabled).toBe(true);
  });

  it('should show the API error message and not close on failure', () => {
    const apiError: ApiError = {
      status: 409,
      code: 'department.code_conflict',
      message: 'That code is already in use.',
    };
    facade = { create: jest.fn().mockReturnValue(throwError(() => apiError)), update: jest.fn() };
    setup({ mode: 'create', availableParents: [] });

    fixture.componentInstance.form.setValue({ name: 'Engineering', code: 'ENG', parentDepartmentId: null });
    fixture.componentInstance.submit();

    expect(fixture.componentInstance.error()).toBe('That code is already in use.');
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('should not submit an invalid form', () => {
    facade = { create: jest.fn().mockReturnValue(of({ id: 'new-id' })), update: jest.fn() };
    setup({ mode: 'create', availableParents: [] });

    fixture.componentInstance.form.controls.name.setValue('');
    fixture.componentInstance.submit();

    expect(facade.create).not.toHaveBeenCalled();
    expect(fixture.componentInstance.form.controls.name.touched).toBe(true);
  });
});
