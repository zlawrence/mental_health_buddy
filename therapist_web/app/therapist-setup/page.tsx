'use client';

import { Suspense, useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { validateInvitation, setupTherapist, type InvitationValidation } from '../../lib/api';

type PageState = 'loading' | 'invalid' | 'expired' | 'used' | 'login-required' | 'setup' | 'success';

export default function TherapistSetupPage() {
  return (
    <Suspense fallback={
      <main className="min-h-screen flex items-center justify-center bg-gray-50 p-4">
        <div className="w-full max-w-md bg-white rounded-xl border border-gray-200 shadow-sm p-8">
          <p className="text-gray-500">Loading...</p>
        </div>
      </main>
    }>
      <TherapistSetupContent />
    </Suspense>
  );
}

function TherapistSetupContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const [pageState, setPageState] = useState<PageState>('loading');
  const [invitation, setInvitation] = useState<InvitationValidation | null>(null);

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [usernameError, setUsernameError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!token) {
      setPageState('invalid');
      return;
    }

    validateInvitation(token)
      .then((result) => {
        setInvitation(result);
        if (!result.valid) {
          if (result.alreadyUsed) setPageState('used');
          else if (result.expired) setPageState('expired');
          else setPageState('invalid');
        } else if (result.emailAlreadyRegistered) {
          setPageState('login-required');
        } else {
          setPageState('setup');
        }
      })
      .catch(() => setPageState('invalid'));
  }, [token]);

  const handleGoToLogin = () => {
    localStorage.setItem('pendingInvitationToken', token);
    router.push('/login');
  };

  const validatePassword = (p: string): string | null => {
    if (p.length < 12 || p.length > 16) return 'Must be 12–16 characters';
    if (!/[A-Z]/.test(p)) return 'Must contain at least 1 uppercase letter';
    if (!/[a-z]/.test(p)) return 'Must contain at least 1 lowercase letter';
    if (p.replace(/[a-zA-Z0-9]/g, '').length < 2) return 'Must contain at least 2 special characters';
    return null;
  };

  const handleSetup = async (e: React.FormEvent) => {
    e.preventDefault();
    setUsernameError(null);
    setPasswordError(null);
    setGeneralError(null);

    if (!username.trim()) {
      setUsernameError('Username is required');
      return;
    }

    const pwError = validatePassword(password);
    if (pwError) {
      setPasswordError(pwError);
      return;
    }

    if (password !== confirmPassword) {
      setPasswordError('Passwords do not match');
      return;
    }

    setIsSubmitting(true);
    try {
      const authToken = await setupTherapist(token, username.trim(), password, confirmPassword);
      localStorage.setItem('therapistToken', authToken);
      setPageState('success');
      setTimeout(() => router.push('/dashboard'), 1500);
    } catch (err) {
      const message = (err as Error).message;
      if (message.toLowerCase().includes('username')) {
        setUsernameError(message);
      } else {
        setGeneralError(message);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const pageWrap = 'min-h-screen flex items-center justify-center bg-gray-50 p-4';
  const card = 'w-full max-w-md bg-white rounded-xl border border-gray-200 shadow-sm p-8';

  if (pageState === 'loading') {
    return (
      <main className={pageWrap}>
        <div className={card}>
          <p className="text-gray-500">Validating invitation...</p>
        </div>
      </main>
    );
  }

  if (pageState === 'invalid') {
    return (
      <main className={pageWrap}>
        <div className={card}>
          <h1 className="text-2xl font-bold text-red-600 mb-3">Invalid Invitation</h1>
          <p className="text-gray-600">This invitation link is invalid. Please ask the patient to send a new invitation.</p>
        </div>
      </main>
    );
  }

  if (pageState === 'expired') {
    return (
      <main className={pageWrap}>
        <div className={card}>
          <h1 className="text-2xl font-bold text-amber-500 mb-3">Invitation Expired</h1>
          <p className="text-gray-600">This invitation link has expired. Please ask the patient to send a new invitation.</p>
          {invitation?.therapistEmail && (
            <p className="text-gray-400 text-sm mt-2">Sent to: {invitation.therapistEmail}</p>
          )}
        </div>
      </main>
    );
  }

  if (pageState === 'used') {
    return (
      <main className={pageWrap}>
        <div className={card}>
          <h1 className="text-2xl font-bold text-gray-500 mb-3">Invitation Already Used</h1>
          <p className="text-gray-600 mb-6">This invitation has already been accepted. You can log in to access your patients.</p>
          <button
            className="w-full py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors cursor-pointer border-none"
            onClick={() => router.push('/login')}
          >
            Go to Login
          </button>
        </div>
      </main>
    );
  }

  if (pageState === 'login-required') {
    return (
      <main className={pageWrap}>
        <div className={card}>
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
            <h2 className="text-lg font-bold text-blue-800 mb-1">You&apos;ve been invited!</h2>
            <p className="text-blue-700 text-sm m-0">A patient has invited you to support them on Anxiety Buddy.</p>
          </div>
          <p className="text-gray-600 mb-6">
            An account is already registered for <strong>{invitation?.therapistEmail}</strong>.
            Please log in to accept this invitation and gain access to the patient&apos;s profile.
          </p>
          <button
            className="w-full py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors cursor-pointer border-none"
            onClick={handleGoToLogin}
          >
            Log in to accept invitation
          </button>
        </div>
      </main>
    );
  }

  if (pageState === 'success') {
    return (
      <main className={pageWrap}>
        <div className={card}>
          <h1 className="text-2xl font-bold text-emerald-600 mb-3">Account Created!</h1>
          <p className="text-gray-600">Your therapist account has been set up. Redirecting to your dashboard...</p>
        </div>
      </main>
    );
  }

  return (
    <main className={pageWrap}>
      <div className={card}>
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
          <h2 className="text-lg font-bold text-blue-800 mb-1">You&apos;ve been invited!</h2>
          <p className="text-blue-700 text-sm m-0">
            A patient has invited you to support them on Anxiety Buddy.
            Create your account below to get started.
          </p>
          {invitation?.therapistEmail && (
            <p className="text-blue-600 text-xs mt-2 mb-0">
              Account email: <strong>{invitation.therapistEmail}</strong>
            </p>
          )}
        </div>

        <h1 className="text-2xl font-bold text-gray-900 mb-6">Create Your Account</h1>

        <form onSubmit={handleSetup} className="flex flex-col gap-4">
          <div>
            <label className="block text-sm font-semibold text-gray-700 mb-1">Username</label>
            <input
              value={username}
              onChange={(e) => { setUsername(e.target.value); setUsernameError(null); }}
              className={`w-full px-3 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 ${usernameError ? 'border-red-500' : 'border-gray-300'}`}
              autoComplete="username"
            />
            {usernameError && <div className="text-red-600 text-xs mt-1">{usernameError}</div>}
          </div>

          <div>
            <label className="block text-sm font-semibold text-gray-700 mb-1">Password</label>
            <div className="relative">
              <input
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={(e) => { setPassword(e.target.value); setPasswordError(null); }}
                className={`w-full px-3 py-2.5 pr-14 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 ${passwordError ? 'border-red-500' : 'border-gray-300'}`}
                autoComplete="new-password"
              />
              <button
                type="button"
                onClick={() => setShowPassword((v) => !v)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-gray-500 hover:text-gray-700 bg-transparent border-none cursor-pointer"
              >
                {showPassword ? 'Hide' : 'Show'}
              </button>
            </div>
          </div>

          <div>
            <label className="block text-sm font-semibold text-gray-700 mb-1">Confirm Password</label>
            <div className="relative">
              <input
                type={showConfirmPassword ? 'text' : 'password'}
                value={confirmPassword}
                onChange={(e) => { setConfirmPassword(e.target.value); setPasswordError(null); }}
                className="w-full px-3 py-2.5 pr-14 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                autoComplete="new-password"
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword((v) => !v)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-gray-500 hover:text-gray-700 bg-transparent border-none cursor-pointer"
              >
                {showConfirmPassword ? 'Hide' : 'Show'}
              </button>
            </div>
            {passwordError && <div className="text-red-600 text-xs mt-1">{passwordError}</div>}
          </div>

          <div className="bg-gray-50 border border-gray-200 rounded-lg p-3 text-xs text-gray-500">
            Password must be 12–16 characters and contain at least 1 uppercase letter, 1 lowercase letter, and 2 special characters.
          </div>

          {generalError && <div className="text-red-600 text-sm text-center">{generalError}</div>}

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 disabled:opacity-60 transition-colors cursor-pointer border-none"
          >
            {isSubmitting ? 'Creating account...' : 'Create Account & Accept Invitation'}
          </button>
        </form>

        <p className="mt-4 text-xs text-gray-500 text-center">
          Already have an account?{' '}
          <button
            onClick={handleGoToLogin}
            className="text-blue-600 hover:underline bg-transparent border-none cursor-pointer text-xs p-0"
          >
            Log in instead
          </button>
        </p>
      </div>
    </main>
  );
}
