import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from 'vespera-shared';
import { AUTO_LOGOUT_TIMEOUT_MS, AutoLogoutService } from './auto-logout.service';

jest.mock('@capacitor/app', () => ({
  App: { addListener: jest.fn().mockResolvedValue({ remove: jest.fn() }) },
}));

describe('AutoLogoutService', () => {
  let service: AutoLogoutService;
  let authService: { isAuthenticated: jest.Mock; logout: jest.Mock };
  let router: { navigateByUrl: jest.Mock };

  beforeEach(() => {
    authService = { isAuthenticated: jest.fn().mockReturnValue(true), logout: jest.fn().mockReturnValue(of(undefined)) };
    router = { navigateByUrl: jest.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: AUTO_LOGOUT_TIMEOUT_MS, useValue: 1000 },
      ],
    });
    service = TestBed.inject(AutoLogoutService);
  });

  it('does nothing on backgrounding — it only records the timestamp', () => {
    service.handleAppStateChange({ isActive: false });

    expect(authService.logout).not.toHaveBeenCalled();
  });

  it('logs out and redirects to login when the background gap exceeds the timeout', () => {
    const nowSpy = jest.spyOn(Date, 'now');
    nowSpy.mockReturnValueOnce(0); // backgrounded at t=0
    service.handleAppStateChange({ isActive: false });

    nowSpy.mockReturnValueOnce(1500); // foregrounded at t=1500, past the 1000ms timeout
    service.handleAppStateChange({ isActive: true });

    expect(authService.logout).toHaveBeenCalledTimes(1);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/auth/login?reason=session-timeout');

    nowSpy.mockRestore();
  });

  it('does not log out when the background gap is under the timeout', () => {
    const nowSpy = jest.spyOn(Date, 'now');
    nowSpy.mockReturnValueOnce(0);
    service.handleAppStateChange({ isActive: false });

    nowSpy.mockReturnValueOnce(500); // well under the 1000ms timeout
    service.handleAppStateChange({ isActive: true });

    expect(authService.logout).not.toHaveBeenCalled();

    nowSpy.mockRestore();
  });

  it('does not log out on foreground when there was no prior background event', () => {
    service.handleAppStateChange({ isActive: true });

    expect(authService.logout).not.toHaveBeenCalled();
  });

  it('does not log out on foreground when no session is active', () => {
    authService.isAuthenticated.mockReturnValue(false);

    const nowSpy = jest.spyOn(Date, 'now');
    nowSpy.mockReturnValueOnce(0);
    service.handleAppStateChange({ isActive: false });

    nowSpy.mockReturnValueOnce(5000);
    service.handleAppStateChange({ isActive: true });

    expect(authService.logout).not.toHaveBeenCalled();

    nowSpy.mockRestore();
  });
});
