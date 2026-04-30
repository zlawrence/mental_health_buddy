'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import {
  getPatientProfile,
  getPatientGuardRails,
  createPatientGuardRail,
  updatePatientGuardRail,
  deletePatientGuardRail,
  getPatientConversations,
  getPatientConversationMessages,
  getTherapistAccess,
  type TherapistAccess,
} from '../../../lib/api';

const CONVOS_PER_PAGE = 5;

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

export default function PatientDetailPage() {
  const params = useParams();
  const router = useRouter();
  const patientId = params?.patientId as string;

  const [profile, setProfile] = useState<any | null>(null);
  const [guardRails, setGuardRails] = useState<any[]>([]);
  const [permissions, setPermissions] = useState<TherapistAccess | null>(null);
  const [conversations, setConversations] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Edit guard rail
  const [editingRail, setEditingRail] = useState<any | null>(null);
  const [editForm, setEditForm] = useState({ keyword: '', action: 'remove', replacement: '', isActive: true });

  // Add guard rail
  const [showAddModal, setShowAddModal] = useState(false);
  const [addForm, setAddForm] = useState({ keyword: '', action: 'remove', replacement: '' });
  const [addError, setAddError] = useState<string | null>(null);
  const [isAdding, setIsAdding] = useState(false);

  // Conversations
  const [convoPage, setConvoPage] = useState(0);
  const [expandedConvoId, setExpandedConvoId] = useState<string | null>(null);
  const [convoMessages, setConvoMessages] = useState<Record<string, any[]>>({});
  const [loadingMessages, setLoadingMessages] = useState<Record<string, boolean>>({});

  useEffect(() => {
    const token = localStorage.getItem('therapistToken');
    if (!token) {
      router.push('/login');
      return;
    }

    const load = async () => {
      try {
        const [patientProfile, rails, access] = await Promise.all([
          getPatientProfile(token, patientId),
          getPatientGuardRails(token, patientId),
          getTherapistAccess(token, patientId),
        ]);
        setProfile(patientProfile);
        setGuardRails(rails);
        setPermissions(access);

        if (access.canViewChats) {
          const convos = await getPatientConversations(token, patientId);
          setConversations(convos);
        }
      } catch (err) {
        setError((err as Error).message);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [patientId, router]);

  const reloadGuardRails = async () => {
    const token = localStorage.getItem('therapistToken');
    if (!token) return;
    const rails = await getPatientGuardRails(token, patientId);
    setGuardRails(rails);
  };

  // Edit handlers
  const handleEdit = (rail: any) => {
    setEditingRail(rail);
    setEditForm({ keyword: rail.keyword, action: rail.action, replacement: rail.replacement || '', isActive: rail.isActive });
  };

  const handleSaveEdit = async () => {
    const token = localStorage.getItem('therapistToken');
    if (!token || !editingRail) return;
    try {
      await updatePatientGuardRail(token, patientId, editingRail.id, editForm);
      setEditingRail(null);
      await reloadGuardRails();
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
      await reloadGuardRails();
    } catch (err) {
      setError((err as Error).message);
    }
  };

  // Add handler
  const handleAddGuardRail = async (e: React.FormEvent) => {
    e.preventDefault();
    setAddError(null);
    if (!addForm.keyword.trim()) { setAddError('Keyword is required'); return; }
    if (addForm.action === 'replace' && !addForm.replacement.trim()) {
      setAddError('Replacement text is required when action is "replace"');
      return;
    }
    const token = localStorage.getItem('therapistToken');
    if (!token) return;
    setIsAdding(true);
    try {
      await createPatientGuardRail(token, patientId, {
        keyword: addForm.keyword.trim(),
        action: addForm.action,
        replacement: addForm.replacement.trim() || undefined,
      });
      setShowAddModal(false);
      setAddForm({ keyword: '', action: 'remove', replacement: '' });
      await reloadGuardRails();
    } catch (err) {
      setAddError((err as Error).message);
    } finally {
      setIsAdding(false);
    }
  };

  const closeAddModal = () => {
    setShowAddModal(false);
    setAddError(null);
    setAddForm({ keyword: '', action: 'remove', replacement: '' });
  };

  // Conversation handlers
  const handleToggleConversation = async (convoId: string) => {
    if (expandedConvoId === convoId) { setExpandedConvoId(null); return; }
    setExpandedConvoId(convoId);
    if (convoMessages[convoId] !== undefined) return;

    const token = localStorage.getItem('therapistToken');
    if (!token) return;
    setLoadingMessages((prev) => ({ ...prev, [convoId]: true }));
    try {
      const messages = await getPatientConversationMessages(token, patientId, convoId);
      setConvoMessages((prev) => ({ ...prev, [convoId]: messages }));
    } catch {
      setConvoMessages((prev) => ({ ...prev, [convoId]: [] }));
    } finally {
      setLoadingMessages((prev) => ({ ...prev, [convoId]: false }));
    }
  };

  const totalConvoPages = Math.ceil(conversations.length / CONVOS_PER_PAGE);
  const pagedConversations = conversations.slice(convoPage * CONVOS_PER_PAGE, (convoPage + 1) * CONVOS_PER_PAGE);

  const inputClass = 'w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500';
  const labelClass = 'block text-sm font-semibold text-gray-700 mb-1';

  return (
    <main className="min-h-screen bg-gray-50 p-4 sm:p-6">
      <div className="max-w-2xl mx-auto">
        <button
          onClick={() => router.back()}
          className="mb-5 px-4 py-2 bg-gray-200 text-gray-700 text-sm rounded-lg hover:bg-gray-300 transition-colors cursor-pointer border-none"
        >
          ← Back
        </button>

        {loading && <p className="text-gray-500">Loading patient details...</p>}
        {error && <p className="text-red-600">{error}</p>}

        {!loading && !error && profile && (
          <div className="flex flex-col gap-6">

            {/* Patient Profile */}
            <div className="bg-white border border-gray-200 rounded-xl p-5">
              <h1 className="text-2xl font-bold text-gray-900 mb-3">{profile.username}</h1>
              <dl className="flex flex-col gap-1.5 text-sm">
                <div className="flex flex-wrap gap-1">
                  <dt className="font-semibold text-gray-700">Email:</dt>
                  <dd className="text-gray-600 m-0">{profile.email}</dd>
                </div>
                <div className="flex flex-wrap gap-1">
                  <dt className="font-semibold text-gray-700">Phone:</dt>
                  <dd className="text-gray-600 m-0">{profile.phoneNumber ?? 'N/A'}</dd>
                </div>
                <div className="flex flex-wrap gap-1">
                  <dt className="font-semibold text-gray-700">Conversation retention:</dt>
                  <dd className="text-gray-600 m-0">{profile.conversationRetentionDays} days</dd>
                </div>
                <div className="flex flex-wrap gap-1">
                  <dt className="font-semibold text-gray-700">Assigned therapist IDs:</dt>
                  <dd className="text-gray-600 m-0 break-all">{profile.therapistIds?.join(', ') || 'None'}</dd>
                </div>
              </dl>
            </div>

            {/* Guard Rails */}
            <section>
              <div className="flex items-center justify-between mb-3">
                <h2 className="text-xl font-bold text-gray-900">Guard Rails</h2>
                {permissions?.canManageGuardRails && (
                  <button
                    onClick={() => setShowAddModal(true)}
                    className="px-3 py-1.5 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer border-none"
                  >
                    + Add Guard Rail
                  </button>
                )}
              </div>

              {guardRails.length === 0 ? (
                <p className="text-gray-500">No guard rails configured for this patient.</p>
              ) : (
                <ul className="flex flex-col gap-3 list-none p-0 m-0">
                  {guardRails.map((rail) => (
                    <li key={rail.id} className="bg-white border border-gray-200 rounded-xl p-4">
                      <strong className="text-gray-900 block">{rail.keyword}</strong>
                      <p className="text-sm text-gray-600 mt-1 mb-0">Action: {rail.action}</p>
                      {rail.replacement && <p className="text-sm text-gray-600 mb-0">Replacement: {rail.replacement}</p>}
                      <p className="text-sm mb-0 mt-0.5">
                        Status:{' '}
                        <span className={rail.isActive ? 'text-emerald-600 font-medium' : 'text-gray-400'}>
                          {rail.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </p>
                      {permissions?.canManageGuardRails && (
                        <div className="flex gap-2 mt-3">
                          <button
                            onClick={() => handleEdit(rail)}
                            className="px-3 py-1.5 bg-blue-500 text-white text-sm rounded-lg hover:bg-blue-600 transition-colors cursor-pointer border-none"
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => handleDelete(rail.id)}
                            className="px-3 py-1.5 bg-red-500 text-white text-sm rounded-lg hover:bg-red-600 transition-colors cursor-pointer border-none"
                          >
                            Delete
                          </button>
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </section>

            {/* Conversations */}
            {permissions?.canViewChats && (
              <section>
                <h2 className="text-xl font-bold text-gray-900 mb-3">Conversations</h2>
                {conversations.length === 0 ? (
                  <p className="text-gray-500">No conversations found for this patient.</p>
                ) : (
                  <>
                    <ul className="flex flex-col gap-2 list-none p-0 m-0 mb-4">
                      {pagedConversations.map((convo) => (
                        <li key={convo.id}>
                          <button
                            onClick={() => handleToggleConversation(convo.id)}
                            className="w-full text-left bg-white border border-gray-200 rounded-xl p-4 hover:border-indigo-300 hover:shadow-sm transition-all cursor-pointer border-solid"
                          >
                            <div className="flex items-start justify-between gap-2">
                              <div className="flex-1 min-w-0">
                                <p className="font-semibold text-gray-900 truncate m-0">
                                  {convo.title || 'Untitled Conversation'}
                                </p>
                                <p className="text-xs text-gray-500 mt-0.5 m-0">
                                  {formatDate(convo.startedDate)} · {convo.messageCount} messages
                                </p>
                                {convo.isArchived && (
                                  <span className="text-xs text-gray-400 italic">Archived</span>
                                )}
                              </div>
                              <span className="text-gray-400 text-xs shrink-0 mt-0.5">
                                {expandedConvoId === convo.id ? '▲' : '▼'}
                              </span>
                            </div>
                          </button>

                          {expandedConvoId === convo.id && (
                            <div className="mt-1 border border-gray-200 rounded-xl bg-white overflow-hidden">
                              {loadingMessages[convo.id] ? (
                                <p className="p-4 text-gray-500 text-sm">Loading messages...</p>
                              ) : (convoMessages[convo.id] ?? []).length === 0 ? (
                                <p className="p-4 text-gray-500 text-sm">No messages in this conversation.</p>
                              ) : (
                                <div className="divide-y divide-gray-100">
                                  {(convoMessages[convo.id] ?? []).map((msg: any) => {
                                    const isUser = msg.role === 'User' || msg.role === 'user';
                                    return (
                                      <div key={msg.id} className={`p-4 ${isUser ? 'bg-blue-50' : 'bg-white'}`}>
                                        <p className={`text-xs font-semibold mb-1 m-0 ${isUser ? 'text-blue-700' : 'text-emerald-700'}`}>
                                          {isUser ? 'Patient' : 'AI Assistant'}
                                        </p>
                                        <p className="text-sm text-gray-800 whitespace-pre-wrap m-0">{msg.content}</p>
                                        <p className="text-xs text-gray-400 mt-1 m-0">{formatDate(msg.timestamp)}</p>
                                      </div>
                                    );
                                  })}
                                </div>
                              )}
                            </div>
                          )}
                        </li>
                      ))}
                    </ul>

                    {totalConvoPages > 1 && (
                      <div className="flex items-center justify-between gap-2">
                        <button
                          onClick={() => { setConvoPage((p) => p - 1); setExpandedConvoId(null); }}
                          disabled={convoPage === 0}
                          className="px-3 py-1.5 text-sm bg-white border border-gray-300 border-solid rounded-lg hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                        >
                          ← Previous
                        </button>
                        <span className="text-sm text-gray-500">Page {convoPage + 1} of {totalConvoPages}</span>
                        <button
                          onClick={() => { setConvoPage((p) => p + 1); setExpandedConvoId(null); }}
                          disabled={convoPage >= totalConvoPages - 1}
                          className="px-3 py-1.5 text-sm bg-white border border-gray-300 border-solid rounded-lg hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                        >
                          Next →
                        </button>
                      </div>
                    )}
                  </>
                )}
              </section>
            )}

          </div>
        )}
      </div>

      {/* Edit Guard Rail Modal */}
      {editingRail && (
        <div className="fixed inset-0 bg-black/50 flex items-end sm:items-center justify-center z-50 p-0 sm:p-4">
          <div className="bg-white w-full sm:max-w-md rounded-t-2xl sm:rounded-xl p-6">
            <h3 className="text-lg font-bold text-gray-900 mb-4">Edit Guard Rail</h3>
            <div className="flex flex-col gap-4">
              <div>
                <label className={labelClass}>Keyword</label>
                <input
                  type="text"
                  value={editForm.keyword}
                  onChange={(e) => setEditForm({ ...editForm, keyword: e.target.value })}
                  className={inputClass}
                />
              </div>
              <div>
                <label className={labelClass}>Action</label>
                <select
                  value={editForm.action}
                  onChange={(e) => setEditForm({ ...editForm, action: e.target.value })}
                  className={inputClass}
                >
                  <option value="remove">Remove</option>
                  <option value="replace">Replace</option>
                </select>
              </div>
              {editForm.action === 'replace' && (
                <div>
                  <label className={labelClass}>Replacement</label>
                  <input
                    type="text"
                    value={editForm.replacement}
                    onChange={(e) => setEditForm({ ...editForm, replacement: e.target.value })}
                    className={inputClass}
                  />
                </div>
              )}
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={editForm.isActive}
                  onChange={(e) => setEditForm({ ...editForm, isActive: e.target.checked })}
                  className="w-4 h-4"
                />
                <span className="text-sm font-medium text-gray-700">Active</span>
              </label>
              <div className="flex gap-2 pt-2">
                <button
                  onClick={handleSaveEdit}
                  className="flex-1 py-2.5 bg-emerald-500 text-white font-medium rounded-lg hover:bg-emerald-600 transition-colors cursor-pointer border-none"
                >
                  Save
                </button>
                <button
                  onClick={() => setEditingRail(null)}
                  className="flex-1 py-2.5 bg-gray-500 text-white font-medium rounded-lg hover:bg-gray-600 transition-colors cursor-pointer border-none"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Add Guard Rail Modal */}
      {showAddModal && (
        <div className="fixed inset-0 bg-black/50 flex items-end sm:items-center justify-center z-50 p-0 sm:p-4">
          <div className="bg-white w-full sm:max-w-md rounded-t-2xl sm:rounded-xl p-6">
            <h3 className="text-lg font-bold text-gray-900 mb-4">Add Guard Rail</h3>
            <form onSubmit={handleAddGuardRail} className="flex flex-col gap-4">
              <div>
                <label className={labelClass}>Keyword</label>
                <input
                  type="text"
                  value={addForm.keyword}
                  onChange={(e) => { setAddForm({ ...addForm, keyword: e.target.value }); setAddError(null); }}
                  className={inputClass}
                  placeholder="e.g. medication, self-harm"
                  autoFocus
                />
              </div>
              <div>
                <label className={labelClass}>Action</label>
                <select
                  value={addForm.action}
                  onChange={(e) => setAddForm({ ...addForm, action: e.target.value })}
                  className={inputClass}
                >
                  <option value="remove">Remove</option>
                  <option value="replace">Replace</option>
                </select>
              </div>
              {addForm.action === 'replace' && (
                <div>
                  <label className={labelClass}>Replacement</label>
                  <input
                    type="text"
                    value={addForm.replacement}
                    onChange={(e) => { setAddForm({ ...addForm, replacement: e.target.value }); setAddError(null); }}
                    className={inputClass}
                    placeholder="Replacement text"
                  />
                </div>
              )}
              {addError && <div className="text-red-600 text-sm">{addError}</div>}
              <div className="flex gap-2 pt-2">
                <button
                  type="submit"
                  disabled={isAdding}
                  className="flex-1 py-2.5 bg-indigo-600 text-white font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-60 transition-colors cursor-pointer border-none"
                >
                  {isAdding ? 'Adding...' : 'Add Guard Rail'}
                </button>
                <button
                  type="button"
                  onClick={closeAddModal}
                  className="flex-1 py-2.5 bg-gray-500 text-white font-medium rounded-lg hover:bg-gray-600 transition-colors cursor-pointer border-none"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </main>
  );
}
