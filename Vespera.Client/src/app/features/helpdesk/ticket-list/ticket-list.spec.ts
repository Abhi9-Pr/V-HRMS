import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { HelpdeskService } from '../helpdesk.service';
import { TicketPriority, TicketStatus } from '../helpdesk.models';
import { TicketList } from './ticket-list';

describe('TicketList', () => {
  let helpdeskServiceSpy: jasmine.SpyObj<HelpdeskService>;

  beforeEach(async () => {
    helpdeskServiceSpy = jasmine.createSpyObj<HelpdeskService>('HelpdeskService', ['getTickets']);
    helpdeskServiceSpy.getTickets.and.returnValue(
      of({
        items: [
          {
            id: 'ticket-1',
            subject: 'Laptop not booting',
            priority: TicketPriority.High,
            status: TicketStatus.Open,
            categoryId: 'cat-1',
            assignedTo: null,
            raisedAt: '2026-01-01T09:00:00Z',
            dueAt: '2026-01-02T09:00:00Z',
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      }),
    );

    await TestBed.configureTestingModule({
      imports: [TicketList],
      providers: [provideRouter([]), { provide: HelpdeskService, useValue: helpdeskServiceSpy }],
    }).compileComponents();
  });

  it('loads and renders tickets from the service on init', () => {
    const fixture = TestBed.createComponent(TicketList);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(helpdeskServiceSpy.getTickets).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
    expect(component['tickets']()).toEqual(jasmine.arrayWithExactContents([jasmine.objectContaining({ id: 'ticket-1' })]));
    expect(component['totalCount']()).toBe(1);
  });

  it('re-requests the next page with the paginator page size', () => {
    const fixture = TestBed.createComponent(TicketList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.onPage({ pageIndex: 1, pageSize: 10, length: 1 });

    expect(helpdeskServiceSpy.getTickets).toHaveBeenCalledWith({ page: 2, pageSize: 10 });
  });
});
