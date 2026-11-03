package com.fathalla.wms.worker

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.fathalla.wms.worker.data.ApiFactory
import com.fathalla.wms.worker.data.ConfirmPickRequest
import com.fathalla.wms.worker.data.CreateInboundRequest
import com.fathalla.wms.worker.data.InboundDto
import com.fathalla.wms.worker.data.LoginRequest
import com.fathalla.wms.worker.data.MapInfoDto
import com.fathalla.wms.worker.data.OrderDetailDto
import com.fathalla.wms.worker.data.RouteDto
import com.fathalla.wms.worker.data.SessionStore
import com.fathalla.wms.worker.data.ShelfDto
import com.fathalla.wms.worker.data.WmsApi
import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import retrofit2.HttpException

data class UiState(
    val ready: Boolean = false,
    val signedIn: Boolean = false,
    val workerName: String = "",
    val baseUrl: String = "http://10.0.2.2:5138/",
    val message: String? = null,
    val orders: List<OrderDetailDto> = emptyList(),
    val inbound: List<InboundDto> = emptyList(),
    val shelves: List<ShelfDto> = emptyList(),
    val map: MapInfoDto? = null,
    val route: RouteDto? = null
)

class WmsViewModel(application: Application) : AndroidViewModel(application) {
    private val session: SessionStore = (application as WmsApp).sessionStore
    private var token: String? = null
    private var api: WmsApi = ApiFactory.create("http://10.0.2.2:5138/") { token }

    private val _state = MutableStateFlow(UiState())
    val state: StateFlow<UiState> = _state

    init {
        viewModelScope.launch {
            token = session.token()
            val url = session.baseUrl()
            api = ApiFactory.create(url) { token }
            _state.value = _state.value.copy(
                ready = true,
                signedIn = !token.isNullOrBlank(),
                workerName = session.name().orEmpty(),
                baseUrl = url
            )
            if (!token.isNullOrBlank()) refresh()
        }
    }

    fun login(email: String, password: String, baseUrl: String) {
        viewModelScope.launch {
            runCatching {
                val url = if (baseUrl.endsWith("/")) baseUrl else "$baseUrl/"
                api = ApiFactory.create(url) { token }
                val result = api.login(LoginRequest(email.trim(), password))
                if (!result.role.equals("Worker", ignoreCase = true)) {
                    error("This app is for warehouse workers. Admins use the PC dashboard.")
                }
                token = result.token
                session.save(result.token, result.fullName, url)
                _state.value = _state.value.copy(signedIn = true, workerName = result.fullName, baseUrl = url, message = null)
                refresh()
            }.onFailure { _state.value = _state.value.copy(message = human(it)) }
        }
    }

    fun logout() {
        viewModelScope.launch {
            token = null
            session.clear()
            _state.value = UiState(ready = true, signedIn = false, baseUrl = _state.value.baseUrl)
        }
    }

    fun refresh() {
        viewModelScope.launch {
            runCatching {
                val orders = api.myOrders()
                val inbound = api.inbound()
                val shelves = api.shelves()
                _state.value = _state.value.copy(orders = orders, inbound = inbound, shelves = shelves, message = null)
            }.onFailure { _state.value = _state.value.copy(message = human(it)) }
        }
    }

    fun confirmPick(orderId: Int, lineId: Int, barcode: String, onDone: () -> Unit) {
        viewModelScope.launch {
            runCatching {
                api.pick(orderId, lineId, ConfirmPickRequest(barcode.ifBlank { null }))
                refresh()
                onDone()
            }.onFailure { _state.value = _state.value.copy(message = human(it)) }
        }
    }

    fun handoff(orderId: Int) {
        viewModelScope.launch {
            runCatching {
                api.handoff(orderId)
                refresh()
            }.onFailure { _state.value = _state.value.copy(message = human(it)) }
        }
    }

    fun createInbound(barcode: String, qty: Int, voucher: String, onDone: () -> Unit) {
        viewModelScope.launch {
            runCatching {
                api.createInbound(CreateInboundRequest(barcode, qty, voucher))
                refresh()
                onDone()
            }.onFailure { _state.value = _state.value.copy(message = human(it)) }
        }
    }

    fun placeInbound(id: Int) {
        viewModelScope.launch {
            runCatching {
                api.placeInbound(id)
                refresh()
            }.onFailure { _state.value = _state.value.copy(message = human(it)) }
        }
    }

    fun loadRoute(orderId: Int, anchorShelfId: Int) {
        viewModelScope.launch {
            runCatching {
                val map = api.map()
                val route = api.route(orderId, anchorShelfId)
                _state.value = _state.value.copy(map = map, route = route, message = null)
            }.onFailure { _state.value = _state.value.copy(message = human(it)) }
        }
    }

    fun clearMessage() {
        _state.value = _state.value.copy(message = null)
    }

    private fun human(error: Throwable): String {
        if (error is HttpException) {
            val body = error.response()?.errorBody()?.string().orEmpty()
            val parsed = runCatching {
                Moshi.Builder().addLast(KotlinJsonAdapterFactory()).build()
                    .adapter(com.fathalla.wms.worker.data.ApiError::class.java)
                    .fromJson(body)
            }.getOrNull()
            return parsed?.message ?: "Server error ${error.code()}"
        }
        return error.message ?: "Something went wrong."
    }
}
