import 'package:shared_preferences/shared_preferences.dart';
import 'package:flutter/material.dart';
import 'package:flutter/scheduler.dart' show timeDilation;
import 'package:flutter_login/flutter_login.dart';
import 'package:warehouse/api/api_client.dart';
import 'dart:convert';

import 'constants.dart';
import 'custom_route.dart';
import 'dashboard_screen.dart';

class LoginScreen extends StatelessWidget {
  static const routeName = '/auth';
  final ApiClient _apiClient = ApiClient();

  LoginScreen({Key? key}) : super(key: key);
  Duration get loginTime => Duration(milliseconds: timeDilation.ceil() * 1000);
  Future<String?> _loginUser(LoginData data) async {
    dynamic res = await _apiClient.login(
      data.name,
      data.password,
    );
    final prefs = await SharedPreferences.getInstance();
    if (res.statusCode == 200) {
      await prefs.setString('UserInfo', jsonEncode(res.data));
      return Future.delayed(loginTime).then((_) {
        return null;
      });
    } else {
      return Future.delayed(loginTime).then((_) {
        return res.data;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return FlutterLogin(
        theme: LoginTheme(
            pageColorLight: Color.fromARGB(255, 157, 220, 168),
            titleStyle: TextStyle(color: Colors.black)),
        userType: LoginUserType.name,
        title: Constants.appName,
        logo: const AssetImage('assets/images/logo.png'),
        logoTag: Constants.logoTag,
        titleTag: Constants.titleTag,
        navigateBackAfterRecovery: true,
        loginAfterSignUp: false,
        initialAuthMode: AuthMode.login,
        userValidator: (value) {
          if (value!.isEmpty) {
            return 'Password is empty';
          }
          return null;
        },
        passwordValidator: (value) {
          if (value!.isEmpty) {
            return 'Password is empty';
          }
          return null;
        },
        onLogin: (loginData) async {
          return await _loginUser(loginData);
        },
        onSubmitAnimationCompleted: () {
          Navigator.of(context).pushReplacement(FadePageRoute(
            builder: (context) => const DashboardScreen(),
          ));
        },
        onRecoverPassword: (name) {
          debugPrint('Recover password info');
          debugPrint('Name: $name');
        });
  }
}
