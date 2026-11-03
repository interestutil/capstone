import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import '../models/models.dart';
import '../services/api_service.dart';
import '../services/session_manager.dart';
import '../theme/app_theme.dart';

class FloorMapScreen extends StatefulWidget {
  final int orderId;
  final ApiService apiService;
  final SessionManager sessionManager;

  const FloorMapScreen({
    super.key,
    required this.orderId,
    required this.apiService,
    required this.sessionManager,
  });

  @override
  State<FloorMapScreen> createState() => _FloorMapScreenState();
}

class _FloorMapScreenState extends State<FloorMapScreen> {
  List<ShelfDto> _shelves = [];
  MapInfoDto? _mapInfo;
  RouteDto? _route;
  ShelfDto? _selectedAnchor;
  bool _isLoading = true;
  bool _isRouteLoading = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadInitialData();
  }

  Future<void> _loadInitialData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final shelves = await widget.apiService.getShelves();
      final mapInfo = await widget.apiService.getMap();

      if (!mounted) return;

      // Section 6.6: Default anchor shelf to the one just picked from, or the first shelf
      final lastAnchorId = widget.sessionManager.lastAnchorShelfId;
      ShelfDto? anchor;
      if (lastAnchorId != null) {
        anchor = shelves.cast<ShelfDto?>().firstWhere(
              (s) => s?.id == lastAnchorId,
              orElse: () => null,
            );
      }
      anchor ??= shelves.isNotEmpty ? shelves.first : null;

      setState(() {
        _shelves = shelves;
        _mapInfo = mapInfo;
        _selectedAnchor = anchor;
        _isLoading = false;
      });

      if (anchor != null) {
        _loadRoute(anchor.id);
      }
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString().replaceFirst('Exception: ', '');
        _isLoading = false;
      });
    }
  }

  Future<void> _loadRoute(int anchorShelfId) async {
    setState(() {
      _isRouteLoading = true;
      _errorMessage = null;
    });

    try {
      final route = await widget.apiService.getRoute(widget.orderId, anchorShelfId);
      // Remember anchor
      await widget.sessionManager.setLastAnchorShelfId(anchorShelfId);
      if (!mounted) return;
      setState(() {
        _route = route;
        _isRouteLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString().replaceFirst('Exception: ', '');
        _isRouteLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Floor Map & Route')),
        body: const Center(child: CircularProgressIndicator(color: AppTheme.amber)),
      );
    }

    final nextLeg = _route?.legs.firstOrNull;

    return Scaffold(
      appBar: AppBar(
        title: Text('Route · Order #${widget.orderId}'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
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
                  const Text(
                    'Where are you standing?',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.paper),
                  ),
                  const SizedBox(height: 8),
                  DropdownButtonFormField<ShelfDto>(
                    initialValue: _selectedAnchor,
                    decoration: const InputDecoration(labelText: 'Starting Anchor Shelf'),
                    items: _shelves.map((shelf) {
                      final isLastPicked = shelf.id == widget.sessionManager.lastAnchorShelfId;
                      return DropdownMenuItem<ShelfDto>(
                        value: shelf,
                        child: Text(
                          isLastPicked ? '${shelf.code} (Last picked shelf)' : shelf.code,
                          style: TextStyle(
                            fontFamily: 'monospace',
                            color: isLastPicked ? AppTheme.amber : AppTheme.paper,
                            fontWeight: isLastPicked ? FontWeight.bold : FontWeight.normal,
                          ),
                        ),
                      );
                    }).toList(),
                    onChanged: (newAnchor) {
                      if (newAnchor != null) {
                        setState(() => _selectedAnchor = newAnchor);
                        _loadRoute(newAnchor.id);
                      }
                    },
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
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
            if (nextLeg != null)
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                decoration: BoxDecoration(
                  color: nextLeg.kind == 'dock'
                      ? AppTheme.rust.withValues(alpha: 0.2)
                      : AppTheme.amber.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(
                    color: nextLeg.kind == 'dock' ? AppTheme.rust : AppTheme.amber,
                  ),
                ),
                child: Row(
                  children: [
                    Icon(
                      nextLeg.kind == 'dock' ? Icons.local_shipping : Icons.navigation,
                      color: nextLeg.kind == 'dock' ? AppTheme.rust : AppTheme.amber,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            nextLeg.kind == 'dock'
                                ? 'Next stop: Loading Dock'
                                : 'Next stop: Shelf stop (Leg #1)',
                            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: AppTheme.paper),
                          ),
                          Text(
                            'Leg distance: ${nextLeg.distance} px',
                            style: const TextStyle(fontSize: 12, color: Color(0xFFB0BDB7)),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            const SizedBox(height: 16),
            if (_mapInfo != null)
              Container(
                height: 280,
                decoration: BoxDecoration(
                  color: const Color(0xFFE8E2D6),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFF2B3531), width: 2),
                ),
                clipBehavior: Clip.antiAlias,
                child: Stack(
                  fit: StackFit.expand,
                  children: [
                    SvgPicture.network(
                      _mapInfo!.imageUrl,
                      fit: BoxFit.fill,
                      placeholderBuilder: (_) => const Center(
                        child: CircularProgressIndicator(color: AppTheme.amber),
                      ),
                    ),
                    if (_route != null)
                      CustomPaint(
                        painter: RoutePainter(
                          route: _route!,
                          mapWidth: _mapInfo!.width.toDouble(),
                          mapHeight: _mapInfo!.height.toDouble(),
                        ),
                      ),
                    if (_isRouteLoading)
                      Container(
                        color: Colors.black38,
                        child: const Center(
                          child: CircularProgressIndicator(color: AppTheme.amber),
                        ),
                      ),
                  ],
                ),
              ),
            const SizedBox(height: 16),
            if (_route != null) ...[
              const Text(
                'ROUTE LEGS (Rack-then-level order)',
                style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, letterSpacing: 1.1, color: Color(0xFF9EABA4)),
              ),
              const SizedBox(height: 8),
              ..._route!.legs.map((leg) => Card(
                    child: ListTile(
                      leading: CircleAvatar(
                        backgroundColor: leg.sequence == 1 ? AppTheme.amber : const Color(0xFF384B43),
                        foregroundColor: leg.sequence == 1 ? AppTheme.ink : AppTheme.paper,
                        child: Text('${leg.sequence}'),
                      ),
                      title: Text(
                        leg.kind == 'dock' ? 'Loading Dock (Handoff)' : 'Pick Shelf ${leg.shelfId ?? ""}',
                        style: const TextStyle(fontWeight: FontWeight.bold),
                      ),
                      subtitle: Text('Distance: ${leg.distance} px · ${leg.points.length} waypoints'),
                      trailing: Icon(
                        leg.kind == 'dock' ? Icons.flag : Icons.inventory,
                        color: leg.kind == 'dock' ? AppTheme.rust : AppTheme.amber,
                      ),
                    ),
                  )),
            ],
          ],
        ),
      ),
    );
  }
}

class RoutePainter extends CustomPainter {
  final RouteDto route;
  final double mapWidth;
  final double mapHeight;

  RoutePainter({
    required this.route,
    required this.mapWidth,
    required this.mapHeight,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final scaleX = size.width / mapWidth;
    final scaleY = size.height / mapHeight;

    for (int i = 0; i < route.legs.length; i++) {
      final leg = route.legs[i];
      if (leg.points.length < 2) continue;

      final isFirst = i == 0;
      final paint = Paint()
        ..color = isFirst ? AppTheme.amber : AppTheme.routePale
        ..strokeWidth = isFirst ? 6.0 : 4.0
        ..style = PaintingStyle.stroke
        ..strokeCap = StrokeCap.round
        ..strokeJoin = StrokeJoin.round;

      final path = Path();
      path.moveTo(leg.points.first.x * scaleX, leg.points.first.y * scaleY);
      for (final pt in leg.points.skip(1)) {
        path.lineTo(pt.x * scaleX, pt.y * scaleY);
      }
      canvas.drawPath(path, paint);

      // Draw destination waypoint circle
      final dest = leg.points.last;
      final circlePaint = Paint()
        ..color = AppTheme.rust
        ..style = PaintingStyle.fill;
      canvas.drawCircle(Offset(dest.x * scaleX, dest.y * scaleY), 6.5, circlePaint);
    }
  }

  @override
  bool shouldRepaint(covariant RoutePainter oldDelegate) {
    return oldDelegate.route != route ||
        oldDelegate.mapWidth != mapWidth ||
        oldDelegate.mapHeight != mapHeight;
  }
}
