import { NgTemplateOutlet } from '@angular/common';
import { Component, Input, computed, inject, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { AuthService, hasPermission } from 'vespera-shared';

/** `<vespera-permission-button [permission]="'Departments.Manage'" (clicked)="...">Edit
 * </vespera-permission-button>` — hides (not just disables) the action when the user lacks the
 * permission, using the same hasPermission() check as the guard and the structural directive. */
@Component({
    selector: 'vespera-permission-button',
    imports: [MatButtonModule, NgTemplateOutlet],
    templateUrl: './permission-button.component.html'
})
export class PermissionButtonComponent {
  private readonly auth = inject(AuthService);

  @Input({ required: true }) permission!: string | string[];
  @Input() color: 'primary' | 'accent' | 'warn' = 'primary';
  @Input() variant: 'flat' | 'stroked' | 'icon' = 'stroked';
  @Input() disabled = false;

  readonly clicked = output<void>();

  readonly allowed = computed(() => hasPermission(this.auth.permissions(), this.permission));
}
