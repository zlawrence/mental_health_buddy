import 'package:flutter/material.dart';
import '../services/api_service.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _api = ApiService();
  bool _isLoading = false;
  String? _error;

  Future<void> _login() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final result = await _api.login(
        _emailController.text.trim(),
        _passwordController.text.trim(),
      );
      _api.authToken = result['token'] as String?;
      if (!mounted) return;

      final requiresSubscription = result['requiresSubscription'] as bool? ?? false;
      if (requiresSubscription) {
        Navigator.pushReplacementNamed(context, '/subscription-required', arguments: _api);
      } else {
        Navigator.pushReplacementNamed(context, '/dashboard', arguments: _api);
      }
    } catch (e) {
      setState(() {
        _error = e.toString().replaceFirst('Exception: ', '');
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
      backgroundColor: const Color(0xfffcf7e4),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Image.asset('assets/anxietybuddy_splash.png', width: 350, height: 350),
            const SizedBox(height: 24),
            const SizedBox(height: 24),
            TextField(
              controller: _emailController,
              decoration: const InputDecoration(labelText: 'Email or username', 
                fillColor:  Color(0xff2b1392),
                labelStyle: TextStyle(
                  fontWeight: FontWeight.bold, // Makes the label bold
                )
                ),
            
            ),
            const SizedBox(height: 12, width:600),
            TextField(
              controller: _passwordController,
              decoration: const InputDecoration(labelText: 'Password', 
                fillColor:  Color(0xff2b1392),
            
                labelStyle: TextStyle(
                  fontWeight: FontWeight.bold, // Makes the label bold
                )
                ),
              obscureText: true,
            ),
            const SizedBox(height: 24),
            if (_error != null)
              Text(_error!, style: const TextStyle(color: Colors.red)),
            Transform.translate(
              offset: const Offset(0, -10),
              child: SizedBox(
                width: 300,
                child: ElevatedButton(
                  onPressed: _isLoading ? null : _login,
                  child: _isLoading
                      ? const CircularProgressIndicator()
                      : const Text('Login'),
                ),
              ),
            ),
            TextButton(
              onPressed: () => Navigator.pushNamed(context, '/signup'),
              child: const Text("Don't have an account? Sign up"),
            ),
          ],
        ),
      ),
    );
  }
}
