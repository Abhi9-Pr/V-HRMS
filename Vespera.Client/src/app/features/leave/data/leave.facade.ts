import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ApiError,
  ApprovalInboxItemDto,
  CreateProxyDelegationCommand,
  EmployeeSummaryDto,
  EmployeesClient,
  EncashLeaveCommand,
  HolidayDto,
  HolidaysClient,
  LeaveBalanceDto,
  LeaveClient,
  LeaveRequestDto,
  LeaveTypeDto,
  LeaveTypesClient,
  ProxyDelegationDto,
  ProxyDelegationsClient,
  SubmitLeaveRequestCommand,
  SubmitLeaveRequestResponse,
  TeamLeaveCalendarDayDto,
} from 'vespera-shared';

/**
 * Wraps every generated client the leave screens need behind signals + plain methods, following
 * DepartmentsFacade's pattern: read state as signals (fire-and-forget `load*` calls), mutations
 * return the raw Observable so the calling component controls the subscribe/error UX. Shared by
 * all four leave screens (apply, approval inbox, delegation settings, team calendar) — none of
 * them talk to a generated client directly.
 */
@Injectable({ providedIn: 'root' })
export class LeaveFacade {
  private readonly leaveClient = inject(LeaveClient);
  private readonly leaveTypesClient = inject(LeaveTypesClient);
  private readonly holidaysClient = inject(HolidaysClient);
  private readonly employeesClient = inject(EmployeesClient);
  private readonly proxyDelegationsClient = inject(ProxyDelegationsClient);

  private readonly leaveTypesSignal = signal<LeaveTypeDto[]>([]);
  private readonly balanceSignal = signal<LeaveBalanceDto | null>(null);
  private readonly myRequestsSignal = signal<LeaveRequestDto[]>([]);
  private readonly approvalInboxSignal = signal<ApprovalInboxItemDto[]>([]);
  private readonly teamCalendarSignal = signal<TeamLeaveCalendarDayDto[]>([]);
  private readonly holidaysSignal = signal<HolidayDto[]>([]);
  private readonly employeesSignal = signal<EmployeeSummaryDto[]>([]);
  private readonly delegationsSignal = signal<ProxyDelegationDto[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<ApiError | null>(null);

  readonly leaveTypes = this.leaveTypesSignal.asReadonly();
  readonly balance = this.balanceSignal.asReadonly();
  readonly myRequests = this.myRequestsSignal.asReadonly();
  readonly approvalInbox = this.approvalInboxSignal.asReadonly();
  readonly teamCalendar = this.teamCalendarSignal.asReadonly();
  readonly holidays = this.holidaysSignal.asReadonly();
  readonly employees = this.employeesSignal.asReadonly();
  readonly delegations = this.delegationsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  loadLeaveTypes(): void {
    this.leaveTypesClient.listLeaveTypes().subscribe({
      next: (types) => this.leaveTypesSignal.set(types ?? []),
      error: (apiError: ApiError) => this.errorSignal.set(apiError),
    });
  }

  loadBalance(leaveTypeId: string): void {
    this.leaveClient.getLeaveBalance(leaveTypeId).subscribe({
      next: (balance) => this.balanceSignal.set(balance),
      error: (apiError: ApiError) => this.errorSignal.set(apiError),
    });
  }

  loadMyRequests(): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    this.leaveClient.getMyLeaveRequests().subscribe({
      next: (requests) => {
        this.myRequestsSignal.set(requests ?? []);
        this.loadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.errorSignal.set(apiError);
        this.loadingSignal.set(false);
      },
    });
  }

  loadApprovalInbox(): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    this.leaveClient.getApprovalInbox().subscribe({
      next: (items) => {
        this.approvalInboxSignal.set(items ?? []);
        this.loadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.errorSignal.set(apiError);
        this.loadingSignal.set(false);
      },
    });
  }

  loadTeamCalendar(from: Date, to: Date, departmentId?: string): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    this.leaveClient.getTeamLeaveCalendar(from, to, departmentId).subscribe({
      next: (days) => {
        this.teamCalendarSignal.set(days ?? []);
        this.loadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.errorSignal.set(apiError);
        this.loadingSignal.set(false);
      },
    });
  }

  /** Every holiday in the tenant, unfiltered by location — a client-side day-count preview aid
   * only; the server (location-aware) is the source of truth for what actually gets charged. */
  loadHolidays(): void {
    this.holidaysClient.list5(1, 500, undefined, undefined, undefined).subscribe({
      next: (result) => this.holidaysSignal.set(result.items ?? []),
      error: (apiError: ApiError) => this.errorSignal.set(apiError),
    });
  }

  loadEmployees(): void {
    this.employeesClient.list4(1, 500, undefined, undefined).subscribe({
      next: (result) => this.employeesSignal.set(result.items ?? []),
      error: (apiError: ApiError) => this.errorSignal.set(apiError),
    });
  }

  loadMyDelegations(): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    this.proxyDelegationsClient.getMyDelegations().subscribe({
      next: (delegations) => {
        this.delegationsSignal.set(delegations ?? []);
        this.loadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.errorSignal.set(apiError);
        this.loadingSignal.set(false);
      },
    });
  }

  submit(command: SubmitLeaveRequestCommand): Observable<SubmitLeaveRequestResponse> {
    return this.leaveClient.submitLeaveRequest(command);
  }

  approve(requestId: string): Observable<void> {
    return this.leaveClient.approveLeaveRequest(requestId);
  }

  reject(requestId: string, reason: string): Observable<void> {
    return this.leaveClient.rejectLeaveRequest(requestId, { reason });
  }

  withdraw(requestId: string): Observable<void> {
    return this.leaveClient.withdrawLeaveRequest(requestId);
  }

  cancel(requestId: string, reason: string): Observable<void> {
    return this.leaveClient.cancelLeaveRequest(requestId, { reason });
  }

  encash(command: EncashLeaveCommand): Observable<void> {
    return this.leaveClient.encashLeave(command);
  }

  createDelegation(command: CreateProxyDelegationCommand): Observable<string> {
    return this.proxyDelegationsClient.createProxyDelegation(command);
  }

  revokeDelegation(delegationId: string): Observable<void> {
    return this.proxyDelegationsClient.revokeProxyDelegation(delegationId);
  }
}
