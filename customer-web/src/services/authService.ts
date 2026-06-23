import { api } from '@/lib/api';
import type { AuthUser } from '@/store/authStore';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber?: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: AuthUser;
}

export interface RequestOtpRequest {
  email: string;
  mobile: string;
}

export interface VerifyOtpRequest {
  email: string;
  otp: string;
}

export const authService = {
  /** OTP login — step 1: find/create account and dispatch OTP. */
  async requestOtp(data: RequestOtpRequest): Promise<void> {
    await api.post('/api/auth/request-otp', data);
  },

  /** OTP login — step 2: verify OTP and receive tokens. */
  async verifyOtp(data: VerifyOtpRequest): Promise<AuthResponse> {
    const res = await api.post<AuthResponse>('/api/auth/verify-otp', data);
    return res.data;
  },

  /** Classic email+password login (kept for admin use). */
  async login(data: LoginRequest): Promise<AuthResponse> {
    const res = await api.post<AuthResponse>('/api/auth/login', data);
    return res.data;
  },

  async register(data: RegisterRequest): Promise<AuthResponse> {
    const res = await api.post<AuthResponse>('/api/auth/register', data);
    return res.data;
  },

  async refreshToken(): Promise<AuthResponse> {
    const res = await api.post<AuthResponse>('/api/auth/refresh-token');
    return res.data;
  },

  async logout(): Promise<void> {
    await api.post('/api/auth/logout', {});
  },
};
