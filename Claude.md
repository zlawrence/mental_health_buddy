CLAUDE.md

SYSTEM OVERVIEW
Mental Health Support Platform with AI chatbot guided by therapist defined guardrails.

COMPONENTS
- iOS Patient App (Swift)
- Therapist Web App (Next.js)
- Admin Portal (Next.js)
- Backend API (.NET Core Clean Architecture)
- AI Chat Engine

CORE PRINCIPLES

SAFETY FIRST
- Detect self harm or suicide intent and escalate immediately
- Never provide harmful or unsafe advice
- If uncertain, respond conservatively and empathetically

THERAPIST GUIDED AI
- All responses must follow therapist defined guardrails
- Guardrails override model output

PRIVACY BY DESIGN
- Do not store diagnoses or medical conditions
- Store only user provided data
- All access is permission based

CONTINUITY
- Conversations persist across sessions
- AI maintains emotional context and tone

SOLUTION STRUCTURE

/src
  /backend
    /Api
    /Application
    /Domain
    /Infrastructure
  /web
    /therapist-portal
    /admin-portal
  /mobile
    /ios-app
/tests
  /backend-tests
  /web-tests
  /mobile-tests
/docs

BACKEND ARCHITECTURE

API LAYER
- Controllers
- Middleware (authentication, logging, rate limiting)
- Request and response mapping

APPLICATION LAYER
- Use cases
- Business workflows
- Interfaces and abstractions

DOMAIN LAYER
- Core entities
- Business rules and invariants

INFRASTRUCTURE LAYER
- Database access
- External services (LLM, SMS, email, authentication)
- Repository implementations

DATA MODELS

Patient
id
username
phoneNumber optional
therapistIds list
createdAt

Therapist
id
name
email
patientIds list

Guardrail
id
patientId
therapistId
rule text
createdAt

Conversation
id
patientId
title
createdAt
lastUpdated

Message
id
conversationId
sender patient or ai
content
timestamp
labels list

AuditLog
id
actorId
action
targetId
timestamp

AI SYSTEM DESIGN

AI RESPONSIBILITIES
- Accept user input
- Retrieve guardrails
- Retrieve conversation history
- Generate safe response
- Persist conversation

PROMPT DESIGN

BASE PROMPT
You are a supportive mental health assistant.
Follow all guardrails strictly.
Do not provide unsafe or medical advice.
Be empathetic, calm, and supportive.

GUARDRAIL PROMPT TEMPLATE
Guardrails:
{guardrails}

Conversation History:
{history}

User Message:
{message}

Respond in a safe, supportive, and compliant way.

SELF HARM DETECTION PROMPT
Classify the following message as SAFE or RISK.
If the message suggests self harm, suicide, or intent to harm, return RISK.
Otherwise return SAFE.

AI PROCESSING PIPELINE

1 Receive user message
2 Retrieve conversation history
3 Retrieve guardrails
4 Run self harm detection classifier
5 If result is RISK then
  - send SMS to emergency contact
  - send email to therapist
  - log escalation event
6 Build LLM prompt using templates
7 Call LLM service
8 Validate response against guardrails
9 If violation detected then
  - filter response or regenerate
10 Store user message and AI response
11 Return response to client

GUARDRAIL ENFORCEMENT

- Inject guardrails into prompt context
- Post process AI output:
  - keyword filtering
  - pattern matching
  - rule validation
- Retry generation if violations occur
- If repeated failures, return safe fallback response

CHAT FEATURES

- Persistent conversations
- Resume previous conversations
- Auto generate conversation titles
- Semantic search across conversations

SEARCH EXAMPLE
User input:
Do you remember when we talked about my pet

System behavior:
- Perform semantic search
- Return matching conversations
- Show preview snippets

USER EXPERIENCE RULES

NEW DAY INTERACTION
- Ask how the user is doing
- Example: How are we doing today

SAME DAY INTERACTION
- Continue prior context
- Example: How did things go with that situation

NON EMOTIONAL QUESTIONS
- Respond with limitation
- Example: I am not really an expert on that, but I can try to help

SELF HARM DETECTION
- Always prioritize safety
- Always trigger escalation workflow

API DESIGN

POST /chat/send
Request
conversationId
message

Response
response

GET /chat/history
- Returns conversation list

GET /chat/search
- Returns filtered conversations

SECURITY

- OAuth2 authentication provider (Auth0 or Cognito)
- JWT validation middleware in API
- Role based access control

ROLES
- Patient
- Therapist
- Admin

PERMISSIONS

- Patient controls therapist access
- Therapist access is scoped to allowed data
- Admin has full access with strict auditing

THERAPIST FEATURES

- View patient conversations if permitted
- Add guardrails
- Remove guardrails
- Guardrails stored as free text
- Manage account credentials

ADMIN FEATURES

- Add and remove admins
- Reset user credentials
- Activate or deactivate users
- Full audit logging of all actions

AUDIT LOGGING

Each log entry must include
- actor id
- action performed
- target entity id
- timestamp

OBSERVABILITY

- Use OpenTelemetry
- Capture distributed traces
- Capture metrics

KEY METRICS
- request latency
- error rates
- AI response failures
- escalation events

TESTING STRATEGY

BACKEND
- NUnit unit tests for all layers
- Integration tests for API and infrastructure

WEB APPLICATIONS
- Jest unit tests
- Integration tests with backend APIs

MOBILE APPLICATION
- Unit tests
- UI tests

NON FUNCTIONAL REQUIREMENTS

- High availability
- Low latency AI responses
- Secure data handling
- Scalable architecture
- Fault tolerant integrations

FUTURE ENHANCEMENTS

- Retrieval augmented generation memory system
- Sentiment analysis for emotional awareness
- Voice interaction capabilities
- Structured guardrail rule engine

AI ASSISTANT RULES

- Follow clean architecture principles strictly
- Never bypass guardrails
- Always include safety checks in logic
- Prefer explicit and readable code
- Ensure all workflows are testable