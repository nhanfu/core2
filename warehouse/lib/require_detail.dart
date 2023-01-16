import 'dart:ffi';

import 'package:flutter/material.dart';
import 'package:flutter/scheduler.dart';
import 'package:flutter/services.dart';
import 'package:flutter_barcode_scanner/flutter_barcode_scanner.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'api/api_client.dart';
import 'custom_route.dart';
import 'dashboard_screen.dart';
import 'transition_route_observer.dart';
import 'widgets/fade_in.dart';
import 'constants.dart';

var isLogin = false;
var token = {};

class DetailScreen extends StatefulWidget {
  static const routeName = '/detail';
  final dynamic record;
  DetailScreen(this.record);
  @override
  _DetailScreenScreenState createState() => _DetailScreenScreenState();
}

class _DetailScreenScreenState extends State<DetailScreen>
    with SingleTickerProviderStateMixin, TransitionRouteAware {
  final ApiClient _apiClient = ApiClient();
  String _scanBarcode = 'Unknown';
  Duration get loginTime => Duration(milliseconds: timeDilation.ceil() * 1000);

  Future<bool> _goToLogin(BuildContext context) {
    SharedPreferences.getInstance().then((value) {
      value.remove('UserInfo');
    }).catchError((error) {});
    return Navigator.of(context)
        .pushReplacementNamed('/auth')
        .then((_) => false);
  }

  final routeObserver = TransitionRouteObserver<PageRoute?>();
  static const headerAniInterval = Interval(.1, .3, curve: Curves.easeOut);
  AnimationController? _loadingController;

  @override
  void initState() {
    super.initState();
    _loadingController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1250),
    );
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    routeObserver.subscribe(
        this, ModalRoute.of(context) as PageRoute<dynamic>?);
  }

  @override
  void dispose() {
    routeObserver.unsubscribe(this);
    _loadingController!.dispose();
    super.dispose();
  }

  @override
  void didPushAfterTransition() => _loadingController!.forward();
  AppBar _buildAppBar(ThemeData theme) {
    final menuBtn = IconButton(
      color: HexColor("#38932e"),
      icon: const Icon(FontAwesomeIcons.arrowLeft),
      onPressed: () {
        Navigator.of(context).pushReplacement(FadePageRoute(
          builder: (context) => const DashboardScreen(),
        ));
      },
    );
    final signOutBtn = IconButton(
      icon: const Icon(FontAwesomeIcons.signOutAlt),
      color: HexColor("#38932e"),
      onPressed: () => _goToLogin(context),
    );
    final title = Center(
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: <Widget>[
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 8.0),
            child: Hero(
              tag: Constants.logoTag,
              child: Image.asset(
                'assets/images/logo.png',
                filterQuality: FilterQuality.high,
                height: 30,
              ),
            ),
          ),
          const SizedBox(width: 20),
        ],
      ),
    );

    return AppBar(
      leading: FadeIn(
        controller: _loadingController,
        offset: .3,
        curve: headerAniInterval,
        fadeDirection: FadeDirection.startToEnd,
        child: menuBtn,
      ),
      actions: <Widget>[
        FadeIn(
          controller: _loadingController,
          offset: .3,
          curve: headerAniInterval,
          fadeDirection: FadeDirection.endToStart,
          child: signOutBtn,
        )
      ],
      title: title,
      backgroundColor: theme.primaryColor.withOpacity(.1),
      elevation: 0,
    );
  }

  Future scanBarcodeNormal(dynamic res) async {
    String barcodeScanRes = '';
    try {
      barcodeScanRes = await FlutterBarcodeScanner.scanBarcode(
          '#ff6666', 'Cancel', true, ScanMode.BARCODE);
    } on PlatformException {
      barcodeScanRes = '-1';
    }
    if (barcodeScanRes != "" && barcodeScanRes != "-1") {
      final userRes = await _apiClient.checkRs(res, barcodeScanRes);
      if (userRes != null) {
        final rs = await _apiClient.UpdateRequire(userRes['Id'], res['Id']);
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text("Scan success " + barcodeScanRes),
          backgroundColor: Colors.green,
        ));
      } else {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
          content: Text("No find lotno", style: TextStyle(color: Colors.black)),
          backgroundColor: Colors.yellow,
        ));
      }
    } else {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
        content: Text("No find lotno", style: TextStyle(color: Colors.black)),
        backgroundColor: Colors.yellow,
      ));
    }
    setState(() {
      _scanBarcode = barcodeScanRes;
    });
  }

  Future<List<dynamic>> _fetchDetais() async {
    final userRes = await _apiClient.getDetail(widget.record['Id']);
    return userRes;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return WillPopScope(
        onWillPop: () async => false,
        child: SafeArea(
          child: Scaffold(
            appBar: _buildAppBar(theme),
            floatingActionButton: widget.record['StatusId'] != 4
                ? null
                : FloatingActionButton(
                    onPressed: () async =>
                        {await _apiClient.approve(widget.record)},
                    backgroundColor: Colors.green,
                    child: const Icon(Icons.thumb_up),
                  ),
            body: Column(children: <Widget>[
              Expanded(
                child: ShaderMask(
                  shaderCallback: (Rect bounds) {
                    return const LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      tileMode: TileMode.clamp,
                      colors: <Color>[
                        Colors.white,
                        Colors.white,
                        Colors.white,
                        Colors.white,
                      ],
                    ).createShader(bounds);
                  },
                  child: Center(
                    child: FutureBuilder<List<dynamic>>(
                      future: _fetchDetais(),
                      builder: (context, snapshot) {
                        if (snapshot.hasData) {
                          return Padding(
                            padding: const EdgeInsets.all(8.0),
                            child: ListView.builder(
                              itemCount: snapshot.data?.length,
                              itemBuilder: (context, index) {
                                var material =
                                    snapshot.data?[index]['MaterialName'];
                                var product =
                                    snapshot.data?[index]['ProductName'];
                                var lotno =
                                    snapshot.data?[index]['LotNo'] ?? "";
                                var supplierName =
                                    snapshot.data?[index]['SupplierName'];
                                return Container(
                                    decoration: BoxDecoration(
                                      borderRadius: const BorderRadius.all(
                                          Radius.circular(16.0)),
                                      boxShadow: <BoxShadow>[
                                        BoxShadow(
                                          color: Colors.grey.withOpacity(0.6),
                                          offset: const Offset(4, 4),
                                          blurRadius: 16,
                                        ),
                                      ],
                                    ),
                                    child: Card(
                                      shape: RoundedRectangleBorder(
                                        side: BorderSide(
                                          color: Colors.green.shade300,
                                        ),
                                        borderRadius:
                                            BorderRadius.circular(15.0),
                                      ),
                                      child: ListTile(
                                        onTap: () => scanBarcodeNormal(
                                            snapshot.data?[index]),
                                        subtitle: Text(
                                          'Material: $material\nSupplier: $supplierName\nProduct: $product',
                                          style: const TextStyle(
                                              color: Colors.black),
                                        ),
                                        trailing: Text(
                                          "LotNo: $lotno",
                                          style: const TextStyle(
                                              color: Colors.black),
                                        ),
                                      ),
                                    ));
                              },
                            ),
                          );
                        }
                        return const CircularProgressIndicator();
                      },
                    ),
                  ),
                ),
              ),
            ]),
          ),
        ));
  }
}
