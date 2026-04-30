# Anxiety Buddy — Mental Health Support Platform

An AI-powered mental health support platform consisting of:

- **Backend API** — .NET 9 REST API (Clean Architecture)
- **Therapist Web Portal** — Next.js 14 web application
- **Patient Mobile App** — Flutter mobile application

---

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 9.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18+ | https://nodejs.org |
| Flutter SDK | 3.14+ | https://flutter.dev/docs/get-started/install |
| MongoDB | 7.0+ | https://www.mongodb.com/try/download/community |

---

## Project Structure

```
/
├── MentalHealthApp.API/          # ASP.NET Core Web API
├── MentalHealthApp.Application/  # Use cases and interfaces
├── MentalHealthApp.Domain/       # Entities and business rules
├── MentalHealthApp.Infrastructure/ # Repositories and external services
├── MentalHealthApp.Tests/        # NUnit test suite
├── therapist_web/                # Next.js therapist portal
└── patient_mobile/               # Flutter patient app
```

---

## Backend API

### Setup

1. **Start MongoDB** locally on the default port:
   ```
   mongod --dbpath /your/data/path
   ```

2. **Configure secrets** — copy the dev settings and fill in your keys:

   Edit `MentalHealthApp.API/appsettings.Development.json`:
   ```json
   {
     "MongoDb": {
       "ConnectionString": "mongodb://localhost:27017",
       "DatabaseName": "MentalHealthApp"
     },
     "Jwt": {
       "Key": "<generate-a-32-byte-base64-key>",
       "Issuer": "MentalHealthApp",
       "Audience": "MentalHealthApp",
       "ExpiryInMinutes": 1440
     },
     "Anthropic": {
       "ApiKey": "<your-anthropic-api-key>",
       "ModelId": "claude-sonnet-4-6"
     },
     "Stripe": {
       "PublishableKey": "<your-stripe-publishable-key>",
       "SecretKey": "<your-stripe-secret-key>"
     },
     "Postmark": {
       "ApiToken": "<your-postmark-api-token>",
       "FromEmail": "noreply@anxietybuddy.app"
     }
   }
   ```

   To generate a JWT key:
   ```bash
   openssl rand -base64 32
   ```

### Build

```bash
dotnet build MentalHealthApp.sln
```

### Run

```bash
dotnet run --project MentalHealthApp.API
```

The API will be available at `http://localhost:5251`.

To use HTTPS:
```bash
dotnet run --project MentalHealthApp.API --launch-profile https
```

### Test

Run all backend unit tests:
```bash
dotnet test MentalHealthApp.Tests
```

Run with verbose output:
```bash
dotnet test MentalHealthApp.Tests --logger "console;verbosity=detailed"
```

Run a specific test class:
```bash
dotnet test MentalHealthApp.Tests --filter "FullyQualifiedName~TherapistServiceTests"
```

Run with code coverage:
```bash
dotnet test MentalHealthApp.Tests --collect:"XPlat Code Coverage"
```

### API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | Register a patient account |
| POST | `/api/auth/login` | Patient login |
| POST | `/api/auth/login-therapist` | Therapist login |
| POST | `/api/auth/setup-therapist` | Complete therapist account setup via invitation |
| POST | `/api/auth/claim-invitation` | Claim a therapist invitation after login |
| GET | `/api/auth/validate-invitation` | Validate an invitation token |
| GET | `/api/therapist/me/patients` | Get assigned patients |
| GET | `/api/therapist/me/patients/{id}` | Get patient profile |
| GET | `/api/therapist/me/patients/{id}/access` | Get permission flags for a patient |
| GET/POST | `/api/therapist/me/patients/{id}/guard-rails` | List or create guard rails |
| PUT/DELETE | `/api/therapist/me/patients/{id}/guard-rails/{grId}` | Update or delete a guard rail |
| GET | `/api/therapist/me/patients/{id}/conversations` | List patient conversations |
| GET | `/api/therapist/me/patients/{id}/conversations/{cId}/messages` | Get conversation messages |

---

## Therapist Web Portal

The Next.js portal allows therapists to view assigned patients, manage guard rails, and review conversation history.

### Setup

```bash
cd therapist_web
npm install
```

Create a `.env.local` file:
```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5251
```

### Run (development)

```bash
npm run dev
```

Opens at `http://localhost:3000`.

### Build (production)

```bash
npm run build
npm run start
```

### Test

Run all tests:
```bash
npm test
```

Run with coverage report (must meet 85% threshold):
```bash
npm run test:coverage
```

Run a specific test file:
```bash
npm test -- __tests__/lib/api.test.ts
```

Run in watch mode:
```bash
npm test -- --watch
```

### Features

- Therapist login with JWT stored in `localStorage`
- View all assigned patients
- Per-patient guard rail management (create, edit, delete)
- View patient conversation and message history (if permitted)
- Permission-gated UI based on `CanViewChats` and `CanManageGuardRails` flags
- Responsive layout for desktop and mobile

---

## Patient Mobile App

The Flutter app is the patient-facing interface for AI-assisted mental health conversations.

### Setup

```bash
cd patient_mobile
flutter pub get
```

Configure the API base URL. Open `lib/services/` and update the base URL constant to point to your running API:
```dart
const String apiBaseUrl = 'http://localhost:5251';
```

For a physical device, replace `localhost` with your machine's local IP address (e.g. `192.168.1.x`).

### Run

List available devices:
```bash
flutter devices
```

Run on a connected device or emulator:
```bash
flutter run
```

Run on a specific device:
```bash
flutter run -d <device-id>
```

### Build

Build an APK (Android):
```bash
flutter build apk --release
```

Build for iOS (macOS only):
```bash
flutter build ios --release
```

Build for web:
```bash
flutter build web
```

### Test

Run all unit and widget tests:
```bash
flutter test
```

Run with coverage:
```bash
flutter test --coverage
```

### Screens

| Screen | Route | Description |
|--------|-------|-------------|
| Login | `/login` | Patient login |
| Sign Up | `/signup` | New patient registration |
| Dashboard | `/dashboard` | Conversation list |
| Conversation | `/conversation/:id` | Active AI chat |
| Profile | `/profile` | Account settings |
| Subscription | `/subscription` | Subscription management |

---

## Running Everything Together

1. Start MongoDB
2. Start the API: `dotnet run --project MentalHealthApp.API`
3. Start the therapist portal: `cd therapist_web && npm run dev`
4. Start the mobile app: `cd patient_mobile && flutter run`

The three components communicate over HTTP. Make sure the API is running before using the portal or mobile app.

