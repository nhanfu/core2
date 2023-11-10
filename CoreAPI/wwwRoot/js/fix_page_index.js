function FixLayoutLogin() {
    var parents = document.getElementsByClassName('signup-headline').item(0);
    var childs = document.getElementsByClassName('tab-group tab-horizontal').item(0);
    if (parents === null || childs === null) {
        return
    }
    parents.appendChild(childs);
}
document.addEventListener('DOMContentLoaded', function () {
    setTimeout(FixLayoutLogin, 2000);
});