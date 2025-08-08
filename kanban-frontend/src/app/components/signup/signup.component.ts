import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { SignupData } from '../../models/user.interface';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.css']
})
export class SignupComponent {
  @Output() switchToLogin = new EventEmitter<void>();
  signupData: SignupData = {
    username: '',
    email: '',
    fullName: '',
    password: '',
    confirmPassword: ''
  };
  
  isLoading = false;
  errorMessage = '';
  showPassword = false;
  showConfirmPassword = false;

  constructor(private authService: AuthService) {}

  onSignup(): void {
    this.errorMessage = '';

    // Validation
    if (!this.signupData.username.trim()) {
      this.errorMessage = 'Username is required';
      return;
    }

    if (!this.signupData.email.trim()) {
      this.errorMessage = 'Email is required';
      return;
    }

    if (!this.isValidEmail(this.signupData.email)) {
      this.errorMessage = 'Please enter a valid email address';
      return;
    }

    if (!this.signupData.fullName.trim()) {
      this.errorMessage = 'Full name is required';
      return;
    }

    if (!this.signupData.password) {
      this.errorMessage = 'Password is required';
      return;
    }

    if (this.signupData.password.length < 6) {
      this.errorMessage = 'Password must be at least 6 characters long';
      return;
    }

    if (this.signupData.password !== this.signupData.confirmPassword) {
      this.errorMessage = 'Passwords do not match';
      return;
    }

    this.isLoading = true;

    this.authService.signup(this.signupData).subscribe({
      next: (user) => {
        // After successful registration, automatically login
        const loginCredentials = {
          username: this.signupData.username,
          password: this.signupData.password
        };
        
        this.authService.login(loginCredentials).subscribe({
          next: (user) => {
            this.isLoading = false;
            // Navigation will be handled by the parent component
          },
          error: (error) => {
            this.isLoading = false;
            this.errorMessage = 'Registration successful, but login failed. Please try logging in manually.';
          }
        });
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.message || 'Signup failed. Please try again.';
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

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  clearError(): void {
    this.errorMessage = '';
  }

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  get passwordStrength(): string {
    const password = this.signupData.password;
    if (password.length === 0) return '';
    if (password.length < 4) return 'weak';
    if (password.length < 8) return 'medium';
    if (password.length >= 8 && /(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(password)) return 'strong';
    return 'medium';
  }

  get passwordsMatch(): boolean {
    return this.signupData.password === this.signupData.confirmPassword && this.signupData.confirmPassword.length > 0;
  }
}