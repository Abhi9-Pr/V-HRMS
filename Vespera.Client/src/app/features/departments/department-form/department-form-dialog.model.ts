import { DepartmentDto } from '../../../core/api/generated/api-client';

export interface DepartmentFormDialogData {
  mode: 'create' | 'edit';
  department?: DepartmentDto;
  availableParents: DepartmentDto[];
}
