package com.fathalla.wms.worker.data

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface WmsApi {
    @POST("api/auth/login")
    suspend fun login(@Body body: LoginRequest): LoginResponse

    @GET("api/my/orders")
    suspend fun myOrders(): List<OrderDetailDto>

    @GET("api/orders/{id}")
    suspend fun order(@Path("id") id: Int): OrderDetailDto

    @POST("api/orders/{id}/lines/{lineId}/pick")
    suspend fun pick(@Path("id") id: Int, @Path("lineId") lineId: Int, @Body body: ConfirmPickRequest): OrderDetailDto

    @POST("api/orders/{id}/handoff")
    suspend fun handoff(@Path("id") id: Int): OrderDetailDto

    @GET("api/products/by-barcode/{code}")
    suspend fun productByBarcode(@Path("code") code: String): ProductDto

    @POST("api/inbound")
    suspend fun createInbound(@Body body: CreateInboundRequest): InboundDto

    @GET("api/inbound")
    suspend fun inbound(): List<InboundDto>

    @POST("api/inbound/{id}/place")
    suspend fun placeInbound(@Path("id") id: Int): InboundDto

    @GET("api/map")
    suspend fun map(): MapInfoDto

    @GET("api/orders/{id}/route")
    suspend fun route(@Path("id") id: Int, @Query("anchorShelfId") anchorShelfId: Int): RouteDto

    @GET("api/shelves")
    suspend fun shelves(): List<ShelfDto>
}
