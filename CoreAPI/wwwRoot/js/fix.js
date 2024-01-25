"use strict";

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
    Date.prototype.ToString$1 = function (format) {
        return System.String.format(`{0:${format}}`, this);
    };
})();

function zoom() {
    const image = document.querySelector(".dark-overlay img");
    let scale = 1;
    const SCALE_FACTOR = 0.1;

    image.addEventListener("mousewheel", function (e) {
        e.preventDefault();
        scale += e.deltaY > 0 ? -SCALE_FACTOR : SCALE_FACTOR;
        scale = Math.min(Math.max(1, scale), 6);
        this.style.transform = `scale(${scale})`;
    });
    image.addEventListener('mousemove', function (e) {
        e.preventDefault();
        const { left, top, width, height } = this.getBoundingClientRect();
        const x = (e.clientX - left) / width * 100;
        const y = (e.clientY - top) / height * 100;
        this.style.transformOrigin = `${x}% ${y}%`;
    });

    image.addEventListener('mouseout', function (e) {
        e.preventDefault();
        this.style.transformOrigin = 'unset';
        this.style.transform = 'scale(1)'
    });
}

function initCodeEditor(com) {
    if (typeof (require) === 'undefined') return;
    require.config({ paths: { 'vs': 'https://unpkg.com/monaco-editor@latest/min/vs' } });
    window.MonacoEnvironment = { getWorkerUrl: () => proxy };

    let proxy = URL.createObjectURL(new Blob([`
	    self.MonacoEnvironment = {
		    baseUrl: 'https://unpkg.com/monaco-editor@latest/min/'
	    };
	    importScripts('https://unpkg.com/monaco-editor@latest/min/vs/base/worker/workerMain.js');`],
        { type: com.Meta?.Template ?? 'text/javascript' }));

    require(["vs/editor/editor.main"], function () {
        let editor = monaco.editor.create(document.getElementById(com.Element.id), {
            value: com.Entity[com.Meta.FieldName],
            language: com.Meta.Lang ?? 'javascript',
            theme: 'vs-light',
            automaticLayout: true
        });
        // hook event from UI
        editor.getModel().onDidChangeContent(() => {
            com.Entity[com.Meta.FieldName] = editor.getValue();
            com.Dirty = true;
        });
    });
    com.Element.style.resize = 'both';
    com.Element.style.border = '1px solid #dde';
    // register change event from UI
    com.addEventListener('UpdateView', () => {
        editor.setValue(com.Entity[com.Meta.FieldName]);
    });
}