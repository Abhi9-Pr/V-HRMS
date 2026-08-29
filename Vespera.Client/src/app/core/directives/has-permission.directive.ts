import { Directive, EffectRef, Input, OnDestroy, TemplateRef, ViewContainerRef, effect, inject } from '@angular/core';
import { AuthService, hasPermission } from 'vespera-shared';

/** `*vesperaHasPermission="'Departments.Manage'"` — renders the host element only if the current
 * user has the permission (or every permission in an array). Structural, like *ngIf. Shares
 * hasPermission() with permissionGuard and vespera-permission-button, so the three can't drift. */
@Directive({
  selector: '[vesperaHasPermission]',
  standalone: true,
})
export class HasPermissionDirective implements OnDestroy {
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly auth = inject(AuthService);

  private required: string | string[] | undefined;
  private rendered = false;
  private readonly effectRef: EffectRef;

  constructor() {
    this.effectRef = effect(() => this.render(this.auth.permissions()));
  }

  @Input({ required: true })
  set vesperaHasPermission(value: string | string[]) {
    this.required = value;
    this.render(this.auth.permissions());
  }

  private render(granted: readonly string[]): void {
    const allowed = hasPermission(granted, this.required);

    if (allowed && !this.rendered) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.rendered = true;
    } else if (!allowed && this.rendered) {
      this.viewContainer.clear();
      this.rendered = false;
    }
  }

  ngOnDestroy(): void {
    this.effectRef.destroy();
  }
}
