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
    ManagePolicy: 'Leave.ManagePolicy',
    ManageBlackout: 'Leave.ManageBlackout',
    ManageDelegation: 'Leave.ManageDelegation',
  },
  Departments: {
    Read: 'Departments.Read',
    Manage: 'Departments.Manage',
  },
  Designations: {
    Read: 'Designations.Read',
    Manage: 'Designations.Manage',
  },
  Locations: {
    Read: 'Locations.Read',
    Manage: 'Locations.Manage',
  },
  EmployeeDocuments: {
    Read: 'EmployeeDocuments.Read',
    Manage: 'EmployeeDocuments.Manage',
    Unmask: 'EmployeeDocuments.Unmask',
  },
  ReportingRelationships: {
    Read: 'ReportingRelationships.Read',
    Manage: 'ReportingRelationships.Manage',
  },
  OrgChart: {
    Read: 'OrgChart.Read',
  },
  Onboarding: {
    Read: 'Onboarding.Read',
    Manage: 'Onboarding.Manage',
  },
  EmployeeImport: {
    Manage: 'EmployeeImport.Manage',
  },
  Offboarding: {
    Read: 'Offboarding.Read',
    Manage: 'Offboarding.Manage',
  },
  Shifts: {
    Read: 'Shifts.Read',
    Manage: 'Shifts.Manage',
  },
  RotationPatterns: {
    Read: 'RotationPatterns.Read',
    Manage: 'RotationPatterns.Manage',
  },
  Rosters: {
    Read: 'Rosters.Read',
    Manage: 'Rosters.Manage',
    Publish: 'Rosters.Publish',
  },
  Attendance: {
    Read: 'Attendance.Read',
    ReadTeam: 'Attendance.ReadTeam',
    ManageTeam: 'Attendance.ManageTeam',
  },
  Regularizations: {
    Request: 'Regularizations.Request',
    Approve: 'Regularizations.Approve',
    ReadTeam: 'Regularizations.ReadTeam',
  },
  Holidays: {
    Read: 'Holidays.Read',
    Manage: 'Holidays.Manage',
  },
  BiometricDevices: {
    Manage: 'BiometricDevices.Manage',
  },
  Expenses: {
    Submit: 'Expenses.Submit',
    Approve: 'Expenses.Approve',
    ManagePolicy: 'Expenses.ManagePolicy',
    Settle: 'Expenses.Settle',
  },
  Assets: {
    Read: 'Assets.Read',
    Write: 'Assets.Write',
    Assign: 'Assets.Assign',
    Recover: 'Assets.Recover',
  },
  Licenses: {
    Read: 'Licenses.Read',
    Manage: 'Licenses.Manage',
  },
  Recruitment: {
    ManageRequisitions: 'Recruitment.ManageRequisitions',
    ApproveRequisitions: 'Recruitment.ApproveRequisitions',
    ManageCandidates: 'Recruitment.ManageCandidates',
    ManageInterviews: 'Recruitment.ManageInterviews',
    ManageOffers: 'Recruitment.ManageOffers',
    ConvertToEmployee: 'Recruitment.ConvertToEmployee',
  },
  Helpdesk: {
    RaiseTickets: 'Helpdesk.RaiseTickets',
    ManageTickets: 'Helpdesk.ManageTickets',
    ManageConfiguration: 'Helpdesk.ManageConfiguration',
    ViewReports: 'Helpdesk.ViewReports',
  },
  Users: {
    Manage: 'Users.Manage',
  },
  Finance: {
    Admin: 'Finance.Admin',
  },
  Workspace: {
    ViewDashboard: 'Workspace.ViewDashboard',
    ManageAnnouncements: 'Workspace.ManageAnnouncements',
    ManageEvents: 'Workspace.ManageEvents',
  },
} as const;
