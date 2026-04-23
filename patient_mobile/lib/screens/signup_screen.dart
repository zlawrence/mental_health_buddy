import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../services/api_service.dart';

class SignupScreen extends StatefulWidget {
  const SignupScreen({super.key});

  @override
  State<SignupScreen> createState() => _SignupScreenState();
}

class _SignupScreenState extends State<SignupScreen> {
  final _usernameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  final _api = ApiService();

  bool _checkingAvailability = false;
  bool _availabilityConfirmed = false;
  bool _registering = false;
  bool _subscriptionOpened = false;

  String? _usernameError;
  String? _emailError;
  String? _passwordError;
  String? _generalError;

  @override
  void dispose() {
    _usernameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  String? _validatePassword(String password) {
    if (password.length < 12 || password.length > 16) {
      return 'Must be 12–16 characters';
    }
    if (!password.contains(RegExp(r'[A-Z]'))) {
      return 'Must contain at least 1 uppercase letter';
    }
    if (!password.contains(RegExp(r'[a-z]'))) {
      return 'Must contain at least 1 lowercase letter';
    }
    final specialCount = password.replaceAll(RegExp(r'[a-zA-Z0-9]'), '').length;
    if (specialCount < 2) {
      return 'Must contain at least 2 special characters';
    }
    return null;
  }

  bool get _formComplete {
    return _usernameController.text.trim().isNotEmpty &&
        _emailController.text.trim().isNotEmpty &&
        _passwordController.text.isNotEmpty &&
        _confirmPasswordController.text.isNotEmpty;
  }

  void _onFieldChanged() {
    if (_availabilityConfirmed) {
      setState(() => _availabilityConfirmed = false);
    }
  }

  Future<void> _checkAvailability() async {
    final username = _usernameController.text.trim();
    final email = _emailController.text.trim();
    final password = _passwordController.text;
    final confirm = _confirmPasswordController.text;

    setState(() {
      _usernameError = null;
      _emailError = null;
      _passwordError = null;
      _generalError = null;
    });

    if (username.isEmpty || email.isEmpty || password.isEmpty) {
      setState(() => _generalError = 'Username, email and password are required');
      return;
    }

    final pwError = _validatePassword(password);
    if (pwError != null) {
      setState(() => _passwordError = pwError);
      return;
    }

    if (password != confirm) {
      setState(() => _passwordError = 'Passwords do not match');
      return;
    }

    setState(() => _checkingAvailability = true);

    try {
      final result = await _api.checkAvailability(username: username, email: email);
      final usernameAvailable = result['usernameAvailable'] as bool;
      final emailAvailable = result['emailAvailable'] as bool;

      setState(() {
        _usernameError = usernameAvailable ? null : 'Username already taken';
        _emailError = emailAvailable ? null : 'Email already registered';
        _availabilityConfirmed = usernameAvailable && emailAvailable;
      });
    } catch (e) {
      setState(() => _generalError = 'Unable to check availability. Please try again.');
    } finally {
      setState(() => _checkingAvailability = false);
    }
  }

  Future<void> _createSubscription() async {
    setState(() {
      _registering = true;
      _generalError = null;
    });

    try {
      final username = _usernameController.text.trim();
      final email = _emailController.text.trim();
      final phone = _phoneController.text.trim();
      final password = _passwordController.text;

      await _api.register(username, email, phone, password);

      final loginResult = await _api.login(email, password);
      _api.authToken = loginResult['token'] as String?;

      final checkoutUrl = await _api.createCheckoutSession();
      await launchUrl(Uri.parse(checkoutUrl), mode: LaunchMode.externalApplication);

      setState(() => _subscriptionOpened = true);
    } catch (e) {
      setState(() => _generalError = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      setState(() => _registering = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Create Account')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _buildTextField(
              controller: _usernameController,
              label: 'Username',
              errorText: _usernameError,
              onChanged: (_) => _onFieldChanged(),
            ),
            const SizedBox(height: 16),
            _buildTextField(
              controller: _emailController,
              label: 'Email',
              errorText: _emailError,
              keyboardType: TextInputType.emailAddress,
              onChanged: (_) => _onFieldChanged(),
            ),
            const SizedBox(height: 16),
            _buildTextField(
              controller: _phoneController,
              label: 'Phone number (optional)',
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: 16),
            _buildPasswordField(),
            const SizedBox(height: 16),
            _buildTextField(
              controller: _confirmPasswordController,
              label: 'Confirm password',
              obscureText: true,
              onChanged: (_) => _onFieldChanged(),
            ),
            const SizedBox(height: 8),
            _buildPasswordRequirements(),
            const SizedBox(height: 24),
            if (_generalError != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: Text(
                  _generalError!,
                  style: const TextStyle(color: Colors.red),
                  textAlign: TextAlign.center,
                ),
              ),
            if (!_availabilityConfirmed) ...[
              SizedBox(
                height: 48,
                child: ElevatedButton(
                  onPressed: (_checkingAvailability || !_formComplete) ? null : _checkAvailability,
                  child: _checkingAvailability
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Check Availability'),
                ),
              ),
            ] else ...[
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.green.shade50,
                  border: Border.all(color: Colors.green.shade300),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Row(
                  children: [
                    Icon(Icons.check_circle, color: Colors.green),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Username and email are available. Complete your setup by subscribing below.',
                        style: TextStyle(color: Colors.green),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              if (!_subscriptionOpened) ...[
                SizedBox(
                  height: 48,
                  child: ElevatedButton.icon(
                    onPressed: _registering ? null : _createSubscription,
                    icon: _registering
                        ? const SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                          )
                        : const Icon(Icons.payment),
                    label: const Text('Create Subscription — \$5/month'),
                  ),
                ),
              ] else ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.blue.shade50,
                    border: Border.all(color: Colors.blue.shade300),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: const Text(
                    'Stripe payment page opened. Complete your payment there, then return here and log in.',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: Colors.blue),
                  ),
                ),
                const SizedBox(height: 16),
                SizedBox(
                  height: 48,
                  child: OutlinedButton(
                    onPressed: () => Navigator.pushReplacementNamed(context, '/'),
                    child: const Text('I\'ve paid — go to Login'),
                  ),
                ),
              ],
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    String? errorText,
    TextInputType? keyboardType,
    bool obscureText = false,
    ValueChanged<String>? onChanged,
  }) {
    return TextField(
      controller: controller,
      decoration: InputDecoration(
        labelText: label,
        labelStyle: const TextStyle(fontWeight: FontWeight.bold),
        errorText: errorText,
      ),
      keyboardType: keyboardType,
      obscureText: obscureText,
      textInputAction: TextInputAction.next,
      onChanged: onChanged,
    );
  }

  Widget _buildPasswordField() {
    return TextField(
      controller: _passwordController,
      decoration: InputDecoration(
        labelText: 'Password',
        labelStyle: const TextStyle(fontWeight: FontWeight.bold),
        errorText: _passwordError,
      ),
      obscureText: true,
      textInputAction: TextInputAction.next,
      onChanged: (_) {
        _onFieldChanged();
        setState(() => _passwordError = null);
      },
    );
  }

  Widget _buildPasswordRequirements() {
    final p = _passwordController.text;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Password requirements:', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
        const SizedBox(height: 4),
        _req('12–16 characters', p.length >= 12 && p.length <= 16),
        _req('1 uppercase letter', p.contains(RegExp(r'[A-Z]'))),
        _req('1 lowercase letter', p.contains(RegExp(r'[a-z]'))),
        _req('2 special characters', p.replaceAll(RegExp(r'[a-zA-Z0-9]'), '').length >= 2),
      ],
    );
  }

  Widget _req(String label, bool met) {
    return Row(
      children: [
        Icon(met ? Icons.check : Icons.close, size: 14, color: met ? Colors.green : Colors.grey),
        const SizedBox(width: 4),
        Text(label, style: TextStyle(fontSize: 12, color: met ? Colors.green : Colors.grey)),
      ],
    );
  }
}
