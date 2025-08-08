import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { LoginComponent } from '../login/login.component';
import { SignupComponent } from '../signup/signup.component';
import { AuthState } from '../../models/user.interface';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, LoginComponent, SignupComponent],
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css']
})
export class AuthComponent implements OnInit {
  authState: AuthState = {
    user: null,
    isAuthenticated: false,
    isLoading: true
  };
  
  showSignup = false;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.authService.authState$.subscribe(state => {
      this.authState = state;
    });
  }

  toggleAuthMode(): void {
    this.showSignup = !this.showSignup;
  }
}