var Host = 'tms.softek.com.vn';
var OriginLocation = 'https://tms.softek.com.vn/';
document.addEventListener('deviceready', onDeviceReady, false);
function onBackKeyDown(e) {
    e.preventDefault();
    document.getElementById('btnBack').click();
}

async function onDeviceReady() {
    if (device.platform === 'Android') {
        RequestPermission('android.permission.ACCESS_COARSE_LOCATION');
        document.addEventListener('backbutton', onBackKeyDown, false);
    }
    var indexPage = await makeRequest('get', OriginLocation);
    var parser = new DOMParser();
    var dom = parser.parseFromString(indexPage, 'text/html');
    var scripts = Array.prototype.map.call(dom.body.childNodes, x => {
        if (!x.src) return null;
        return OriginLocation + x.getAttribute('src');
    }).filter(Boolean);
    for (var i = 0; i < dom.body.childNodes.length; i++) {
        var ele = dom.body.childNodes[i];
        if (!ele.src) continue;
        ele.remove();
        i--;
    }
    Array.prototype.forEach.call(dom.head.childNodes, x => {
        if (!x.href) return null;
        if (x instanceof HTMLBaseElement) x.remove();
        x.href = OriginLocation + x.getAttribute('href');
    });
    while (dom.head.firstChild) {
        document.head.appendChild(dom.head.firstChild);
    }
    while (dom.body.firstChild) document.body.appendChild(dom.body.firstChild);

    var scriptContents = await Promise.all(scripts.map(x => makeRequest('Get', x)));
    scriptContents.forEach(x => {
        var scriptTag = document.createElement('script');
        scriptTag.text = x;
        document.head.appendChild(scriptTag);
    });
}

function makeRequest(method, url, data) {
    return new Promise(function (resolve, reject) {
        let xhr = new XMLHttpRequest();
        xhr.open(method, url);
        xhr.onload = function () {
            if (this.status >= 200 && this.status < 300) {
                resolve(xhr.response);
            } else {
                reject({
                    status: this.status,
                    statusText: xhr.statusText
                });
            }
        };
        xhr.onerror = function () {
            reject({
                status: this.status,
                statusText: xhr.statusText
            });
        };
        xhr.send(data);
    });
}

function RequestPermission(permissionName) {
    var Permission = window.plugins.Permission
    Permission.has(permissionName, function (results) {
        if (!results[permissionName]) {
            Permission.request(permissionName, function (results) {
                if (!results[permissionName]) {
                    Core.Extensions.Toast.Small("Không có quyền GPS");
                }
            }, alert)
        }
    }, alert)
}

const coordinations = [];
setInterval(function () {
    AddLocation();
}, 10 * 1000);


const script = document.createElement('script')
script.src = 'https://maps.googleapis.com/maps/api/js?key=AIzaSyBfVrTUFatsZTyqaCKwRzbj09DD72VxSwc&libraries=&v=weekly'
document.head.append(script)

function getDistance(origin1, destinationB) {
    return new Promise(function (resolve, reject) {
        const service = new google.maps.DistanceMatrixService();
        let results = 0;
        service.getDistanceMatrix(
            {
                origins: [origin1],
                destinations: [destinationB],
                travelMode: google.maps.TravelMode.DRIVING,
                unitSystem: google.maps.UnitSystem.METRIC,
                avoidHighways: false,
                avoidTolls: false,
            },
            (response, status) => {
                if (status !== "OK") {
                    alert("Error was: " + status);
                } else {
                    results = response.rows[parseInt(response.rows["length"]) - 1].elements[response.rows[parseInt(response.rows["length"]) - 1].elements.length - 1].distance.value;
                    resolve(results);
                }
            }
        );
    });
}

function AddLocation() {
    navigator.geolocation.getCurrentPosition(
        data => {
            var feed = { latitude: data.coords.latitude, longitude: data.coords.longitude };
            const coordinationlocalStorage = JSON.parse(window.localStorage.getItem("coordinations"));
            if (coordinationlocalStorage == null || coordinationlocalStorage == "") {
                coordinations.push(feed);
                window.localStorage.setItem("coordinations", JSON.stringify(coordinations));
                return;
            }
            var last = coordinationlocalStorage[coordinationlocalStorage.length - 1];
            const origin1 = { lat: last.latitude, lng: last.longitude };
            const destinationB = { lat: data.coords.latitude, lng: data.coords.longitude };
            getDistance(origin1, destinationB).then(distance => {
                if ((parseFloat(distance) / 1000) <= 50) {
                    return;
                }
                coordinations.push(feed);
                window.localStorage.setItem("coordinations", JSON.stringify(coordinations));
            });
        },
        error => console.log(error), {
        enableHighAccuracy: true
    });
}
