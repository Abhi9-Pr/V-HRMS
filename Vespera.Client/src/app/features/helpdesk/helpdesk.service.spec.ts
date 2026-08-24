import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { HelpdeskService } from './helpdesk.service';
import { TicketPriority } from './helpdesk.models';

describe('HelpdeskService', () => {
  let service: HelpdeskService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [HelpdeskService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(HelpdeskService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('raiseTicket posts to /api/v1/helpdesk/tickets with an Idempotency-Key header', () => {
    service
      .raiseTicket({
        categoryId: 'cat-1',
        subject: 'Laptop not booting',
        description: "Won't power on.",
        priority: TicketPriority.High,
      })
      .subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/tickets');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.subject).toBe('Laptop not booting');
    expect(req.request.body.priority).toBe(TicketPriority.High);
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'ticket-1' });
  });

  it('uploadAttachment posts multipart form data with an Idempotency-Key header', () => {
    const file = new File(['bytes'], 'photo.png', { type: 'image/png' });
    service.uploadAttachment('ticket-1', file).subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/tickets/ticket-1/attachments');
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBeTrue();
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ reference: 'att-ref-1' });
  });

  it('addComment posts the comment body, internal flag, parent id, and attachment references', () => {
    service
      .addComment('ticket-1', {
        body: 'Reply body',
        isInternal: true,
        parentCommentId: 'comment-1',
        attachmentReferences: ['att-ref-1'],
      })
      .subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/tickets/ticket-1/comments');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      body: 'Reply body',
      isInternal: true,
      parentCommentId: 'comment-1',
      attachmentReferences: ['att-ref-1'],
    });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });

  it('assignTicket posts the employee id', () => {
    service.assignTicket('ticket-1', { employeeId: 'emp-1' }).subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/tickets/ticket-1/assign');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ employeeId: 'emp-1' });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });

  it('resolveTicket and closeTicket post with no body but an Idempotency-Key header', () => {
    service.resolveTicket('ticket-1').subscribe();
    const resolveReq = httpMock.expectOne('/api/v1/helpdesk/tickets/ticket-1/resolve');
    expect(resolveReq.request.method).toBe('POST');
    expect(resolveReq.request.headers.has('Idempotency-Key')).toBeTrue();
    resolveReq.flush(null);

    service.closeTicket('ticket-1').subscribe();
    const closeReq = httpMock.expectOne('/api/v1/helpdesk/tickets/ticket-1/close');
    expect(closeReq.request.method).toBe('POST');
    expect(closeReq.request.headers.has('Idempotency-Key')).toBeTrue();
    closeReq.flush(null);
  });

  it('rateSatisfaction posts the rating', () => {
    service.rateSatisfaction('ticket-1', { rating: 5 }).subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/tickets/ticket-1/satisfaction');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ rating: 5 });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });

  it('getTickets sends paging params with no Idempotency-Key header (a GET)', () => {
    service.getTickets({ page: 2, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/helpdesk/tickets');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.headers.has('Idempotency-Key')).toBeFalse();
    req.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
  });

  it('getTicketById gets the ticket by id', () => {
    service.getTicketById('ticket-1').subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/tickets/ticket-1');
    expect(req.request.method).toBe('GET');
    req.flush(null);
  });

  it('getSlaComplianceReport gets the report with no query params', () => {
    service.getSlaComplianceReport().subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/tickets/sla-compliance-report');
    expect(req.request.method).toBe('GET');
    req.flush(null);
  });

  it('createCategory posts to /api/v1/helpdesk/ticket-categories', () => {
    service.createCategory({ name: 'IT Support', departmentId: 'dept-1', defaultSlaPolicyId: 'sla-1' }).subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/ticket-categories');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'IT Support', departmentId: 'dept-1', defaultSlaPolicyId: 'sla-1' });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'cat-1' });
  });

  it('getCategories sends paging params', () => {
    service.getCategories({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/helpdesk/ticket-categories');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
  });

  it('createSlaPolicy posts business hours as HH:mm:ss strings', () => {
    service
      .createSlaPolicy({
        name: 'Standard',
        responseTimeHours: 4,
        resolutionTimeHours: 24,
        businessHoursStart: '09:00:00',
        businessHoursEnd: '18:00:00',
      })
      .subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/sla-policies');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      name: 'Standard',
      responseTimeHours: 4,
      resolutionTimeHours: 24,
      businessHoursStart: '09:00:00',
      businessHoursEnd: '18:00:00',
    });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'sla-1' });
  });

  it('getSlaPolicies sends paging params', () => {
    service.getSlaPolicies({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/helpdesk/sla-policies');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
  });

  it('createHoliday posts the date and name', () => {
    service.createHoliday({ date: '2026-01-26', name: 'Republic Day' }).subscribe();

    const req = httpMock.expectOne('/api/v1/helpdesk/public-holidays');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ date: '2026-01-26', name: 'Republic Day' });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'holiday-1' });
  });

  it('getHolidays sends paging params', () => {
    service.getHolidays({ page: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/helpdesk/public-holidays');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
  });
});
