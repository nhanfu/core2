/**
 * Defines a class containing static properties for different HTML event types.
 * Each property corresponds to an HTML event name, with descriptions provided
 * for each type of event.
 */
export default class EventType {
    /**
     * uIEvent: The loading of a resource has been aborted.
     * @static
     */
    static abort = "abort";

    /**
     * Event: The associated document has started printing or the print preview has been closed.
     * @static
     */
    static afterPrint = "afterprint";

    /**
     * animationEvent: A CSS animation has completed.
     * @static
     */
    static animationEnd = "animationend";

    /**
     * animationEvent: A CSS animation is repeated.
     * @static
     */
    static animationIteration = "animationiteration";

    /**
     * animationEvent: A CSS animation has started.
     * @static
     */
    static animationStart = "animationstart";

    /**
     * Event: The associated document is about to be printed or previewed for printing.
     * @static
     */
    static beforePrint = "beforeprint";

    /**
     * beforeUnloadEvent: The beforeunload event is fired when the window, the document and its resources are about to be unloaded.
     * @static
     */
    static beforeUnload = "beforeunload";

    /**
     * indexedDB: An open connection to a database is blocking a versionchange transaction on the same database.
     * @static
     */
    static blocked = "blocked";

    /**
     * focusEvent: An element has lost focus (does not bubble).
     * @static
     */
    static blur = "blur";

    /**
     * Event: The resources listed in the manifest have been downloaded, and the application is now cached.
     * @static
     */
    static cached = "cached";

    /**
     * Event: The user agent can play the media, but estimates that not enough data has been loaded to play the media up to its end without having to stop for further buffering of content.
     * @static
     */
    static canPlay = "canplay";

    /**
     * Event: The user agent can play the media, and estimates that enough data has been loaded to play the media up to its end without having to stop for further buffering of content.
     * @static
     */
    static canPlayThrough = "canplaythrough";

    /**
     * Event: An element loses focus and its value changed since gaining focus.
     * @static
     */
    static change = "change";

    /**
     * Event: The user agent is checking for an update, or attempting to download the cache manifest for the first time.
     * @static
     */
    static checking = "checking";

    /**
     * mouseEvent: A pointing device button has been pressed and released on an element.
     * @static
     */
    static click = "click";

    /**
     * Event: A webSocket connection has been closed.
     * @static
     */
    static close = "close";

    /**
     * indexedDB: The complete handler is executed when a transaction successfully completed.
     * @static
     */
    static complete = "complete";

    /**
     * compositionEvent: The composition of a passage of text has been completed or canceled.
     * @static
     */
    static compositionEnd = "compositionend";

    /**
     * compositionEvent: The composition of a passage of text is prepared (similar to keydown for a keyboard input, but works with other inputs such as speech recognition).
     * @static
     */
    static compositionStart = "compositionstart";

    /**
     * compositionEvent: A character is added to a passage of text being composed.
     * @static
     */
    static compositionUpdate = "compositionupdate";

    /**
     * mouseEvent: The right button of the mouse is clicked (before the context menu is displayed).
     * @static
     */
    static contextMenu = "contextmenu";

    /**
     * clipboardEvent: The text selection has been added to the clipboard.
     * @static
     */
    static copy = "copy";

    /**
     * clipboardEvent: The text selection has been removed from the document and added to the clipboard.
     * @static
     */
    static cut = "cut";

    /**
     * mouseEvent: A pointing device button is clicked twice on an element.
     * @static
     */
    static dblClick = "dblclick";

    /**
     * deviceLightEvent: Fresh data is available from a light sensor.
     * @static
     */
    static deviceLight = "devicelight";

    /**
     * deviceMotionEvent: Fresh data is available from a motion sensor.
     * @static
     */
    static deviceMotion = "devicemotion";

    /**
 * deviceOrientationEvent: Fresh data is available from an orientation sensor.
 * @static
 */
    static deviceOrientation = "deviceorientation";

    /**
     * deviceProximityEvent: Fresh data is available from a proximity sensor (indicates an approximated distance between the device and a nearby object).
     * @static
     */
    static deviceProximity = "deviceproximity";

    /**
     * Event: The dischargingTime attribute has been updated.
     * @static
     */
    static dischargingTimeChange = "dischargingtimechange";

    /**
     * Event: The document has finished loading (but not its dependent resources).
     * @static
     */
    static domContentLoaded = "domcontentloaded";

    /**
     * Event: The user agent has found an update and is fetching it, or is downloading the resources listed by the cache manifest for the first time.
     * @static
     */
    static downLoading = "downloading";

    /**
     * dragEvent: An element or text selection is being dragged (every 350ms).
     * @static
     */
    static drag = "drag";

    /**
     * dragEvent: A drag operation is being ended (by releasing a mouse button or hitting the escape key).
     * @static
     */
    static dragEnd = "dragend";

    /**
     * dragEvent: A dragged element or text selection enters a valid drop target.
     * @static
     */
    static dragEnter = "dragenter";

    /**
     * dragEvent: A dragged element or text selection leaves a valid drop target.
     * @static
     */
    static dragLeave = "dragleave";

    /**
     * dragEvent: An element or text selection is being dragged over a valid drop target (every 350ms).
     * @static
     */
    static dragOver = "dragover";

    /**
     * dragEvent: The user starts dragging an element or text selection.
     * @static
     */
    static dragStart = "dragstart";

    /**
     * dragEvent: An element is dropped on a valid drop target.
     * @static
     */
    static drop = "drop";

    /**
     * Event: The duration attribute has been updated.
     * @static
     */
    static durationChange = "durationchange";

    /**
     * Event: The media has become empty; for example, this event is sent if the media has already been loaded (or partially loaded), and the load() method is called to reload it.
     * @static
     */
    static emptied = "emptied";

    /**
     * Event: Playback has stopped because the end of the media was reached.
     * @static
     */
    static ended = "ended";

    /**
     * Event: An error occurred during the loading of an event.
     * @static
     */
    static error = "error";

    /**
     * focusEvent: An element has received focus (does not bubble).
     * @static
     */
    static focus = "focus";

    /**
     * focusEvent: An element is about to receive focus (bubbles).
     * @static
     */
    static focusIn = "focusin";

    /**
     * focusEvent: An element is about to lose focus (bubbles).
     * @static
     */
    static focusOut = "focusout";

    /**
     * Event: An element was turned to fullscreen mode or back to normal mode.
     * @static
     */
    static fullScreenChange = "fullscreenchange";

    /**
     * Event: It was impossible to switch to fullscreen mode for technical reasons or because the permission was denied.
     * @static
     */
    static fullScreenError = "fullscreenerror";

    /**
     * gamepadEvent: A gamepad has been connected.
     * @static
     */
    static gamepadConnected = "gamepadconnected";

    /**
     * gamepadEvent: A gamepad has been disconnected.
     * @static
     */
    static gamepadDisconnected = "gamepaddisconnected";

    /**
     * hashChangeEvent: The fragment identifier of the URL has changed (the part of the URL after the #).
     * @static
     */
    static hashChange = "hashchange";

    /**
     * Event: The value of an element changes or the content of an element with the attribute contenteditable is modified.
     * @static
     */
    static input = "input";

    /**
     * Event: A submittable element has been checked and doesn't satisfy its constraints.
     * @static
     */
    static invalid = "invalid";

    /**
     * keyboardEvent: A key is pressed down.
     * @static
     */
    static keyDown = "keydown";

    /**
     * keyboardEvent: A key is pressed down and that key normally produces a character value (use input instead).
     * @static
     */
    static keyPress = "keypress";

    /**
     * keyboardEvent: A key is released.
     * @static
     */
    static keyUp = "keyup";

    /**
     * Event: The level attribute has been updated.
     * @static
     */
    static levelChange = "levelchange";

    /**
     * uIEvent: A resource and its dependent resources have finished loading.
     * @static
     */
    static load = "load";

    /**
     * Event: The first frame of the media has finished loading.
     * @static
     */
    static loadedData = "loadeddata";

    /**
     * Event: The metadata has been loaded.
     * @static
     */
    static loadedMetaData = "loadedmetadata";

    /**
     * progressEvent: Progress has stopped (after "error", "abort" or "load" have been dispatched).
     * @static
     */
    static loadEnd = "loadend";

    /**
     * progressEvent: Progress has begun.
     * @static
     */
    static loadStart = "loadstart";

    /**
     * messageEvent: A message is received through a webSocket.
     * @static
     */
    static message = "message";

    /**
     * mouseEvent: A pointing device button (usually a mouse) is pressed on an element.
     * @static
     */
    static mouseDown = "mousedown";

    /**
     * mouseEvent: A pointing device is moved onto the element that has the listener attached.
     * @static
     */
    static mouseEnter = "mouseenter";

    /**
     * mouseEvent: A pointing device is moved off the element that has the listener attached.
     * @static
     */
    static mouseLeave = "mouseleave";

    /**
     * mouseEvent: A pointing device is moved over an element.
     * @static
     */
    static mouseMove = "mousemove";

    /**
     * mouseEvent: A pointing device is moved off the element that has the listener attached or off one of its children.
     * @static
     */
    static mouseOut = "mouseout";

    /**
     * mouseEvent: A pointing device is moved onto the element that has the listener attached or onto one of its children.
     * @static
     */
    static mouseOver = "mouseover";

    /**
     * mouseEvent: A pointing device button is released over an element.
     * @static
     */
    static mouseUp = "mouseup";

    /**
     * Event: The manifest hadn't changed.
     * @static
     */
    static noUpdate = "noupdate";

    /**
     * Event: The manifest was found to have become a 404 or 410 page, so the application cache is being deleted.
     * @static
     */
    static obsolete = "obsolete";

    /**
     * Event: The browser has lost access to the network.
     * @static
     */
    static offline = "offline";

    /**
     * Event: The browser has gained access to the network (but particular websites might be unreachable).
     * @static
     */
    static online = "online";

    /**
     * Event: A webSocket connection has been established.
     * @static
     */
    static open = "open";

    /**
     * Event: The orientation of the device (portrait/landscape) has changed.
     * @static
     */
    static orientationChange = "orientationchange";

    /**
     * pageTransitionEvent: A session history entry is being traversed from.
     * @static
     */
    static pageHide = "pagehide";

    /**
     * pageTransitionEvent: A session history entry is being traversed to.
     * @static
     */
    static pageShow = "pageshow";

    /**
     * clipboardEvent: Data has been transferred from the system clipboard to the document.
     * @static
     */
    static paste = "paste";

    /**
     * Event: Playback has been paused.
     * @static
     */
    static pause = "pause";

    /**
     * Event: The pointer was locked or released.
     * @static
     */
    static pointerLockChange = "pointerlockchange";

    /**
     * Event: It was impossible to lock the pointer for technical reasons or because the permission was denied.
     * @static
     */
    static pointerLockError = "pointerlockerror";

    /**
     * Event: Playback has begun.
     * @static
     */
    static play = "play";

    /**
     * Event: Playback is ready to start after having been paused or delayed due to lack of data.
     * @static
     */
    static playing = "playing";

    /**
     * popStateEvent: A session history entry is being navigated to (in certain cases).
     * @static
     */
    static popState = "popstate";

    /**
     * progressEvent: In progress.
     * @static
     */
    static progress = "progress";

    /**
     * Event: The playback rate has changed.
     * @static
     */
    static rateChange = "ratechange";

    /**
     * Event: The readyState attribute of a document has changed.
     * @static
     */
    static readyStateChange = "readystatechange";

    /**
     * timeEvent: A SMIL animation element is repeated.
     * @static
     */
    static repeatEvent = "repeatevent";

    /**
     * Event: A form is reset.
     * @static
     */
    static reset = "reset";

    /**
     * uIEvent: The document view has been resized.
     * @static
     */
    static resize = "resize";

    /**
     * uIEvent: The document view or an element has been scrolled.
     * @static
     */
    static scroll = "scroll";

    /**
     * Event: A seek operation completed.
     * @static
     */
    static seeked = "seeked";

    /**
     * Event: A seek operation began.
     * @static
     */
    static seeking = "seeking";

    /**
     * uIEvent: Some text is being selected.
     * @static
     */
    static select = "select";

    /**
     * mouseEvent: A context menu event was fired on/bubbled to an element that has a context menu attribute.
     * @static
     */
    static show = "show";

    /**
     * Event: The user agent is trying to fetch media data, but data is unexpectedly not forthcoming.
     * @static
     */
    static stalled = "stalled";

    /**
     * storageEvent: A storage area (localStorage or sessionStorage) has changed.
     * @static
     */
    static storage = "storage";

    /**
     * Event: A form is submitted.
     * @static
     */
    static submit = "submit";

    /**
     * Event: A request successfully completed.
     * @static
     */
    static success = "success";

    /**
     * Event: Media data loading has been suspended.
     * @static
     */
    static suspend = "suspend";

    /**
     * sVGEvent: Page loading has been stopped before the SVG was loaded.
     * @static
     */
    static sVGAbort = "svgabort";

    /**
     * sVGEvent: An error has occurred before the SVG was loaded.
     * @static
     */
    static sVGError = "svgerror";

    /**
     * sVGEvent: An SVG document has been loaded and parsed.
     * @static
     */
    static sVGLoad = "svgload";

    /**
     * sVGEvent: An SVG document is being resized.
     * @static
     */
    static sVGResize = "svgresize";

    /**
     * sVGEvent: An SVG document is being scrolled.
     * @static
     */
    static sVGScroll = "svgscroll";

    /**
     * sVGEvent: An SVG document has been removed from a window or frame.
     * @static
     */
    static sVGUnload = "svgunload";

    /**
     * sVGZoomEvent: An SVG document is being zoomed.
     * @static
     */
    static sVGZoom = "svgzoom";

    /**
     * progressEvent: A request timed out.
     * @static
     */
    static timeout = "timeout";

    /**
     * Event: The time indicated by the currentTime attribute has been updated.
     * @static
     */
    static timeUpdate = "timeupdate";

    /**
     * touchEvent: A touch point has been disrupted in an implementation-specific manner (too many touch points for example).
     * @static
     */
    static touchCancel = "touchcancel";

    /**
     * touchEvent: A touch point is removed from the touch surface.
     * @static
     */
    static touchEnd = "touchend";

    /**
     * touchEvent: A touch point is moved onto the interactive area of an element.
     * @static
     */
    static touchEnter = "touchenter";

    /**
     * touchEvent: A touch point is moved off the interactive area of an element.
     * @static
     */
    static touchLeave = "touchleave";

    /**
     * touchEvent: A touch point is moved along the touch surface.
     * @static
     */
    static touchMove = "touchmove";

    /**
     * touchEvent: A touch point is placed on the touch surface.
     * @static
     */
    static touchStart = "touchstart";

    /**
     * transitionEvent: A CSS transition has completed.
     * @static
     */
    static transitionEnd = "transitionend";

    /**
     * uIEvent: The document or a dependent resource is being unloaded.
     * @static
     */
    static unload = "unload";

    /**
     * Event: The resources listed in the manifest have been newly redownloaded, and the script can use swapCache() to switch to the new cache.
     * @static
     */
    static updateReady = "updateready";

    /**
     * indexedDB: An attempt was made to open a database with a version number higher than its current version. A versionchange transaction has been created.
     * @static
     */
    static upgradeNeeded = "upgradeneeded";

    /**
     * sensorEvent: Fresh data is available from a proximity sensor (indicates whether the nearby object is near the device or not).
     * @static
     */
    static userProximity = "userproximity";

    /**
     * Event: A versionchange transaction completed.
     * @static
     */
    static versionChange = "versionchange";

    /**
     * Event: The content of a tab has become visible or has been hidden.
     * @static
     */
    static visibilityChange = "visibilitychange";

    /**
     * Event: The volume has changed.
     * @static
     */
    static volumeChange = "volumechange";

    /**
     * Event: Playback has stopped because of a temporary lack of data.
     * @static
     */
    static waiting = "waiting";

    /**
     * wheelEvent: A wheel button of a pointing device is rotated in any direction.
     * @static
     */
    static wheel = "wheel";

    /**
     * wheelEvent: A wheel button of a pointing device is rotated in any direction.
     * @static
     */
    static onSave = "onsave";
}
