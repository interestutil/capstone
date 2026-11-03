import 'package:flutter/material.dart';

class AppTheme {
  static const Color ink = Color(0xFF1C241F);
  static const Color paper = Color(0xFFF3EEE4);
  static const Color amber = Color(0xFFE3A01A);
  static const Color rust = Color(0xFFC45C26);
  static const Color panel = Color(0xFF24302B);
  static const Color panelDark = Color(0xFF1B2420);
  static const Color cardSurface = Color(0xFF2A3832);
  static const Color okGreen = Color(0xFF2F7D4A);
  static const Color routePale = Color(0xFFB7C4B1);

  static ThemeData get darkTheme {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      scaffoldBackgroundColor: ink,
      colorScheme: const ColorScheme.dark(
        primary: amber,
        onPrimary: ink,
        secondary: amber,
        onSecondary: ink,
        surface: panel,
        onSurface: paper,
        error: rust,
        onError: paper,
      ),
      cardTheme: CardThemeData(
        color: cardSurface,
        elevation: 2,
        margin: const EdgeInsets.only(bottom: 12),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: const BorderSide(color: Color(0xFF384B43), width: 1),
        ),
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: panelDark,
        foregroundColor: paper,
        elevation: 0,
        centerTitle: false,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: cardSurface,
        labelStyle: const TextStyle(color: Color(0xFFB0BDB7)),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: Color(0xFF455A52)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: Color(0xFF455A52)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: amber, width: 2),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: amber,
          foregroundColor: ink,
          minimumSize: const Size.fromHeight(52),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: paper,
          minimumSize: const Size.fromHeight(52),
          side: const BorderSide(color: Color(0xFF5A7268)),
          textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
      ),
    );
  }
}
