import { DepartmentDto } from 'vespera-shared';

export interface DepartmentFormDialogData {
  mode: 'create' | 'edit';
  department?: DepartmentDto;
  availableParents: DepartmentDto[];
}
