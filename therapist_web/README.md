# Therapist Web Portal

Next.js 14 portal for therapists to view assigned patients, manage guard rails, and review conversation history.

See the [root README](../README.md) for full setup instructions covering all platform components.

## Quick Start

```bash
npm install
```

Create `.env.local`:
```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5251
```

```bash
npm run dev        # development server at http://localhost:3000
npm run build      # production build
npm run start      # serve production build
npm test           # run tests
npm run test:coverage  # run tests with coverage report (≥85% threshold)
```

## Stack

- Next.js 14 (App Router)
- Tailwind CSS v4
- React Testing Library + Jest (SWC)
