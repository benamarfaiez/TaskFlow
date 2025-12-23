import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, BehaviorSubject, of, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { LoginDto, RegisterDto, AuthResponse, User, ChangePasswordDto } from '../models/auth.models';
import { Router } from '@angular/router';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private http = inject(HttpClient);
    private router = inject(Router);
    private apiUrl = `${environment.apiUrl}/auth`;

    // State
    private currentUserSubject = new BehaviorSubject<User | null>(null);
    public currentUser$ = this.currentUserSubject.asObservable();

    // Signals (Angular 17+)
    public currentUser = signal<User | null>(null);

    constructor() {
        this.loadUserFromStorage();
    }

    private loadUserFromStorage(): void {
        const userJson = localStorage.getItem('user');
        if (userJson) {
            const user = JSON.parse(userJson);
            this.currentUserSubject.next(user);
            this.currentUser.set(user);
        }
    }

    login(credentials: LoginDto): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
            tap(response => this.handleAuthResponse(response))
        );
    }

    register(data: RegisterDto): Observable<AuthResponse> {
        var x = data;
        return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data).pipe(
            tap(response => this.handleAuthResponse(response))
        );
    }

    logout(): void {
        // Optional: Call API to invalidate token
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        this.currentUserSubject.next(null);
        this.currentUser.set(null);
        this.router.navigate(['/auth/login']);
    }

    refreshToken(): Observable<AuthResponse> {
        const refreshToken = localStorage.getItem('refreshToken');
        if (!refreshToken) {
            this.logout();
            return throwError(() => new Error('No refresh token'));
        }

        return this.http.post<AuthResponse>(`${this.apiUrl}/refresh-token`, { refreshToken }).pipe(
            tap(response => this.handleAuthResponse(response)),
            catchError(err => {
                this.logout();
                return throwError(() => err);
            })
        );
    }

    changePassword(data: ChangePasswordDto): Observable<void> {
        return this.http.post<void>(`${this.apiUrl}/change-password`, data);
    }

    private handleAuthResponse(response: AuthResponse): void {
        localStorage.setItem('token', response.token);
        localStorage.setItem('refreshToken', response.refreshToken);
        localStorage.setItem('user', JSON.stringify(response.user));
        this.currentUserSubject.next(response.user);
        this.currentUser.set(response.user);
    }

    getToken(): string | null {
        return localStorage.getItem('token');
    }

    isLoggedIn(): boolean {
        return !!this.getToken();
    }
}
