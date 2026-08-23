/** Mirrors Vespera.Application.Authorization.Permissions on the backend — every route's
 * `data['permissions']` and every `*vesperaHasPermission`/vespera-permission-button check
 * references one of these constants, never a bare string, for the same reason the backend
 * catalog exists: a typo fails to compile instead of silently checking a permission that can
 * never be granted. Keep in sync by hand when the backend catalog changes. */
export const Permissions = {
  Employees: {
    Read: 'Employees.Read',
    Write: 'Employees.Write',
    ReadAny: 'Employees.ReadAny',
  },
  Payroll: {
    Read: 'Payroll.Read',
    Write: 'Payroll.Write',
    Finalize: 'Payroll.Finalize',
    SelfService: 'Payroll.SelfService',
  },
  Leave: {
    Request: 'Leave.Request',
    Approve: 'Leave.Approve',
    Cancel: 'Leave.Cancel',
    Encash: 'Leave.Encash',
    ReadTeam: 'Leave.ReadTeam',
    ManageDelegation: 'Leave.ManageDelegation',
  },
  Departments: {
    Read: 'Departments.Read',
    Manage: 'Departments.Manage',
  },
  Users: {
    Manage: 'Users.Manage',
  },
  Finance: {
    Admin: 'Finance.Admin',
  },
} as const;
