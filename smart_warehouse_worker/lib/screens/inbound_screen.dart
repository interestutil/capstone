import 'dart:async';
import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../models/models.dart';
import '../services/api_service.dart';
import '../services/session_manager.dart';
import '../theme/app_theme.dart';

class InboundScreen extends StatefulWidget {
  final ApiService apiService;
  final SessionManager sessionManager;

  const InboundScreen({
    super.key,
    required this.apiService,
    required this.sessionManager,
  });

  @override
  State<InboundScreen> createState() => _InboundScreenState();
}

class _InboundScreenState extends State<InboundScreen> {
  final _barcodeController = TextEditingController();
  final _qtyController = TextEditingController(text: '1');
  final _voucherController = TextEditingController();

  List<InboundDto> _records = [];
  bool _isLoading = true;
  bool _isSubmitting = false;
  bool _showScanner = false;
  String? _errorMessage;
  ProductDto? _resolvedProduct;
  bool _isLookingUpBarcode = false;
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    _fetchRecords();
    // Auto-poll inbound records so admin shelf assignments appear in real-time
    _pollingTimer = Timer.periodic(const Duration(seconds: 4), (_) => _fetchRecords(silent: true));
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    _barcodeController.dispose();
    _qtyController.dispose();
    _voucherController.dispose();
    super.dispose();
  }

  Future<void> _fetchRecords({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _errorMessage = null;
      });
    }

    try {
      final records = await widget.apiService.getInbound();
      if (!mounted) return;
      setState(() {
        _records = records;
        _isLoading = false;
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

  // Live barcode lookup before inbound submit
  Future<void> _lookupBarcode(String code) async {
    final clean = code.trim();
    if (clean.isEmpty) {
      setState(() => _resolvedProduct = null);
      return;
    }

    setState(() => _isLookingUpBarcode = true);
    try {
      final product = await widget.apiService.getProductByBarcode(clean);
      if (!mounted) return;
      setState(() {
        _resolvedProduct = product;
        _isLookingUpBarcode = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _resolvedProduct = null;
        _isLookingUpBarcode = false;
      });
    }
  }

  Future<void> _handleSubmitInbound() async {
    final barcode = _barcodeController.text.trim();
    final qty = int.tryParse(_qtyController.text.trim()) ?? 0;
    final voucher = _voucherController.text.trim();

    if (barcode.isEmpty || qty <= 0 || voucher.isEmpty) {
      setState(() => _errorMessage = 'Please provide a valid barcode, positive cartons, and voucher number.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await widget.apiService.createInbound(barcode, qty, voucher);
      _barcodeController.clear();
      _voucherController.clear();
      _qtyController.text = '1';
      _resolvedProduct = null;
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          backgroundColor: AppTheme.okGreen,
          content: Text('Inbound record created! Waiting for admin shelf assignment.'),
        ),
      );
      _fetchRecords();
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = e.toString().replaceFirst('Exception: ', '');
        });
      }
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  Future<void> _handleConfirmPlacement(int id) async {
    try {
      await widget.apiService.placeInbound(id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          backgroundColor: AppTheme.okGreen,
          content: Text('Stock placed on shelf! OnHand inventory updated.'),
        ),
      );
      _fetchRecords();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: AppTheme.rust,
          content: Text(e.toString().replaceFirst('Exception: ', '')),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Inbound Registration'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => _fetchRecords(),
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
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Text(
                  'REGISTER INCOMING STOCK',
                  style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold, letterSpacing: 1.1, color: AppTheme.amber),
                ),
                const SizedBox(height: 6),
                const Text(
                  'Scan the carton or piece barcode and enter the voucher number.',
                  style: TextStyle(fontSize: 13, color: Color(0xFFB0BDB7)),
                ),
                const SizedBox(height: 16),
                if (_showScanner) ...[
                  Container(
                    height: 220,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: AppTheme.amber),
                    ),
                    clipBehavior: Clip.antiAlias,
                    child: MobileScanner(
                      onDetect: (capture) {
                        for (final barcode in capture.barcodes) {
                          if (barcode.rawValue != null) {
                            final raw = barcode.rawValue!;
                            setState(() {
                              _barcodeController.text = raw;
                              _showScanner = false;
                            });
                            _lookupBarcode(raw);
                            break;
                          }
                        }
                      },
                    ),
                  ),
                  const SizedBox(height: 8),
                  OutlinedButton(
                    onPressed: () => setState(() => _showScanner = false),
                    child: const Text('Close Camera'),
                  ),
                  const SizedBox(height: 12),
                ] else ...[
                  OutlinedButton.icon(
                    icon: const Icon(Icons.camera_alt),
                    label: const Text('Scan Product Barcode'),
                    onPressed: () => setState(() => _showScanner = true),
                  ),
                  const SizedBox(height: 12),
                ],
                TextField(
                  controller: _barcodeController,
                  decoration: InputDecoration(
                    labelText: 'Product Barcode',
                    hintText: 'e.g. C-1001 or P-1001',
                    suffixIcon: _isLookingUpBarcode
                        ? const Padding(
                            padding: EdgeInsets.all(12.0),
                            child: SizedBox(
                              width: 16,
                              height: 16,
                              child: CircularProgressIndicator(strokeWidth: 2, color: AppTheme.amber),
                            ),
                          )
                        : null,
                  ),
                  onChanged: (val) => _lookupBarcode(val),
                ),
                if (_resolvedProduct != null) ...[
                  const SizedBox(height: 8),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    decoration: BoxDecoration(
                      color: AppTheme.okGreen.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: AppTheme.okGreen),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.check_circle, color: AppTheme.okGreen, size: 18),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            'Resolved: ${_resolvedProduct!.name} (${_resolvedProduct!.productTypeName})',
                            style: const TextStyle(color: Color(0xFF86E2A3), fontWeight: FontWeight.bold),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                const SizedBox(height: 12),
                TextField(
                  controller: _qtyController,
                  decoration: const InputDecoration(labelText: 'Quantity (Cartons)'),
                  keyboardType: TextInputType.number,
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _voucherController,
                  decoration: const InputDecoration(labelText: 'Receipt Voucher Number', hintText: 'e.g. V-101'),
                ),
                const SizedBox(height: 16),
                ElevatedButton(
                  onPressed: _isSubmitting ? null : _handleSubmitInbound,
                  child: _isSubmitting
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(strokeWidth: 2, color: AppTheme.ink),
                        )
                      : const Text('Submit Inbound Batch'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),
          const Text(
            'INBOUND BATCHES & PLACEMENT',
            style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, letterSpacing: 1.1, color: Color(0xFF9EABA4)),
          ),
          const SizedBox(height: 8),
          if (_errorMessage != null)
            Container(
              padding: const EdgeInsets.all(12),
              margin: const EdgeInsets.only(bottom: 12),
              decoration: BoxDecoration(
                color: AppTheme.rust.withValues(alpha: 0.15),
                border: Border.all(color: AppTheme.rust),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(_errorMessage!, style: const TextStyle(color: Color(0xFFFF8B7A))),
            ),
          if (_isLoading && _records.isEmpty)
            const Center(
              child: Padding(
                padding: EdgeInsets.all(24.0),
                child: CircularProgressIndicator(color: AppTheme.amber),
              ),
            )
          else if (_records.isEmpty)
            const Padding(
              padding: EdgeInsets.all(24.0),
              child: Center(
                child: Text('No inbound records yet.', style: TextStyle(color: Color(0xFF9EABA4))),
              ),
            )
          else
            ..._records.map((rec) => _buildInboundCard(rec)),
        ],
      ),
    );
  }

  Widget _buildInboundCard(InboundDto rec) {
    final isShelfAssigned = rec.status == 'ShelfAssigned';
    final isPlaced = rec.status == 'Placed';

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Voucher: ${rec.voucherNumber}',
                  style: const TextStyle(fontFamily: 'monospace', fontWeight: FontWeight.bold, fontSize: 15),
                ),
                _buildStatusBadge(rec.status),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              rec.productName,
              style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: AppTheme.paper),
            ),
            const SizedBox(height: 4),
            Text(
              '${rec.qtyCartons} Cartons · Registered by ${rec.createdByWorkerName}',
              style: const TextStyle(fontSize: 13, color: Color(0xFFB0BDB7)),
            ),
            if (rec.targetShelfCode != null) ...[
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                decoration: BoxDecoration(
                  color: const Color(0xFF1B2420),
                  borderRadius: BorderRadius.circular(6),
                  border: Border.all(color: AppTheme.amber.withValues(alpha: 0.5)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.shelves, color: AppTheme.amber, size: 18),
                    const SizedBox(width: 8),
                    Text(
                      'Assigned Shelf: ${rec.targetShelfCode}',
                      style: const TextStyle(
                        fontFamily: 'monospace',
                        fontWeight: FontWeight.bold,
                        color: AppTheme.amber,
                      ),
                    ),
                  ],
                ),
              ),
            ],
            if (isShelfAssigned) ...[
              const SizedBox(height: 12),
              ElevatedButton.icon(
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.okGreen,
                  foregroundColor: Colors.white,
                ),
                icon: const Icon(Icons.check),
                label: Text('Confirm Placement on ${rec.targetShelfCode}'),
                onPressed: () => _handleConfirmPlacement(rec.id),
              ),
            ] else if (!isPlaced) ...[
              const SizedBox(height: 8),
              const Row(
                children: [
                  SizedBox(
                    width: 14,
                    height: 14,
                    child: CircularProgressIndicator(strokeWidth: 2, color: AppTheme.amber),
                  ),
                  SizedBox(width: 8),
                  Text(
                    'Waiting for admin to assign shelf...',
                    style: TextStyle(fontSize: 12, color: Color(0xFFFFD56B)),
                  ),
                ],
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
    if (status == 'Received') {
      bg = const Color(0xFF3E3C26);
      text = const Color(0xFFFFD56B);
    } else if (status == 'ShelfAssigned') {
      bg = const Color(0xFF1E3F47);
      text = const Color(0xFF6BD0FF);
    } else if (status == 'Placed') {
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
