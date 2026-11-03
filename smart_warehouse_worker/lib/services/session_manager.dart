import 'package:shared_preferences/shared_preferences.dart';

class SessionManager {
  static const String _keyToken = 'wms_token';
  static const String _keyWorkerName = 'wms_worker_name';
  static const String _keyWorkerEmail = 'wms_worker_email';
  static const String _keyBaseUrl = 'wms_base_url';
  static const String _keyLastAnchor = 'wms_last_anchor_shelf_id';

  static const String defaultBaseUrl = 'http://10.0.2.2:5138/';

  final SharedPreferences _prefs;

  SessionManager(this._prefs);

  static Future<SessionManager> create() async {
    final prefs = await SharedPreferences.getInstance();
    return SessionManager(prefs);
  }

  String? get token => _prefs.getString(_keyToken);
  String? get workerName => _prefs.getString(_keyWorkerName);
  String? get workerEmail => _prefs.getString(_keyWorkerEmail);
  String get baseUrl => _prefs.getString(_keyBaseUrl) ?? defaultBaseUrl;
  int? get lastAnchorShelfId => _prefs.getInt(_keyLastAnchor);

  bool get isSignedIn => token != null && token!.isNotEmpty;

  Future<void> saveSession({
    required String token,
    required String workerName,
    required String workerEmail,
    required String baseUrl,
  }) async {
    final cleanUrl = baseUrl.endsWith('/') ? baseUrl : '$baseUrl/';
    await _prefs.setString(_keyToken, token);
    await _prefs.setString(_keyWorkerName, workerName);
    await _prefs.setString(_keyWorkerEmail, workerEmail);
    await _prefs.setString(_keyBaseUrl, cleanUrl);
  }

  Future<void> setLastAnchorShelfId(int shelfId) async {
    await _prefs.setInt(_keyLastAnchor, shelfId);
  }

  Future<void> clearSession() async {
    await _prefs.remove(_keyToken);
    await _prefs.remove(_keyWorkerName);
    await _prefs.remove(_keyWorkerEmail);
  }
}
