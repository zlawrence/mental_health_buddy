import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import DashboardPage from '../../../app/dashboard/page';

const mockPush = jest.fn();
const mockRouter = { push: mockPush };

jest.mock('next/navigation', () => ({
  useRouter: () => mockRouter,
}));

jest.mock('../../../lib/api', () => ({
  getAssignedPatients: jest.fn(),
}));

import { getAssignedPatients } from '../../../lib/api';

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.clear();
});

const patients = [
  { userId: 'u1', username: 'alice', email: 'alice@test.com', phoneNumber: '555-1111' },
  { userId: 'u2', username: 'bob', email: 'bob@test.com', phoneNumber: null },
];

describe('DashboardPage', () => {
  it('redirects to /login when no token', () => {
    render(<DashboardPage />);
    expect(mockPush).toHaveBeenCalledWith('/login');
  });

  it('shows loading state initially when token exists', () => {
    localStorage.setItem('therapistToken', 'tok');
    (getAssignedPatients as jest.Mock).mockReturnValueOnce(new Promise(() => {}));
    render(<DashboardPage />);
    expect(screen.getByText(/loading patients/i)).toBeInTheDocument();
  });

  it('renders patient list on success', async () => {
    localStorage.setItem('therapistToken', 'tok');
    (getAssignedPatients as jest.Mock).mockResolvedValueOnce(patients);
    render(<DashboardPage />);

    await waitFor(() => {
      expect(screen.getByText('alice')).toBeInTheDocument();
      expect(screen.getByText('bob')).toBeInTheDocument();
    });
  });

  it('renders patient email and phone', async () => {
    localStorage.setItem('therapistToken', 'tok');
    (getAssignedPatients as jest.Mock).mockResolvedValueOnce(patients);
    render(<DashboardPage />);

    await waitFor(() => {
      expect(screen.getByText('alice@test.com')).toBeInTheDocument();
      expect(screen.getByText('Phone: 555-1111')).toBeInTheDocument();
      expect(screen.getByText('Phone: N/A')).toBeInTheDocument();
    });
  });

  it('renders patient links with correct href', async () => {
    localStorage.setItem('therapistToken', 'tok');
    (getAssignedPatients as jest.Mock).mockResolvedValueOnce(patients);
    render(<DashboardPage />);

    await waitFor(() => {
      const links = screen.getAllByText(/view patient details/i);
      expect(links[0].closest('a')).toHaveAttribute('href', '/patients/u1');
    });
  });

  it('shows error message on fetch failure', async () => {
    localStorage.setItem('therapistToken', 'tok');
    (getAssignedPatients as jest.Mock).mockRejectedValueOnce(new Error('Network error'));
    render(<DashboardPage />);

    await waitFor(() => {
      expect(screen.getByText('Network error')).toBeInTheDocument();
    });
  });

  it('calls getAssignedPatients with token', async () => {
    localStorage.setItem('therapistToken', 'my-token');
    (getAssignedPatients as jest.Mock).mockResolvedValueOnce([]);
    render(<DashboardPage />);

    await waitFor(() => {
      expect(getAssignedPatients).toHaveBeenCalledWith('my-token');
    });
  });

  it('logs out and redirects to /login', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    (getAssignedPatients as jest.Mock).mockResolvedValueOnce([]);
    render(<DashboardPage />);

    await waitFor(() => screen.getByRole('button', { name: /logout/i }));
    await user.click(screen.getByRole('button', { name: /logout/i }));

    expect(localStorage.getItem('therapistToken')).toBeNull();
    expect(mockPush).toHaveBeenCalledWith('/login');
  });

  it('renders empty list without error when no patients', async () => {
    localStorage.setItem('therapistToken', 'tok');
    (getAssignedPatients as jest.Mock).mockResolvedValueOnce([]);
    render(<DashboardPage />);

    await waitFor(() => {
      expect(screen.queryByText(/loading/i)).not.toBeInTheDocument();
      expect(screen.queryByRole('listitem')).not.toBeInTheDocument();
    });
  });
});
