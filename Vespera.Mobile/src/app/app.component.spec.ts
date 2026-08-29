import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideVesperaApiClients, TokenStorageService } from 'vespera-shared';
import { AppComponent } from './app.component';
import { SecureTokenStorageService } from './core/auth/secure-token-storage.service';

jest.mock('@capacitor/app', () => ({
  App: { addListener: jest.fn().mockResolvedValue({ remove: jest.fn() }) },
}));

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideVesperaApiClients('https://localhost:7095'),
        { provide: TokenStorageService, useClass: SecureTokenStorageService },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
