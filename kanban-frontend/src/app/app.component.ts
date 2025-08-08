import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KanbanBoardComponent } from './components/kanban-board/kanban-board.component';
import { AuthComponent } from './components/auth/auth.component';
import { AppHeaderComponent } from './components/app-header/app-header.component';
import { AuthService } from './services/auth.service';
import { AuthState } from './models/user.interface';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, KanbanBoardComponent, AuthComponent, AppHeaderComponent],
  template: `
    <div class="app-container">
      <app-auth *ngIf="!authState.isAuthenticated"></app-auth>
      <div *ngIf="authState.isAuthenticated" class="main-app">
        <app-header></app-header>
        <main class="main-content">
          <app-kanban-board></app-kanban-board>
        </main>
      </div>
    </div>
  `,
  styles: [`
    .app-container {
      min-height: 100vh;
      background: var(--bg-gradient);
    }
    
    .main-app {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }
    
    .main-content {
      flex: 1;
      overflow-y: auto;
    }
  `]
})
export class AppComponent implements OnInit {
  title = 'Kanban Board';
  
  authState: AuthState = {
    user: null,
    isAuthenticated: false,
    isLoading: true
  };

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.authService.authState$.subscribe(state => {
      this.authState = state;
    });
  }
}