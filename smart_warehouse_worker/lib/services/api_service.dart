import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/models.dart';
import 'session_manager.dart';

class ApiException implements Exception {
  final int statusCode;
  final ApiError error;

  ApiException(this.statusCode, this.error);

  @override
  String toString() => error.message;
}

class ApiService {
  final SessionManager sessionManager;
  final http.Client _client = http.Client();

  ApiService(this.sessionManager);

  String get _baseUrl => sessionManager.baseUrl;
  String? get _token => sessionManager.token;

  Map<String, String> _headers([bool withAuth = true]) {
    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };
    if (withAuth && _token != null) {
      headers['Authorization'] = 'Bearer $_token';
    }
    return headers;
  }

  Uri _uri(String path, [Map<String, dynamic>? queryParams]) {
    final base = _baseUrl.endsWith('/') ? _baseUrl : '$_baseUrl/';
    final cleanPath = path.startsWith('/') ? path.substring(1) : path;
    var uri = Uri.parse('$base$cleanPath');
    if (queryParams != null && queryParams.isNotEmpty) {
      uri = uri.replace(queryParameters: queryParams.map((k, v) => MapEntry(k, v.toString())));
    }
    return uri;
  }

  Future<LoginResponse> login(String email, String password, [String? baseUrl]) async {
    final url = baseUrl ?? _baseUrl;
    final cleanBase = url.endsWith('/') ? url : '$url/';
    final uri = Uri.parse('${cleanBase}api/auth/login');

    final response = await _client.post(
      uri,
      headers: {'Content-Type': 'application/json', 'Accept': 'application/json'},
      body: jsonEncode({'email': email.trim(), 'password': password}),
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final json = jsonDecode(response.body) as Map<String, dynamic>;
      final result = LoginResponse.fromJson(json);
      if (result.role.toLowerCase() != 'worker') {
        throw ApiException(403, ApiError(message: 'This app is for warehouse workers. Admins use the PC dashboard.'));
      }
      return result;
    }

    _handleError(response);
  }

  Future<List<OrderDetailDto>> getMyOrders() async {
    final response = await _client.get(_uri('api/my/orders'), headers: _headers());
    if (response.statusCode == 200) {
      final list = jsonDecode(response.body) as List<dynamic>;
      return list.map((e) => OrderDetailDto.fromJson(e as Map<String, dynamic>)).toList();
    }
    _handleError(response);
  }

  Future<OrderDetailDto> getOrder(int id) async {
    final response = await _client.get(_uri('api/orders/$id'), headers: _headers());
    if (response.statusCode == 200) {
      return OrderDetailDto.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    _handleError(response);
  }

  Future<OrderDetailDto> confirmPick(int orderId, int lineId, String? barcode) async {
    final body = barcode != null && barcode.trim().isNotEmpty
        ? jsonEncode({'barcode': barcode.trim()})
        : jsonEncode({'barcode': null});

    final response = await _client.post(
      _uri('api/orders/$orderId/lines/$lineId/pick'),
      headers: _headers(),
      body: body,
    );

    if (response.statusCode == 200) {
      return OrderDetailDto.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    _handleError(response);
  }

  Future<OrderDetailDto> confirmHandoff(int orderId) async {
    final response = await _client.post(
      _uri('api/orders/$orderId/handoff'),
      headers: _headers(),
    );

    if (response.statusCode == 200) {
      return OrderDetailDto.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    _handleError(response);
  }

  Future<ProductDto> getProductByBarcode(String barcode) async {
    final response = await _client.get(
      _uri('api/products/by-barcode/${Uri.encodeComponent(barcode.trim())}'),
      headers: _headers(),
    );

    if (response.statusCode == 200) {
      return ProductDto.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    _handleError(response);
  }

  Future<InboundDto> createInbound(String barcode, int qtyCartons, String voucherNumber) async {
    final response = await _client.post(
      _uri('api/inbound'),
      headers: _headers(),
      body: jsonEncode({
        'barcode': barcode.trim(),
        'qtyCartons': qtyCartons,
        'voucherNumber': voucherNumber.trim(),
      }),
    );

    if (response.statusCode == 200 || response.statusCode == 201) {
      return InboundDto.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    _handleError(response);
  }

  Future<List<InboundDto>> getInbound() async {
    final response = await _client.get(_uri('api/inbound'), headers: _headers());
    if (response.statusCode == 200) {
      final list = jsonDecode(response.body) as List<dynamic>;
      return list.map((e) => InboundDto.fromJson(e as Map<String, dynamic>)).toList();
    }
    _handleError(response);
  }

  Future<InboundDto> placeInbound(int id) async {
    final response = await _client.post(
      _uri('api/inbound/$id/place'),
      headers: _headers(),
    );

    if (response.statusCode == 200) {
      return InboundDto.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    _handleError(response);
  }

  Future<MapInfoDto> getMap() async {
    final response = await _client.get(_uri('api/map'), headers: _headers());
    if (response.statusCode == 200) {
      return MapInfoDto.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    _handleError(response);
  }

  Future<RouteDto> getRoute(int orderId, int anchorShelfId) async {
    final response = await _client.get(
      _uri('api/orders/$orderId/route', {'anchorShelfId': anchorShelfId}),
      headers: _headers(),
    );

    if (response.statusCode == 200) {
      return RouteDto.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    _handleError(response);
  }

  Future<List<ShelfDto>> getShelves() async {
    final response = await _client.get(_uri('api/shelves'), headers: _headers());
    if (response.statusCode == 200) {
      final list = jsonDecode(response.body) as List<dynamic>;
      return list.map((e) => ShelfDto.fromJson(e as Map<String, dynamic>)).toList();
    }
    _handleError(response);
  }

  Never _handleError(http.Response response) {
    try {
      final json = jsonDecode(response.body) as Map<String, dynamic>;
      final apiError = ApiError.fromJson(json);
      throw ApiException(response.statusCode, apiError);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiException(
        response.statusCode,
        ApiError(message: 'Request failed with status ${response.statusCode}'),
      );
    }
  }
}
