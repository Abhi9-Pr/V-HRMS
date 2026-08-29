import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { TicketListComponent } from './ticket-list.component';
import { AuthService } from 'vespera-shared';

describe('TicketListComponent', () => {
  let fixture: ComponentFixture<TicketListComponent>;
  let facade: jest.Mocked<
    Pick<HelpdeskFacade, 'loadTickets' | 'tickets' | 'ticketsTotalCount' | 'ticketsLoading' | 'ticketsError'>
  >;
  let dialog: jest.Mocked<Pick<MatDialog, 'open'>>;
  let router: jest.Mocked<Pick<Router, 'navigate'>>;

  beforeEach(async () => {
    facade = {
      loadTickets: jest.fn(),
      tickets: signal([]),
      ticketsTotalCount: signal(0),
      ticketsLoading: signal(false),
      ticketsError: signal(null),
    } as never;

    dialog = { open: jest.fn() };
    router = { navigate: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [TicketListComponent],
      providers: [
        { provide: HelpdeskFacade, useValue: facade },
        { provide: MatDialog, useValue: dialog },
        { provide: Router, useValue: router },
        { provide: AuthService, useValue: { permissions: signal(['Helpdesk.RaiseTickets']) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketListComponent);
  });

  it('should load tickets on init', () => {
    fixture.detectChanges();

    expect(facade.loadTickets).toHaveBeenCalledWith({ page: 1, pageSize: 20, sortDescending: false });
  });

  it('should reload with the new page on queryChange', () => {
    fixture.detectChanges();
    facade.loadTickets.mockClear();

    fixture.componentInstance.onQueryChange({ page: 2, pageSize: 20, sortDescending: false });

    expect(facade.loadTickets).toHaveBeenCalledWith({ page: 2, pageSize: 20, sortDescending: false });
  });

  it('openCreateDialog should reload the list when a ticket was raised', () => {
    dialog.open.mockReturnValue({ afterClosed: () => of(true) } as never);
    fixture.detectChanges();
    facade.loadTickets.mockClear();

    fixture.componentInstance.openCreateDialog();

    expect(dialog.open).toHaveBeenCalled();
    expect(facade.loadTickets).toHaveBeenCalled();
  });

  it('openCreateDialog should not reload when the dialog is cancelled', () => {
    dialog.open.mockReturnValue({ afterClosed: () => of(false) } as never);
    fixture.detectChanges();
    facade.loadTickets.mockClear();

    fixture.componentInstance.openCreateDialog();

    expect(facade.loadTickets).not.toHaveBeenCalled();
  });

  it('openTicket should navigate to the ticket detail route', () => {
    fixture.detectChanges();

    fixture.componentInstance.openTicket({ id: 'ticket-1' });

    expect(router.navigate).toHaveBeenCalledWith(['/helpdesk', 'ticket-1']);
  });
});
