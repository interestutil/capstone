package com.fathalla.wms.worker.ui

import android.widget.Toast
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import coil.compose.AsyncImage
import com.fathalla.wms.worker.WmsViewModel
import com.fathalla.wms.worker.data.OrderDetailDto
import com.fathalla.wms.worker.data.OrderLineDto
import com.journeyapps.barcodescanner.ScanContract
import com.journeyapps.barcodescanner.ScanOptions

@Composable
fun WorkerApp(viewModel: WmsViewModel) {
    val state by viewModel.state.collectAsState()
    val nav = rememberNavController()
    val context = LocalContext.current

    LaunchedEffect(state.message) {
        state.message?.let {
            Toast.makeText(context, it, Toast.LENGTH_LONG).show()
            viewModel.clearMessage()
        }
    }

    if (!state.ready) return

    NavHost(navController = nav, startDestination = if (state.signedIn) "tasks" else "login") {
        composable("login") {
            LoginScreen(state.baseUrl, onLogin = viewModel::login)
        }
        composable("tasks") {
            LaunchedEffect(Unit) { viewModel.refresh() }
            TasksScreen(
                name = state.workerName,
                orders = state.orders,
                inbound = state.inbound,
                onOpenOrder = { nav.navigate("order/$it") },
                onInbound = { nav.navigate("inbound") },
                onRefresh = viewModel::refresh,
                onLogout = {
                    viewModel.logout()
                    nav.navigate("login") { popUpTo("tasks") { inclusive = true } }
                }
            )
        }
        composable("order/{id}", arguments = listOf(navArgument("id") { type = NavType.IntType })) { entry ->
            val id = entry.arguments?.getInt("id") ?: return@composable
            val order = state.orders.firstOrNull { it.id == id }
            if (order == null) {
                Text("Order not found", modifier = Modifier.padding(24.dp))
            } else {
                OrderScreen(
                    order = order,
                    onPick = { lineId -> nav.navigate("pick/$id/$lineId") },
                    onMap = { nav.navigate("map/$id") },
                    onHandoff = { viewModel.handoff(id) },
                    onBack = { nav.popBackStack() }
                )
            }
        }
        composable(
            "pick/{orderId}/{lineId}",
            arguments = listOf(
                navArgument("orderId") { type = NavType.IntType },
                navArgument("lineId") { type = NavType.IntType }
            )
        ) { entry ->
            val orderId = entry.arguments!!.getInt("orderId")
            val lineId = entry.arguments!!.getInt("lineId")
            val line = state.orders.firstOrNull { it.id == orderId }?.lines?.firstOrNull { it.id == lineId }
            if (line == null) {
                Text("Line not found", modifier = Modifier.padding(24.dp))
            } else {
                PickScreen(line = line, onConfirm = { barcode ->
                    viewModel.confirmPick(orderId, lineId, barcode) { nav.popBackStack() }
                }, onBack = { nav.popBackStack() })
            }
        }
        composable("map/{id}", arguments = listOf(navArgument("id") { type = NavType.IntType })) { entry ->
            val id = entry.arguments!!.getInt("id")
            MapScreen(
                shelves = state.shelves,
                map = state.map,
                route = state.route,
                onLoad = { anchor -> viewModel.loadRoute(id, anchor) },
                onBack = { nav.popBackStack() }
            )
        }
        composable("inbound") {
            InboundScreen(
                records = state.inbound,
                onCreate = { barcode, qty, voucher ->
                    viewModel.createInbound(barcode, qty, voucher) { }
                },
                onPlace = viewModel::placeInbound,
                onBack = { nav.popBackStack() }
            )
        }
    }
}

@Composable
private fun LoginScreen(defaultUrl: String, onLogin: (String, String, String) -> Unit) {
    var email by remember { mutableStateOf("ahmed@warehouse.local") }
    var password by remember { mutableStateOf("Worker123!") }
    var url by remember { mutableStateOf(defaultUrl) }

    Column(Modifier.fillMaxSize().padding(24.dp), verticalArrangement = Arrangement.Center) {
        Text("Warehouse worker", style = MaterialTheme.typography.headlineMedium)
        Text("Sign in with your email. Large buttons, few steps.")
        Spacer(Modifier.height(16.dp))
        OutlinedTextField(email, { email = it }, label = { Text("Email") }, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(password, { password = it }, label = { Text("Password") }, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(url, { url = it }, label = { Text("API address") }, modifier = Modifier.fillMaxWidth())
        Spacer(Modifier.height(16.dp))
        Button(onClick = { onLogin(email, password, url) }, modifier = Modifier.fillMaxWidth().height(56.dp)) {
            Text("Sign in")
        }
    }
}

@Composable
private fun TasksScreen(
    name: String,
    orders: List<OrderDetailDto>,
    inbound: List<com.fathalla.wms.worker.data.InboundDto>,
    onOpenOrder: (Int) -> Unit,
    onInbound: () -> Unit,
    onRefresh: () -> Unit,
    onLogout: () -> Unit
) {
    Scaffold { padding ->
        Column(Modifier.padding(padding).padding(20.dp).verticalScroll(rememberScrollState())) {
            Text("Hello, $name", style = MaterialTheme.typography.headlineSmall)
            Text("Your assigned lines, grouped by order.")
            Spacer(Modifier.height(12.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(onClick = onRefresh) { Text("Refresh") }
                OutlinedButton(onClick = onInbound) { Text("Inbound") }
                TextButton(onClick = onLogout) { Text("Sign out") }
            }
            Spacer(Modifier.height(16.dp))
            orders.forEach { order ->
                Card(Modifier.fillMaxWidth().padding(bottom = 12.dp), onClick = { onOpenOrder(order.id) }) {
                    Column(Modifier.padding(16.dp)) {
                        Text("Order #${order.id} · ${order.branchName}", style = MaterialTheme.typography.titleMedium)
                        Text("Deadline ${order.deadline.take(16)} · ${order.status}")
                        order.lines.forEach { line ->
                            Text("${line.pickSequence}. ${line.shelfCode}  ${line.productName}  ×${line.qtyCartons}  ${line.status}")
                        }
                    }
                }
            }
            if (orders.isEmpty()) Text("No assigned orders yet.")
            if (inbound.any { it.status != "Placed" }) {
                Text("Inbound waiting: ${inbound.count { it.status != "Placed" }}", modifier = Modifier.padding(top = 8.dp))
            }
        }
    }
}

@Composable
private fun OrderScreen(
    order: OrderDetailDto,
    onPick: (Int) -> Unit,
    onMap: () -> Unit,
    onHandoff: () -> Unit,
    onBack: () -> Unit
) {
    val pending = order.lines.filter { it.status == "Pending" }
    val readyForHandoff = pending.isEmpty() && order.lines.any { it.handedOffAt == null }

    Column(Modifier.fillMaxSize().padding(20.dp).verticalScroll(rememberScrollState())) {
        TextButton(onClick = onBack) { Text("Back") }
        Text("Order #${order.id}", style = MaterialTheme.typography.headlineSmall)
        Text("${order.branchName} · ${order.status}")
        Spacer(Modifier.height(12.dp))
        Button(onClick = onMap, modifier = Modifier.fillMaxWidth().height(52.dp)) { Text("Floor map") }
        Spacer(Modifier.height(12.dp))
        order.lines.forEach { line ->
            Card(Modifier.fillMaxWidth().padding(bottom = 10.dp)) {
                Column(Modifier.padding(16.dp)) {
                    Text("${line.shelfCode} · ${line.productName}", style = MaterialTheme.typography.titleMedium)
                    Text("${line.qtyCartons} cartons · ${line.status}")
                    if (line.status == "Pending") {
                        Button(onClick = { onPick(line.id) }, modifier = Modifier.fillMaxWidth().height(52.dp)) {
                            Text("Scan and pick")
                        }
                    }
                }
            }
        }
        if (readyForHandoff) {
            Button(onClick = onHandoff, modifier = Modifier.fillMaxWidth().height(56.dp)) {
                Text("Confirm handoff at dock")
            }
        }
    }
}

@Composable
private fun PickScreen(line: OrderLineDto, onConfirm: (String) -> Unit, onBack: () -> Unit) {
    var barcode by remember { mutableStateOf("") }
    val scan = rememberLauncherForActivityResult(ScanContract()) { result ->
        if (result.contents != null) barcode = result.contents
    }

    Column(Modifier.fillMaxSize().padding(20.dp)) {
        TextButton(onClick = onBack) { Text("Back") }
        Text("Pick ${line.productName}", style = MaterialTheme.typography.headlineSmall)
        Text("Shelf ${line.shelfCode} · ${line.qtyCartons} cartons")
        Spacer(Modifier.height(16.dp))
        OutlinedTextField(barcode, { barcode = it }, label = { Text("Barcode") }, modifier = Modifier.fillMaxWidth())
        Spacer(Modifier.height(8.dp))
        OutlinedButton(
            onClick = {
                scan.launch(ScanOptions().setDesiredBarcodeFormats(ScanOptions.ALL_CODE_TYPES).setPrompt("Scan product"))
            },
            modifier = Modifier.fillMaxWidth().height(52.dp)
        ) { Text("Open camera scan") }
        Spacer(Modifier.height(12.dp))
        Button(onClick = { onConfirm(barcode) }, modifier = Modifier.fillMaxWidth().height(56.dp)) {
            Text("Confirm pick")
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun MapScreen(
    shelves: List<com.fathalla.wms.worker.data.ShelfDto>,
    map: com.fathalla.wms.worker.data.MapInfoDto?,
    route: com.fathalla.wms.worker.data.RouteDto?,
    onLoad: (Int) -> Unit,
    onBack: () -> Unit
) {
    var expanded by remember { mutableStateOf(false) }
    var selected by remember { mutableStateOf(shelves.firstOrNull()) }
    val nextLeg = route?.legs?.firstOrNull { it.kind == "shelf" } ?: route?.legs?.firstOrNull()

    Column(Modifier.fillMaxSize().padding(16.dp)) {
        TextButton(onClick = onBack) { Text("Back") }
        Text("Where are you standing?", style = MaterialTheme.typography.titleLarge)
        ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = it }) {
            OutlinedTextField(
                value = selected?.code ?: "Choose a shelf",
                onValueChange = {},
                readOnly = true,
                trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
                modifier = Modifier.menuAnchor().fillMaxWidth()
            )
            ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
                shelves.forEach { shelf ->
                    DropdownMenuItem(text = { Text(shelf.code) }, onClick = {
                        selected = shelf
                        expanded = false
                        onLoad(shelf.id)
                    })
                }
            }
        }
        Spacer(Modifier.height(12.dp))
        nextLeg?.let {
            Text(if (it.kind == "dock") "Walk to the loading dock" else "Next stop: shelf ${it.shelfId} · ${it.distance} px")
        }
        Box(Modifier.fillMaxWidth().height(280.dp)) {
            if (map != null) {
                AsyncImage(model = map.imageUrl, contentDescription = "Floor map", modifier = Modifier.fillMaxSize(), contentScale = ContentScale.FillBounds)
                Canvas(Modifier.fillMaxSize()) {
                    val sx = size.width / map.width
                    val sy = size.height / map.height
                    route?.legs?.forEachIndexed { index, leg ->
                        if (leg.points.size < 2) return@forEachIndexed
                        val path = Path()
                        path.moveTo((leg.points.first().x * sx).toFloat(), (leg.points.first().y * sy).toFloat())
                        leg.points.drop(1).forEach { point ->
                            path.lineTo((point.x * sx).toFloat(), (point.y * sy).toFloat())
                        }
                        drawPath(
                            path,
                            color = if (index == 0) Color(0xFFE3A01A) else Color(0xFFB7C4B1),
                            style = Stroke(width = 8f, cap = StrokeCap.Round)
                        )
                        val last = leg.points.last()
                        drawCircle(Color(0xFFC45C26), 12f, Offset((last.x * sx).toFloat(), (last.y * sy).toFloat()))
                    }
                }
            }
        }
    }
}

@Composable
private fun InboundScreen(
    records: List<com.fathalla.wms.worker.data.InboundDto>,
    onCreate: (String, Int, String) -> Unit,
    onPlace: (Int) -> Unit,
    onBack: () -> Unit
) {
    var barcode by remember { mutableStateOf("") }
    var qty by remember { mutableStateOf("1") }
    var voucher by remember { mutableStateOf("") }
    val scan = rememberLauncherForActivityResult(ScanContract()) { result ->
        if (result.contents != null) barcode = result.contents
    }

    Column(Modifier.fillMaxSize().padding(20.dp).verticalScroll(rememberScrollState())) {
        TextButton(onClick = onBack) { Text("Back") }
        Text("Inbound stock", style = MaterialTheme.typography.headlineSmall)
        OutlinedTextField(barcode, { barcode = it }, label = { Text("Product barcode") }, modifier = Modifier.fillMaxWidth())
        OutlinedButton(onClick = { scan.launch(ScanOptions().setPrompt("Scan inbound barcode")) }, modifier = Modifier.fillMaxWidth()) {
            Text("Scan barcode")
        }
        OutlinedTextField(qty, { qty = it }, label = { Text("Cartons") }, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(voucher, { voucher = it }, label = { Text("Voucher number") }, modifier = Modifier.fillMaxWidth())
        Button(
            onClick = { onCreate(barcode, qty.toIntOrNull() ?: 0, voucher) },
            modifier = Modifier.fillMaxWidth().height(56.dp)
        ) { Text("Submit inbound") }

        Spacer(Modifier.height(16.dp))
        records.forEach { item ->
            Card(Modifier.fillMaxWidth().padding(bottom = 8.dp)) {
                Column(Modifier.padding(16.dp)) {
                    Text("${item.voucherNumber} · ${item.productName} ×${item.qtyCartons}")
                    Text(item.status + if (item.targetShelfCode != null) " · ${item.targetShelfCode}" else "")
                    if (item.status == "ShelfAssigned") {
                        Button(onClick = { onPlace(item.id) }, modifier = Modifier.fillMaxWidth()) {
                            Text("Confirm placement")
                        }
                    }
                }
            }
        }
    }
}
