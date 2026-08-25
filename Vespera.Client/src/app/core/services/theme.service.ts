import { Injectable, signal } from '@angular/core';

export type VesperaTheme = 'light' | 'dark';

const THEME_KEY = 'vespera.theme';

/** Toggles the `data-theme` attribute tokens.dark.css and tailwind.config.js's darkMode selector
 * both key off. A UI preference, not a security-sensitive token, so localStorage (persists across
 * tabs/sessions) is the right store — unlike the auth tokens in TokenStorageService. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly themeSignal = signal<VesperaTheme>(this.readInitialTheme());

  readonly theme = this.themeSignal.asReadonly();

  constructor() {
    this.apply(this.themeSignal());
  }

  toggle(): void {
    this.set(this.themeSignal() === 'dark' ? 'light' : 'dark');
  }

  set(theme: VesperaTheme): void {
    this.themeSignal.set(theme);
    localStorage.setItem(THEME_KEY, theme);
    this.apply(theme);
  }

  private apply(theme: VesperaTheme): void {
    document.documentElement.setAttribute('data-theme', theme);
  }

  private readInitialTheme(): VesperaTheme {
    const stored = localStorage.getItem(THEME_KEY);
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }

    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
