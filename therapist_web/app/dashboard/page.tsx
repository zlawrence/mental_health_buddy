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
    <main className="min-h-screen bg-gray-50 p-4 sm:p-6">
      <div className="max-w-2xl mx-auto">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Assigned Patients</h1>
            <p className="text-gray-600 text-sm mt-1">Choose a patient to view details and manage guard rails.</p>
          </div>
          <button
            onClick={handleLogout}
            className="self-start sm:self-auto px-4 py-2 bg-indigo-500 text-white text-sm font-medium rounded-lg hover:bg-indigo-600 transition-colors cursor-pointer border-none"
          >
            Logout
          </button>
        </div>

        {loading && <p className="text-gray-500">Loading patients...</p>}
        {error && <p className="text-red-600">{error}</p>}
        {!loading && !error && (
          <ul className="flex flex-col gap-3 list-none p-0 m-0">
            {patients.map((patient) => (
              <li key={patient.userId}>
                <Link
                  href={`/patients/${patient.userId}`}
                  className="block p-4 bg-white border border-gray-200 rounded-xl hover:border-blue-300 hover:shadow-sm transition-all no-underline text-inherit"
                >
                  <strong className="text-gray-900 text-base block">{patient.username}</strong>
                  <p className="text-gray-600 text-sm mt-1 mb-0">{patient.email}</p>
                  <p className="text-gray-500 text-sm mb-0">Phone: {patient.phoneNumber ?? 'N/A'}</p>
                  <p className="text-blue-600 text-sm mt-2 mb-0">View patient details →</p>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </main>
  );
}
