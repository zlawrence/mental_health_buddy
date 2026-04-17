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
  List<dynamic> _conversations = [];
  bool _todayConversationOpened = false;

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
      final conversations = await api.getConversations();

      setState(() {
        _profile = profile;
        _conversations = conversations;
      });

      if (!_todayConversationOpened) {
        final todayConversation = _findTodaysConversation(conversations);
        if (todayConversation != null) {
          _todayConversationOpened = true;
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (!mounted) return;
            _openConversation(todayConversation);
          });
        }
      }
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

  List<Map<String, dynamic>> get _recentConversations {
    final sorted = _conversations
        .cast<Map<String, dynamic>>()
        .toList();

    sorted.sort((a, b) {
      final aDate = DateTime.tryParse(a['createdAt'] ?? '') ?? DateTime.fromMillisecondsSinceEpoch(0);
      final bDate = DateTime.tryParse(b['createdAt'] ?? '') ?? DateTime.fromMillisecondsSinceEpoch(0);
      return bDate.compareTo(aDate);
    });

    return sorted.take(3).toList();
  }

  Map<String, dynamic>? _findTodaysConversation(List<dynamic> conversations) {
    final now = DateTime.now().toLocal();
    final todayConversations = conversations.cast<Map<String, dynamic>>().where((convo) {
      final createdAt = DateTime.tryParse(convo['createdAt'] ?? '');
      if (createdAt == null) return false;
      final local = createdAt.toLocal();
      return local.year == now.year && local.month == now.month && local.day == now.day;
    }).toList();

    if (todayConversations.isEmpty) return null;

    todayConversations.sort((a, b) {
      final aDate = DateTime.tryParse(a['createdAt'] ?? '') ?? DateTime.fromMillisecondsSinceEpoch(0);
      final bDate = DateTime.tryParse(b['createdAt'] ?? '') ?? DateTime.fromMillisecondsSinceEpoch(0);
      return bDate.compareTo(aDate);
    });

    return todayConversations.first;
  }

  String _generateChatTitle() {
    final now = DateTime.now().toLocal();
    const weekdayNames = [
      'Monday',
      'Tuesday',
      'Wednesday',
      'Thursday',
      'Friday',
      'Saturday',
      'Sunday',
    ];
    const monthNames = [
      'January',
      'February',
      'March',
      'April',
      'May',
      'June',
      'July',
      'August',
      'September',
      'October',
      'November',
      'December',
    ];
    final weekday = weekdayNames[now.weekday - 1];
    final month = monthNames[now.month - 1];
    final hour = now.hour == 0 ? 12 : (now.hour > 12 ? now.hour - 12 : now.hour);
    final minute = now.minute.toString().padLeft(2, '0');
    final ampm = now.hour >= 12 ? 'PM' : 'AM';
    return '$weekday $month ${now.day}, ${now.year} $hour:$minute $ampm';
  }

  void _openConversation(Map<String, dynamic> convo) {
    Navigator.pushNamed(
      context,
      '/conversation',
      arguments: {
        'api': api,
        'conversation': convo,
      },
    );
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

  Future<void> _createConversation() async {
    final title = _generateChatTitle();
    try {
      final conversation = await api.createConversation(title);
      if (!mounted) return;
      setState(() {
        _conversations.insert(0, conversation);
      });
      _openConversation(conversation);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e.toString())),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Image.asset('assets/anxietybuddy.png', width: 32, height: 32),
            const SizedBox(width: 12),
            const Text('Patient Dashboard'),
          ],
        ),
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
                      const SizedBox(height: 20),
                      const Text('Recent Conversations', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                      const SizedBox(height: 8),
                      _recentConversations.isEmpty
                          ? const Text('No recent conversations yet.')
                          : Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: _recentConversations.map((convo) {
                                final createdAt = DateTime.tryParse(convo['createdAt'] ?? '')?.toLocal();
                                return Padding(
                                  padding: const EdgeInsets.only(bottom: 8.0),
                                  child: Text(
                                    '${convo['title'] ?? 'Untitled'} • ${createdAt != null ? '${createdAt.month}/${createdAt.day}/${createdAt.year}' : 'Unknown date'}',
                                  ),
                                );
                              }).toList(),
                            ),
                      const SizedBox(height: 16),
                      const Text('Conversations', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                      const SizedBox(height: 8),
                      Expanded(
                        child: _conversations.isEmpty
                            ? const Center(child: Text('No conversations yet. Start a new one to begin.'))
                            : ListView.builder(
                                itemCount: _conversations.length,
                                itemBuilder: (context, index) {
                                  final convo = _conversations[index] as Map<String, dynamic>;
                                  final createdAt = DateTime.tryParse(convo['createdAt'] ?? '')?.toLocal();
                                  return Card(
                                    child: ListTile(
                                      title: Text(convo['title'] ?? 'Untitled'),
                                      subtitle: Text(
                                        'Messages: ${convo['messageCount'] ?? 0} • Started: ${createdAt != null ? '${createdAt.month}/${createdAt.day}/${createdAt.year}' : 'Unknown'}',
                                      ),
                                      onTap: () {
                                        Navigator.pushNamed(
                                          context,
                                          '/conversation',
                                          arguments: {
                                            'api': api,
                                            'conversation': convo,
                                          },
                                        );
                                      },
                                    ),
                                  );
                                },
                              ),
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          Expanded(
                            child: ElevatedButton(
                              onPressed: _createConversation,
                              child: const Text('New Chat'),
                            ),
                          ),
                          const SizedBox(width: 12),
                          ElevatedButton(onPressed: _inviteTherapist, child: const Text('Invite Therapist')),
                        ],
                      ),
                    ],
                  ),
      ),
    );
  }
}
