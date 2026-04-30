import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import LoginPage from '../../../app/login/page';

const mockPush = jest.fn();
const mockRouter = { push: mockPush };

jest.mock('next/navigation', () => ({
  useRouter: () => mockRouter,
}));

jest.mock('../../../lib/api', () => ({
  loginTherapist: jest.fn(),
  claimInvitation: jest.fn(),
}));

import { loginTherapist, claimInvitation } from '../../../lib/api';

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.clear();
});

function getPasswordInput(): HTMLInputElement {
  return document.querySelector('input[type="password"]') as HTMLInputElement;
}

describe('LoginPage', () => {
  it('renders the login form', () => {
    render(<LoginPage />);
    expect(screen.getByText('Therapist Login')).toBeInTheDocument();
    expect(screen.getByText('Username')).toBeInTheDocument();
    expect(screen.getByText('Password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('redirects to dashboard if already logged in', async () => {
    localStorage.setItem('therapistToken', 'existing-token');
    render(<LoginPage />);
    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/dashboard');
    });
  });

  it('does not redirect when no token stored', () => {
    render(<LoginPage />);
    expect(mockPush).not.toHaveBeenCalled();
  });

  it('toggles password visibility', async () => {
    const user = userEvent.setup();
    render(<LoginPage />);
    expect(screen.getByRole('button', { name: /show/i })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /show/i }));
    expect(screen.getByRole('button', { name: /hide/i })).toBeInTheDocument();
  });

  it('submits credentials and redirects on success', async () => {
    const user = userEvent.setup();
    (loginTherapist as jest.Mock).mockResolvedValueOnce('new-token');
    render(<LoginPage />);

    await user.type(screen.getByRole('textbox'), 'therapist1');
    await user.type(getPasswordInput(), 'Password123!@');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(loginTherapist).toHaveBeenCalledWith('therapist1', 'Password123!@');
      expect(localStorage.getItem('therapistToken')).toBe('new-token');
      expect(mockPush).toHaveBeenCalledWith('/dashboard');
    });
  });

  it('shows error message on login failure', async () => {
    const user = userEvent.setup();
    (loginTherapist as jest.Mock).mockRejectedValueOnce(new Error('Invalid credentials'));
    render(<LoginPage />);

    await user.type(screen.getByRole('textbox'), 'bad');
    await user.type(getPasswordInput(), 'wrong');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText('Invalid credentials')).toBeInTheDocument();
    });
  });

  it('shows signing in state while loading', async () => {
    const user = userEvent.setup();
    let resolveLogin: (v: string) => void;
    (loginTherapist as jest.Mock).mockReturnValueOnce(new Promise((r) => { resolveLogin = r; }));
    render(<LoginPage />);

    await user.type(screen.getByRole('textbox'), 'user');
    await user.type(getPasswordInput(), 'pass');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(screen.getByRole('button', { name: /signing in/i })).toBeDisabled();

    await act(async () => { resolveLogin!('tok'); });
  });

  it('claims pending invitation after login', async () => {
    const user = userEvent.setup();
    localStorage.setItem('pendingInvitationToken', 'inv-tok');
    (loginTherapist as jest.Mock).mockResolvedValueOnce('auth-tok');
    (claimInvitation as jest.Mock).mockResolvedValueOnce(undefined);
    render(<LoginPage />);

    await user.type(screen.getByRole('textbox'), 'user');
    await user.type(getPasswordInput(), 'pass');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(claimInvitation).toHaveBeenCalledWith('inv-tok', 'auth-tok');
      expect(localStorage.getItem('pendingInvitationToken')).toBeNull();
      expect(mockPush).toHaveBeenCalledWith('/dashboard');
    });
  });

  it('still redirects to dashboard even if claim invitation fails', async () => {
    const user = userEvent.setup();
    localStorage.setItem('pendingInvitationToken', 'inv-tok');
    (loginTherapist as jest.Mock).mockResolvedValueOnce('auth-tok');
    (claimInvitation as jest.Mock).mockRejectedValueOnce(new Error('Claim failed'));
    render(<LoginPage />);

    await user.type(screen.getByRole('textbox'), 'user');
    await user.type(getPasswordInput(), 'pass');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/dashboard');
    });
  });
});
