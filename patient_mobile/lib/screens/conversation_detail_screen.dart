import 'package:flutter/material.dart';
import '../services/api_service.dart';

class _BouncingDots extends StatefulWidget {
  const _BouncingDots();

  @override
  State<_BouncingDots> createState() => _BouncingDotsState();
}

class _BouncingDotsState extends State<_BouncingDots> with TickerProviderStateMixin {
  late final List<AnimationController> _controllers;
  late final List<Animation<double>> _animations;

  @override
  void initState() {
    super.initState();
    _controllers = List.generate(3, (i) => AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 500),
    ));
    _animations = _controllers.map((c) => Tween<double>(begin: 0, end: -8).animate(
      CurvedAnimation(parent: c, curve: Curves.easeInOut),
    )).toList();

    for (var i = 0; i < _controllers.length; i++) {
      Future.delayed(Duration(milliseconds: i * 150), () {
        if (mounted) {
          _controllers[i].repeat(reverse: true);
        }
      });
    }
  }

  @override
  void dispose() {
    for (final c in _controllers) {
      c.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: List.generate(3, (i) => AnimatedBuilder(
        animation: _animations[i],
        builder: (_, __) => Transform.translate(
          offset: Offset(0, _animations[i].value),
          child: Container(
            margin: const EdgeInsets.symmetric(horizontal: 3),
            width: 8,
            height: 8,
            decoration: const BoxDecoration(
              color: Color(0xfffcf7e4),
              shape: BoxShape.circle,
            ),
          ),
        ),
      )),
    );
  }
}

class ConversationDetailScreen extends StatefulWidget {
  const ConversationDetailScreen({super.key});

  @override
  State<ConversationDetailScreen> createState() => _ConversationDetailScreenState();
}

class _ConversationDetailScreenState extends State<ConversationDetailScreen> {
  late ApiService api;
  late Map<String, dynamic> conversation;
  bool _loading = false;
  bool _sending = false;
  String? _error;
  List<dynamic> _messages = [];
  final _messageController = TextEditingController();
  final _scrollController = ScrollController();

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final args = ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>;
    api = args['api'] as ApiService;
    conversation = args['conversation'] as Map<String, dynamic>;
    _loadMessages();
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
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
      _scrollToBottom();
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
    if (text.isEmpty || _sending) return;

    _messageController.clear();

    setState(() {
      _sending = true;
      _error = null;
    });
    _scrollToBottom();

    try {
      await api.sendMessage(conversation['id'] as String, text);
      final messages = await api.getConversationMessages(conversation['id'] as String);
      setState(() {
        _messages = messages;
      });
      _scrollToBottom();
    } catch (e) {
      setState(() {
        _error = e.toString();
      });
    } finally {
      setState(() {
        _sending = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: const Color(0xff6f5acd),
        title: Row(
          children: [
            Image.asset('assets/ab_face_logo.gif', width: 32, height: 32),
            const SizedBox(width: 12),
            const Text('Chat'),
          ],
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null && _messages.isEmpty
                ? Center(child: Text(_error!, style: const TextStyle(color: Colors.red)))
                : Column(
                    children: [
                      Expanded(
                        child: ListView.builder(
                          controller: _scrollController,
                          itemCount: _messages.isEmpty && !_sending
                              ? 1
                              : _messages.length + (_sending ? 1 : 0),
                          itemBuilder: (context, index) {
                            if (_messages.isEmpty && !_sending) {
                              return const Center(
                                child: Padding(
                                  padding: EdgeInsets.all(32.0),
                                  child: Text('No messages yet. Send the first message to begin.'),
                                ),
                              );
                            }

                            if (_sending && index == _messages.length) {
                              return Align(
                                alignment: Alignment.centerLeft,
                                child: Container(
                                  margin: const EdgeInsets.symmetric(vertical: 6.0),
                                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                                  decoration: BoxDecoration(
                                    color: const Color(0xff4a3d8c),
                                    borderRadius: BorderRadius.circular(12.0),
                                  ),
                                  child: const _BouncingDots(),
                                ),
                              );
                            }

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
                                  color: role == 'assistant'
                                      ? const Color(0xff4a3d8c)
                                      : const Color(0xff9b8ed6),
                                  borderRadius: BorderRadius.circular(12.0),
                                ),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(content, style: const TextStyle(color: Color(0xfffcf7e4))),
                                    if (timestamp != null) ...[
                                      const SizedBox(height: 6),
                                      Text(
                                        '${timestamp.month}/${timestamp.day}/${timestamp.year} ${timestamp.hour.toString().padLeft(2, '0')}:${timestamp.minute.toString().padLeft(2, '0')}',
                                        style: const TextStyle(fontSize: 12, color: Color(0xfffcf7e4)),
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
                              onSubmitted: (_) => _sendMessage(),
                            ),
                          ),
                          const SizedBox(width: 12),
                          ElevatedButton(
                            onPressed: _sending ? null : _sendMessage,
                            child: const Text('Send'),
                          ),
                        ],
                      ),
                    ],
                  ),
      ),
    );
  }
}
