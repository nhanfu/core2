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
        let editor = monaco.editor.create(com.Element, {
            value: com.Entity[com.Meta.FieldName],
            language: com.Meta.Lang ?? 'javascript',
            theme: 'vs-light',
            automaticLayout: true,
            minimap: {
                enabled: false,
            }
        });
        // hook event from UI
        editor.getModel().onDidChangeContent(() => {
            com.Entity[com.Meta.FieldName] = editor.getValue();
            com.Dirty = true;
        });
    });
    com.Element.classList.add('code-editor');
    com.Element.style.resize = 'both';
    com.Element.style.border = '1px solid #dde';
    // register change event from UI
    com.addEventListener('UpdateView', () => {
        editor.setValue(com.Entity[com.Meta.FieldName]);
    });
    Core.MVVM.Html.Take(com.Element).Icon('fa fal fa-compress-wide')
        .Event('click', () => {
            Core.Components.Extensions.ComponentExt.FullScreen(com.Element);
        });
}

function initCkEditor(com) {
    if (com == null || typeof (CKEDITOR) === 'undefined') return;
    CKEDITOR.ClassicEditor.create(com.Element, {
        // https://ckeditor.com/docs/ckeditor5/latest/features/toolbar/toolbar.html#extended-toolbar-configuration-format
        toolbar: {
            items: [
                'exportPDF', 'exportWord', '|',
                'findAndReplace', 'selectAll', '|',
                'heading', '|',
                'bold', 'italic', 'strikethrough', 'underline', 'code', 'subscript', 'superscript', 'removeFormat', '|',
                'bulletedList', 'numberedList', 'todoList', '|',
                'outdent', 'indent', '|',
                'undo', 'redo',
                '-',
                'fontSize', 'fontFamily', 'fontColor', 'fontBackgroundColor', 'highlight', '|',
                'alignment', '|',
                'link', 'uploadImage', 'blockQuote', 'insertTable', 'mediaEmbed', 'codeBlock', 'htmlEmbed', '|',
                'specialCharacters', 'horizontalLine', 'pageBreak', '|',
                'textPartLanguage', '|',
                'sourceEditing'
            ],
            shouldNotGroupWhenFull: true
        },
        // Changing the language of the interface requires loading the language file using the <script> tag.
        // language: 'es',
        list: {
            properties: {
                styles: true,
                startIndex: true,
                reversed: true
            }
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/headings.html#configuration
        heading: {
            options: [
                { model: 'paragraph', title: 'Paragraph', class: 'ck-heading_paragraph' },
                { model: 'heading1', view: 'h1', title: 'Heading 1', class: 'ck-heading_heading1' },
                { model: 'heading2', view: 'h2', title: 'Heading 2', class: 'ck-heading_heading2' },
                { model: 'heading3', view: 'h3', title: 'Heading 3', class: 'ck-heading_heading3' },
                { model: 'heading4', view: 'h4', title: 'Heading 4', class: 'ck-heading_heading4' },
                { model: 'heading5', view: 'h5', title: 'Heading 5', class: 'ck-heading_heading5' },
                { model: 'heading6', view: 'h6', title: 'Heading 6', class: 'ck-heading_heading6' }
            ]
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/editor-placeholder.html#using-the-editor-configuration
        placeholder: 'Welcome to CKEditor 5!',
        // https://ckeditor.com/docs/ckeditor5/latest/features/font.html#configuring-the-font-family-feature
        fontFamily: {
            options: [
                'default',
                'Arial, Helvetica, sans-serif',
                'Courier New, Courier, monospace',
                'Georgia, serif',
                'Lucida Sans Unicode, Lucida Grande, sans-serif',
                'Tahoma, Geneva, sans-serif',
                'Times New Roman, Times, serif',
                'Trebuchet MS, Helvetica, sans-serif',
                'Verdana, Geneva, sans-serif'
            ],
            supportAllValues: true
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/font.html#configuring-the-font-size-feature
        fontSize: {
            options: [10, 12, 14, 'default', 18, 20, 22],
            supportAllValues: true
        },
        // Be careful with the setting below. It instructs CKEditor to accept ALL HTML markup.
        // https://ckeditor.com/docs/ckeditor5/latest/features/general-html-support.html#enabling-all-html-features
        htmlSupport: {
            allow: [
                {
                    name: /.*/,
                    attributes: true,
                    classes: true,
                    styles: true
                }
            ]
        },
        // Be careful with enabling previews
        // https://ckeditor.com/docs/ckeditor5/latest/features/html-embed.html#content-previews
        htmlEmbed: {
            showPreviews: true
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/link.html#custom-link-attributes-decorators
        link: {
            decorators: {
                addTargetToExternalLinks: true,
                defaultProtocol: 'https://',
                toggleDownloadable: {
                    mode: 'manual',
                    label: 'Downloadable',
                    attributes: {
                        download: 'file'
                    }
                }
            }
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/mentions.html#configuration
        mention: {
            feeds: [
                {
                    marker: '@',
                    feed: [
                        '@apple', '@bears', '@brownie', '@cake', '@cake', '@candy', '@canes', '@chocolate', '@cookie', '@cotton', '@cream',
                        '@cupcake', '@danish', '@donut', '@dragée', '@fruitcake', '@gingerbread', '@gummi', '@ice', '@jelly-o',
                        '@liquorice', '@macaroon', '@marzipan', '@oat', '@pie', '@plum', '@pudding', '@sesame', '@snaps', '@soufflé',
                        '@sugar', '@sweet', '@topping', '@wafer'
                    ],
                    minimumCharacters: 1
                }
            ]
        },
        // The "superbuild" contains more premium features that require additional configuration, disable them below.
        // Do not turn them on unless you read the documentation and know how to configure them and setup the editor.
        removePlugins: [
            // These two are commercial, but you can try them out without registering to a trial.
            // 'ExportPdf',
            // 'ExportWord',
            'AIAssistant',
            'CKBox',
            'CKFinder',
            'EasyImage',
            // This sample uses the Base64UploadAdapter to handle image uploads as it requires no configuration.
            // https://ckeditor.com/docs/ckeditor5/latest/features/images/image-upload/base64-upload-adapter.html
            // Storing images as Base64 is usually a very bad idea.
            // Replace it on production website with other solutions:
            // https://ckeditor.com/docs/ckeditor5/latest/features/images/image-upload/image-upload.html
            // 'Base64UploadAdapter',
            'RealTimeCollaborativeComments',
            'RealTimeCollaborativeTrackChanges',
            'RealTimeCollaborativeRevisionHistory',
            'PresenceList',
            'Comments',
            'TrackChanges',
            'TrackChangesData',
            'RevisionHistory',
            'Pagination',
            'WProofreader',
            // Careful, with the Mathtype plugin CKEditor will not load when loading this sample
            // from a local file system (file://) - load this site via HTTP server if you enable MathType.
            'MathType',
            // The following features are part of the Productivity Pack and require additional license.
            'SlashCommand',
            'Template',
            'DocumentOutline',
            'FormatPainter',
            'TableOfContents',
            'PasteFromOfficeEnhanced',
            'CaseChange'
        ]
    }).then(editor => {
        editor.setData(com.Entity == null ? '' : (com.Entity[com.FieldName] ?? ''));
        editor.model.document.on('change:data', () => {
            com.Entity[com.FieldName] = editor.getData();
            com.Dirty = true;
        });
        editor.plugins.get('FileRepository').createUploadAdapter = function (loader) {
            return new MyUploadAdapter(loader, editor);
        };
        com.addEventListener('UpdateView', () => {
            editor.setHtml(com.Entity == null ? null : com.Entity[com.FieldName]);
        });
    });
}

class MyUploadAdapter {
    constructor(loader, editor) {
        this.loader = loader;
        this.editor = editor;
    }

    upload() {
        return this.loader.file
            .then(file => new Promise((resolve, reject) => {
                const reader = new FileReader();

                reader.onload = () => {
                    this._sendRequest(reader.result, file, resolve, reject);
                };

                reader.readAsDataURL(file);
            }));
    };

    _sendRequest(base64, file, resolve, reject) {
        var $step = 0,
            $task1,
            $taskResult1,
            $jumpFromFinally,
            uploader,
            path,
            content,
            $asyncBody = Bridge.fn.bind(this, function () {
                for (; ;) {
                    $step = System.Array.min([0, 1], $step);
                    switch ($step) {
                        case 0: {
                            uploader = new Core.Components.ImageUploader(new Core.Models.Component());
                            $task1 = uploader.UploadBase64Image(Bridge.toString(base64), file.name);
                            $step = 1;
                            if ($task1.isCompleted()) {
                                continue;
                            }
                            $task1.continue($asyncBody);
                            return;
                        }
                        case 1: {
                            $taskResult1 = $task1.getAwaitedResult();
                            path = $taskResult1;
                            resolve({
                                default: path
                            });
                            return;
                        }
                        default: {
                            return;
                        }
                    }
                }
            }, arguments);

        $asyncBody();
    }
}

((window, factory) => {
    if (typeof define == 'function' && define.amd) {
        define(['draggabilly'], Draggabilly => factory(window, Draggabilly))
    } else if (typeof module == 'object' && module.exports) {
        module.exports = factory(window, require('draggabilly'))
    } else {
        window.ChromeTabs = factory(window, window.Draggabilly)
    }
})(window, (window, Draggabilly) => {
    const TAB_CONTENT_MARGIN = 9
    const TAB_CONTENT_OVERLAP_DISTANCE = 1

    const TAB_OVERLAP_DISTANCE = (TAB_CONTENT_MARGIN * 2) + TAB_CONTENT_OVERLAP_DISTANCE

    const TAB_CONTENT_MIN_WIDTH = 24
    const TAB_CONTENT_MAX_WIDTH = 240

    const TAB_SIZE_SMALL = 84
    const TAB_SIZE_SMALLER = 60
    const TAB_SIZE_MINI = 48

    const noop = _ => { }

    const closest = (value, array) => {
        let closest = Infinity
        let closestIndex = -1

        array.forEach((v, i) => {
            if (Math.abs(value - v) < closest) {
                closest = Math.abs(value - v)
                closestIndex = i
            }
        })

        return closestIndex
    }

    const tabTemplate = `
    <div class="chrome-tab">
      <div class="chrome-tab-dividers"></div>
      <div class="chrome-tab-background">
        <svg version="1.1" xmlns="http://www.w3.org/2000/svg"><defs><symbol id="chrome-tab-geometry-left" viewBox="0 0 214 36"><path d="M17 0h197v36H0v-2c4.5 0 9-3.5 9-8V8c0-4.5 3.5-8 8-8z"/></symbol><symbol id="chrome-tab-geometry-right" viewBox="0 0 214 36"><use xlink:href="#chrome-tab-geometry-left"/></symbol><clipPath id="crop"><rect class="mask" width="100%" height="100%" x="0"/></clipPath></defs><svg width="52%" height="100%"><use xlink:href="#chrome-tab-geometry-left" width="214" height="36" class="chrome-tab-geometry"/></svg><g transform="scale(-1, 1)"><svg width="52%" height="100%" x="-100%" y="0"><use xlink:href="#chrome-tab-geometry-right" width="214" height="36" class="chrome-tab-geometry"/></svg></g></svg>
      </div>
      <div class="chrome-tab-content">
        <div class="chrome-tab-favicon"></div>
        <div class="chrome-tab-title"></div>
        <div class="chrome-tab-drag-handle"></div>
        <div class="chrome-tab-close"></div>
      </div>
    </div>
  `

    const defaultTapProperties = {
        title: 'New tab',
        favicon: false
    }

    let instanceId = 0

    class ChromeTabs {
        constructor() {
            this.draggabillies = [];
            this.tabs = [];
        }

        init(el) {
            this.el = el

            this.instanceId = instanceId
            this.el.setAttribute('data-chrome-tabs-instance-id', this.instanceId)
            instanceId += 1

            this.setupCustomProperties()
            this.setupStyleEl()
            this.setupEvents()
            this.layoutTabs()
            this.setupDraggabilly()
        }

        emit(eventName, data) {
            this.el.dispatchEvent(new CustomEvent(eventName, { detail: data }))
        }

        setupCustomProperties() {
            this.el.style.setProperty('--tab-content-margin', `${TAB_CONTENT_MARGIN}px`)
        }

        setupStyleEl() {
            this.styleEl = document.createElement('style')
            this.el.appendChild(this.styleEl)
        }

        setupEvents() {
            window.addEventListener('resize', _ => {
                this.cleanUpPreviouslyDraggedTabs()
                this.layoutTabs()
            })

            this.el.addEventListener('dblclick', event => {
                if ([this.el, this.tabContentEl].includes(event.target)) this.addTab()
            })

            this.tabEls.forEach((tabEl) => this.setTabCloseEventListener(tabEl))
        }

        get tabEls() {
            return Array.prototype.slice.call(this.el.querySelectorAll('.chrome-tab'))
        }

        get tabContentEl() {
            return this.el.querySelector('.chrome-tabs-content')
        }

        get tabContentWidths() {
            const numberOfTabs = this.tabEls.length
            const tabsContentWidth = this.tabContentEl.clientWidth
            const tabsCumulativeOverlappedWidth = (numberOfTabs - 1) * TAB_CONTENT_OVERLAP_DISTANCE
            const targetWidth = (tabsContentWidth - (2 * TAB_CONTENT_MARGIN) + tabsCumulativeOverlappedWidth) / numberOfTabs
            const clampedTargetWidth = Math.max(TAB_CONTENT_MIN_WIDTH, Math.min(TAB_CONTENT_MAX_WIDTH, targetWidth))
            const flooredClampedTargetWidth = Math.floor(clampedTargetWidth)
            const totalTabsWidthUsingTarget = (flooredClampedTargetWidth * numberOfTabs) + (2 * TAB_CONTENT_MARGIN) - tabsCumulativeOverlappedWidth
            const totalExtraWidthDueToFlooring = tabsContentWidth - totalTabsWidthUsingTarget

            // TODO - Support tabs with different widths / e.g. "pinned" tabs
            const widths = []
            let extraWidthRemaining = totalExtraWidthDueToFlooring
            for (let i = 0; i < numberOfTabs; i += 1) {
                const extraWidth = flooredClampedTargetWidth < TAB_CONTENT_MAX_WIDTH && extraWidthRemaining > 0 ? 1 : 0
                widths.push(flooredClampedTargetWidth + extraWidth)
                if (extraWidthRemaining > 0) extraWidthRemaining -= 1
            }

            return widths
        }

        get tabContentPositions() {
            const positions = []
            const tabContentWidths = this.tabContentWidths

            let position = TAB_CONTENT_MARGIN
            tabContentWidths.forEach((width, i) => {
                const offset = i * TAB_CONTENT_OVERLAP_DISTANCE
                positions.push(position - offset)
                position += width
            })

            return positions
        }

        get tabPositions() {
            const positions = []

            this.tabContentPositions.forEach((contentPosition) => {
                positions.push(contentPosition - TAB_CONTENT_MARGIN)
            })

            return positions
        }

        layoutTabs() {
            const tabContentWidths = this.tabContentWidths

            this.tabEls.forEach((tabEl, i) => {
                const contentWidth = tabContentWidths[i]
                const width = contentWidth + (2 * TAB_CONTENT_MARGIN)

                tabEl.style.width = width + 'px'
                tabEl.removeAttribute('is-small')
                tabEl.removeAttribute('is-smaller')
                tabEl.removeAttribute('is-mini')

                if (contentWidth < TAB_SIZE_SMALL) tabEl.setAttribute('is-small', '')
                if (contentWidth < TAB_SIZE_SMALLER) tabEl.setAttribute('is-smaller', '')
                if (contentWidth < TAB_SIZE_MINI) tabEl.setAttribute('is-mini', '')
            })

            let styleHTML = ''
            this.tabPositions.forEach((position, i) => {
                styleHTML += `
          .chrome-tabs[data-chrome-tabs-instance-id="${this.instanceId}"] .chrome-tab:nth-child(${i + 1}) {
            transform: translate3d(${position}px, 0, 0)
          }
        `
            })
            this.styleEl.innerHTML = styleHTML
        }

        createNewTabEl() {
            const div = document.createElement('div')
            div.innerHTML = tabTemplate
            return div.firstElementChild
        }

        addTab(tabProperties, { animate = true, background = false } = {}) {
            const tabEl = this.createNewTabEl()

            if (animate) {
                tabEl.classList.add('chrome-tab-was-just-added')
                setTimeout(() => tabEl.classList.remove('chrome-tab-was-just-added'), 500)
            }

            tabProperties = Object.assign({}, defaultTapProperties, tabProperties)
            this.tabContentEl.appendChild(tabEl)
            this.setTabCloseEventListener(tabEl)
            this.updateTab(tabEl, tabProperties)
            this.emit('tabAdd', { tabEl })
            if (!background) this.setCurrentTab(tabEl)
            this.cleanUpPreviouslyDraggedTabs()
            this.layoutTabs()
            this.setupDraggabilly()
            this.tabs.push({
                ul: tabEl,
                content: tabProperties.content
            })
            return tabEl;
        }

        setTabCloseEventListener(tabEl) {
            tabEl.querySelector('.chrome-tab-close').addEventListener('click', _ => this.removeTab(tabEl))
        }

        get activeTabEl() {
            return this.el.querySelector('.chrome-tab[active]')
        }

        hasActiveTab() {
            return !!this.activeTabEl
        }

        setCurrentTab(tabEl) {
            const activeTabEl = this.activeTabEl
            if (activeTabEl === tabEl) return
            if (activeTabEl) activeTabEl.removeAttribute('active')
            tabEl.setAttribute('active', '')
            this.emit('activeTabChange', { tabEl })
            const elementToFind = this.tabs.find(item => item.ul === tabEl);
            if (elementToFind != null) {
                elementToFind.content.Focus();
            }
        }

        removeTab(tabEl) {
            if (tabEl === this.activeTabEl) {
                if (tabEl.nextElementSibling) {
                    this.setCurrentTab(tabEl.nextElementSibling);
                } else if (tabEl.previousElementSibling) {
                    this.setCurrentTab(tabEl.previousElementSibling)
                }
            }
            if (tabEl != null & tabEl.parentNode != null) {
                tabEl.parentNode.removeChild(tabEl)
                this.emit('tabRemove', { tabEl })
            }
            else {
                window.history.replaceState(null, "Home", (window.location.origin || ""));
            }
            let existingTabIndex = this.tabs.findIndex(tab => tab.ul === tabEl);
            if (existingTabIndex !== -1) {
                const elementToFind = this.tabs[existingTabIndex];
                if (elementToFind != null) {
                    elementToFind.content.DirtyCheckAndCancel()
                }
                this.tabs.splice(existingTabIndex, 1);
            }
            this.cleanUpPreviouslyDraggedTabs()
            this.layoutTabs()
            this.setupDraggabilly()
        }

        updateTab(tabEl, tabProperties) {
            tabEl.querySelector('.chrome-tab-title').textContent = tabProperties.title

            const faviconEl = tabEl.querySelector('.chrome-tab-favicon')
            if (tabProperties.favicon) {
                if (tabProperties.favicon.includes('fa-')) {
                    faviconEl.className = tabProperties.favicon;
                }
                else {
                    faviconEl.style.backgroundImage = `url('${tabProperties.favicon}')`
                }
                faviconEl.removeAttribute('hidden', '')
            } else {
                faviconEl.setAttribute('hidden', '')
                faviconEl.removeAttribute('style')
            }

            if (tabProperties.id) {
                tabEl.setAttribute('data-tab-id', tabProperties.id)
            }
        }

        cleanUpPreviouslyDraggedTabs() {
            this.tabEls.forEach((tabEl) => tabEl.classList.remove('chrome-tab-was-just-dragged'))
        }

        setupDraggabilly() {
            const tabEls = this.tabEls
            const tabPositions = this.tabPositions

            if (this.isDragging) {
                this.isDragging = false
                this.el.classList.remove('chrome-tabs-is-sorting')
                this.draggabillyDragging.element.classList.remove('chrome-tab-is-dragging')
                this.draggabillyDragging.element.style.transform = ''
                this.draggabillyDragging.dragEnd()
                this.draggabillyDragging.isDragging = false
                this.draggabillyDragging.positionDrag = noop // Prevent Draggabilly from updating tabEl.style.transform in later frames
                this.draggabillyDragging.destroy()
                this.draggabillyDragging = null
            }

            this.draggabillies.forEach(d => d.destroy())

            tabEls.forEach((tabEl, originalIndex) => {
                const originalTabPositionX = tabPositions[originalIndex]
                const draggabilly = new Draggabilly(tabEl, {
                    axis: 'x',
                    handle: '.chrome-tab-drag-handle',
                    containment: this.tabContentEl
                })

                this.draggabillies.push(draggabilly)

                draggabilly.on('pointerDown', _ => {
                    this.setCurrentTab(tabEl)
                })

                draggabilly.on('dragStart', _ => {
                    this.isDragging = true
                    this.draggabillyDragging = draggabilly
                    tabEl.classList.add('chrome-tab-is-dragging')
                    this.el.classList.add('chrome-tabs-is-sorting')
                })

                draggabilly.on('dragEnd', _ => {
                    this.isDragging = false
                    const finalTranslateX = parseFloat(tabEl.style.left, 10)
                    tabEl.style.transform = `translate3d(0, 0, 0)`

                    // Animate dragged tab back into its place
                    requestAnimationFrame(_ => {
                        tabEl.style.left = '0'
                        tabEl.style.transform = `translate3d(${finalTranslateX}px, 0, 0)`

                        requestAnimationFrame(_ => {
                            tabEl.classList.remove('chrome-tab-is-dragging')
                            this.el.classList.remove('chrome-tabs-is-sorting')

                            tabEl.classList.add('chrome-tab-was-just-dragged')

                            requestAnimationFrame(_ => {
                                tabEl.style.transform = ''

                                this.layoutTabs()
                                this.setupDraggabilly()
                            })
                        })
                    })
                })

                draggabilly.on('dragMove', (event, pointer, moveVector) => {
                    // Current index be computed within the event since it can change during the dragMove
                    const tabEls = this.tabEls
                    const currentIndex = tabEls.indexOf(tabEl)

                    const currentTabPositionX = originalTabPositionX + moveVector.x
                    const destinationIndexTarget = closest(currentTabPositionX, tabPositions)
                    const destinationIndex = Math.max(0, Math.min(tabEls.length, destinationIndexTarget))

                    if (currentIndex !== destinationIndex) {
                        this.animateTabMove(tabEl, currentIndex, destinationIndex)
                    }
                })
            })
        }

        animateTabMove(tabEl, originIndex, destinationIndex) {
            if (destinationIndex < originIndex) {
                tabEl.parentNode.insertBefore(tabEl, this.tabEls[destinationIndex])
            } else {
                tabEl.parentNode.insertBefore(tabEl, this.tabEls[destinationIndex + 1])
            }
            this.emit('tabReorder', { tabEl, originIndex, destinationIndex })
            this.layoutTabs()
        }
    }

    return ChromeTabs
});