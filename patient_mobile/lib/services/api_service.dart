import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  static const baseUrl = 'http://10.0.2.2:5000';
  String? authToken;

  Map<String, String> _headers() {
    return {
      'Content-Type': 'application/json',
      if (authToken != null) 'Authorization': 'Bearer $authToken',
    };
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

  Future<List<dynamic>> getGuardRails() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/patients/me/guard-rails'),
      headers: _headers(),
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to load guard rails');
    }

    return jsonDecode(response.body) as List<dynamic>;
  }

  Future<void> createGuardRail(String keyword, String action, String? replacement) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/patients/me/guard-rails'),
      headers: _headers(),
      body: jsonEncode({
        'keyword': keyword,
        'action': action,
        'replacement': replacement,
      }),
    );

    if (response.statusCode != 201) {
      throw Exception('Unable to create guard rail');
    }
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
}
