import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AutoLogoutService } from './core/auth/auto-logout.service';

@Component({
  selector: 'vespera-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  // Injected purely as a side effect — its constructor is what starts the appStateChange
  // listener. Nothing here calls it directly.
  private readonly autoLogout = inject(AutoLogoutService);
}
