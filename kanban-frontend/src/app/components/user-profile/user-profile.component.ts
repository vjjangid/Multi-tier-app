import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { User } from '../../models/user.interface';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit {
  user: User | null = null;
  showDropdown = false;
  currentTheme = 'light';

  constructor(
    private authService: AuthService,
    private themeService: ThemeService
  ) {}

  ngOnInit(): void {
    this.authService.authState$.subscribe(state => {
      this.user = state.user;
    });
    
    this.themeService.theme$.subscribe(theme => {
      this.currentTheme = theme;
    });
  }

  toggleDropdown(): void {
    this.showDropdown = !this.showDropdown;
  }

  closeDropdown(): void {
    this.showDropdown = false;
  }

  onLogout(): void {
    this.authService.logout();
    this.closeDropdown();
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  getUserInitials(): string {
    if (!this.user) return '';
    
    if (this.user.isGuest) {
      return '👤';
    }
    
    const names = this.user.fullName.split(' ');
    if (names.length >= 2) {
      return `${names[0][0]}${names[names.length - 1][0]}`.toUpperCase();
    }
    return this.user.fullName.substring(0, 2).toUpperCase();
  }

  get userDisplayName(): string {
    return this.user?.isGuest ? 'Guest' : this.user?.fullName || '';
  }

  get userRole(): string {
    return this.user?.isGuest ? 'Guest User' : 'Registered User';
  }

  // Close dropdown when clicking outside
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.user-profile')) {
      this.closeDropdown();
    }
  }
}