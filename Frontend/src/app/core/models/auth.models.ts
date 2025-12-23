export interface User {
    id: string;
    email: string;
    fullName: string;
    avatarUrl?: string;
    roles: string[];
}

export interface LoginDto {
    email: string;
    password: string;
}

export interface RegisterDto {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
}

export interface AuthResponse {
    token: string;
    refreshToken: string;
    user: User;
}

export interface ChangePasswordDto {
    oldPassword: string;
    newPassword: string;
}

export interface ApiError {
    code: string;
    message: string;
    details?: any;
}
