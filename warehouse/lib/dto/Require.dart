class Require {
  final int? id;
  final String? freightStateText;

  Require({this.id, this.freightStateText});

  factory Require.fromJson(Map<String, dynamic> json) {
    return Require(
      id: json['id'],
      freightStateText: json['FreightStateText'],
    );
  }
}
