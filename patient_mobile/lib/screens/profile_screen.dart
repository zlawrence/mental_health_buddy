import 'package:flutter/material.dart';
import '../services/api_service.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  late ApiService api;
  final _phoneController = TextEditingController();
  final _retentionController = TextEditingController();
  bool _isLoading = false;
  String? _error;
  String? _email;
  List<dynamic> _therapists = [];

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    api = ModalRoute.of(context)!.settings.arguments as ApiService;
    _loadProfile();
  }

  Future<void> _loadProfile() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final profile = await api.getProfile();
      setState(() {
        _email = profile['email'];
        _therapists = profile['therapistIds'] ?? [];
      });
      _phoneController.text = profile['phoneNumber'] ?? '';
      _retentionController.text = (profile['conversationRetentionDays'] ?? -1).toString();
    } catch (e) {
      setState(() {
        _error = e.toString();
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  Future<void> _saveProfile() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      await api.updateProfile(
        _phoneController.text.trim(),
        int.tryParse(_retentionController.text.trim()) ?? -1,
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Profile updated')),
        );
      }
    } catch (e) {
      setState(() {
        _error = e.toString();
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  Future<void> _inviteTherapist() async {
    final emailController = TextEditingController();
    final hostContext = context;
    final navigator = Navigator.of(hostContext);
    final messenger = ScaffoldMessenger.of(hostContext);

    await showDialog(
      context: hostContext,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Invite Therapist'),
        content: TextField(
          controller: emailController,
          decoration: const InputDecoration(labelText: 'Therapist email'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () async {
              final email = emailController.text.trim();
              if (email.isEmpty) return;
              try {
                await api.inviteTherapist(email);
                if (!mounted) return;
                navigator.pop();
                messenger.showSnackBar(
                  const SnackBar(content: Text('Invitation sent')),
                );
                await _loadProfile();
              } catch (e) {
                if (!mounted) return;
                navigator.pop();
                messenger.showSnackBar(
                  SnackBar(content: Text(e.toString())),
                );
              }
            },
            child: const Text('Send'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Image.asset('assets/anxietybuddy.png', width: 32, height: 32),
            const SizedBox(width: 12),
            const Text('Profile'),
          ],
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : SingleChildScrollView(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text('Email: ${_email ?? '-'}'),
                    const SizedBox(height: 12),
                    if (_therapists.isNotEmpty)
                      const Text('Therapist: Connected', style: TextStyle(color: Color(0xff2b1392), fontWeight: FontWeight.bold))
                    else
                      const Text('Therapist: Not connected', style: TextStyle(color: Colors.grey)),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _phoneController,
                      decoration: const InputDecoration(labelText: 'Phone number'),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _retentionController,
                      decoration: const InputDecoration(labelText: 'Conversation retention days (-1 = forever)'),
                      keyboardType: TextInputType.number,
                    ),
                    const SizedBox(height: 24),
                    if (_error != null)
                      Text(_error!, style: const TextStyle(color: Colors.red)),
                    ElevatedButton(onPressed: _saveProfile, child: const Text('Save')),
                    const SizedBox(height: 12),
                    ElevatedButton(
                      onPressed: _therapists.isEmpty ? _inviteTherapist : null,
                      child: const Text('Invite Therapist'),
                    ),
                  ],
                ),
              ),
      ),
    );
  }
}
