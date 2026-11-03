import 'dart:async';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/models.dart';
import '../services/api_service.dart';
import '../services/session_manager.dart';
import '../theme/app_theme.dart';
import 'inbound_screen.dart';
import 'login_screen.dart';
import 'order_detail_screen.dart';

class TasksScreen extends StatefulWidget {
  final SessionManager sessionManager;
  final ApiService apiService;

  const TasksScreen({
    super.key,
    required this.sessionManager,
    required this.apiService,
  });

  @override
  State<TasksScreen> createState() => _TasksScreenState();
}

class _TasksScreenState extends State<TasksScreen> {
  List<OrderDetailDto> _orders = [];
  List<InboundDto> _inbound = [];
  bool _isLoading = true;
  String? _errorMessage;
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    _fetchData();
    // Requirements 4.3 & 3.2: Poll for task changes periodically
    _pollingTimer = Timer.periodic(const Duration(seconds: 5), (_) => _fetchData(silent: true));
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  Future<void> _fetchData({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _errorMessage = null;
      });
    }

    try {
      final orders = await widget.apiService.getMyOrders();
      final inbound = await widget.apiService.getInbound();
      if (!mounted) return;
      setState(() {
        _orders = orders;
        _inbound = inbound;
        _isLoading = false;
        _errorMessage = null;
      });
    } catch (e) {
      if (!mounted) return;
      if (!silent) {
        setState(() {
          _errorMessage = e.toString().replaceFirst('Exception: ', '');
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _handleSignOut() async {
    _pollingTimer?.cancel();
    await widget.sessionManager.clearSession();
    if (!mounted) return;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(
        builder: (_) => LoginScreen(
          sessionManager: widget.sessionManager,
          apiService: widget.apiService,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final workerName = widget.sessionManager.workerName ?? 'Worker';
    final waitingInboundCount = _inbound.where((i) => i.status != 'Placed').length;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Hello, $workerName', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            const Text('Your assigned warehouse orders', style: TextStyle(fontSize: 12, color: Color(0xFFB0BDB7))),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: () => _fetchData(),
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Sign Out',
            onPressed: _handleSignOut,
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () => _fetchData(),
        child: ListView(
          padding: const EdgeInsets.all(16.0),
          children: [
            Row(
              children: [
                Expanded(
                  child: ElevatedButton.icon(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: waitingInboundCount > 0 ? AppTheme.rust : AppTheme.panel,
                      foregroundColor: AppTheme.paper,
                      side: const BorderSide(color: Color(0xFF5A7268)),
                    ),
                    icon: const Icon(Icons.move_to_inbox),
                    label: Text(
                      waitingInboundCount > 0
                          ? 'Inbound Stock ($waitingInboundCount waiting)'
                          : 'Inbound Stock',
                    ),
                    onPressed: () async {
                      await Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => InboundScreen(
                            apiService: widget.apiService,
                            sessionManager: widget.sessionManager,
                          ),
                        ),
                      );
                      _fetchData();
                    },
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            if (_errorMessage != null)
              Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                  color: AppTheme.rust.withValues(alpha: 0.15),
                  border: Border.all(color: AppTheme.rust),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(_errorMessage!, style: const TextStyle(color: Color(0xFFFF8B7A))),
              ),
            if (_isLoading && _orders.isEmpty)
              const Center(
                child: Padding(
                  padding: EdgeInsets.all(32.0),
                  child: CircularProgressIndicator(color: AppTheme.amber),
                ),
              )
            else if (_orders.isEmpty)
              Container(
                padding: const EdgeInsets.all(32),
                alignment: Alignment.center,
                child: const Column(
                  children: [
                    Icon(Icons.inventory_2_outlined, size: 48, color: Color(0xFF6B8077)),
                    SizedBox(height: 12),
                    Text(
                      'No assigned orders at the moment.',
                      style: TextStyle(fontSize: 16, color: Color(0xFF9EABA4)),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'The app checks for new orders every 5 seconds.',
                      style: TextStyle(fontSize: 12, color: Color(0xFF6B8077)),
                    ),
                  ],
                ),
              )
            else
              ..._orders.map((order) => _buildOrderCard(order)),
          ],
        ),
      ),
    );
  }

  Widget _buildOrderCard(OrderDetailDto order) {
    final pendingCount = order.lines.where((l) => l.status == 'Pending').length;
    final totalCount = order.lines.length;
    final deadlineStr = DateFormat('dd MMM, HH:mm').format(order.deadline.toLocal());

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () async {
          await Navigator.of(context).push(
            MaterialPageRoute(
              builder: (_) => OrderDetailScreen(
                orderId: order.id,
                apiService: widget.apiService,
                sessionManager: widget.sessionManager,
              ),
            ),
          );
          _fetchData();
        },
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Order #${order.id} · ${order.branchName}',
                    style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: AppTheme.paper),
                  ),
                  _buildStatusBadge(order.status),
                ],
              ),
              const SizedBox(height: 6),
              Row(
                children: [
                  const Icon(Icons.timer_outlined, size: 14, color: AppTheme.amber),
                  const SizedBox(width: 4),
                  Text('Deadline: $deadlineStr', style: const TextStyle(fontSize: 13, color: Color(0xFFB0BDB7))),
                  const Spacer(),
                  Text(
                    '$pendingCount pending / $totalCount lines',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: pendingCount > 0 ? AppTheme.amber : AppTheme.okGreen,
                    ),
                  ),
                ],
              ),
              const Divider(color: Color(0xFF384B43), height: 20),
              ...order.lines.map((line) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4.0),
                    child: Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                          decoration: BoxDecoration(
                            color: const Color(0xFF1E2824),
                            borderRadius: BorderRadius.circular(4),
                            border: Border.all(color: const Color(0xFF43584F)),
                          ),
                          child: Text(
                            line.shelfCode,
                            style: const TextStyle(
                              fontFamily: 'monospace',
                              fontWeight: FontWeight.bold,
                              fontSize: 12,
                              color: AppTheme.amber,
                            ),
                          ),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            line.productName,
                            style: const TextStyle(fontSize: 14, color: AppTheme.paper),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        Text(
                          '×${line.qtyCartons}',
                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: AppTheme.paper),
                        ),
                        const SizedBox(width: 8),
                        Icon(
                          line.status == 'Picked' ? Icons.check_circle : Icons.radio_button_unchecked,
                          size: 16,
                          color: line.status == 'Picked' ? AppTheme.okGreen : const Color(0xFF6B8077),
                        ),
                      ],
                    ),
                  )),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildStatusBadge(String status) {
    Color bg = const Color(0xFF394A42);
    Color text = AppTheme.paper;
    if (status == 'Assigned') {
      bg = const Color(0xFF2E4639);
      text = AppTheme.amber;
    } else if (status == 'InProgress') {
      bg = const Color(0xFF473E24);
      text = const Color(0xFFFFD56B);
    } else if (status == 'AwaitingHandoff') {
      bg = const Color(0xFF4A3423);
      text = const Color(0xFFFF9E66);
    } else if (status == 'Completed') {
      bg = const Color(0xFF1F442C);
      text = const Color(0xFF86E2A3);
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(6)),
      child: Text(status, style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: text)),
    );
  }
}
