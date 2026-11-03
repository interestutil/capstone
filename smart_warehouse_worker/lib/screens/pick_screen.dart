import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../models/models.dart';
import '../services/api_service.dart';
import '../services/session_manager.dart';
import '../theme/app_theme.dart';

class PickScreen extends StatefulWidget {
  final int orderId;
  final OrderLineDto line;
  final ApiService apiService;
  final SessionManager sessionManager;

  const PickScreen({
    super.key,
    required this.orderId,
    required this.line,
    required this.apiService,
    required this.sessionManager,
  });

  @override
  State<PickScreen> createState() => _PickScreenState();
}

class _PickScreenState extends State<PickScreen> {
  final _barcodeController = TextEditingController();
  bool _isLoading = false;
  String? _errorMessage;
  bool _showScanner = false;

  @override
  void dispose() {
    _barcodeController.dispose();
    super.dispose();
  }

  Future<void> _handleConfirmPick() async {
    final barcode = _barcodeController.text.trim();

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      await widget.apiService.confirmPick(
        widget.orderId,
        widget.line.id,
        barcode.isNotEmpty ? barcode : null,
      );

      // Section 6.6: Establish the shelf just picked from as the new anchor
      await widget.sessionManager.setLastAnchorShelfId(widget.line.shelfId);

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: AppTheme.okGreen,
          content: Text('Pick confirmed for ${widget.line.productName}!'),
        ),
      );
      Navigator.of(context).pop(true);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString().replaceFirst('Exception: ', '');
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Pick · Shelf ${widget.line.shelfCode}'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: AppTheme.panel,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppTheme.amber, width: 1.5),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.shelves, color: AppTheme.amber, size: 28),
                      const SizedBox(width: 10),
                      Text(
                        widget.line.shelfCode,
                        style: const TextStyle(
                          fontSize: 26,
                          fontWeight: FontWeight.bold,
                          fontFamily: 'monospace',
                          color: AppTheme.amber,
                        ),
                      ),
                      const Spacer(),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppTheme.amber.withValues(alpha: 0.2),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          '${widget.line.qtyCartons} Cartons',
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.amber,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Text(
                    widget.line.productName,
                    style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: AppTheme.paper),
                  ),
                  const SizedBox(height: 6),
                  const Text(
                    'Walk to this shelf. The shelf LED lights up automatically.',
                    style: TextStyle(color: Color(0xFFB0BDB7), fontSize: 13),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),
            if (_errorMessage != null)
              Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                  color: AppTheme.rust.withValues(alpha: 0.15),
                  border: Border.all(color: AppTheme.rust),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  _errorMessage!,
                  style: const TextStyle(color: Color(0xFFFF8B7A), fontWeight: FontWeight.w500),
                ),
              ),
            if (_showScanner) ...[
              Container(
                height: 240,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: AppTheme.amber),
                ),
                clipBehavior: Clip.antiAlias,
                child: MobileScanner(
                  onDetect: (capture) {
                    final List<Barcode> barcodes = capture.barcodes;
                    for (final barcode in barcodes) {
                      if (barcode.rawValue != null) {
                        setState(() {
                          _barcodeController.text = barcode.rawValue!;
                          _showScanner = false;
                        });
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
              const SizedBox(height: 16),
            ] else ...[
              OutlinedButton.icon(
                icon: const Icon(Icons.camera_alt),
                label: const Text('Scan with Camera'),
                onPressed: () => setState(() => _showScanner = true),
              ),
              const SizedBox(height: 16),
            ],
            TextField(
              controller: _barcodeController,
              decoration: InputDecoration(
                labelText: 'Product / Carton Barcode',
                hintText: 'e.g. C-1001 or P-1001',
                suffixIcon: IconButton(
                  icon: const Icon(Icons.clear),
                  onPressed: () => _barcodeController.clear(),
                ),
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: _isLoading ? null : _handleConfirmPick,
              child: _isLoading
                  ? const SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(strokeWidth: 2.5, color: AppTheme.ink),
                    )
                  : const Text('Confirm Pick & Deduct Stock'),
            ),
          ],
        ),
      ),
    );
  }
}
