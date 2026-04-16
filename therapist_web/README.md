# Therapist Web App

This Next.js app is the therapist-facing portal for the mental health backend.

## Setup

1. Install Node.js
2. Run `npm install`
3. Create a `.env.local` file with `NEXT_PUBLIC_API_BASE_URL=http://localhost:5000`
4. Run `npm run dev`

## Notes

- The login screen stores the JWT in `localStorage`.
- Use the backend's therapist login endpoint at `/api/auth/login-therapist`.
