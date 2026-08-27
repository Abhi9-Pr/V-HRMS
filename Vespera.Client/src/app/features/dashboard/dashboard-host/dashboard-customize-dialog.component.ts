import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { DashboardWidgetPreferenceDto, WidgetPreferenceInput } from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { DashboardFacade } from '../data/dashboard.facade';
import { WIDGET_REGISTRY } from '../widget-registry';

interface CustomizeRow {
  widgetKey: string;
  title: string;
  icon: string;
  isVisible: boolean;
  size: 'Small' | 'Medium' | 'Large';
}

/**
 * Reorder/show-hide/resize, all in one accessible list rather than dragging cards directly on
 * the live dashboard grid: `cdkDropList`/`cdkDrag` here give mouse users a drag handle, and
 * every CDK drag item is natively keyboard-operable (focus the handle, Enter/Space to lift,
 * arrow keys to move, Enter/Space to drop) — the explicit ↑/↓ buttons are a second, even more
 * discoverable way to do the same reorder for anyone who doesn't know that gesture, satisfying
 * "keyboard-navigable drag-and-drop" without relying on users discovering an obscure keyboard
 * convention.
 */
@Component({
  selector: 'vespera-dashboard-customize-dialog',
  standalone: true,
  imports: [
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatSelectModule,
    CdkDropList,
    CdkDrag,
    CdkDragHandle,
  ],
  templateUrl: './dashboard-customize-dialog.component.html',
})
export class DashboardCustomizeDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<DashboardCustomizeDialogComponent>);
  private readonly facade = inject(DashboardFacade);
  private readonly data = inject<{ layout: DashboardWidgetPreferenceDto[] }>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly rows = signal<CustomizeRow[]>(
    [...this.data.layout]
      .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
      .map((preference) => ({
        widgetKey: preference.widgetKey!,
        title: WIDGET_REGISTRY[preference.widgetKey!]?.title ?? preference.widgetKey!,
        icon: WIDGET_REGISTRY[preference.widgetKey!]?.icon ?? 'widgets',
        isVisible: preference.isVisible ?? true,
        size: (preference.size as CustomizeRow['size']) ?? 'Medium',
      })),
  );

  readonly sizes: CustomizeRow['size'][] = ['Small', 'Medium', 'Large'];

  drop(event: CdkDragDrop<CustomizeRow[]>): void {
    const updated = [...this.rows()];
    moveItemInArray(updated, event.previousIndex, event.currentIndex);
    this.rows.set(updated);
  }

  moveUp(index: number): void {
    if (index === 0) {
      return;
    }
    const updated = [...this.rows()];
    moveItemInArray(updated, index, index - 1);
    this.rows.set(updated);
  }

  moveDown(index: number): void {
    if (index === this.rows().length - 1) {
      return;
    }
    const updated = [...this.rows()];
    moveItemInArray(updated, index, index + 1);
    this.rows.set(updated);
  }

  toggleVisible(row: CustomizeRow, visible: boolean): void {
    this.rows.set(this.rows().map((r) => (r.widgetKey === row.widgetKey ? { ...r, isVisible: visible } : r)));
  }

  setSize(row: CustomizeRow, size: CustomizeRow['size']): void {
    this.rows.set(this.rows().map((r) => (r.widgetKey === row.widgetKey ? { ...r, size } : r)));
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);

    const widgets: WidgetPreferenceInput[] = this.rows().map((row, index) => ({
      widgetKey: row.widgetKey,
      sortOrder: index,
      isVisible: row.isVisible,
      size: row.size,
    }));

    this.facade.saveLayout(widgets).subscribe({
      next: () => this.dialogRef.close(true),
      error: (apiError: ApiError) => {
        this.error.set(apiError.message);
        this.saving.set(false);
      },
    });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
