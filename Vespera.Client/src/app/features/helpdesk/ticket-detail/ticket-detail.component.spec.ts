import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TicketDto, TicketStatus } from '../../../core/api/generated/api-client';
import { AuthService } from '../../../core/auth/auth.service';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { TicketDetailComponent } from './ticket-detail.component';

describe('TicketDetailComponent', () => {
  let fixture: ComponentFixture<TicketDetailComponent>;
  let facade: jest.Mocked<Pick<HelpdeskFacade, 'loadTicketById' | 'ticket' | 'ticketLoading' | 'ticketError'>>;

  function setup(ticket: TicketDto): void {
    facade = {
      loadTicketById: jest.fn(),
      ticket: signal(ticket),
      ticketLoading: signal(false),
      ticketError: signal(null),
    } as never;

    TestBed.configureTestingModule({
      imports: [TicketDetailComponent],
      providers: [
        provideNoopAnimations(),
        { provide: HelpdeskFacade, useValue: facade },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: ticket.id }) } } },
        { provide: AuthService, useValue: { permissions: signal(['Helpdesk.RaiseTickets', 'Helpdesk.ManageTickets']) } },
      ],
    });

    fixture = TestBed.createComponent(TicketDetailComponent);
    fixture.detectChanges();
  }

  function testIds(): string[] {
    return Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map(
      (element: Element) => (element as HTMLElement).getAttribute('data-testid')!,
    );
  }

  it('should load the ticket on init', () => {
    setup({ id: 't1', status: TicketStatus._0, comments: [] });

    expect(facade.loadTicketById).toHaveBeenCalledWith('t1');
  });

  it('an Open ticket should show assign and resolve, not close or rate', () => {
    setup({ id: 't1', status: TicketStatus._0, comments: [] });

    expect(testIds()).toEqual(expect.arrayContaining(['assign-action', 'resolve-action']));
    expect(testIds()).not.toContain('close-action');
    expect(testIds()).not.toContain('rate-satisfaction-action');
  });

  it('a Resolved ticket should show only close, not assign/resolve/rate', () => {
    setup({ id: 't1', status: TicketStatus._3, comments: [] });

    expect(testIds()).toEqual(['close-action']);
  });

  it('a Closed ticket with no rating yet should show only rate satisfaction', () => {
    setup({ id: 't1', status: TicketStatus._4, satisfactionRating: undefined, comments: [] });

    expect(testIds()).toEqual(['rate-satisfaction-action']);
  });

  it('a Closed, already-rated ticket should show no actions at all', () => {
    setup({ id: 't1', status: TicketStatus._4, satisfactionRating: 5, comments: [] });

    expect(testIds()).toHaveLength(0);
  });

  it('a reply should render nested (indented) under the comment it replied to, after it', () => {
    setup({
      id: 't1',
      status: TicketStatus._0,
      comments: [
        { id: 'c1', authorId: 'agent-1', body: 'Root comment', isInternal: false, createdAt: new Date('2026-01-01') },
        { id: 'c2', authorId: 'agent-2', body: 'A reply', isInternal: false, createdAt: new Date('2026-01-02'), parentCommentId: 'c1' },
      ],
    });

    const items = Array.from(fixture.nativeElement.querySelectorAll('li')) as HTMLElement[];
    expect(items).toHaveLength(2);
    expect(items[0].textContent).toContain('Root comment');
    expect(items[1].textContent).toContain('A reply');

    const rootIndent = items[0].style.marginLeft;
    const replyIndent = items[1].style.marginLeft;
    expect(parseFloat(replyIndent)).toBeGreaterThan(parseFloat(rootIndent || '0'));
  });

  it('an internal-only comment should render the internal-note badge, a public one should not', () => {
    setup({
      id: 't1',
      status: TicketStatus._0,
      comments: [
        { id: 'c1', authorId: 'agent-1', body: 'Public note', isInternal: false, createdAt: new Date('2026-01-01') },
        { id: 'c2', authorId: 'agent-2', body: 'Internal note', isInternal: true, createdAt: new Date('2026-01-02') },
      ],
    });

    const badges = fixture.nativeElement.querySelectorAll('[data-testid="internal-note-badge"]');
    expect(badges).toHaveLength(1);
    expect((badges[0] as HTMLElement).closest('li')?.textContent).toContain('Internal note');
  });
});
