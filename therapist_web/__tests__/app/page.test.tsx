import { render, screen } from '@testing-library/react';
import Home from '../../app/page';

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn() }),
}));

describe('Home page', () => {
  it('renders login link', () => {
    render(<Home />);
    expect(screen.getByRole('link', { name: /login/i })).toBeInTheDocument();
  });

  it('renders dashboard link', () => {
    render(<Home />);
    expect(screen.getByRole('link', { name: /dashboard/i })).toBeInTheDocument();
  });

  it('login link points to /login', () => {
    render(<Home />);
    expect(screen.getByRole('link', { name: /login/i })).toHaveAttribute('href', '/login');
  });

  it('dashboard link points to /dashboard', () => {
    render(<Home />);
    expect(screen.getByRole('link', { name: /dashboard/i })).toHaveAttribute('href', '/dashboard');
  });
});
