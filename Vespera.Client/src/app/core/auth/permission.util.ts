/** Shared by permissionGuard, *vesperaHasPermission, and vespera-permission-button — one
 * definition of "does this user have this permission" so the three never drift apart. */
export function hasPermission(granted: readonly string[], required: string | string[] | undefined | null): boolean {
  if (!required || (Array.isArray(required) && required.length === 0)) {
    return true;
  }

  const requiredList = Array.isArray(required) ? required : [required];
  return requiredList.every((permission) => granted.includes(permission));
}
