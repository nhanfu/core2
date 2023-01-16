package com.example.tms;

import android.annotation.TargetApi;
import android.content.ActivityNotFoundException;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.content.pm.ResolveInfo;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.os.Parcelable;
import android.provider.MediaStore;
import android.util.Log;
import android.webkit.PermissionRequest;
import android.webkit.ValueCallback;
import android.webkit.WebChromeClient;
import android.webkit.WebView;
import android.widget.Toast;

import java.io.File;
import java.util.ArrayList;
import java.util.List;

import static androidx.core.graphics.TypefaceCompatUtil.getTempFile;

public class MyWebChromeClient extends WebChromeClient {
    public MyWebChromeClient(MainActivity activity) {
        mainActivity = activity;
    }
    private MainActivity mainActivity;

    public boolean onShowFileChooser(WebView webView, ValueCallback<Uri[]> filePathCallback, FileChooserParams fileChooserParams) {
        if (mainActivity.uploadMessage != null) {
            mainActivity.uploadMessage.onReceiveValue(null);
            mainActivity.uploadMessage = null;
        }
        mainActivity.uploadMessage = filePathCallback;
        Intent intent = fileChooserParams.createIntent();
        mainActivity.startActivityForResult(intent, MainActivity.REQUEST_SELECT_FILE);
        return true;
    }

    @Override
    public void onPermissionRequest(final PermissionRequest request) {
        request.grant(request.getResources());
    }

}
