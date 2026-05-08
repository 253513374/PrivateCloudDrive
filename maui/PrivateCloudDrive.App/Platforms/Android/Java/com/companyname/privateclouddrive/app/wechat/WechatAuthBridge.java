package com.companyname.privateclouddrive.app.wechat;

import android.content.Context;

import com.tencent.mm.opensdk.modelmsg.SendAuth;
import com.tencent.mm.opensdk.openapi.IWXAPI;
import com.tencent.mm.opensdk.openapi.WXAPIFactory;

public final class WechatAuthBridge {
    public static final String PREFERENCES_NAME = "privateclouddrive_wechat_auth";
    public static final String PREFERENCE_APP_ID = "app_id";
    public static final String ACTION_AUTH_RESULT = "com.companyname.privateclouddrive.app.WECHAT_AUTH_RESULT";
    public static final String EXTRA_CODE = "code";
    public static final String EXTRA_STATE = "state";
    public static final String EXTRA_ERROR = "error";
    public static final String EXTRA_ERROR_CODE = "error_code";
    public static final String EXTRA_ERROR_STRING = "error_string";

    private WechatAuthBridge() {
    }

    public static boolean isWechatInstalled(Context context, String appId) {
        if (context == null || appId == null || appId.trim().isEmpty()) {
            return false;
        }

        IWXAPI api = WXAPIFactory.createWXAPI(context, appId, true);
        return api != null && api.isWXAppInstalled();
    }

    public static boolean sendAuth(Context context, String appId, String scope, String state) {
        if (context == null || appId == null || appId.trim().isEmpty()) {
            return false;
        }

        IWXAPI api = WXAPIFactory.createWXAPI(context, appId, true);
        if (api == null) {
            return false;
        }

        context.getApplicationContext()
            .getSharedPreferences(PREFERENCES_NAME, Context.MODE_PRIVATE)
            .edit()
            .putString(PREFERENCE_APP_ID, appId)
            .apply();

        api.registerApp(appId);

        SendAuth.Req request = new SendAuth.Req();
        request.scope = scope;
        request.state = state;
        return api.sendReq(request);
    }
}
