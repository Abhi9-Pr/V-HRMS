import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { HelpdeskService } from '../helpdesk.service';
import { TicketCommentDto, TicketDto, TicketPriority, TicketStatus } from '../helpdesk.models';
import { TicketDetail } from './ticket-detail';

function ticket(overrides: Partial<TicketDto>): TicketDto {
  return {
    id: 'ticket-1',
    subject: 'Laptop not booting',
    description: "Won't power on.",
    priority: TicketPriority.High,
    status: TicketStatus.Open,
    categoryId: 'cat-1',
    raisedBy: 'emp-1',
    assignedTo: null,
    raisedAt: '2026-01-01T09:00:00Z',
    dueAt: '2026-01-02T09:00:00Z',
    resolvedAt: null,
    slaBreachNotified: false,
    slaWarningNotified: false,
    satisfactionRating: null,
    comments: [],
    ...overrides,
  };
}

describe('TicketDetail', () => {
  let helpdeskServiceSpy: jasmine.SpyObj<HelpdeskService>;

  function setUp(dto: TicketDto) {
    helpdeskServiceSpy = jasmine.createSpyObj<HelpdeskService>('HelpdeskService', ['getTicketById']);
    helpdeskServiceSpy.getTicketById.and.returnValue(of(dto));

    return TestBed.configureTestingModule({
      imports: [TicketDetail],
      providers: [
        provideRouter([]),
        { provide: HelpdeskService, useValue: helpdeskServiceSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: dto.id }) } } },
      ],
    }).compileComponents();
  }

  it('shows only Assign and Resolve for an Open ticket', async () => {
    await setUp(ticket({ status: TicketStatus.Open }));
    const fixture = TestBed.createComponent(TicketDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.canAssign(TicketStatus.Open)).toBeTrue();
    expect(component.canResolve(TicketStatus.Open)).toBeTrue();
    expect(component.canClose(TicketStatus.Open)).toBeFalse();
    expect(component.canRate(TicketStatus.Open)).toBeFalse();

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map((el: any) => el.getAttribute('data-testid'));
    expect(testIds).toContain('action-assign');
    expect(testIds).toContain('action-resolve');
    expect(testIds).not.toContain('action-close');
    expect(testIds).not.toContain('action-rate');
  });

  it('shows only Rate Satisfaction for a Closed ticket', async () => {
    await setUp(ticket({ status: TicketStatus.Closed }));
    const fixture = TestBed.createComponent(TicketDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.canAssign(TicketStatus.Closed)).toBeFalse();
    expect(component.canResolve(TicketStatus.Closed)).toBeFalse();
    expect(component.canClose(TicketStatus.Closed)).toBeFalse();
    expect(component.canRate(TicketStatus.Closed)).toBeTrue();

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map((el: any) => el.getAttribute('data-testid'));
    expect(testIds).not.toContain('action-assign');
    expect(testIds).not.toContain('action-resolve');
    expect(testIds).not.toContain('action-close');
    expect(testIds).toContain('action-rate');
  });

  it('shows only Close for a Resolved ticket', async () => {
    await setUp(ticket({ status: TicketStatus.Resolved }));
    const fixture = TestBed.createComponent(TicketDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.canClose(TicketStatus.Resolved)).toBeTrue();
    expect(component.canAssign(TicketStatus.Resolved)).toBeFalse();
    expect(component.canResolve(TicketStatus.Resolved)).toBeFalse();

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map((el: any) => el.getAttribute('data-testid'));
    expect(testIds).toContain('action-close');
    expect(testIds).not.toContain('action-assign');
  });

  it('threads a reply under the comment it was replying to', async () => {
    const comments: TicketCommentDto[] = [
      {
        id: 'root-1',
        authorId: 'emp-1',
        body: 'Original issue report',
        isInternal: false,
        createdAt: '2026-01-01T09:00:00Z',
        parentCommentId: null,
        attachmentReferences: [],
      },
      {
        id: 'reply-1',
        authorId: 'emp-2',
        body: 'Have you tried the power cable?',
        isInternal: true,
        createdAt: '2026-01-01T10:00:00Z',
        parentCommentId: 'root-1',
        attachmentReferences: [],
      },
      {
        id: 'root-2',
        authorId: 'emp-1',
        body: 'A separate, unrelated top-level comment',
        isInternal: false,
        createdAt: '2026-01-01T11:00:00Z',
        parentCommentId: null,
        attachmentReferences: [],
      },
    ];

    await setUp(ticket({ comments }));
    const fixture = TestBed.createComponent(TicketDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    const threaded = component.threadedComments(comments);

    expect(threaded.map((entry) => entry.comment.id)).toEqual(['root-1', 'reply-1', 'root-2']);
    expect(threaded.find((entry) => entry.comment.id === 'root-1')!.depth).toBe(0);
    expect(threaded.find((entry) => entry.comment.id === 'reply-1')!.depth).toBe(1);
    expect(threaded.find((entry) => entry.comment.id === 'root-2')!.depth).toBe(0);

    // The internal-only reply is visually badged in the rendered thread.
    const replyElement = fixture.nativeElement.querySelector('[data-testid="comment-reply-1"]');
    expect(replyElement.querySelector('[data-testid="internal-badge"]')).toBeTruthy();
    const rootElement = fixture.nativeElement.querySelector('[data-testid="comment-root-1"]');
    expect(rootElement.querySelector('[data-testid="internal-badge"]')).toBeFalsy();
  });
});
