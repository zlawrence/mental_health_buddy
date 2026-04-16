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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Profile')),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
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
                ],
              ),
      ),
    );
  }
}
