import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:warehouse/custom_route.dart';
import 'package:warehouse/api/api_client.dart';
import 'package:warehouse/dashboard_screen.dart';
import 'package:warehouse/require_detail.dart';

class LayOutDrawer extends StatelessWidget {
  final user = {};
  LayOutDrawer(user, {Key? key}) : super(key: key);
  Future _openRequire(BuildContext context) {
    return Navigator.of(context).pushReplacement(FadePageRoute(
      builder: (context) => const DashboardScreen(),
    ));
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: EdgeInsets.zero,
      children: [
        UserAccountsDrawerHeader(
          accountName: Text(user != null ? user['FullName'].toString() : ""),
          accountEmail: Text(user != null ? user['Email'].toString() : ""),
          currentAccountPicture: CircleAvatar(
            child: ClipOval(
              child: Image.network(
                'https://oflutter.com/wp-content/uploads/2021/02/girl-profile.png',
                fit: BoxFit.cover,
                width: 90,
                height: 90,
              ),
            ),
          ),
          decoration: const BoxDecoration(
            color: Colors.blue,
            image: DecorationImage(
                fit: BoxFit.fill,
                image: NetworkImage(
                    'https://oflutter.com/wp-content/uploads/2021/02/profile-bg3.jpg')),
          ),
        ),
        ListTile(
          leading: const Icon(Icons.thumb_up),
          title: const Text('Approve Require'),
          onTap: () => _openRequire(context),
        ),
        ListTile(
          leading: const Icon(Icons.turn_right),
          title: const Text('Export Product'),
          onTap: () => _openRequire(context),
        )
      ],
    );
  }
}

class TabRequire extends StatelessWidget {
  final ApiClient _apiClient = ApiClient();
  final dynamic record;
  TabRequire(this.record, {Key? key}) : super(key: key);

  Future<List<dynamic>> _fetchJobs(int statusId) async {
    final userRes = await _apiClient.getRequire(statusId);
    return userRes;
  }

  void openDetail(dynamic res, BuildContext context) async {
    Navigator.push(
      context,
      MaterialPageRoute(builder: (context) => DetailScreen(res)),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Column(children: <Widget>[
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
              future: _fetchJobs(record),
              builder: (context, snapshot) {
                if (snapshot.hasData) {
                  return Padding(
                    padding: const EdgeInsets.all(8.0),
                    child: ListView.builder(
                      itemCount: snapshot.data?.length,
                      itemBuilder: (context, index) {
                        var name = snapshot.data?[index]['Name'];
                        var insertedBy = snapshot.data?[index]['InsertedBy'];
                        var pICName = snapshot.data?[index]['PICName'];
                        var cuttingDate = DateFormat('dd-MM-yyyy HH:mm').format(
                            DateTime.parse(
                                snapshot.data?[index]['CuttingDate']));
                        var countNonCan = snapshot.data?[index]['CountNonCan'];
                        var countHasCan = snapshot.data?[index]['CountHasCan'];
                        return Container(
                          decoration: BoxDecoration(
                            borderRadius:
                                const BorderRadius.all(Radius.circular(16.0)),
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
                              borderRadius: BorderRadius.circular(15.0),
                            ),
                            child: ListTile(
                              onTap: () =>
                                  openDetail(snapshot.data?[index], context),
                              trailing: Text("PIC: " +
                                  pICName +
                                  '\n' +
                                  "Created: " +
                                  insertedBy),
                              subtitle: Text(
                                "Date: " +
                                    cuttingDate +
                                    '\n' +
                                    "Name: " +
                                    name +
                                    '\n' +
                                    "PIC: " +
                                    pICName +
                                    '\n' +
                                    "Scanned: " +
                                    countHasCan.toString() +
                                    '\n' +
                                    "Rest:" +
                                    countNonCan.toString(),
                                style: const TextStyle(color: Colors.black),
                              ),
                            ),
                          ),
                        );
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
    ]);
  }
}
