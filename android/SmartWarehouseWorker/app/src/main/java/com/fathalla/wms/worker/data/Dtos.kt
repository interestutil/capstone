package com.fathalla.wms.worker.data

data class LoginRequest(val email: String, val password: String)
data class LoginResponse(
    val token: String,
    val userId: Int,
    val fullName: String,
    val role: String,
    val email: String
)

data class ProductDto(
    val id: Int,
    val name: String,
    val productTypeId: Int,
    val productTypeName: String,
    val pieceBarcode: String,
    val cartonBarcode: String
)

data class ShelfDto(
    val id: Int,
    val code: String,
    val rack: Int,
    val level: Int,
    val productTypeId: Int,
    val productTypeName: String,
    val productId: Int?,
    val productName: String?,
    val maxCapacity: Int,
    val onHand: Int,
    val committed: Int,
    val available: Int,
    val isEmpty: Boolean,
    val isFull: Boolean,
    val ledDeviceId: String?,
    val ledIndex: Int
)

data class OrderDetailDto(
    val id: Int,
    val branchId: Int,
    val branchName: String,
    val deadline: String,
    val status: String,
    val createdAt: String,
    val firstPickAt: String?,
    val completedAt: String?,
    val cancelledAt: String?,
    val lines: List<OrderLineDto>
)

data class OrderLineDto(
    val id: Int,
    val shelfId: Int,
    val shelfCode: String,
    val productId: Int,
    val productName: String,
    val qtyCartons: Int,
    val status: String,
    val pickSequence: Int,
    val assignedWorkerId: Int,
    val assignedWorkerName: String,
    val pickedAt: String?,
    val handedOffAt: String?
)

data class ConfirmPickRequest(val barcode: String?)
data class CreateInboundRequest(val barcode: String, val qtyCartons: Int, val voucherNumber: String)

data class InboundDto(
    val id: Int,
    val voucherNumber: String,
    val productId: Int,
    val productName: String,
    val qtyCartons: Int,
    val targetShelfId: Int?,
    val targetShelfCode: String?,
    val status: String,
    val createdByWorkerId: Int,
    val createdByWorkerName: String,
    val createdAt: String,
    val placedAt: String?
)

data class MapInfoDto(val imageUrl: String, val width: Int, val height: Int)
data class RouteDto(val orderId: Int, val legs: List<RouteLegDto>)
data class RouteLegDto(
    val sequence: Int,
    val kind: String,
    val shelfId: Int?,
    val lineId: Int?,
    val points: List<PointDto>,
    val distance: Double
)
data class PointDto(val x: Double, val y: Double)
data class ApiError(val message: String? = null, val shelfId: Int? = null)
