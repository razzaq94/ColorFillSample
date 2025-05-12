using UnityEngine;

public enum HapticTypes{ LightImpact, MediumImpact, HeavyImpact}

public static class Haptics
{
#if UNITY_ANDROID && !UNITY_EDITOR
    public static AndroidJavaClass  unityPlayer     = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    public static AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    public static AndroidJavaObject vibrator        = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#else
    public static AndroidJavaClass  unityPlayer;
    public static AndroidJavaObject currentActivity;
    public static AndroidJavaObject vibrator;
#endif

    public static void Vibrate()
    {
        if (isAndroid())
            vibrator.Call("vibrate");
        else
            Handheld.Vibrate();
    }//Vibrate() end

    public static void Vibrate(long milliseconds)
    {
        if (isAndroid())
            vibrator.Call("vibrate", milliseconds);
        else
            Handheld.Vibrate();
    }//Vibrate() end

    public static void Generate(HapticTypes type)
    {
        if(Application.isEditor)
            return;
#if UNITY_ANDROID
        if (type == HapticTypes.LightImpact)
            Vibrate(40);
        else if (type == HapticTypes.MediumImpact)
            Vibrate(60);
        else
            Vibrate(100);
#elif UNITY_IOS
        if (type == HapticTypes.LightImpact)
            HapticFeedback.Generate(UIFeedbackType.ImpactLight);
        else if (type == HapticTypes.MediumImpact)
            HapticFeedback.Generate(UIFeedbackType.ImpactMedium);
        else
            HapticFeedback.Generate(UIFeedbackType.ImpactHeavy);
#endif
    }//Generate() end

    private static bool isAndroid()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }//isAndroid() end

}//class end