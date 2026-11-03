import 'package:flutter/material.dart';
import 'screens/login_screen.dart';
import 'screens/tasks_screen.dart';
import 'services/api_service.dart';
import 'services/session_manager.dart';
import 'theme/app_theme.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final sessionManager = await SessionManager.create();
  final apiService = ApiService(sessionManager);

  runApp(SmartWarehouseWorkerApp(
    sessionManager: sessionManager,
    apiService: apiService,
  ));
}

class SmartWarehouseWorkerApp extends StatelessWidget {
  final SessionManager sessionManager;
  final ApiService apiService;

  const SmartWarehouseWorkerApp({
    super.key,
    required this.sessionManager,
    required this.apiService,
  });

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Smart Warehouse Worker',
      theme: AppTheme.darkTheme,
      debugShowCheckedModeBanner: false,
      home: sessionManager.isSignedIn
          ? TasksScreen(sessionManager: sessionManager, apiService: apiService)
          : LoginScreen(sessionManager: sessionManager, apiService: apiService),
    );
  }
}
