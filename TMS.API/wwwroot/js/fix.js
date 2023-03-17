(function () {
    var getValue = System.Nullable.getValue;
    System.Nullable.getValue = function (obj) {
        if (obj === null) {
            return null;
        }
        if (obj && obj.m_dateTime) {
            return obj.m_dateTime;
        }
        return getValue(obj);
    };
    System.DateTimeOffset.prototype.getUTCFullYear = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getUTCFullYear();
    };
    System.DateTimeOffset.prototype.getFullYear = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getFullYear();
    };
    System.DateTimeOffset.prototype.getYear = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getFullYear();
    };
    System.DateTimeOffset.prototype.getDay = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getDay();
    };
    System.DateTimeOffset.prototype.getUTCMonth = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getUTCMonth();
    };
    System.DateTimeOffset.prototype.getMonth = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getMonth();
    };

    System.DateTimeOffset.prototype.getUTCDate = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getUTCDate();
    };
    System.DateTimeOffset.prototype.getDate = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getDate();
    };

    System.DateTimeOffset.prototype.getTime = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getTime();
    };
    System.DateTimeOffset.prototype.getTimezoneOffset = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getTimezoneOffset();
    };
    System.DateTimeOffset.prototype.getTicks = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.ticks;
    };
    System.DateTime.gt = function (a, b) {
        return a.getTime() > b.getTime();
    };
})();