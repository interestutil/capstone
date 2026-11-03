import 'package:flutter_test/flutter_test.dart';
import 'package:smart_warehouse_worker/models/models.dart';

void main() {
  test('ProductDto deserialization test', () {
    final json = {
      'id': 1,
      'name': 'Laundry detergent',
      'productTypeId': 1,
      'productTypeName': 'Cleaning supplies',
      'pieceBarcode': 'P-1001',
      'cartonBarcode': 'C-1001',
    };
    final product = ProductDto.fromJson(json);
    expect(product.id, 1);
    expect(product.name, 'Laundry detergent');
    expect(product.pieceBarcode, 'P-1001');
    expect(product.cartonBarcode, 'C-1001');
  });

  test('ShelfDto availability test', () {
    final json = {
      'id': 1,
      'code': 'R1-L1',
      'rack': 1,
      'level': 1,
      'productTypeId': 1,
      'productTypeName': 'Cleaning supplies',
      'productId': 1,
      'productName': 'Laundry detergent',
      'maxCapacity': 24,
      'onHand': 20,
      'committed': 2,
      'available': 18,
      'isEmpty': false,
      'isFull': false,
      'ledDeviceId': 'esp32-1',
      'ledIndex': 0,
    };
    final shelf = ShelfDto.fromJson(json);
    expect(shelf.available, 18);
    expect(shelf.isEmpty, false);
    expect(shelf.code, 'R1-L1');
  });
}
