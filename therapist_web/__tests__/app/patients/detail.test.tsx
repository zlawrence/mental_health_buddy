import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import PatientDetailPage from '../../../app/patients/[patientId]/page';

const mockPush = jest.fn();
const mockBack = jest.fn();
const mockRouter = { push: mockPush, back: mockBack };
let mockPatientId = 'p1';

jest.mock('next/navigation', () => ({
  useRouter: () => mockRouter,
  useParams: () => ({ patientId: mockPatientId }),
}));

jest.mock('../../../lib/api', () => ({
  getPatientProfile: jest.fn(),
  getPatientGuardRails: jest.fn(),
  getTherapistAccess: jest.fn(),
  getPatientConversations: jest.fn(),
  getPatientConversationMessages: jest.fn(),
  createPatientGuardRail: jest.fn(),
  updatePatientGuardRail: jest.fn(),
  deletePatientGuardRail: jest.fn(),
}));

import {
  getPatientProfile,
  getPatientGuardRails,
  getTherapistAccess,
  getPatientConversations,
  getPatientConversationMessages,
  createPatientGuardRail,
  updatePatientGuardRail,
  deletePatientGuardRail,
} from '../../../lib/api';

const profile = {
  userId: 'p1',
  username: 'Alice',
  email: 'alice@test.com',
  phoneNumber: '555-1234',
  therapistIds: ['t1'],
  conversationRetentionDays: 30,
};

const guardRails = [
  { id: 'gr1', keyword: 'stress', action: 'remove', replacement: null, isActive: true, createdAt: '2024-01-01' },
  { id: 'gr2', keyword: 'medication', action: 'replace', replacement: 'wellness', isActive: false, createdAt: '2024-01-02' },
];

const conversations = [
  { id: 'c1', title: 'Session One', startedDate: '2024-03-01T10:00:00Z', messageCount: 5, isArchived: false },
  { id: 'c2', title: 'Session Two', startedDate: '2024-03-02T10:00:00Z', messageCount: 3, isArchived: true },
];

const messages = [
  { id: 'm1', role: 'User', content: 'Hello', timestamp: '2024-03-01T10:00:00Z' },
  { id: 'm2', role: 'Assistant', content: 'Hi there', timestamp: '2024-03-01T10:01:00Z' },
];

function setupAllPermissions() {
  (getPatientProfile as jest.Mock).mockResolvedValue(profile);
  (getPatientGuardRails as jest.Mock).mockResolvedValue(guardRails);
  (getTherapistAccess as jest.Mock).mockResolvedValue({ canViewChats: true, canManageGuardRails: true });
  (getPatientConversations as jest.Mock).mockResolvedValue(conversations);
}

function setupNoPermissions() {
  (getPatientProfile as jest.Mock).mockResolvedValue(profile);
  (getPatientGuardRails as jest.Mock).mockResolvedValue(guardRails);
  (getTherapistAccess as jest.Mock).mockResolvedValue({ canViewChats: false, canManageGuardRails: false });
}

beforeEach(() => {
  jest.clearAllMocks();
  localStorage.clear();
  (window.confirm as jest.Mock).mockReturnValue(true);
});

describe('PatientDetailPage — auth redirect', () => {
  it('redirects to /login when no token', () => {
    render(<PatientDetailPage />);
    expect(mockPush).toHaveBeenCalledWith('/login');
  });
});

describe('PatientDetailPage — loading and error states', () => {
  it('shows loading text initially', () => {
    localStorage.setItem('therapistToken', 'tok');
    (getPatientProfile as jest.Mock).mockReturnValue(new Promise(() => {}));
    (getPatientGuardRails as jest.Mock).mockReturnValue(new Promise(() => {}));
    (getTherapistAccess as jest.Mock).mockReturnValue(new Promise(() => {}));
    render(<PatientDetailPage />);
    expect(screen.getByText(/loading patient details/i)).toBeInTheDocument();
  });

  it('shows error message on fetch failure', async () => {
    localStorage.setItem('therapistToken', 'tok');
    (getPatientProfile as jest.Mock).mockRejectedValue(new Error('Forbidden'));
    (getPatientGuardRails as jest.Mock).mockResolvedValue([]);
    (getTherapistAccess as jest.Mock).mockResolvedValue({ canViewChats: false, canManageGuardRails: false });
    render(<PatientDetailPage />);
    await waitFor(() => {
      expect(screen.getByText('Forbidden')).toBeInTheDocument();
    });
  });
});

describe('PatientDetailPage — profile display', () => {
  it('renders patient profile details', async () => {
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('Alice')).toBeInTheDocument();
      expect(screen.getByText('alice@test.com')).toBeInTheDocument();
      expect(screen.getByText('555-1234')).toBeInTheDocument();
    });
  });

  it('back button calls router.back()', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByText('Alice'));
    await user.click(screen.getByRole('button', { name: /back/i }));
    expect(mockBack).toHaveBeenCalled();
  });
});

describe('PatientDetailPage — guard rails display', () => {
  it('renders guard rail keywords', async () => {
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('stress')).toBeInTheDocument();
      expect(screen.getByText('medication')).toBeInTheDocument();
    });
  });

  it('renders guard rail replacement text', async () => {
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => {
      expect(screen.getByText(/replacement:.*wellness/i)).toBeInTheDocument();
    });
  });

  it('shows no guard rails message when list is empty', async () => {
    localStorage.setItem('therapistToken', 'tok');
    (getPatientProfile as jest.Mock).mockResolvedValue(profile);
    (getPatientGuardRails as jest.Mock).mockResolvedValue([]);
    (getTherapistAccess as jest.Mock).mockResolvedValue({ canViewChats: false, canManageGuardRails: false });
    render(<PatientDetailPage />);

    await waitFor(() => {
      expect(screen.getByText(/no guard rails configured/i)).toBeInTheDocument();
    });
  });
});

describe('PatientDetailPage — guard rail permissions', () => {
  it('shows add/edit/delete buttons when canManageGuardRails is true', async () => {
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /add guard rail/i })).toBeInTheDocument();
      expect(screen.getAllByRole('button', { name: /edit/i }).length).toBeGreaterThan(0);
      expect(screen.getAllByRole('button', { name: /delete/i }).length).toBeGreaterThan(0);
    });
  });

  it('hides add/edit/delete buttons when canManageGuardRails is false', async () => {
    localStorage.setItem('therapistToken', 'tok');
    setupNoPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByText('stress'));
    expect(screen.queryByRole('button', { name: /add guard rail/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /edit/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /delete/i })).not.toBeInTheDocument();
  });
});

describe('PatientDetailPage — conversations permission', () => {
  it('shows conversations section when canViewChats is true', async () => {
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('Session One')).toBeInTheDocument();
      expect(screen.getByText('Session Two')).toBeInTheDocument();
    });
  });

  it('hides conversations section when canViewChats is false', async () => {
    localStorage.setItem('therapistToken', 'tok');
    setupNoPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByText('Alice'));
    expect(screen.queryByText('Session One')).not.toBeInTheDocument();
    // Guard Rails heading is present but Conversations heading is not
    expect(screen.queryByRole('heading', { name: /conversations/i })).not.toBeInTheDocument();
  });

  it('shows no conversations message when list is empty', async () => {
    localStorage.setItem('therapistToken', 'tok');
    (getPatientProfile as jest.Mock).mockResolvedValue(profile);
    (getPatientGuardRails as jest.Mock).mockResolvedValue([]);
    (getTherapistAccess as jest.Mock).mockResolvedValue({ canViewChats: true, canManageGuardRails: false });
    (getPatientConversations as jest.Mock).mockResolvedValue([]);
    render(<PatientDetailPage />);

    await waitFor(() => {
      expect(screen.getByText(/no conversations found/i)).toBeInTheDocument();
    });
  });
});

describe('PatientDetailPage — expand conversation', () => {
  it('loads and shows messages when conversation is clicked', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (getPatientConversationMessages as jest.Mock).mockResolvedValueOnce(messages);
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByText('Session One'));
    await user.click(screen.getByText('Session One'));

    await waitFor(() => {
      expect(getPatientConversationMessages).toHaveBeenCalledWith('tok', 'p1', 'c1');
      expect(screen.getByText('Hello')).toBeInTheDocument();
      expect(screen.getByText('Hi there')).toBeInTheDocument();
    });
  });

  it('collapses conversation on second click', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (getPatientConversationMessages as jest.Mock).mockResolvedValue(messages);
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByText('Session One'));
    await user.click(screen.getByText('Session One'));
    await waitFor(() => screen.getByText('Hello'));
    await user.click(screen.getByText('Session One'));

    await waitFor(() => {
      expect(screen.queryByText('Hello')).not.toBeInTheDocument();
    });
  });

  it('shows no messages text for empty conversation', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (getPatientConversationMessages as jest.Mock).mockResolvedValueOnce([]);
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByText('Session One'));
    await user.click(screen.getByText('Session One'));

    await waitFor(() => {
      expect(screen.getByText(/no messages in this conversation/i)).toBeInTheDocument();
    });
  });

  it('does not reload messages on second expand of same conversation', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (getPatientConversationMessages as jest.Mock).mockResolvedValue(messages);
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByText('Session One'));
    await user.click(screen.getByText('Session One'));
    await waitFor(() => screen.getByText('Hello'));

    // Collapse then re-expand
    await user.click(screen.getByText('Session One'));
    await user.click(screen.getByText('Session One'));

    // Should have only called once (cached)
    expect(getPatientConversationMessages).toHaveBeenCalledTimes(1);
  });
});

describe('PatientDetailPage — add guard rail modal', () => {
  it('opens and closes add modal', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByRole('button', { name: /add guard rail/i }));
    await user.click(screen.getByRole('button', { name: /add guard rail/i }));
    expect(screen.getByRole('heading', { name: /add guard rail/i })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /cancel/i }));
    await waitFor(() => {
      expect(screen.queryByRole('heading', { name: /add guard rail/i })).not.toBeInTheDocument();
    });
  });

  it('shows error when keyword is empty on submit', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByRole('button', { name: /add guard rail/i }));
    await user.click(screen.getByRole('button', { name: /add guard rail/i }));

    // Submit with empty keyword
    const submitBtn = screen.getAllByRole('button', { name: /add guard rail/i }).find(
      (btn) => btn.getAttribute('type') === 'submit'
    )!;
    await user.click(submitBtn);

    expect(screen.getByText(/keyword is required/i)).toBeInTheDocument();
  });

  it('shows error when action is replace but no replacement provided', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByRole('button', { name: /add guard rail/i }));
    await user.click(screen.getByRole('button', { name: /add guard rail/i }));

    // Find the modal - keyword input inside the form
    const form = document.querySelector('form')!;
    const keywordInput = within(form).getAllByRole('textbox')[0];
    await user.type(keywordInput, 'anxiety');
    await user.selectOptions(within(form).getByRole('combobox'), 'replace');

    const submitBtn = within(form).getByRole('button', { name: /add guard rail/i });
    await user.click(submitBtn);

    expect(screen.getByText(/replacement text is required/i)).toBeInTheDocument();
  });

  it('creates guard rail successfully', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (createPatientGuardRail as jest.Mock).mockResolvedValueOnce({ id: 'gr3', keyword: 'anxiety' });
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByRole('button', { name: /add guard rail/i }));
    await user.click(screen.getByRole('button', { name: /add guard rail/i }));

    const form = document.querySelector('form')!;
    await user.type(within(form).getAllByRole('textbox')[0], 'anxiety');

    const submitBtn = within(form).getByRole('button', { name: /add guard rail/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(createPatientGuardRail).toHaveBeenCalledWith(
        'tok', 'p1', expect.objectContaining({ keyword: 'anxiety', action: 'remove' })
      );
    });
  });
});

describe('PatientDetailPage — edit guard rail modal', () => {
  it('opens edit modal with prefilled data', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getAllByRole('button', { name: /edit/i }));
    await user.click(screen.getAllByRole('button', { name: /edit/i })[0]);

    expect(screen.getByText('Edit Guard Rail')).toBeInTheDocument();
    expect(screen.getByDisplayValue('stress')).toBeInTheDocument();
  });

  it('saves edit and reloads guard rails', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (updatePatientGuardRail as jest.Mock).mockResolvedValueOnce(undefined);
    render(<PatientDetailPage />);

    await waitFor(() => screen.getAllByRole('button', { name: /edit/i }));
    await user.click(screen.getAllByRole('button', { name: /edit/i })[0]);
    await user.click(screen.getByRole('button', { name: /^save$/i }));

    await waitFor(() => {
      expect(updatePatientGuardRail).toHaveBeenCalledWith('tok', 'p1', 'gr1', expect.any(Object));
    });
  });

  it('cancels edit modal', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getAllByRole('button', { name: /edit/i }));
    await user.click(screen.getAllByRole('button', { name: /edit/i })[0]);
    expect(screen.getByText('Edit Guard Rail')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /^cancel$/i }));
    await waitFor(() => {
      expect(screen.queryByText('Edit Guard Rail')).not.toBeInTheDocument();
    });
  });
});

describe('PatientDetailPage — error paths', () => {
  it('shows error when handleSaveEdit fails', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (updatePatientGuardRail as jest.Mock).mockRejectedValueOnce(new Error('Save failed'));
    render(<PatientDetailPage />);

    await waitFor(() => screen.getAllByRole('button', { name: /edit/i }));
    await user.click(screen.getAllByRole('button', { name: /edit/i })[0]);
    await user.click(screen.getByRole('button', { name: /^save$/i }));

    await waitFor(() => {
      expect(screen.getByText('Save failed')).toBeInTheDocument();
    });
  });

  it('shows error when handleDelete fails', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (deletePatientGuardRail as jest.Mock).mockRejectedValueOnce(new Error('Delete failed'));
    render(<PatientDetailPage />);

    await waitFor(() => screen.getAllByRole('button', { name: /delete/i }));
    await user.click(screen.getAllByRole('button', { name: /delete/i })[0]);

    await waitFor(() => {
      expect(screen.getByText('Delete failed')).toBeInTheDocument();
    });
  });

  it('shows addError when createPatientGuardRail fails', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (createPatientGuardRail as jest.Mock).mockRejectedValueOnce(new Error('Create failed'));
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByRole('button', { name: /add guard rail/i }));
    await user.click(screen.getByRole('button', { name: /add guard rail/i }));

    const form = document.querySelector('form')!;
    await user.type(within(form).getAllByRole('textbox')[0], 'stress');
    await user.click(within(form).getByRole('button', { name: /add guard rail/i }));

    await waitFor(() => {
      expect(screen.getByText('Create failed')).toBeInTheDocument();
    });
  });

  it('handles error when loading conversation messages fails', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (getPatientConversationMessages as jest.Mock).mockRejectedValueOnce(new Error('Messages failed'));
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByText('Session One'));
    await user.click(screen.getByText('Session One'));

    await waitFor(() => {
      expect(screen.getByText(/no messages in this conversation/i)).toBeInTheDocument();
    });
  });
});

describe('PatientDetailPage — pagination', () => {
  it('shows pagination controls with more than 5 conversations and navigates pages', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    const manyConvos = Array.from({ length: 7 }, (_, i) => ({
      id: `c${i + 1}`,
      title: `Session ${i + 1}`,
      startedDate: `2024-03-0${i + 1}T10:00:00Z`,
      messageCount: i + 1,
      isArchived: false,
    }));
    (getPatientProfile as jest.Mock).mockResolvedValue(profile);
    (getPatientGuardRails as jest.Mock).mockResolvedValue([]);
    (getTherapistAccess as jest.Mock).mockResolvedValue({ canViewChats: true, canManageGuardRails: false });
    (getPatientConversations as jest.Mock).mockResolvedValue(manyConvos);
    render(<PatientDetailPage />);

    await waitFor(() => screen.getByText('Session 1'));
    expect(screen.getByText(/page 1 of 2/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /← previous/i })).toBeDisabled();

    await user.click(screen.getByRole('button', { name: /next →/i }));

    await waitFor(() => {
      expect(screen.getByText(/page 2 of 2/i)).toBeInTheDocument();
      expect(screen.getByText('Session 6')).toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: /next →/i })).toBeDisabled();

    await user.click(screen.getByRole('button', { name: /← previous/i }));
    await waitFor(() => {
      expect(screen.getByText(/page 1 of 2/i)).toBeInTheDocument();
    });
  });
});

describe('PatientDetailPage — edit modal replace action', () => {
  it('shows replacement field when edit action is replace', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getAllByRole('button', { name: /edit/i }));
    // Click the second guard rail (medication, which has action 'replace')
    await user.click(screen.getAllByRole('button', { name: /edit/i })[1]);

    await waitFor(() => {
      expect(screen.getByDisplayValue('wellness')).toBeInTheDocument();
    });
  });
});

describe('PatientDetailPage — delete guard rail', () => {
  it('deletes guard rail after confirmation', async () => {
    const user = userEvent.setup();
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    (deletePatientGuardRail as jest.Mock).mockResolvedValueOnce(undefined);
    render(<PatientDetailPage />);

    await waitFor(() => screen.getAllByRole('button', { name: /delete/i }));
    await user.click(screen.getAllByRole('button', { name: /delete/i })[0]);

    await waitFor(() => {
      expect(deletePatientGuardRail).toHaveBeenCalledWith('tok', 'p1', 'gr1');
    });
  });

  it('does not delete when confirm returns false', async () => {
    const user = userEvent.setup();
    (window.confirm as jest.Mock).mockReturnValueOnce(false);
    localStorage.setItem('therapistToken', 'tok');
    setupAllPermissions();
    render(<PatientDetailPage />);

    await waitFor(() => screen.getAllByRole('button', { name: /delete/i }));
    await user.click(screen.getAllByRole('button', { name: /delete/i })[0]);

    expect(deletePatientGuardRail).not.toHaveBeenCalled();
  });
});
