import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { User, LoginCredentials, SignupData, AuthState } from '../models/user.interface';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly AUTH_KEY = 'kanban-auth';
  private readonly TOKEN_KEY = 'kanban-token';
  private readonly apiUrl = environment.apiUrl;
  
  private authState = new BehaviorSubject<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: true
  });

  public authState$ = this.authState.asObservable();

  constructor(private http: HttpClient) {
    this.loadAuthState();
  }

  login(credentials: LoginCredentials): Observable<User> {
    const loginData = {
      username: credentials.username,
      password: credentials.password
    };

    return this.http.post<any>(`${this.apiUrl}/auth/login`, loginData).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.storeAuthData(response.data.token, response.data.user);
        }
      }),
      map(response => response.data.user),
      catchError(this.handleError.bind(this))
    );
  }

  signup(signupData: SignupData): Observable<User> {
    if (signupData.password !== signupData.confirmPassword) {
      return throwError(() => ({ message: 'Passwords do not match' }));
    }

    const registerData = {
      username: signupData.username,
      email: signupData.email,
      fullName: signupData.fullName,
      password: signupData.password
    };

    return this.http.post<any>(`${this.apiUrl}/auth/register`, registerData).pipe(
      map(response => {
        if (response.success && response.data) {
          // After successful registration, we need to login
          return response.data;
        }
        throw new Error('Registration failed');
      }),
      catchError(this.handleError.bind(this))
    );
  }

  loginAsGuest(): Observable<User> {
    return this.http.post<any>(`${this.apiUrl}/auth/guest`, {}).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.storeAuthData(response.data.token, response.data.user);
        }
      }),
      map(response => response.data.user),
      catchError(this.handleError.bind(this))
    );
  }

  logout(): void {
    localStorage.removeItem(this.AUTH_KEY);
    localStorage.removeItem(this.TOKEN_KEY);
    this.authState.next({
      user: null,
      isAuthenticated: false,
      isLoading: false
    });
  }

  getCurrentUser(): User | null {
    return this.authState.value.user;
  }

  isAuthenticated(): boolean {
    return this.authState.value.isAuthenticated;
  }

  isGuest(): boolean {
    const user = this.getCurrentUser();
    return user ? user.isGuest : false;
  }

  private storeAuthData(token: string, user: User): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    localStorage.setItem(this.AUTH_KEY, JSON.stringify(user));
    this.authState.next({
      user,
      isAuthenticated: true,
      isLoading: false
    });
  }

  private loadAuthState(): void {
    const storedUser = localStorage.getItem(this.AUTH_KEY);
    const storedToken = localStorage.getItem(this.TOKEN_KEY);
    
    if (storedUser && storedToken) {
      try {
        const user = JSON.parse(storedUser);
        user.createdAt = new Date(user.createdAt);
        this.authState.next({
          user,
          isAuthenticated: true,
          isLoading: false
        });
      } catch {
        this.logout();
      }
    } else {
      this.authState.next({
        user: null,
        isAuthenticated: false,
        isLoading: false
      });
    }
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred';
    
    if (error.error) {
      if (typeof error.error === 'string') {
        errorMessage = error.error;
      } else if (error.error.message) {
        errorMessage = error.error.message;
      } else if (error.error.errors && error.error.errors.length > 0) {
        errorMessage = error.error.errors[0];
      }
    } else if (error.message) {
      errorMessage = error.message;
    }
    
    return throwError(() => ({ message: errorMessage }));
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  validateToken(): Observable<boolean> {
    const token = this.getToken();
    if (!token) {
      return throwError(() => ({ message: 'No token found' }));
    }

    return this.http.get<any>(`${this.apiUrl}/auth/validate`).pipe(
      map(response => response.success),
      catchError(() => {
        this.logout();
        return throwError(() => ({ message: 'Token validation failed' }));
      })
    );
  }
}