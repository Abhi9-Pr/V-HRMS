import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateTodoItemRequest, TodosClient, TodoUrgency } from '../../../core/api/generated/api-client';

@Injectable({ providedIn: 'root' })
export class TodosFacade {
  private readonly client = inject(TodosClient);

  create(request: CreateTodoItemRequest): Observable<string> {
    return this.client.todos_Create(request);
  }

  setDone(id: string, done: boolean): Observable<void> {
    return this.client.todos_SetDone(id, done);
  }

  setUrgency(id: string, urgency: TodoUrgency): Observable<void> {
    return this.client.todos_SetUrgency(id, urgency);
  }

  reorder(orderedTodoItemIds: string[]): Observable<void> {
    return this.client.todos_Reorder(orderedTodoItemIds);
  }
}
