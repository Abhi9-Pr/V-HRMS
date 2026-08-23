import { Injectable, computed, signal } from '@angular/core';

/** A simple in-flight-request counter — the shell's global loading indicator (and anything else
 * that cares) reads `isLoading`. loading.interceptor.ts is the only writer. */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly count = signal(0);

  readonly isLoading = computed(() => this.count() > 0);

  start(): void {
    this.count.update((value) => value + 1);
  }

  stop(): void {
    this.count.update((value) => Math.max(0, value - 1));
  }
}
