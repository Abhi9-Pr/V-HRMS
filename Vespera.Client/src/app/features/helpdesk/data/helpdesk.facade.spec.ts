import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import {
  PublicHolidaysClient,
  SlaPoliciesClient,
  TicketCategoriesClient,
  TicketsClient,
} from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { HelpdeskFacade } from './helpdesk.facade';

describe('HelpdeskFacade', () => {
  let ticketsClient: jest.Mocked<
    Pick<
      TicketsClient,
      | 'tickets_GetTickets'
      | 'tickets_GetTicketById'
      | 'tickets_GetSlaComplianceReport'
      | 'tickets_RaiseTicket'
      | 'tickets_UploadAttachment'
      | 'tickets_AddComment'
      | 'tickets_Assign'
      | 'tickets_Resolve'
      | 'tickets_Close'
      | 'tickets_RateSatisfaction'
    >
  >;
  let categoriesClient: jest.Mocked<Pick<TicketCategoriesClient, 'ticketCategories_GetCategories' | 'ticketCategories_Create'>>;
  let policiesClient: jest.Mocked<Pick<SlaPoliciesClient, 'slaPolicies_GetPolicies' | 'slaPolicies_Create'>>;
  let holidaysClient: jest.Mocked<Pick<PublicHolidaysClient, 'publicHolidays_GetHolidays' | 'publicHolidays_Create'>>;
  let facade: HelpdeskFacade;

  beforeEach(() => {
    ticketsClient = {
      tickets_GetTickets: jest.fn(),
      tickets_GetTicketById: jest.fn(),
      tickets_GetSlaComplianceReport: jest.fn(),
      tickets_RaiseTicket: jest.fn(),
      tickets_UploadAttachment: jest.fn(),
      tickets_AddComment: jest.fn(),
      tickets_Assign: jest.fn(),
      tickets_Resolve: jest.fn(),
      tickets_Close: jest.fn(),
      tickets_RateSatisfaction: jest.fn(),
    };
    categoriesClient = { ticketCategories_GetCategories: jest.fn(), ticketCategories_Create: jest.fn() };
    policiesClient = { slaPolicies_GetPolicies: jest.fn(), slaPolicies_Create: jest.fn() };
    holidaysClient = { publicHolidays_GetHolidays: jest.fn(), publicHolidays_Create: jest.fn() };

    TestBed.configureTestingModule({
      providers: [
        HelpdeskFacade,
        { provide: TicketsClient, useValue: ticketsClient },
        { provide: TicketCategoriesClient, useValue: categoriesClient },
        { provide: SlaPoliciesClient, useValue: policiesClient },
        { provide: PublicHolidaysClient, useValue: holidaysClient },
      ],
    });

    facade = TestBed.inject(HelpdeskFacade);
  });

  it('loadTickets() should populate tickets/totalCount on success', () => {
    ticketsClient.tickets_GetTickets.mockReturnValue(
      of({ items: [{ id: 't1', status: 0 }], page: 1, pageSize: 20, totalCount: 1 } as never),
    );

    facade.loadTickets({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.ticketsLoading()).toBe(false);
    expect(facade.ticketsError()).toBeNull();
    expect(facade.tickets()).toHaveLength(1);
    expect(facade.ticketsTotalCount()).toBe(1);
    expect(ticketsClient.tickets_GetTickets).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('loadTickets() should populate error on failure and stop loading', () => {
    const apiError: ApiError = { status: 500, code: 'server_error', message: 'boom' };
    ticketsClient.tickets_GetTickets.mockReturnValue(throwError(() => apiError));

    facade.loadTickets({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.ticketsLoading()).toBe(false);
    expect(facade.ticketsError()).toEqual(apiError);
    expect(facade.tickets()).toHaveLength(0);
  });

  it('loadTicketById() should populate ticket on success', () => {
    ticketsClient.tickets_GetTicketById.mockReturnValue(of({ id: 't1', status: 0, comments: [] } as never));

    facade.loadTicketById('t1');

    expect(facade.ticketLoading()).toBe(false);
    expect(facade.ticket()?.id).toBe('t1');
    expect(ticketsClient.tickets_GetTicketById).toHaveBeenCalledWith('t1');
  });

  it('loadSlaComplianceReport() should populate the report on success', () => {
    ticketsClient.tickets_GetSlaComplianceReport.mockReturnValue(
      of({ totalResolvedOrClosed: 10, breachedCount: 2, onTimeCount: 8, compliancePercentage: 80, byCategory: [] } as never),
    );

    facade.loadSlaComplianceReport();

    expect(facade.slaReportLoading()).toBe(false);
    expect(facade.slaReport()?.compliancePercentage).toBe(80);
    expect(ticketsClient.tickets_GetSlaComplianceReport).toHaveBeenCalledWith();
  });

  it('loadCategories() should populate categories/totalCount on success', () => {
    categoriesClient.ticketCategories_GetCategories.mockReturnValue(
      of({ items: [{ id: 'c1', name: 'IT Support' }], page: 1, pageSize: 20, totalCount: 1 } as never),
    );

    facade.loadCategories({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.categories()).toHaveLength(1);
    expect(facade.categoriesTotalCount()).toBe(1);
    expect(categoriesClient.ticketCategories_GetCategories).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('loadPolicies() should populate policies/totalCount on success', () => {
    policiesClient.slaPolicies_GetPolicies.mockReturnValue(
      of({ items: [{ id: 'p1', name: 'Standard' }], page: 1, pageSize: 20, totalCount: 1 } as never),
    );

    facade.loadPolicies({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.policies()).toHaveLength(1);
    expect(facade.policiesTotalCount()).toBe(1);
    expect(policiesClient.slaPolicies_GetPolicies).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('loadHolidays() should populate holidays/totalCount on success', () => {
    holidaysClient.publicHolidays_GetHolidays.mockReturnValue(
      of({ items: [{ id: 'h1', name: 'Republic Day' }], page: 1, pageSize: 20, totalCount: 1 } as never),
    );

    facade.loadHolidays({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.holidays()).toHaveLength(1);
    expect(facade.holidaysTotalCount()).toBe(1);
    expect(holidaysClient.publicHolidays_GetHolidays).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('raiseTicket() should delegate to the generated client with a fresh idempotency key', (done) => {
    ticketsClient.tickets_RaiseTicket.mockReturnValue(of({ id: 't1' } as never));

    facade.raiseTicket({ categoryId: 'c1', subject: 'Laptop broken', description: 'Will not boot', priority: 2 }).subscribe((result) => {
      expect(result.id).toBe('t1');
      const [idempotencyKey, body] = ticketsClient.tickets_RaiseTicket.mock.calls[0];
      expect(typeof idempotencyKey).toBe('string');
      expect(idempotencyKey!.length).toBeGreaterThan(0);
      expect(body).toEqual({ categoryId: 'c1', subject: 'Laptop broken', description: 'Will not boot', priority: 2 });
      done();
    });
  });

  it('uploadAttachment() should wrap the File into a FileParameter and delegate to the generated client', (done) => {
    ticketsClient.tickets_UploadAttachment.mockReturnValue(of({ reference: 'ref-1' } as never));
    const file = new File(['x'], 'screenshot.png', { type: 'image/png' });

    facade.uploadAttachment('t1', file).subscribe((result) => {
      expect(result.reference).toBe('ref-1');
      const [ticketId, idempotencyKey, fileParameter] = ticketsClient.tickets_UploadAttachment.mock.calls[0];
      expect(ticketId).toBe('t1');
      expect(typeof idempotencyKey).toBe('string');
      expect(fileParameter).toEqual({ data: file, fileName: 'screenshot.png' });
      done();
    });
  });

  it('addComment() should delegate to the generated client', (done) => {
    ticketsClient.tickets_AddComment.mockReturnValue(of(undefined));
    const request = { body: 'Reproduced it', isInternal: true, parentCommentId: 'comment-1', attachmentReferences: ['ref-1'] };

    facade.addComment('t1', request).subscribe(() => {
      const [ticketId, , body] = ticketsClient.tickets_AddComment.mock.calls[0];
      expect(ticketId).toBe('t1');
      expect(body).toEqual(request);
      done();
    });
  });

  it('assignTicket() should delegate to the generated client', (done) => {
    ticketsClient.tickets_Assign.mockReturnValue(of(undefined));

    facade.assignTicket('t1', { employeeId: 'e1' }).subscribe(() => {
      const [ticketId, , body] = ticketsClient.tickets_Assign.mock.calls[0];
      expect(ticketId).toBe('t1');
      expect(body).toEqual({ employeeId: 'e1' });
      done();
    });
  });

  it('resolveTicket() should delegate to the generated client', (done) => {
    ticketsClient.tickets_Resolve.mockReturnValue(of(undefined));

    facade.resolveTicket('t1').subscribe(() => {
      expect(ticketsClient.tickets_Resolve).toHaveBeenCalledWith('t1', expect.any(String));
      done();
    });
  });

  it('closeTicket() should delegate to the generated client', (done) => {
    ticketsClient.tickets_Close.mockReturnValue(of(undefined));

    facade.closeTicket('t1').subscribe(() => {
      expect(ticketsClient.tickets_Close).toHaveBeenCalledWith('t1', expect.any(String));
      done();
    });
  });

  it('rateSatisfaction() should delegate to the generated client', (done) => {
    ticketsClient.tickets_RateSatisfaction.mockReturnValue(of(undefined));

    facade.rateSatisfaction('t1', { rating: 5 }).subscribe(() => {
      const [ticketId, , body] = ticketsClient.tickets_RateSatisfaction.mock.calls[0];
      expect(ticketId).toBe('t1');
      expect(body).toEqual({ rating: 5 });
      done();
    });
  });

  it('createCategory() should delegate to the generated client', (done) => {
    categoriesClient.ticketCategories_Create.mockReturnValue(of({ id: 'c1' } as never));

    facade.createCategory({ name: 'IT Support', departmentId: 'd1' }).subscribe((result) => {
      expect(result.id).toBe('c1');
      done();
    });
  });

  it('createPolicy() should delegate to the generated client', (done) => {
    policiesClient.slaPolicies_Create.mockReturnValue(of({ id: 'p1' } as never));

    facade
      .createPolicy({ name: 'Standard', responseTimeHours: 4, resolutionTimeHours: 24, businessHoursStart: '09:00:00', businessHoursEnd: '18:00:00' })
      .subscribe((result) => {
        expect(result.id).toBe('p1');
        done();
      });
  });

  it('createHoliday() should delegate to the generated client', (done) => {
    holidaysClient.publicHolidays_Create.mockReturnValue(of({ id: 'h1' } as never));

    facade.createHoliday({ date: new Date('2026-01-26'), name: 'Republic Day' }).subscribe((result) => {
      expect(result.id).toBe('h1');
      done();
    });
  });
});
