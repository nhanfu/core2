import 'package:flutter/material.dart';
import 'package:flutter/scheduler.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:warehouse/login_screen.dart';
import 'package:warehouse/require_detail.dart';
import 'api/api_client.dart';
import 'custom_route.dart';
import 'layout.dart';
import 'transition_route_observer.dart';
import 'widgets/fade_in.dart';
import 'constants.dart';

var isLogin = false;
var token = {};

class DashboardScreen extends StatefulWidget {
  static const routeName = '/dashboard';

  const DashboardScreen({Key? key}) : super(key: key);
  @override
  _DashboardScreenState createState() => _DashboardScreenState();
}

class HexColor extends Color {
  static int _getColorFromHex(String hexColor) {
    hexColor = hexColor.toUpperCase().replaceAll("#", "");
    if (hexColor.length == 6) {
      hexColor = "FF" + hexColor;
    }
    return int.parse(hexColor, radix: 16);
  }

  HexColor(final String hexColor) : super(_getColorFromHex(hexColor));
}

class _DashboardScreenState extends State<DashboardScreen>
    with TickerProviderStateMixin, TransitionRouteAware {
  final ApiClient _apiClient = ApiClient();
  dynamic _user = null;
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
  late TabController _tabController;
  @override
  void initState() {
    _apiClient.getUser().then((res) => {
          setState(() {
            _user = res;
          })
        });
    super.initState();
    _loadingController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1250),
    );
    _tabController = TabController(length: 2, vsync: this);
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
        actions: <Widget>[
          FadeIn(
            controller: _loadingController,
            offset: .3,
            curve: headerAniInterval,
            fadeDirection: FadeDirection.endToStart,
            child: signOutBtn,
          ),
        ],
        title: title,
        iconTheme: IconThemeData(color: HexColor("#38932e")),
        backgroundColor: const Color(0xFFFFFFFF),
        elevation: 0);
  }

  Future openDetail(int id) async {
    dynamic res = await _apiClient.getDetail(id);
    Navigator.of(context).pushReplacement(FadePageRoute(
      builder: (context) => DetailScreen(res),
    ));
  }

  Future<bool> _onWillPop() async {
    return (await showDialog(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('Are you sure?'),
            content: const Text('Do you want to exit an App'),
            actions: <Widget>[
              TextButton(
                onPressed: () => Navigator.of(context).pop(false),
                child: const Text('No'),
              ),
              TextButton(
                onPressed: () => Navigator.of(context).pop(true),
                child: const Text('Yes'),
              ),
            ],
          ),
        )) ??
        false;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return WillPopScope(
        onWillPop: () => _onWillPop(),
        child: SafeArea(
          child: Scaffold(
            appBar: _buildAppBar(theme),
            drawer: Drawer(
              child: LayOutDrawer(_user),
            ),
            body: TabBarView(
                controller: _tabController,
                children: <Widget>[TabRequire(4), TabRequire(1)]),
            bottomNavigationBar: TabBar(
              labelColor: Colors.amber[800],
              unselectedLabelColor: HexColor("#38932e"),
              indicatorSize: TabBarIndicatorSize.tab,
              indicatorColor: HexColor("#38932e"),
              controller: _tabController,
              tabs: const <Widget>[
                Tab(
                  text: "Pending",
                  icon: Icon(Icons.circle_notifications, color: Color.fromARGB(255, 73, 213, 65)),
                ),
                Tab(
                  text: "Approved",
                  icon: Icon(Icons.check_circle, color: Color.fromARGB(255, 73, 213, 65)),
                )
              ],
            ),
          ),
        ));
  }
}
