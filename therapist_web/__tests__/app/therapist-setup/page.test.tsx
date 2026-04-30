import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import TherapistSetupPage from '../../../app/therapist-setup/page';

const mockPush = jest.fn();
const mockRouter = { push: mockPush };
let mockToken = '';

jest.mock('next/navigation', () => ({
  useRouter: () => mockRouter,
  useSearchParams: () => ({ get: (key: string) => key === 'token' ? mockToken : null }),
}));

jest.mock('../../../lib/api', () => ({
  validateInvitation: jest.fn(),
  setupTherapist: jest.fn(),
}));

import { validateInvitation, setupTherapist } from '../../../lib/api';

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.clear();
  mockToken = 'valid-token';
});

const validInvitation = {
  valid: true,
  expired: false,
  alreadyUsed: false,
  therapistEmail: 'therapist@test.com',
  emailAlreadyRegistered: false,
};

function getPasswordInputs() {
  return document.querySelectorAll('input[type="password"]');
}

describe('TherapistSetupPage — invalid/missing token', () => {
  it('shows invalid page when token is empty', async () => {
    mockToken = '';
    render(<TherapistSetupPage />);
    await waitFor(() => {
      expect(screen.getByText(/invalid invitation/i)).toBeInTheDocument();
    });
  });

  it('shows invalid page when validateInvitation rejects', async () => {
    (validateInvitation as jest.Mock).mockRejectedValueOnce(new Error('Network error'));
    render(<TherapistSetupPage />);
    await waitFor(() => {
      expect(screen.getByText(/invalid invitation/i)).toBeInTheDocument();
    });
  });

  it('shows invalid page when valid is false and not expired/used', async () => {
    (validateInvitation as jest.Mock).mockResolvedValueOnce({
      valid: false, expired: false, alreadyUsed: false, therapistEmail: null, emailAlreadyRegistered: false,
    });
    render(<TherapistSetupPage />);
    await waitFor(() => {
      expect(screen.getByText(/invalid invitation/i)).toBeInTheDocument();
    });
  });
});

describe('TherapistSetupPage — expired state', () => {
  it('shows expired page', async () => {
    (validateInvitation as jest.Mock).mockResolvedValueOnce({
      valid: false, expired: true, alreadyUsed: false, therapistEmail: 'therapist@test.com', emailAlreadyRegistered: false,
    });
    render(<TherapistSetupPage />);
    await waitFor(() => {
      expect(screen.getByText(/invitation expired/i)).toBeInTheDocument();
      expect(screen.getByText(/therapist@test.com/)).toBeInTheDocument();
    });
  });
});

describe('TherapistSetupPage — already used state', () => {
  it('shows already used page with go to login button', async () => {
    (validateInvitation as jest.Mock).mockResolvedValueOnce({
      valid: false, expired: false, alreadyUsed: true, therapistEmail: null, emailAlreadyRegistered: false,
    });
    render(<TherapistSetupPage />);
    await waitFor(() => {
      expect(screen.getByText(/already used/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /go to login/i })).toBeInTheDocument();
    });
  });

  it('navigates to /login when clicking go to login', async () => {
    const user = userEvent.setup();
    (validateInvitation as jest.Mock).mockResolvedValueOnce({
      valid: false, expired: false, alreadyUsed: true, therapistEmail: null, emailAlreadyRegistered: false,
    });
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('button', { name: /go to login/i }));
    await user.click(screen.getByRole('button', { name: /go to login/i }));
    expect(mockPush).toHaveBeenCalledWith('/login');
  });
});

describe('TherapistSetupPage — login required state', () => {
  it('shows login required page', async () => {
    (validateInvitation as jest.Mock).mockResolvedValueOnce({
      valid: true, expired: false, alreadyUsed: false, therapistEmail: 'therapist@test.com', emailAlreadyRegistered: true,
    });
    render(<TherapistSetupPage />);
    await waitFor(() => {
      expect(screen.getByText(/therapist@test.com/)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /log in to accept/i })).toBeInTheDocument();
    });
  });

  it('saves pending token and redirects to login', async () => {
    const user = userEvent.setup();
    (validateInvitation as jest.Mock).mockResolvedValueOnce({
      valid: true, expired: false, alreadyUsed: false, therapistEmail: 't@t.com', emailAlreadyRegistered: true,
    });
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('button', { name: /log in to accept/i }));
    await user.click(screen.getByRole('button', { name: /log in to accept/i }));

    expect(localStorage.getItem('pendingInvitationToken')).toBe('valid-token');
    expect(mockPush).toHaveBeenCalledWith('/login');
  });
});

describe('TherapistSetupPage — setup form', () => {
  beforeEach(() => {
    (validateInvitation as jest.Mock).mockResolvedValue(validInvitation);
  });

  it('renders setup form', async () => {
    render(<TherapistSetupPage />);
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /create your account/i })).toBeInTheDocument();
    });
    expect(document.querySelector('button[type="submit"]')).toBeInTheDocument();
  });

  it('shows username error when empty', async () => {
    const user = userEvent.setup();
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('heading', { name: /create your account/i }));
    await user.click(document.querySelector('button[type="submit"]')!);
    await waitFor(() => {
      expect(screen.getByText(/username is required/i)).toBeInTheDocument();
    });
  });

  it('shows password validation error for short password', async () => {
    const user = userEvent.setup();
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('heading', { name: /create your account/i }));

    await user.type(screen.getAllByRole('textbox')[0], 'myuser');
    await user.type(getPasswordInputs()[0], 'short');
    await user.click(document.querySelector('button[type="submit"]')!);

    await waitFor(() => {
      // The error starts with "Must be" — distinguishes it from the hint text "Password must be..."
      expect(screen.getByText(/^must be 12/i)).toBeInTheDocument();
    });
  });

  it('shows passwords do not match error', async () => {
    const user = userEvent.setup();
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('heading', { name: /create your account/i }));

    await user.type(screen.getAllByRole('textbox')[0], 'myuser');
    await user.type(getPasswordInputs()[0], 'ValidPass12!@');
    await user.type(getPasswordInputs()[1], 'DifferentPass12!@');
    await user.click(document.querySelector('button[type="submit"]')!);

    await waitFor(() => {
      expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument();
    });
  });

  it('calls setupTherapist and shows success state', async () => {
    jest.useFakeTimers();
    const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
    (setupTherapist as jest.Mock).mockResolvedValueOnce('new-auth-token');
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('heading', { name: /create your account/i }));

    await user.type(screen.getAllByRole('textbox')[0], 'myuser');
    await user.type(getPasswordInputs()[0], 'ValidPass12!@');
    await user.type(getPasswordInputs()[1], 'ValidPass12!@');
    await user.click(document.querySelector('button[type="submit"]')!);

    await waitFor(() => {
      expect(setupTherapist).toHaveBeenCalledWith('valid-token', 'myuser', 'ValidPass12!@', 'ValidPass12!@');
      expect(screen.getByText(/account created/i)).toBeInTheDocument();
      expect(localStorage.getItem('therapistToken')).toBe('new-auth-token');
    });

    act(() => { jest.advanceTimersByTime(1500); });
    expect(mockPush).toHaveBeenCalledWith('/dashboard');
    jest.useRealTimers();
  });

  it('shows username error on api rejection with username keyword', async () => {
    const user = userEvent.setup();
    (setupTherapist as jest.Mock).mockRejectedValueOnce(new Error('Username already taken'));
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('heading', { name: /create your account/i }));

    await user.type(screen.getAllByRole('textbox')[0], 'myuser');
    await user.type(getPasswordInputs()[0], 'ValidPass12!@');
    await user.type(getPasswordInputs()[1], 'ValidPass12!@');
    await user.click(document.querySelector('button[type="submit"]')!);

    await waitFor(() => {
      expect(screen.getByText(/username already taken/i)).toBeInTheDocument();
    });
  });

  it('shows general error on api rejection without username keyword', async () => {
    const user = userEvent.setup();
    (setupTherapist as jest.Mock).mockRejectedValueOnce(new Error('Server error'));
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('heading', { name: /create your account/i }));

    await user.type(screen.getAllByRole('textbox')[0], 'myuser');
    await user.type(getPasswordInputs()[0], 'ValidPass12!@');
    await user.type(getPasswordInputs()[1], 'ValidPass12!@');
    await user.click(document.querySelector('button[type="submit"]')!);

    await waitFor(() => {
      expect(screen.getByText(/server error/i)).toBeInTheDocument();
    });
  });

  it('toggles password visibility in setup form', async () => {
    const user = userEvent.setup();
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('heading', { name: /create your account/i }));

    const showButtons = screen.getAllByRole('button', { name: /show/i });
    // First Show button is for Password field
    await user.click(showButtons[0]);
    expect(screen.getAllByRole('button', { name: /hide/i }).length).toBeGreaterThan(0);

    // Second Show button is for Confirm Password field
    await user.click(showButtons[1]);
    expect(screen.getAllByRole('button', { name: /hide/i }).length).toBe(2);
  });

  it('saves pending invitation token and redirects to login when clicking log in instead', async () => {
    const user = userEvent.setup();
    render(<TherapistSetupPage />);
    await waitFor(() => screen.getByRole('button', { name: /log in instead/i }));

    await user.click(screen.getByRole('button', { name: /log in instead/i }));

    expect(localStorage.getItem('pendingInvitationToken')).toBe('valid-token');
    expect(mockPush).toHaveBeenCalledWith('/login');
  });
});
