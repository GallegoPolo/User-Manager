export interface LoginRequest {
    email: string
    password: string
  }
  export interface LoginResponse {
    accessToken: string
    tokenType: string
    expiresIn: number
  }