import 'package:flutter/material.dart';
import '../services/api_service.dart';

class ConversationDetailScreen extends StatefulWidget {
  const ConversationDetailScreen({super.key});

  @override
  State<ConversationDetailScreen> createState() => _ConversationDetailScreenState();
}

class _ConversationDetailScreenState extends State<ConversationDetailScreen> {
  late ApiService api;
  late Map<String, dynamic> conversation;
  bool _loading = false;
  String? _error;
  List<dynamic> _messages = [];
  final _messageController = TextEditingController();

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final args = ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>;
    api = args['api'] as ApiService;
    conversation = args['conversation'] as Map<String, dynamic>;
    _loadMessages();
  }

  Future<void> _loadMessages() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final messages = await api.getConversationMessages(conversation['id'] as String);
      setState(() {
        _messages = messages;
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

  Future<void> _sendMessage() async {
    final text = _messageController.text.trim();
    if (text.isEmpty) return;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      await api.sendMessage(conversation['id'] as String, text);
      _messageController.clear();
      await _loadMessages();
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Image.asset('assets/anxietybuddy.png', width: 32, height: 32),
            const SizedBox(width: 12),
            const Text('Chat'),
          ],
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? Center(child: Text(_error!, style: const TextStyle(color: Colors.red)))
                : Column(
                    children: [
                      Expanded(
                        child: _messages.isEmpty
                            ? const Center(child: Text('No messages yet. Send the first message to begin.'))
                            : ListView.builder(
                                itemCount: _messages.length,
                                itemBuilder: (context, index) {
                                  final message = _messages[index] as Map<String, dynamic>;
                                  final role = message['role'] ?? 'user';
                                  final content = message['content'] ?? '';
                                  final timestamp = DateTime.tryParse(message['timestamp'] ?? '')?.toLocal();
                                  return Align(
                                    alignment: role == 'assistant' ? Alignment.centerLeft : Alignment.centerRight,
                                    child: Container(
                                      margin: const EdgeInsets.symmetric(vertical: 6.0),
                                      padding: const EdgeInsets.all(12.0),
                                      decoration: BoxDecoration(
                                        color: role == 'assistant' ? Colors.grey.shade200 : Colors.blue.shade100,
                                        borderRadius: BorderRadius.circular(12.0),
                                      ),
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Text(content),
                                          if (timestamp != null) ...[
                                            const SizedBox(height: 6),
                                            Text(
                                              '${timestamp.month}/${timestamp.day}/${timestamp.year} ${timestamp.hour.toString().padLeft(2, '0')}:${timestamp.minute.toString().padLeft(2, '0')}',
                                              style: const TextStyle(fontSize: 12, color: Colors.black54),
                                            ),
                                          ],
                                        ],
                                      ),
                                    ),
                                  );
                                },
                              ),
                      ),
                      if (_error != null)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 12.0),
                          child: Text(_error!, style: const TextStyle(color: Colors.red)),
                        ),
                      Row(
                        children: [
                          Expanded(
                            child: TextField(
                              controller: _messageController,
                              decoration: const InputDecoration(labelText: 'Type your message'),
                            ),
                          ),
                          const SizedBox(width: 12),
                          ElevatedButton(onPressed: _sendMessage, child: const Text('Send')),
                        ],
                      ),
                    ],
                  ),
      ),
    );
  }
}
