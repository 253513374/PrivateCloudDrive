package com.companyname.privateclouddrive.app.wxapi;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;

import com.companyname.privateclouddrive.app.wechat.WechatAuthBridge;
import com.tencent.mm.opensdk.constants.ConstantsAPI;
import com.tencent.mm.opensdk.modelbase.BaseReq;
import com.tencent.mm.opensdk.modelbase.BaseResp;
import com.tencent.mm.opensdk.modelmsg.SendAuth;
import com.tencent.mm.opensdk.openapi.IWXAPI;
import com.tencent.mm.opensdk.openapi.IWXAPIEventHandler;
import com.tencent.mm.opensdk.openapi.WXAPIFactory;

public final class WXEntryActivity extends Activity implements IWXAPIEventHandler {
    private IWXAPI api;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        handleWechatIntent(getIntent());
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        handleWechatIntent(intent);
    }

    @Override
    public void onReq(BaseReq req) {
    }

    @Override
    public void onResp(BaseResp resp) {
        try {
            Intent result = new Intent(WechatAuthBridge.ACTION_AUTH_RESULT);
            result.setPackage(getPackageName());

            if (resp == null) {
                result.putExtra(WechatAuthBridge.EXTRA_ERROR, "WeChat did not return a response.");
                sendBroadcast(result);
                return;
            }

            result.putExtra(WechatAuthBridge.EXTRA_ERROR_CODE, resp.errCode);
            result.putExtra(WechatAuthBridge.EXTRA_ERROR_STRING, resp.errStr);

            if (resp.getType() != ConstantsAPI.COMMAND_SENDAUTH) {
                result.putExtra(WechatAuthBridge.EXTRA_ERROR, "WeChat returned an unsupported response.");
                sendBroadcast(result);
                return;
            }

            SendAuth.Resp authResponse = (SendAuth.Resp) resp;
            result.putExtra(WechatAuthBridge.EXTRA_STATE, authResponse.state);

            if (authResponse.errCode == BaseResp.ErrCode.ERR_OK) {
                result.putExtra(WechatAuthBridge.EXTRA_CODE, authResponse.code);
            } else {
                result.putExtra(WechatAuthBridge.EXTRA_ERROR, getErrorMessage(authResponse));
            }

            sendBroadcast(result);
        } finally {
            finish();
        }
    }

    private void handleWechatIntent(Intent intent) {
        if (intent == null) {
            finish();
            return;
        }

        if (api == null) {
            String appId = getSharedPreferences(WechatAuthBridge.PREFERENCES_NAME, MODE_PRIVATE)
                .getString(WechatAuthBridge.PREFERENCE_APP_ID, null);
            api = WXAPIFactory.createWXAPI(this, appId, true);
            if (appId != null && !appId.trim().isEmpty()) {
                api.registerApp(appId);
            }
        }

        if (api == null || !api.handleIntent(intent, this)) {
            Intent result = new Intent(WechatAuthBridge.ACTION_AUTH_RESULT);
            result.setPackage(getPackageName());
            result.putExtra(WechatAuthBridge.EXTRA_ERROR, "WeChat authorization callback could not be handled.");
            sendBroadcast(result);
            finish();
        }
    }

    private static String getErrorMessage(SendAuth.Resp response) {
        if (response.errStr != null && !response.errStr.trim().isEmpty()) {
            return response.errStr;
        }

        switch (response.errCode) {
            case BaseResp.ErrCode.ERR_USER_CANCEL:
                return "WeChat authorization was canceled.";
            case BaseResp.ErrCode.ERR_AUTH_DENIED:
                return "WeChat authorization was denied.";
            case BaseResp.ErrCode.ERR_UNSUPPORT:
                return "This WeChat version does not support authorization.";
            case BaseResp.ErrCode.ERR_SENT_FAILED:
                return "WeChat authorization request failed.";
            case BaseResp.ErrCode.ERR_BAN:
                return "WeChat authorization is blocked. Check the app signature and Open Platform settings.";
            default:
                return "WeChat authorization failed.";
        }
    }
}
