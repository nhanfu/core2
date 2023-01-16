package com.example.tms;

import android.content.Intent;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Bundle;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import java.net.URISyntaxException;

public class MyWebClient extends WebViewClient {
    public MyWebClient(MainActivity activity) {
        mainActivity = activity;
    }
    private MainActivity mainActivity;

    @Override
    public boolean shouldOverrideUrlLoading(WebView view, String url) {
        if (url.startsWith("geo:") || url.contains("www.google.com/maps/") || url.contains("maps.google.com")) {
            OpenMap(url);
            return true;
        } else if (url.startsWith("http")) return false;//open web links as usual
        Uri parsedUri = Uri.parse(url);
        PackageManager packageManager = mainActivity.getPackageManager();
        Intent intent = new Intent(Intent.ACTION_VIEW).setData(parsedUri);
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_MULTIPLE_TASK);
        if (intent.resolveActivity(packageManager) != null) {
            mainActivity.startActivity(intent);
            return true;
        }
        if (url.startsWith("intent:")) {
            try {
                intent = Intent.parseUri(url, Intent.URI_INTENT_SCHEME);
                intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_MULTIPLE_TASK);
                if (intent.resolveActivity(mainActivity.getPackageManager()) != null) {
                    mainActivity.startActivity(intent);
                    return true;
                }
                String fallbackUrl = intent.getStringExtra("browser_fallback_url");
                if (fallbackUrl != null) {
                    view.loadUrl(fallbackUrl);
                    return true;
                }
                Intent marketIntent = new Intent(Intent.ACTION_VIEW).setData(
                        Uri.parse("market://details?id=" + intent.getPackage()));
                if (marketIntent.resolveActivity(packageManager) != null) {
                    mainActivity.startActivity(marketIntent);
                    return true;
                }
            } catch (URISyntaxException e) {
                //not an intent uri
            }
        }
        view.loadUrl(url);
        return true;
    }

    private void OpenMap(String url) {
        Uri gmmIntentUri = Uri.parse(url);
        Intent intent = new Intent(Intent.ACTION_VIEW, gmmIntentUri);
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_MULTIPLE_TASK);
        intent.setPackage("com.google.android.apps.maps");
        if (intent.resolveActivity(mainActivity.getPackageManager()) != null) {
            mainActivity.startActivity(intent);
        }
    }
}
