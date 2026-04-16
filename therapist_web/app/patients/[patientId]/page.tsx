'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { getPatientProfile, getPatientGuardRails, updatePatientGuardRail, deletePatientGuardRail } from '../../../lib/api';

export default function PatientDetailPage() {
  const params = useParams();
  const router = useRouter();
  const patientId = params?.patientId as string;
  const [profile, setProfile] = useState<any | null>(null);
  const [guardRails, setGuardRails] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingRail, setEditingRail] = useState<any | null>(null);
  const [editForm, setEditForm] = useState({ keyword: '', action: 'remove', replacement: '', isActive: true });

  useEffect(() => {
    const token = localStorage.getItem('therapistToken');
    if (!token) {
      router.push('/login');
      return;
    }

    const load = async () => {
      try {
        const patientProfile = await getPatientProfile(token, patientId);
        const rails = await getPatientGuardRails(token, patientId);
        setProfile(patientProfile);
        setGuardRails(rails);
      } catch (err) {
        setError((err as Error).message);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [patientId, router]);

  const handleEdit = (rail: any) => {
    setEditingRail(rail);
    setEditForm({
      keyword: rail.keyword,
      action: rail.action,
      replacement: rail.replacement || '',
      isActive: rail.isActive,
    });
  };

  const handleSaveEdit = async () => {
    const token = localStorage.getItem('therapistToken');
    if (!token || !editingRail) return;

    try {
      await updatePatientGuardRail(token, patientId, editingRail.id, editForm);
      setEditingRail(null);
      // Reload guard rails
      const rails = await getPatientGuardRails(token, patientId);
      setGuardRails(rails);
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const handleDelete = async (guardRailId: string) => {
    if (!confirm('Are you sure you want to delete this guard rail?')) return;

    const token = localStorage.getItem('therapistToken');
    if (!token) return;

    try {
      await deletePatientGuardRail(token, patientId, guardRailId);
      // Reload guard rails
      const rails = await getPatientGuardRails(token, patientId);
      setGuardRails(rails);
    } catch (err) {
      setError((err as Error).message);
    }
  };

  return (
    <main style={{ padding: 24, fontFamily: 'Arial, sans-serif' }}>
      <button onClick={() => router.back()} style={{ marginBottom: 20, padding: '8px 14px', background: '#e5e7eb', border: 'none', borderRadius: 6 }}>
        ← Back
      </button>

      {loading && <p>Loading patient details...</p>}
      {error && <p style={{ color: 'red' }}>{error}</p>}

      {!loading && !error && profile && (
        <div>
          <h1>{profile.username}</h1>
          <p><strong>Email:</strong> {profile.email}</p>
          <p><strong>Phone:</strong> {profile.phoneNumber ?? 'N/A'}</p>
          <p><strong>Conversation retention:</strong> {profile.conversationRetentionDays}</p>
          <p><strong>Assigned therapist ids:</strong> {profile.therapistIds?.join(', ') || 'None'}</p>

          <section style={{ marginTop: 24 }}>
            <h2>Guard Rails</h2>
            {guardRails.length === 0 ? (
              <p>No guard rails available for this patient.</p>
            ) : (
              <ul style={{ listStyle: 'none', padding: 0 }}>
                {guardRails.map((rail) => (
                  <li key={rail.id} style={{ marginBottom: 12, padding: 16, border: '1px solid #ddd', borderRadius: 8 }}>
                    <strong>{rail.keyword}</strong>
                    <p>Action: {rail.action}</p>
                    {rail.replacement && <p>Replacement: {rail.replacement}</p>}
                    <p>Status: {rail.isActive ? 'Active' : 'Inactive'}</p>
                    <div style={{ marginTop: 8 }}>
                      <button onClick={() => handleEdit(rail)} style={{ marginRight: 8, padding: '4px 8px', background: '#3b82f6', color: 'white', border: 'none', borderRadius: 4 }}>
                        Edit
                      </button>
                      <button onClick={() => handleDelete(rail.id)} style={{ padding: '4px 8px', background: '#ef4444', color: 'white', border: 'none', borderRadius: 4 }}>
                        Delete
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>
      )}

      {editingRail && (
        <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <div style={{ background: 'white', padding: 24, borderRadius: 8, width: 400 }}>
            <h3>Edit Guard Rail</h3>
            <div style={{ marginBottom: 16 }}>
              <label>Keyword:</label>
              <input
                type="text"
                value={editForm.keyword}
                onChange={(e) => setEditForm({ ...editForm, keyword: e.target.value })}
                style={{ width: '100%', padding: 8, border: '1px solid #ddd', borderRadius: 4 }}
              />
            </div>
            <div style={{ marginBottom: 16 }}>
              <label>Action:</label>
              <select
                value={editForm.action}
                onChange={(e) => setEditForm({ ...editForm, action: e.target.value })}
                style={{ width: '100%', padding: 8, border: '1px solid #ddd', borderRadius: 4 }}
              >
                <option value="remove">Remove</option>
                <option value="replace">Replace</option>
              </select>
            </div>
            {editForm.action === 'replace' && (
              <div style={{ marginBottom: 16 }}>
                <label>Replacement:</label>
                <input
                  type="text"
                  value={editForm.replacement}
                  onChange={(e) => setEditForm({ ...editForm, replacement: e.target.value })}
                  style={{ width: '100%', padding: 8, border: '1px solid #ddd', borderRadius: 4 }}
                />
              </div>
            )}
            <div style={{ marginBottom: 16 }}>
              <label>
                <input
                  type="checkbox"
                  checked={editForm.isActive}
                  onChange={(e) => setEditForm({ ...editForm, isActive: e.target.checked })}
                />
                Active
              </label>
            </div>
            <div>
              <button onClick={handleSaveEdit} style={{ marginRight: 8, padding: '8px 16px', background: '#10b981', color: 'white', border: 'none', borderRadius: 4 }}>
                Save
              </button>
              <button onClick={() => setEditingRail(null)} style={{ padding: '8px 16px', background: '#6b7280', color: 'white', border: 'none', borderRadius: 4 }}>
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}
