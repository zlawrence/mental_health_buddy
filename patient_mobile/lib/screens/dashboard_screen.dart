import 'package:flutter/material.dart';
import '../services/api_service.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  late ApiService api;
  bool _loading = false;
  String? _error;
  Map<String, dynamic>? _profile;
  List<dynamic> _guardRails = [];

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    api = ModalRoute.of(context)!.settings.arguments as ApiService;
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final profile = await api.getProfile();
      final guardRails = await api.getGuardRails();

      setState(() {
        _profile = profile;
        _guardRails = guardRails;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
      });
    } finally {
      setState(() {
        _loading = false;
      });
    }
  }

  void _openProfile() {
    Navigator.pushNamed(context, '/profile', arguments: api);
  }

  Future<void> _logout() async {
    Navigator.pushReplacementNamed(context, '/');
  }

  Future<void> _refresh() async {
    await _loadData();
  }

  Future<void> _inviteTherapist() async {
    final emailController = TextEditingController();

    await showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Invite Therapist'),
        content: TextField(
          controller: emailController,
          decoration: const InputDecoration(labelText: 'Therapist email'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () async {
              final email = emailController.text.trim();
              if (email.isEmpty) return;
              try {
                await api.inviteTherapist(email);
                if (mounted) {
                  Navigator.pop(context);
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Invitation sent')),
                  );
                }
              } catch (e) {
                if (mounted) {
                  Navigator.pop(context);
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text(e.toString())),
                  );
                }
              }
            },
            child: const Text('Send'),
          ),
        ],
      ),
    );
  }

  Future<void> _createGuardRail() async {
    final keywordController = TextEditingController();
    final replacementController = TextEditingController();
    String selectedAction = 'remove';

    await showDialog(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setState) => AlertDialog(
          title: const Text('Create Guard Rail'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: keywordController,
                decoration: const InputDecoration(labelText: 'Keyword'),
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                value: selectedAction,
                decoration: const InputDecoration(labelText: 'Action'),
                items: const [
                  DropdownMenuItem(value: 'remove', child: Text('Remove')),
                  DropdownMenuItem(value: 'replace', child: Text('Replace')),
                ],
                onChanged: (value) {
                  setState(() {
                    selectedAction = value!;
                  });
                },
              ),
              if (selectedAction == 'replace') ...[
                const SizedBox(height: 16),
                TextField(
                  controller: replacementController,
                  decoration: const InputDecoration(labelText: 'Replacement text'),
                ),
              ],
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancel'),
            ),
            ElevatedButton(
              onPressed: () async {
                final keyword = keywordController.text.trim();
                final replacement = selectedAction == 'replace' ? replacementController.text.trim() : null;

                if (keyword.isEmpty) return;
                if (selectedAction == 'replace' && (replacement == null || replacement.isEmpty)) return;

                try {
                  await api.createGuardRail(keyword, selectedAction, replacement);
                  if (mounted) {
                    Navigator.pop(context);
                    await _refresh();
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Guard rail created')),
                    );
                  }
                } catch (e) {
                  if (mounted) {
                    Navigator.pop(context);
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text(e.toString())),
                    );
                  }
                }
              },
              child: const Text('Create'),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Patient Dashboard'),
        actions: [
          IconButton(onPressed: _refresh, icon: const Icon(Icons.refresh)),
          IconButton(onPressed: _openProfile, icon: const Icon(Icons.person)),
          IconButton(onPressed: _logout, icon: const Icon(Icons.logout)),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? Center(child: Text(_error!, style: const TextStyle(color: Colors.red)))
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Welcome, ${_profile?['username'] ?? 'Patient'}', style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                      const SizedBox(height: 16),
                      Text('Email: ${_profile?['email'] ?? '-'}'),
                      Text('Phone: ${_profile?['phoneNumber'] ?? '-'}'),
                      const SizedBox(height: 16),
                      const Text('Guard Rails', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                      const SizedBox(height: 8),
                      Expanded(
                        child: _guardRails.isEmpty
                            ? const Text('No guard rails configured')
                            : ListView.builder(
                                itemCount: _guardRails.length,
                                itemBuilder: (context, index) {
                                  final rail = _guardRails[index] as Map<String, dynamic>;
                                  return Card(
                                    child: ListTile(
                                      title: Text(rail['keyword'] ?? ''),
                                      subtitle: Text('Action: ${rail['action']}'),
                                    ),
                                  );
                                },
                              ),
                      ),
                      ElevatedButton(onPressed: _createGuardRail, child: const Text('Create Guard Rail')),
                      const SizedBox(height: 8),
                      ElevatedButton(onPressed: _inviteTherapist, child: const Text('Invite Therapist')),
                    ],
                  ),
      ),
    );
  }
}
