import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../services/session_manager.dart';
import '../theme/app_theme.dart';
import 'tasks_screen.dart';

class LoginScreen extends StatefulWidget {
  final SessionManager sessionManager;
  final ApiService apiService;

  const LoginScreen({
    super.key,
    required this.sessionManager,
    required this.apiService,
  });

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController(text: 'ahmed@warehouse.local');
  final _passwordController = TextEditingController(text: 'Worker123!');
  late final TextEditingController _urlController;
  bool _isLoading = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _urlController = TextEditingController(text: widget.sessionManager.baseUrl);
  }

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _urlController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    final email = _emailController.text.trim();
    final password = _passwordController.text;
    final url = _urlController.text.trim();

    if (email.isEmpty || password.isEmpty || url.isEmpty) {
      setState(() => _errorMessage = 'Please enter email, password, and API address.');
      return;
    }

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final response = await widget.apiService.login(email, password, url);
      await widget.sessionManager.saveSession(
        token: response.token,
        workerName: response.fullName,
        workerEmail: response.email,
        baseUrl: url,
      );

      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => TasksScreen(
            sessionManager: widget.sessionManager,
            apiService: widget.apiService,
          ),
        ),
      );
    } catch (e) {
      setState(() {
        _errorMessage = e.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Container(
                      width: 16,
                      height: 16,
                      decoration: const BoxDecoration(
                        color: AppTheme.amber,
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(color: AppTheme.amber, blurRadius: 12, spreadRadius: 2),
                        ],
                      ),
                    ),
                    const SizedBox(width: 12),
                    const Text(
                      'SMART WAREHOUSE',
                      style: TextStyle(fontSize: 14, letterSpacing: 2, fontWeight: FontWeight.bold, color: AppTheme.amber),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                const Text(
                  'Worker Sign In',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: AppTheme.paper),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Large targets, fast operation. Pick-to-light warehouse.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Color(0xFF9EABA4), fontSize: 14),
                ),
                const SizedBox(height: 32),
                if (_errorMessage != null) ...[
                  Container(
                    padding: const EdgeInsets.all(12),
                    margin: const EdgeInsets.only(bottom: 16),
                    decoration: BoxDecoration(
                      color: AppTheme.rust.withValues(alpha: 0.15),
                      border: Border.all(color: AppTheme.rust),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      _errorMessage!,
                      style: const TextStyle(color: Color(0xFFFF8B7A), fontWeight: FontWeight.w500),
                    ),
                  ),
                ],
                TextField(
                  controller: _emailController,
                  decoration: const InputDecoration(labelText: 'Email Address'),
                  keyboardType: TextInputType.emailAddress,
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: _passwordController,
                  decoration: const InputDecoration(labelText: 'Password'),
                  obscureText: true,
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: _urlController,
                  decoration: const InputDecoration(
                    labelText: 'API Base Address',
                    hintText: 'http://10.0.2.2:5138/',
                  ),
                ),
                const SizedBox(height: 24),
                ElevatedButton(
                  onPressed: _isLoading ? null : _handleLogin,
                  child: _isLoading
                      ? const SizedBox(
                          height: 24,
                          width: 24,
                          child: CircularProgressIndicator(strokeWidth: 2.5, color: AppTheme.ink),
                        )
                      : const Text('Sign In'),
                ),
                const SizedBox(height: 16),
                const Text(
                  'Demo credentials:\nWorker: ahmed@warehouse.local / Worker123!\nWorker: sara@warehouse.local / Worker123!',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Color(0xFF75857E), fontSize: 12),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
