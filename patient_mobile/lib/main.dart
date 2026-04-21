import 'package:flutter/material.dart';
import 'screens/login_screen.dart';
import 'screens/dashboard_screen.dart';
import 'screens/profile_screen.dart';
import 'screens/conversation_detail_screen.dart';

void main() {
  runApp(const PatientApp());
}

class PatientApp extends StatelessWidget {
  const PatientApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Patient Mobile App',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff6f5acd)),
        useMaterial3: true,
        appBarTheme: const AppBarTheme(
          backgroundColor: Color(0xff6f5acd),
          foregroundColor: Color(0xfffcf7e4),
          iconTheme: IconThemeData(color: Color(0xfffcf7e4)),
          titleTextStyle: TextStyle(color: Color(0xfffcf7e4), fontSize: 20, fontWeight: FontWeight.bold),
        ),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            backgroundColor: const Color(0xff6f5acd),
            foregroundColor: const Color(0xfffcf7e4),
            padding: const EdgeInsets.symmetric(horizontal: 10),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(4),
            ),
          ),
        ),
        scaffoldBackgroundColor: const Color(0xfffcf7e4),
        textTheme: const TextTheme(
          bodyMedium: TextStyle(color: Color(0xff2b1392)),
          bodySmall: TextStyle(color: Color(0xff2b1392)),
          bodyLarge: TextStyle(color: Color(0xff2b1392)),
          headlineSmall: TextStyle(color: Color(0xff2b1392)),
          headlineMedium: TextStyle(color: Color(0xff2b1392)),
          headlineLarge: TextStyle(color: Color(0xff2b1392)),
          labelLarge: TextStyle(color: Color(0xff2b1392)),
        ),
      ),
      initialRoute: '/',
      routes: {
        '/': (context) => const LoginScreen(),
        '/dashboard': (context) => const DashboardScreen(),
        '/profile': (context) => const ProfileScreen(),
        '/conversation': (context) => const ConversationDetailScreen(),
      },
    );
  }
}
