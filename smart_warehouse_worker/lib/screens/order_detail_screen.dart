import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/models.dart';
import '../services/api_service.dart';
import '../services/session_manager.dart';
import '../theme/app_theme.dart';
import 'floor_map_screen.dart';
import 'pick_screen.dart';

class OrderDetailScreen extends StatefulWidget {
  final int orderId;
  final ApiService apiService;
  final SessionManager sessionManager;

  const OrderDetailScreen({
    super.key,
    required this.orderId,
    required this.apiService,
    required this.sessionManager,
  });

  @override
  State<OrderDetailScreen> createState() => _OrderDetailScreenState();
}

class _OrderDetailScreenState extends State<OrderDetailScreen> {
  OrderDetailDto? _order;
  bool _isLoading = true;
  bool _isHandoffLoading = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _fetchOrder();
  }

  Future<void> _fetchOrder() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final order = await widget.apiService.getOrder(widget.orderId);
      if (!mounted) return;
      setState(() {
        _order = order;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString().replaceFirst('Exception: ', '');
        _isLoading = false;
      });
    }
  }

  Future<void> _handleHandoff() async {
    setState(() => _isHandoffLoading = true);
    try {
      final updated = await widget.apiService.confirmHandoff(widget.orderId);
      if (!mounted) return;
      setState(() => _order = updated);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          backgroundColor: AppTheme.okGreen,
          content: Text('Handoff confirmed at dock! Goods handed off.'),
        ),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: AppTheme.rust,
          content: Text(e.toString().replaceFirst('Exception: ', '')),
        ),
      );
    } finally {
      if (mounted) setState(() => _isHandoffLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: Text('Order #${widget.orderId}')),
        body: const Center(child: CircularProgressIndicator(color: AppTheme.amber)),
      );
    }

    if (_order == null) {
      return Scaffold(
        appBar: AppBar(title: Text('Order #${widget.orderId}')),
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(_errorMessage ?? 'Failed to load order.', style: const TextStyle(color: Color(0xFFFF8B7A))),
              const SizedBox(height: 12),
              ElevatedButton(onPressed: _fetchOrder, child: const Text('Retry')),
            ],
          ),
        ),
      );
    }

    final order = _order!;
    final pendingLines = order.lines.where((l) => l.status == 'Pending').toList();
    final allPicked = pendingLines.isEmpty && order.lines.isNotEmpty;
    final anyNeedsHandoff = order.lines.any((l) => l.handedOffAt == null);
    final readyForHandoff = allPicked && anyNeedsHandoff;
    final deadlineStr = DateFormat('dd MMM yyyy, HH:mm').format(order.deadline.toLocal());

    return Scaffold(
      appBar: AppBar(
        title: Text('Order #${order.id} · ${order.branchName}'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: _fetchOrder,
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16.0),
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: AppTheme.panel,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: const Color(0xFF384B43)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      order.branchName,
                      style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppTheme.paper),
                    ),
                    _buildStatusBadge(order.status),
                  ],
                ),
                const SizedBox(height: 8),
                Text('Deadline: $deadlineStr', style: const TextStyle(fontSize: 13, color: Color(0xFFB0BDB7))),
                const SizedBox(height: 16),
                ElevatedButton.icon(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.amber,
                    foregroundColor: AppTheme.ink,
                  ),
                  icon: const Icon(Icons.map),
                  label: const Text('Floor Map & Route'),
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => FloorMapScreen(
                          orderId: order.id,
                          apiService: widget.apiService,
                          sessionManager: widget.sessionManager,
                        ),
                      ),
                    );
                  },
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),
          const Text(
            'ASSIGNED LINES',
            style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, letterSpacing: 1.2, color: Color(0xFF9EABA4)),
          ),
          const SizedBox(height: 8),
          ...order.lines.map((line) => _buildLineCard(line, order)),
          if (readyForHandoff) ...[
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AppTheme.okGreen.withValues(alpha: 0.15),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppTheme.okGreen),
              ),
              child: Column(
                children: [
                  const Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.check_circle, color: AppTheme.okGreen),
                      SizedBox(width: 8),
                      Text(
                        'All your picks are complete!',
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Color(0xFF86E2A3)),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  const Text(
                    'Walk to the loading dock to hand off cartons.',
                    style: TextStyle(fontSize: 13, color: Color(0xFFB0BDB7)),
                  ),
                  const SizedBox(height: 14),
                  ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppTheme.okGreen,
                      foregroundColor: Colors.white,
                    ),
                    onPressed: _isHandoffLoading ? null : _handleHandoff,
                    child: _isHandoffLoading
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                          )
                        : const Text('Confirm Handoff at Loading Dock'),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildLineCard(OrderLineDto line, OrderDetailDto order) {
    final isPicked = line.status == 'Picked';
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: const Color(0xFF1B2420),
                    borderRadius: BorderRadius.circular(6),
                    border: Border.all(color: AppTheme.amber.withValues(alpha: 0.5)),
                  ),
                  child: Text(
                    'Seq #${line.pickSequence} · Shelf ${line.shelfCode}',
                    style: const TextStyle(
                      fontFamily: 'monospace',
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
                      color: AppTheme.amber,
                    ),
                  ),
                ),
                const Spacer(),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: isPicked ? AppTheme.okGreen.withValues(alpha: 0.2) : const Color(0xFF384B43),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    line.status,
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                      color: isPicked ? const Color(0xFF86E2A3) : const Color(0xFFFFD56B),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Text(
              line.productName,
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppTheme.paper),
            ),
            const SizedBox(height: 4),
            Text(
              'Quantity to pick: ${line.qtyCartons} carton${line.qtyCartons > 1 ? 's' : ''}',
              style: const TextStyle(fontSize: 14, color: Color(0xFFB0BDB7)),
            ),
            if (line.pickedAt != null)
              Padding(
                padding: const EdgeInsets.only(top: 4.0),
                child: Text(
                  'Picked at: ${DateFormat('HH:mm:ss').format(line.pickedAt!.toLocal())}',
                  style: const TextStyle(fontSize: 12, color: Color(0xFF86E2A3)),
                ),
              ),
            if (line.handedOffAt != null)
              Padding(
                padding: const EdgeInsets.only(top: 2.0),
                child: Text(
                  'Handed off at: ${DateFormat('HH:mm:ss').format(line.handedOffAt!.toLocal())}',
                  style: const TextStyle(fontSize: 12, color: Color(0xFF86E2A3)),
                ),
              ),
            if (!isPicked) ...[
              const SizedBox(height: 12),
              ElevatedButton.icon(
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('Scan & Pick Line'),
                onPressed: () async {
                  final picked = await Navigator.of(context).push<bool>(
                    MaterialPageRoute(
                      builder: (_) => PickScreen(
                        orderId: order.id,
                        line: line,
                        apiService: widget.apiService,
                        sessionManager: widget.sessionManager,
                      ),
                    ),
                  );
                  if (picked == true) {
                    _fetchOrder();
                  }
                },
              ),
            ],
          ],
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
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(6)),
      child: Text(status, style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: text)),
    );
  }
}
