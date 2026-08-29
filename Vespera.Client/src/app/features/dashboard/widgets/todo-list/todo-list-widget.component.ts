import { CdkDrag, CdkDragDrop, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, Input, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { urgencyLabelByName } from '../../dashboard.labels';
import { TodosFacade } from '../../data/todos.facade';
import { TodoItemDto, TodoUrgency } from 'vespera-shared';

/** Drag-and-drop reorder here is a single flat list (unlike the dashboard-wide customize dialog,
 * which needed a redundant up/down control because it mixes drag with several other controls per
 * row) — CDK's native keyboard support (focus an item, Enter/Space to lift, arrow keys to move,
 * Enter/Space to drop) is sufficient on its own for a plain reorderable list like this one. */
@Component({
  selector: 'vespera-todo-list-widget',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatCheckboxModule, MatIconModule, MatInputModule, CdkDropList, CdkDrag],
  templateUrl: './todo-list-widget.component.html',
})
export class TodoListWidgetComponent {
  private readonly todosFacade = inject(TodosFacade);

  readonly urgencyLabelByName = urgencyLabelByName;
  readonly newTitle = signal('');

  private readonly itemsSignal = signal<TodoItemDto[]>([]);
  readonly items = this.itemsSignal.asReadonly();

  @Input({ required: true })
  set data(value: TodoItemDto[]) {
    this.itemsSignal.set([...(value ?? [])].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)));
  }

  toggleDone(item: TodoItemDto, done: boolean): void {
    if (!item.id) {
      return;
    }
    this.todosFacade.setDone(item.id, done).subscribe(() => {
      this.itemsSignal.update((items) => items.map((i) => (i.id === item.id ? { ...i, isDone: done } : i)));
    });
  }

  /** Index-aligned with the backend's TodoUrgency enum ordinal (Low=0, Medium=1, High=2) — the
   * wire carries both shapes for this one field: the numeric enum on write
   * (SetTodoItemUrgencyCommand), the `.ToString()` name on read (TodoItemDto.urgency). */
  private static readonly urgencyNames = ['Low', 'Medium', 'High'];

  cycleUrgency(item: TodoItemDto): void {
    if (!item.id) {
      return;
    }
    const currentIndex = Math.max(TodoListWidgetComponent.urgencyNames.indexOf(item.urgency ?? 'Low'), 0);
    const nextIndex = (currentIndex + 1) % TodoListWidgetComponent.urgencyNames.length;
    const nextName = TodoListWidgetComponent.urgencyNames[nextIndex];

    this.todosFacade.setUrgency(item.id, nextIndex as TodoUrgency).subscribe(() => {
      this.itemsSignal.update((items) => items.map((i) => (i.id === item.id ? { ...i, urgency: nextName } : i)));
    });
  }

  drop(event: CdkDragDrop<TodoItemDto[]>): void {
    const updated = [...this.itemsSignal()];
    moveItemInArray(updated, event.previousIndex, event.currentIndex);
    this.itemsSignal.set(updated);

    const orderedIds = updated.map((i) => i.id!).filter(Boolean);
    this.todosFacade.reorder(orderedIds).subscribe();
  }

  addTodo(): void {
    const title = this.newTitle().trim();
    if (!title) {
      return;
    }

    this.todosFacade.create({ title, dueDate: undefined, urgency: TodoUrgency._1 }).subscribe((id) => {
      this.itemsSignal.update((items) => [
        ...items,
        { id, title, dueDate: undefined, urgency: 'Medium', sortOrder: items.length, isDone: false },
      ]);
      this.newTitle.set('');
    });
  }
}
