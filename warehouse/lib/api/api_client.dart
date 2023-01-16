import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../appseting.dart';

class ApiClient {
  final Dio _dio = Dio();
  dynamic _token = {};

  Future<dynamic> getUser() async {
    final SharedPreferences _sharedPreferences =
        await SharedPreferences.getInstance();
    _token = jsonDecode(_sharedPreferences.getString('UserInfo').toString());
    if (_token == null) {
      return null;
    }
    return _token;
  }

  get context => null;

  Future<dynamic> registerUser(Map<String, dynamic>? data) async {
    try {
      Response response = await _dio.post(
          'https://api.loginradius.com/identity/v2/auth/register',
          data: data);
      return response.data;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<void> getToken() async {
    final SharedPreferences _sharedPreferences =
        await SharedPreferences.getInstance();
    _token = jsonDecode(_sharedPreferences.getString('UserInfo').toString());
    if (_token == null) {
      return;
    }
    if (_token['AccessToken'] != null && _token['RefreshToken'] != null) {
      var refreshTokenExp = DateTime.parse(_token['AccessTokenExp']).toLocal();
      if (refreshTokenExp.isBefore(DateTime.now())) {
        await refreshToken();
      }
    }
  }

  Future<bool> isLogin() async {
    if (_token['AccessToken'] != null && _token['RefreshToken'] != null) {
      return Future<bool>.value(true);
    } else {
      return Future<bool>.value(false);
    }
  }

  Future<dynamic> login(String email, String password) async {
    try {
      Response response = await _dio.post(
        "${setting['url']}/${setting['loginPath']}?tenant=${setting['tenant']}",
        data: json.encode({
          'AutoSignIn': true,
          'ClientId': null,
          'CompanyName': setting['tenant'],
          'Password': password,
          'RecoveryToken': '',
          'UserName': email
        }),
        queryParameters: {
          "accept": "*/*",
          "authorization": "Bearer",
          "cache-control": "no-cache",
          "content-type": "application/json",
        },
      );
      return response;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<void> refreshToken() async {
    try {
      Response response = await _dio.post(
        "${setting['url']}/${setting['refreshTokenPath']}?tenant=${setting['tenant']}",
        data: json.encode({
          'RefreshToken': _token['RefreshToken'],
          'AccessToken': _token['AccessToken'],
        }),
        queryParameters: {
          "accept": "*/*",
          "authorization": "Bearer",
          "cache-control": "no-cache",
          "content-type": "application/json",
        },
      );
      final SharedPreferences _sharedPreferences =
          await SharedPreferences.getInstance();
      await _sharedPreferences.setString('UserInfo', jsonEncode(response.data));
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<dynamic> checkRs(dynamic res, String lotNo) async {
    try {
      await getToken();
      Response response = await _dio.get(
        "${setting['url']}/api/ImportDetail?\$expand=Import&\$filter=LotNo eq '$lotNo' and Active eq true and EntityId eq 4201  and RecordId eq ${res['MaterialId']} and SupplierId eq ${res['SupplierId']} and Import/StatusId eq 1 and RemainQuantity gt 0",
        options: Options(
          headers: {
            "accept": "*/*",
            "authorization": "Bearer",
            "cache-control": "no-cache",
            "content-type": "application/json",
            "Authorization": "Bearer ${_token['AccessToken']}"
          },
        ),
      );
      var list = (response.data['value'] as List<dynamic>).isEmpty
          ? null
          : response.data['value'][0];
      return list;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<dynamic> UpdateRequire(int importDetailId, int id) async {
    try {
      await getToken();
      Response response = await _dio.post(
        "${setting['url']}/api/CutDetail/UpdateImportDetail",
        data: json.encode({
          "Id": id,
          "ImportDetailId": importDetailId,
        }),
        options: Options(headers: {
          "accept": "*/*",
          "authorization": "Bearer",
          "cache-control": "no-cache",
          "content-type": "application/json",
          "Authorization": "Bearer ${_token['AccessToken']}"
        }),
      );
      var list = response.data;
      return list;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<List<dynamic>> getRequire(int statusId) async {
    try {
      await getToken();
      Response response = await _dio.get(
        "${setting['url']}/${setting['getRequirePath']}?statusId=$statusId",
        options: Options(
          headers: {
            "accept": "*/*",
            "authorization": "Bearer",
            "cache-control": "no-cache",
            "content-type": "application/json",
            "Authorization": "Bearer ${_token['AccessToken']}"
          },
        ),
      );
      var list = response.data as List<dynamic>;
      return list;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<dynamic> approve(dynamic res) async {
    try {
      await getToken();
      Response response = await _dio.get(
        "${setting['url']}/api/Cut/ApproveMobile?Id=${res['Id']}",
        options: Options(
          headers: {
            "accept": "*/*",
            "authorization": "Bearer",
            "cache-control": "no-cache",
            "content-type": "application/json",
            "Authorization": "Bearer ${_token['AccessToken']}"
          },
        ),
      );
      var list = response.data;
      return list;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<List<dynamic>> getDetail(int id) async {
    try {
      await getToken();
      Response response = await _dio.get(
        "${setting['url']}/${setting['getRequireDetailPath']}?cutId=$id",
        options: Options(
          headers: {
            "accept": "*/*",
            "authorization": "Bearer",
            "cache-control": "no-cache",
            "content-type": "application/json",
            "Authorization": "Bearer ${_token['AccessToken']}"
          },
        ),
      );
      var list = response.data as List<dynamic>;
      return list;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<dynamic> getUserProfileData(String accessToken) async {
    try {
      Response response = await _dio.get(
        'https://api.loginradius.com/identity/v2/auth/account',
        options: Options(
          headers: {'Authorization': 'Bearer $accessToken'},
        ),
      );
      return response.data;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<dynamic> updateUserProfile({
    required String accessToken,
    required Map<String, dynamic> data,
  }) async {
    try {
      Response response = await _dio.put(
        'https://api.loginradius.com/identity/v2/auth/account',
        data: data,
        options: Options(
          headers: {'Authorization': 'Bearer $accessToken'},
        ),
      );
      return response.data;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }

  Future<dynamic> logout(String accessToken) async {
    try {
      Response response = await _dio.get(
        'https://api.loginradius.com/identity/v2/auth/access_token/InValidate',
        options: Options(
          headers: {'Authorization': 'Bearer $accessToken'},
        ),
      );
      return response.data;
    } on DioError catch (e) {
      return e.response!.data;
    }
  }
}
