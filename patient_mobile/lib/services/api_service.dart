import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  static const baseUrl = 'https://localhost:7046';
  String? authToken;

  Map<String, String> _headers() {
    return {
      'Content-Type': 'application/json',
      if (authToken != null) 'Authorization': 'Bearer $authToken',
    };
  }

  Future<Map<String, dynamic>> checkAvailability({String? username, String? email}) async {
    final params = <String, String>{};
    if (username != null && username.isNotEmpty) params['username'] = username;
    if (email != null && email.isNotEmpty) params['email'] = email;

    final uri = Uri.parse('$baseUrl/api/auth/check-availability').replace(queryParameters: params);
    final response = await http.get(uri, headers: _headers());

    if (response.statusCode != 200) {
      throw Exception('Unable to check availability');
    }

    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  Future<void> register(String username, String email, String phoneNumber, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/register-patient'),
      headers: _headers(),
      body: jsonEncode({
        'username': username,
        'email': email,
        'phoneNumber': phoneNumber.isEmpty ? null : phoneNumber,
        'password': password,
        'confirmPassword': password,
      }),
    );

    if (response.statusCode != 200) {
      final body = jsonDecode(response.body) as Map<String, dynamic>;
      throw Exception(body['message'] ?? 'Registration failed');
    }
  }

  Future<Map<String, dynamic>> login(String emailOrUsername, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/login-patient'),
      headers: _headers(),
      body: jsonEncode({
        'emailOrUsername': emailOrUsername,
        'password': password,
      }),
    );

    if (response.statusCode != 200) {
      throw Exception('Login failed: ${response.body}');
    }

    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getProfile() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/patients/me/profile'),
      headers: _headers(),
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to load profile');
    }

    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  Future<void> updateProfile(String phoneNumber, int conversationRetentionDays) async {
    final response = await http.put(
      Uri.parse('$baseUrl/api/patients/me/profile'),
      headers: _headers(),
      body: jsonEncode({
        'phoneNumber': phoneNumber,
        'conversationRetentionDays': conversationRetentionDays,
      }),
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to update profile');
    }
  }

  Future<List<dynamic>> getConversations() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/conversations'),
      headers: _headers(),
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to load conversations');
    }

    return jsonDecode(response.body) as List<dynamic>;
  }

  Future<Map<String, dynamic>> createConversation(String title) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/conversations'),
      headers: _headers(),
      body: jsonEncode({
        'title': title,
      }),
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to create conversation');
    }

    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  Future<List<dynamic>> getConversationMessages(String conversationId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/conversations/$conversationId/messages'),
      headers: _headers(),
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to load conversation messages');
    }

    return jsonDecode(response.body) as List<dynamic>;
  }

  Future<Map<String, dynamic>> sendMessage(String conversationId, String message) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/conversations/send-message'),
      headers: _headers(),
      body: jsonEncode({
        'conversationId': conversationId,
        'message': message,
      }),
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to send message');
    }

    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  Future<void> inviteTherapist(String therapistEmail) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/patients/me/invite-therapist'),
      headers: _headers(),
      body: jsonEncode({'therapistEmail': therapistEmail}),
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to send invitation');
    }
  }

  Future<Map<String, dynamic>> getSubscriptionStatus() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/subscriptions/status'),
      headers: _headers(),
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to load subscription status');
    }

    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  Future<String> createCheckoutSession() async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/subscriptions/create-checkout-session'),
      headers: _headers(),
    );

    if (response.statusCode != 200) {
      final body = jsonDecode(response.body) as Map<String, dynamic>;
      throw Exception(body['message'] ?? 'Unable to create checkout session');
    }

    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return body['checkoutUrl'] as String;
  }

  Future<void> cancelSubscription() async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/subscriptions/cancel'),
      headers: _headers(),
    );

    if (response.statusCode != 200) {
      final body = jsonDecode(response.body) as Map<String, dynamic>;
      throw Exception(body['message'] ?? 'Unable to cancel subscription');
    }
  }
}
