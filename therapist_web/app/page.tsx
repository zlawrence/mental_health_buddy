import Link from 'next/link';

export default function HomePage() {
  return (
    <main className="min-h-screen flex flex-col items-center justify-center bg-gray-50 p-6">
      <div className="w-full max-w-lg">
        <h1 className="text-3xl font-bold text-gray-900 mb-3">Therapist Portal</h1>
        <p className="text-gray-600 mb-8">Use this portal to sign in, view assigned patients, and manage care plans.</p>
        <div className="flex flex-col sm:flex-row gap-4">
          <Link
            href="/login"
            className="px-5 py-3 bg-blue-600 text-white rounded-lg text-center font-medium hover:bg-blue-700 transition-colors no-underline"
          >
            Login
          </Link>
          <Link
            href="/dashboard"
            className="px-5 py-3 bg-emerald-500 text-white rounded-lg text-center font-medium hover:bg-emerald-600 transition-colors no-underline"
          >
            Dashboard
          </Link>
        </div>
      </div>
    </main>
  );
}
