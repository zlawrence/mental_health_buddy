import Link from 'next/link';

export default function HomePage() {
  return (
    <main style={{ padding: '40px', fontFamily: 'Arial, sans-serif' }}>
      <h1>Therapist Portal</h1>
      <p>Use this portal to sign in, view assigned patients, and manage care plans.</p>
      <div style={{ display: 'flex', gap: '16px', marginTop: '24px' }}>
        <Link href="/login" style={{ padding: '12px 20px', background: '#2563eb', color: 'white', borderRadius: '8px', textDecoration: 'none' }}>
          Login
        </Link>
        <Link href="/dashboard" style={{ padding: '12px 20px', background: '#10b981', color: 'white', borderRadius: '8px', textDecoration: 'none' }}>
          Dashboard
        </Link>
      </div>
    </main>
  );
}
