package ru.rustore.unitysdk;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import java.lang.reflect.Field;

import ru.rustore.unitysdk.payclient.RuStoreUnityPayClient;

public class RuStoreDeeplinkActivityDefault extends Activity {
	
    private static final String DEFAULT_ENTRY_ACTIVITY = "com.unity3d.player.UnityPlayerActivity";
	
	private static final String TAG = "RuStoreIntentFilter";

    // Генерируемый класс (может отсутствовать)
    private static final String OVERRIDE_CLASS = "ru.rustore.unitysdk.RuStoreEntryPointOverride";
    private static final String OVERRIDE_FIELD = "UNITY_PLAYER_ACTIVITY_CLASS";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        if (savedInstanceState == null) {
            RuStoreUnityPayClient.INSTANCE.proceedIntent(getIntent());
        }

        if (!isTaskRoot()) {
            finish();
            return;
        }

        startGameActivity();
        finish();
    }

    @Override
    public void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        RuStoreUnityPayClient.INSTANCE.proceedIntent(intent);
    }

    private void startGameActivity() {
        String className = resolveEntryActivityClassName();
		Class<?> entryClass = LoadClass(className);

		if (entryClass == null) {
			Log.e(TAG, "Entry activity class not found: " + className);
			return;
		}

        Intent intent = new Intent(this, entryClass);
        intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        startActivity(intent);
    }

    private String resolveEntryActivityClassName() {
        String override = GetStaticString(OVERRIDE_CLASS, OVERRIDE_FIELD);
        if (override != null && !override.trim().isEmpty()) {
            return override.trim();
        }
		
        return DEFAULT_ENTRY_ACTIVITY;
    }

    private static String GetStaticString(String className, String fieldName) {
        try {
            Class<?> c = Class.forName(className);
            Field f = c.getField(fieldName);
            Object v = f.get(null);
			
            return (v instanceof String) ? (String) v : null;
        } catch (Throwable ignored) {
            return null;
        }
    }

    private static Class<?> LoadClass(String className) {
        if (className == null) return null;

        String s = className.trim();
        if (s.isEmpty()) return null;

        try {
            return Class.forName(s);
        } catch (Throwable ignored) {
            return null;
        }
    }
}
