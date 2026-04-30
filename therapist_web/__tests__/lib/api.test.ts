import {
  loginTherapist,
  getAssignedPatients,
  getPatientProfile,
  getTherapistAccess,
  getPatientGuardRails,
  createPatientGuardRail,
  updatePatientGuardRail,
  deletePatientGuardRail,
  getPatientConversations,
  getPatientConversationMessages,
  validateInvitation,
  setupTherapist,
  claimInvitation,
} from '../../lib/api';

global.fetch = jest.fn();

function mockFetch(status: number, body: unknown) {
  (fetch as jest.Mock).mockResolvedValueOnce({
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  });
}

beforeEach(() => {
  jest.clearAllMocks();
});

describe('loginTherapist', () => {
  it('returns token on success', async () => {
    mockFetch(200, { token: 'abc123' });
    const result = await loginTherapist('user', 'pass');
    expect(result).toBe('abc123');
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/auth/login-therapist'),
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('throws on non-ok response', async () => {
    mockFetch(401, { message: 'Invalid credentials' });
    await expect(loginTherapist('u', 'p')).rejects.toThrow('Invalid credentials');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(loginTherapist('u', 'p')).rejects.toThrow('Login failed');
  });
});

describe('getAssignedPatients', () => {
  it('returns patients array on success', async () => {
    const patients = [{ userId: '1', username: 'alice' }];
    mockFetch(200, patients);
    const result = await getAssignedPatients('token');
    expect(result).toEqual(patients);
  });

  it('throws on non-ok response', async () => {
    mockFetch(403, { message: 'Forbidden' });
    await expect(getAssignedPatients('token')).rejects.toThrow('Forbidden');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(getAssignedPatients('token')).rejects.toThrow('Unable to load patients');
  });
});

describe('getPatientProfile', () => {
  it('returns profile on success', async () => {
    const profile = { userId: 'p1', username: 'bob' };
    mockFetch(200, profile);
    const result = await getPatientProfile('token', 'p1');
    expect(result).toEqual(profile);
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('/patients/p1'),
      expect.any(Object)
    );
  });

  it('throws on error', async () => {
    mockFetch(404, { message: 'Not found' });
    await expect(getPatientProfile('token', 'p1')).rejects.toThrow('Not found');
  });
});

describe('getTherapistAccess', () => {
  it('returns access object on success', async () => {
    const access = { canViewChats: true, canManageGuardRails: false };
    mockFetch(200, access);
    const result = await getTherapistAccess('token', 'p1');
    expect(result).toEqual(access);
  });

  it('throws on error', async () => {
    mockFetch(403, { message: 'No access' });
    await expect(getTherapistAccess('token', 'p1')).rejects.toThrow('No access');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(getTherapistAccess('token', 'p1')).rejects.toThrow('Unable to load access permissions');
  });
});

describe('getPatientGuardRails', () => {
  it('returns guard rails on success', async () => {
    const rails = [{ id: 'gr1', keyword: 'test' }];
    mockFetch(200, rails);
    const result = await getPatientGuardRails('token', 'p1');
    expect(result).toEqual(rails);
  });

  it('throws on error', async () => {
    mockFetch(400, { message: 'Bad request' });
    await expect(getPatientGuardRails('token', 'p1')).rejects.toThrow('Bad request');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(getPatientGuardRails('token', 'p1')).rejects.toThrow('Unable to load patient guard rails');
  });
});

describe('createPatientGuardRail', () => {
  it('returns created guard rail on success', async () => {
    const newRail = { id: 'gr2', keyword: 'stress' };
    mockFetch(201, newRail);
    const result = await createPatientGuardRail('token', 'p1', { keyword: 'stress', action: 'remove' });
    expect(result).toEqual(newRail);
    expect(fetch).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('throws on error', async () => {
    mockFetch(400, { message: 'Invalid action' });
    await expect(
      createPatientGuardRail('token', 'p1', { keyword: 'k', action: 'bad' })
    ).rejects.toThrow('Invalid action');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(
      createPatientGuardRail('token', 'p1', { keyword: 'k', action: 'remove' })
    ).rejects.toThrow('Unable to create guard rail');
  });
});

describe('updatePatientGuardRail', () => {
  it('resolves on success', async () => {
    mockFetch(204, null);
    await expect(
      updatePatientGuardRail('token', 'p1', 'gr1', { keyword: 'k', action: 'remove', isActive: true })
    ).resolves.toBeUndefined();
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('/gr1'),
      expect.objectContaining({ method: 'PUT' })
    );
  });

  it('throws on error', async () => {
    mockFetch(400, { message: 'Not found' });
    await expect(
      updatePatientGuardRail('token', 'p1', 'gr1', { keyword: 'k', action: 'remove', isActive: true })
    ).rejects.toThrow('Not found');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(
      updatePatientGuardRail('token', 'p1', 'gr1', { keyword: 'k', action: 'remove', isActive: true })
    ).rejects.toThrow('Unable to update guard rail');
  });
});

describe('deletePatientGuardRail', () => {
  it('resolves on success', async () => {
    mockFetch(204, null);
    await expect(deletePatientGuardRail('token', 'p1', 'gr1')).resolves.toBeUndefined();
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('/gr1'),
      expect.objectContaining({ method: 'DELETE' })
    );
  });

  it('throws on error', async () => {
    mockFetch(404, { message: 'Not found' });
    await expect(deletePatientGuardRail('token', 'p1', 'gr1')).rejects.toThrow('Not found');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(deletePatientGuardRail('token', 'p1', 'gr1')).rejects.toThrow('Unable to delete guard rail');
  });
});

describe('getPatientConversations', () => {
  it('returns conversations on success', async () => {
    const convos = [{ id: 'c1', title: 'Session 1' }];
    mockFetch(200, convos);
    const result = await getPatientConversations('token', 'p1');
    expect(result).toEqual(convos);
  });

  it('throws on error', async () => {
    mockFetch(403, { message: 'Forbidden' });
    await expect(getPatientConversations('token', 'p1')).rejects.toThrow('Forbidden');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(getPatientConversations('token', 'p1')).rejects.toThrow('Unable to load conversations');
  });
});

describe('getPatientConversationMessages', () => {
  it('returns messages on success', async () => {
    const msgs = [{ id: 'm1', content: 'hello' }];
    mockFetch(200, msgs);
    const result = await getPatientConversationMessages('token', 'p1', 'c1');
    expect(result).toEqual(msgs);
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('/conversations/c1/messages'),
      expect.any(Object)
    );
  });

  it('throws on error', async () => {
    mockFetch(404, { message: 'Not found' });
    await expect(getPatientConversationMessages('token', 'p1', 'c1')).rejects.toThrow('Not found');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(getPatientConversationMessages('token', 'p1', 'c1')).rejects.toThrow('Unable to load messages');
  });
});

describe('validateInvitation', () => {
  it('returns validation result on success', async () => {
    const validation = { valid: true, expired: false, alreadyUsed: false, therapistEmail: 't@t.com', emailAlreadyRegistered: false };
    mockFetch(200, validation);
    const result = await validateInvitation('inv-token');
    expect(result).toEqual(validation);
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('validate-invitation?token=inv-token')
    );
  });

  it('throws on error', async () => {
    mockFetch(400, { message: 'Invalid token' });
    await expect(validateInvitation('bad')).rejects.toThrow('Invalid token');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(validateInvitation('bad')).rejects.toThrow('Unable to validate invitation');
  });
});

describe('setupTherapist', () => {
  it('returns auth token on success', async () => {
    mockFetch(200, { token: 'auth-tok' });
    const result = await setupTherapist('inv-token', 'user', 'pass', 'pass');
    expect(result).toBe('auth-tok');
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('setup-therapist'),
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('throws on error', async () => {
    mockFetch(400, { message: 'Username taken' });
    await expect(setupTherapist('t', 'u', 'p', 'p')).rejects.toThrow('Username taken');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(setupTherapist('t', 'u', 'p', 'p')).rejects.toThrow('Setup failed');
  });
});

describe('claimInvitation', () => {
  it('resolves on success', async () => {
    mockFetch(200, {});
    await expect(claimInvitation('inv-token', 'auth-tok')).resolves.toBeUndefined();
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('claim-invitation'),
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('throws on error', async () => {
    mockFetch(400, { message: 'Already claimed' });
    await expect(claimInvitation('inv-token', 'auth')).rejects.toThrow('Already claimed');
  });

  it('throws generic message when no message field', async () => {
    mockFetch(500, {});
    await expect(claimInvitation('inv-token', 'auth')).rejects.toThrow('Unable to claim invitation');
  });
});
