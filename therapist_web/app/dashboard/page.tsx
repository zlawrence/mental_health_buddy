'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { getAssignedPatients } from '../../lib/api';

export default function DashboardPage() {
  const router = useRouter();
  const [patients, setPatients] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('therapistToken');
    if (!token) {
      router.push('/login');
      return;
    }

    getAssignedPatients(token)
      .then(setPatients)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [router]);

  const handleLogout = () => {
    localStorage.removeItem('therapistToken');
    router.push('/login');
  };

  return (
    <main style={{ padding: 24, fontFamily: 'Arial, sans-serif' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1>Assigned Patients</h1>
          <p>Choose a patient to view details and manage guard rails.</p>
        </div>
        <button onClick={handleLogout} style={{ padding: '10px 18px', background: '#ef4444', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer' }}>
          Logout
        </button>
      </div>

      {loading && <p>Loading patients...</p>}
      {error && <p style={{ color: 'red' }}>{error}</p>}
      {!loading && !error && (
        <ul style={{ listStyle: 'none', padding: 0 }}>
          {patients.map((patient) => (
            <li key={patient.userId} style={{ marginBottom: 16, padding: 16, border: '1px solid #ddd', borderRadius: 8 }}>
              <Link href={`/patients/${patient.userId}`} style={{ textDecoration: 'none', color: 'inherit' }}>
                <strong>{patient.username}</strong>
                <p>{patient.email}</p>
                <p>Phone: {patient.phoneNumber ?? 'N/A'}</p>
                <p style={{ color: '#2563eb' }}>View patient details →</p>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
