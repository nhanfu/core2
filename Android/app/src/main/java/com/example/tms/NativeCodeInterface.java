package com.example.tms;

import android.webkit.JavascriptInterface;

public class NativeCodeInterface {
    private MainActivity mainActivity;
    public  NativeCodeInterface(MainActivity activity) {
        mainActivity = activity;
    }

    @JavascriptInterface
    public void showNotification(String title, String body) {
        mainActivity.sendOnChannel(1, title, body);
    }
}
