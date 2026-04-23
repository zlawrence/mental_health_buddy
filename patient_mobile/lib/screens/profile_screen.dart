import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
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
  bool _subscriptionLoading = false;
  String? _error;
  String? _email;
  List<dynamic> _therapists = [];

  String _subscriptionStatus = 'Inactive';
  bool _subscriptionActive = false;
  DateTime? _renewalDate;
  DateTime? _canceledAt;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    api = ModalRoute.of(context)!.settings.arguments as ApiService;
    _loadProfile();
    _loadSubscriptionStatus();
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

  Future<void> _loadSubscriptionStatus() async {
    try {
      final status = await api.getSubscriptionStatus();
      setState(() {
        _subscriptionStatus = status['status'] as String? ?? 'Inactive';
        _subscriptionActive = status['isActive'] as bool? ?? false;
        final renewalStr = status['renewalDate'] as String?;
        _renewalDate = renewalStr != null ? DateTime.tryParse(renewalStr)?.toLocal() : null;
        final canceledStr = status['canceledAt'] as String?;
        _canceledAt = canceledStr != null ? DateTime.tryParse(canceledStr)?.toLocal() : null;
      });
    } catch (_) {
      // Non-critical — profile still usable without subscription info
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

  Future<void> _subscribe() async {
    setState(() => _subscriptionLoading = true);
    try {
      final checkoutUrl = await api.createCheckoutSession();
      final uri = Uri.parse(checkoutUrl);
      if (await canLaunchUrl(uri)) {
        await launchUrl(uri, mode: LaunchMode.externalApplication);
      } else {
        throw Exception('Could not open payment page');
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceFirst('Exception: ', ''))),
        );
      }
    } finally {
      if (mounted) setState(() => _subscriptionLoading = false);
    }
  }

  Future<void> _unsubscribe() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cancel subscription?'),
        content: const Text(
          'Your subscription will remain active until the end of the current billing period.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Keep'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Cancel subscription'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    setState(() => _subscriptionLoading = true);
    try {
      await api.cancelSubscription();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
              content: Text('Subscription will be canceled at the end of the billing period')),
        );
        await _loadSubscriptionStatus();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceFirst('Exception: ', ''))),
        );
      }
    } finally {
      if (mounted) setState(() => _subscriptionLoading = false);
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
                messenger.showSnackBar(SnackBar(content: Text(e.toString())));
              }
            },
            child: const Text('Send'),
          ),
        ],
      ),
    );
  }

  String _formatDate(DateTime dt) => '${dt.month}/${dt.day}/${dt.year}';

  Widget _buildSubscriptionSection() {
    if (_subscriptionLoading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 12),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    final canSubscribe = _subscriptionStatus == 'Inactive' ||
        _subscriptionStatus == 'Pending' ||
        _subscriptionStatus == 'PastDue';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Divider(height: 32),
        const Text(
          'Subscription',
          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
        ),
        const SizedBox(height: 8),
        if (_subscriptionActive) ...[
          const Row(
            children: [
              Icon(Icons.check_circle, color: Colors.green, size: 18),
              SizedBox(width: 6),
              Text('Active',
                  style: TextStyle(color: Colors.green, fontWeight: FontWeight.bold)),
              Text(' — \$5/month'),
            ],
          ),
          if (_renewalDate != null)
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Text('Renews ${_formatDate(_renewalDate!)}',
                  style: const TextStyle(fontSize: 13)),
            ),
          const SizedBox(height: 12),
          OutlinedButton(
            onPressed: _unsubscribe,
            style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Cancel subscription'),
          ),
        ] else if (_subscriptionStatus == 'Canceled') ...[
          const Row(
            children: [
              Icon(Icons.cancel_outlined, color: Colors.orange, size: 18),
              SizedBox(width: 6),
              Text('Canceled',
                  style: TextStyle(color: Colors.orange, fontWeight: FontWeight.bold)),
            ],
          ),
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: Text(
              _canceledAt != null
                  ? 'Canceled on ${_formatDate(_canceledAt!)} — access continues until billing period end'
                  : 'Access continues until the current billing period end',
              style: const TextStyle(fontSize: 13),
            ),
          ),
          const SizedBox(height: 12),
          ElevatedButton(
            onPressed: _subscribe,
            child: const Text('Resubscribe — \$5/month'),
          ),
        ] else if (canSubscribe) ...[
          const Text('No active subscription'),
          const SizedBox(height: 12),
          ElevatedButton(
            onPressed: _subscribe,
            child: const Text('Subscribe — \$5/month'),
          ),
        ],
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Image.asset('assets/ab_face_logo.gif', width: 32, height: 32),
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
                      const Text('Therapist: Connected',
                          style: TextStyle(
                              color: Color(0xff2b1392), fontWeight: FontWeight.bold))
                    else
                      const Text('Therapist: Not connected',
                          style: TextStyle(color: Colors.grey)),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _phoneController,
                      decoration: const InputDecoration(labelText: 'Phone number'),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _retentionController,
                      decoration: const InputDecoration(
                          labelText: 'Conversation retention days (-1 = forever)'),
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
                    _buildSubscriptionSection(),
                    const SizedBox(height: 24),
                  ],
                ),
              ),
      ),
    );
  }
}
