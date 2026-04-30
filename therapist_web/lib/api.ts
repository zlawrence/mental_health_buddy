const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5251';

export async function loginTherapist(username: string, password: string): Promise<string> {
  const response = await fetch(`${apiBase}/api/auth/login-therapist`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Login failed');
  }

  const result = await response.json();
  return result.token;
}

export async function getAssignedPatients(token: string): Promise<any[]> {
  const response = await fetch(`${apiBase}/api/therapist/me/patients`, {
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to load patients');
  }

  return response.json();
}

export async function getPatientProfile(token: string, patientId: string): Promise<any> {
  const response = await fetch(`${apiBase}/api/therapist/me/patients/${patientId}`, {
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to load patient profile');
  }

  return response.json();
}

export interface TherapistAccess {
  canViewChats: boolean;
  canManageGuardRails: boolean;
}

export async function getTherapistAccess(token: string, patientId: string): Promise<TherapistAccess> {
  const response = await fetch(`${apiBase}/api/therapist/me/patients/${patientId}/access`, {
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to load access permissions');
  }

  return response.json();
}

export async function getPatientGuardRails(token: string, patientId: string): Promise<any[]> {
  const response = await fetch(`${apiBase}/api/therapist/me/patients/${patientId}/guard-rails`, {
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to load patient guard rails');
  }

  return response.json();
}

export async function createPatientGuardRail(
  token: string,
  patientId: string,
  data: { keyword: string; action: string; replacement?: string }
): Promise<any> {
  const response = await fetch(`${apiBase}/api/therapist/me/patients/${patientId}/guard-rails`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to create guard rail');
  }

  return response.json();
}

export async function updatePatientGuardRail(
  token: string,
  patientId: string,
  guardRailId: string,
  data: { keyword: string; action: string; replacement?: string; isActive: boolean }
): Promise<void> {
  const response = await fetch(`${apiBase}/api/therapist/me/patients/${patientId}/guard-rails/${guardRailId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to update guard rail');
  }
}

export async function deletePatientGuardRail(token: string, patientId: string, guardRailId: string): Promise<void> {
  const response = await fetch(`${apiBase}/api/therapist/me/patients/${patientId}/guard-rails/${guardRailId}`, {
    method: 'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to delete guard rail');
  }
}

export async function getPatientConversations(token: string, patientId: string): Promise<any[]> {
  const response = await fetch(`${apiBase}/api/therapist/me/patients/${patientId}/conversations`, {
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to load conversations');
  }

  return response.json();
}

export async function getPatientConversationMessages(
  token: string,
  patientId: string,
  conversationId: string
): Promise<any[]> {
  const response = await fetch(
    `${apiBase}/api/therapist/me/patients/${patientId}/conversations/${conversationId}/messages`,
    { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` } }
  );

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to load messages');
  }

  return response.json();
}

export interface InvitationValidation {
  valid: boolean;
  expired: boolean;
  alreadyUsed: boolean;
  therapistEmail: string | null;
  emailAlreadyRegistered: boolean;
}

export async function validateInvitation(token: string): Promise<InvitationValidation> {
  const response = await fetch(`${apiBase}/api/auth/validate-invitation?token=${encodeURIComponent(token)}`);
  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to validate invitation');
  }
  return response.json();
}

export async function setupTherapist(token: string, username: string, password: string, confirmPassword: string): Promise<string> {
  const response = await fetch(`${apiBase}/api/auth/setup-therapist`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token, username, password, confirmPassword }),
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Setup failed');
  }

  const result = await response.json();
  return result.token;
}

export async function claimInvitation(invitationToken: string, authToken: string): Promise<void> {
  const response = await fetch(`${apiBase}/api/auth/claim-invitation`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${authToken}` },
    body: JSON.stringify({ token: invitationToken }),
  });

  if (!response.ok) {
    const result = await response.json();
    throw new Error(result.message || 'Unable to claim invitation');
  }
}
