import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { LoginCredentials } from '../../models/user.interface';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  @Output() switchToSignup = new EventEmitter<void>();
  credentials: LoginCredentials = {
    username: '',
    password: ''
  };
  
  isLoading = false;
  errorMessage = '';
  showPassword = false;

  constructor(private authService: AuthService) {}

  onLogin(): void {
    if (!this.credentials.username.trim() || !this.credentials.password.trim()) {
      this.errorMessage = 'Please fill in all fields';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login(this.credentials).subscribe({
      next: (user) => {
        this.isLoading = false;
        // Navigation will be handled by the parent component
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.message || 'Login failed. Please try again.';
      }
    });
  }

  onGuestLogin(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.authService.loginAsGuest().subscribe({
      next: (user) => {
        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Failed to login as guest. Please try again.';
      }
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  clearError(): void {
    this.errorMessage = '';
  }
}