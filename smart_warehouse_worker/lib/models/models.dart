class LoginResponse {
  final String token;
  final int userId;
  final String fullName;
  final String role;
  final String email;

  LoginResponse({
    required this.token,
    required this.userId,
    required this.fullName,
    required this.role,
    required this.email,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> json) {
    return LoginResponse(
      token: json['token'] as String,
      userId: json['userId'] as int,
      fullName: json['fullName'] as String,
      role: json['role'] as String,
      email: json['email'] as String,
    );
  }
}

class ProductDto {
  final int id;
  final String name;
  final int productTypeId;
  final String productTypeName;
  final String pieceBarcode;
  final String cartonBarcode;

  ProductDto({
    required this.id,
    required this.name,
    required this.productTypeId,
    required this.productTypeName,
    required this.pieceBarcode,
    required this.cartonBarcode,
  });

  factory ProductDto.fromJson(Map<String, dynamic> json) {
    return ProductDto(
      id: json['id'] as int,
      name: json['name'] as String,
      productTypeId: json['productTypeId'] as int,
      productTypeName: json['productTypeName'] as String,
      pieceBarcode: json['pieceBarcode'] as String,
      cartonBarcode: json['cartonBarcode'] as String,
    );
  }
}

class ShelfDto {
  final int id;
  final String code;
  final int rack;
  final int level;
  final int productTypeId;
  final String productTypeName;
  final int? productId;
  final String? productName;
  final int maxCapacity;
  final int onHand;
  final int committed;
  final int available;
  final bool isEmpty;
  final bool isFull;
  final String? ledDeviceId;
  final int ledIndex;

  ShelfDto({
    required this.id,
    required this.code,
    required this.rack,
    required this.level,
    required this.productTypeId,
    required this.productTypeName,
    this.productId,
    this.productName,
    required this.maxCapacity,
    required this.onHand,
    required this.committed,
    required this.available,
    required this.isEmpty,
    required this.isFull,
    this.ledDeviceId,
    required this.ledIndex,
  });

  factory ShelfDto.fromJson(Map<String, dynamic> json) {
    return ShelfDto(
      id: json['id'] as int,
      code: json['code'] as String,
      rack: json['rack'] as int,
      level: json['level'] as int,
      productTypeId: json['productTypeId'] as int,
      productTypeName: json['productTypeName'] as String,
      productId: json['productId'] as int?,
      productName: json['productName'] as String?,
      maxCapacity: json['maxCapacity'] as int,
      onHand: json['onHand'] as int,
      committed: json['committed'] as int,
      available: json['available'] as int,
      isEmpty: json['isEmpty'] as bool,
      isFull: json['isFull'] as bool,
      ledDeviceId: json['ledDeviceId'] as String?,
      ledIndex: json['ledIndex'] as int,
    );
  }
}

class OrderDetailDto {
  final int id;
  final int branchId;
  final String branchName;
  final DateTime deadline;
  final String status;
  final DateTime createdAt;
  final DateTime? firstPickAt;
  final DateTime? completedAt;
  final DateTime? cancelledAt;
  final List<OrderLineDto> lines;

  OrderDetailDto({
    required this.id,
    required this.branchId,
    required this.branchName,
    required this.deadline,
    required this.status,
    required this.createdAt,
    this.firstPickAt,
    this.completedAt,
    this.cancelledAt,
    required this.lines,
  });

  factory OrderDetailDto.fromJson(Map<String, dynamic> json) {
    return OrderDetailDto(
      id: json['id'] as int,
      branchId: json['branchId'] as int,
      branchName: json['branchName'] as String,
      deadline: DateTime.parse(json['deadline'] as String),
      status: json['status'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      firstPickAt: json['firstPickAt'] != null ? DateTime.parse(json['firstPickAt'] as String) : null,
      completedAt: json['completedAt'] != null ? DateTime.parse(json['completedAt'] as String) : null,
      cancelledAt: json['cancelledAt'] != null ? DateTime.parse(json['cancelledAt'] as String) : null,
      lines: (json['lines'] as List<dynamic>)
          .map((e) => OrderLineDto.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

class OrderLineDto {
  final int id;
  final int shelfId;
  final String shelfCode;
  final int productId;
  final String productName;
  final int qtyCartons;
  final String status;
  final int pickSequence;
  final int assignedWorkerId;
  final String assignedWorkerName;
  final DateTime? pickedAt;
  final DateTime? handedOffAt;

  OrderLineDto({
    required this.id,
    required this.shelfId,
    required this.shelfCode,
    required this.productId,
    required this.productName,
    required this.qtyCartons,
    required this.status,
    required this.pickSequence,
    required this.assignedWorkerId,
    required this.assignedWorkerName,
    this.pickedAt,
    this.handedOffAt,
  });

  factory OrderLineDto.fromJson(Map<String, dynamic> json) {
    return OrderLineDto(
      id: json['id'] as int,
      shelfId: json['shelfId'] as int,
      shelfCode: json['shelfCode'] as String,
      productId: json['productId'] as int,
      productName: json['productName'] as String,
      qtyCartons: json['qtyCartons'] as int,
      status: json['status'] as String,
      pickSequence: json['pickSequence'] as int,
      assignedWorkerId: json['assignedWorkerId'] as int,
      assignedWorkerName: json['assignedWorkerName'] as String,
      pickedAt: json['pickedAt'] != null ? DateTime.parse(json['pickedAt'] as String) : null,
      handedOffAt: json['handedOffAt'] != null ? DateTime.parse(json['handedOffAt'] as String) : null,
    );
  }
}

class InboundDto {
  final int id;
  final String voucherNumber;
  final int productId;
  final String productName;
  final int qtyCartons;
  final int? targetShelfId;
  final String? targetShelfCode;
  final String status;
  final int createdByWorkerId;
  final String createdByWorkerName;
  final DateTime createdAt;
  final DateTime? placedAt;

  InboundDto({
    required this.id,
    required this.voucherNumber,
    required this.productId,
    required this.productName,
    required this.qtyCartons,
    this.targetShelfId,
    this.targetShelfCode,
    required this.status,
    required this.createdByWorkerId,
    required this.createdByWorkerName,
    required this.createdAt,
    this.placedAt,
  });

  factory InboundDto.fromJson(Map<String, dynamic> json) {
    return InboundDto(
      id: json['id'] as int,
      voucherNumber: json['voucherNumber'] as String,
      productId: json['productId'] as int,
      productName: json['productName'] as String,
      qtyCartons: json['qtyCartons'] as int,
      targetShelfId: json['targetShelfId'] as int?,
      targetShelfCode: json['targetShelfCode'] as String?,
      status: json['status'] as String,
      createdByWorkerId: json['createdByWorkerId'] as int,
      createdByWorkerName: json['createdByWorkerName'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      placedAt: json['placedAt'] != null ? DateTime.parse(json['placedAt'] as String) : null,
    );
  }
}

class MapInfoDto {
  final String imageUrl;
  final int width;
  final int height;

  MapInfoDto({
    required this.imageUrl,
    required this.width,
    required this.height,
  });

  factory MapInfoDto.fromJson(Map<String, dynamic> json) {
    return MapInfoDto(
      imageUrl: json['imageUrl'] as String,
      width: json['width'] as int,
      height: json['height'] as int,
    );
  }
}

class RouteDto {
  final int orderId;
  final List<RouteLegDto> legs;

  RouteDto({
    required this.orderId,
    required this.legs,
  });

  factory RouteDto.fromJson(Map<String, dynamic> json) {
    return RouteDto(
      orderId: json['orderId'] as int,
      legs: (json['legs'] as List<dynamic>)
          .map((e) => RouteLegDto.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

class RouteLegDto {
  final int sequence;
  final String kind;
  final int? shelfId;
  final int? lineId;
  final List<PointDto> points;
  final double distance;

  RouteLegDto({
    required this.sequence,
    required this.kind,
    this.shelfId,
    this.lineId,
    required this.points,
    required this.distance,
  });

  factory RouteLegDto.fromJson(Map<String, dynamic> json) {
    return RouteLegDto(
      sequence: json['sequence'] as int,
      kind: json['kind'] as String,
      shelfId: json['shelfId'] as int?,
      lineId: json['lineId'] as int?,
      points: (json['points'] as List<dynamic>)
          .map((e) => PointDto.fromJson(e as Map<String, dynamic>))
          .toList(),
      distance: (json['distance'] as num).toDouble(),
    );
  }
}

class PointDto {
  final double x;
  final double y;

  PointDto({required this.x, required this.y});

  factory PointDto.fromJson(Map<String, dynamic> json) {
    return PointDto(
      x: (json['x'] as num).toDouble(),
      y: (json['y'] as num).toDouble(),
    );
  }
}

class ApiError {
  final String message;
  final int? shelfId;
  final int? requested;
  final int? available;
  final int? nodeId;

  ApiError({
    required this.message,
    this.shelfId,
    this.requested,
    this.available,
    this.nodeId,
  });

  factory ApiError.fromJson(Map<String, dynamic> json) {
    return ApiError(
      message: json['message'] as String? ?? 'An error occurred.',
      shelfId: json['shelfId'] as int?,
      requested: json['requested'] as int?,
      available: json['available'] as int?,
      nodeId: json['nodeId'] as int?,
    );
  }
}
