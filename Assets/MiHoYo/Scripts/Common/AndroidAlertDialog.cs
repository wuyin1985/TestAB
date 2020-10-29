using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndroidAlertDialog : MonoBehaviour
{
    public AndroidJavaObject activity;

    private AndroidJavaObject builder;

    private bool isTitleSet = false;
    private bool isMessageSet = false;
    private bool isPositiveButtonSet = false;
    private bool isNegativeButtonSet = false;
    private bool isCreated = false;

    
    public static AndroidJavaObject ToJavaString(string csString)
    {
        return new AndroidJavaObject("java.lang.String", csString);
    }

    
    public AndroidAlertDialog(AndroidJavaObject context)
    {
        activity = context;
        builder = new AndroidJavaObject("android.app.AlertDialog$Builder", activity);
    }

    public void SetTitle(string title)
    {
        builder = builder.Call<AndroidJavaObject>("setTitle", ToJavaString(title));
        isTitleSet = true;
    }

    public void SetMessage(string message)
    {
        builder = builder.Call<AndroidJavaObject>("setMessage", ToJavaString(message));
        isMessageSet = true;
    }

    public void SetPositiveButton(string name, AlertDialogClickListener confirmListener)
    {
        builder = builder.Call<AndroidJavaObject>("setPositiveButton", ToJavaString(name), confirmListener);
        isPositiveButtonSet = true;
    }

    public void SetNegativeButton(string name, AlertDialogClickListener cancelListener)
    {
        builder = builder.Call<AndroidJavaObject>("setNegativeButton", ToJavaString(name), cancelListener);
        isNegativeButtonSet = true;
    }

    public void Create()
    {
        builder = builder.Call<AndroidJavaObject>("create");
        isCreated = true;
    }

    public void Show()
    {
        if (!isTitleSet)
        {
            CommonLog.Error("Not Set Title");
            return;
        }
        else if (!isMessageSet)
        {
            CommonLog.Error("Not Set Message");
            return;
        }
        else if (!isPositiveButtonSet)
        {
            CommonLog.Error("Not Set PositiveButton");
            return;
        }
        else if (!isNegativeButtonSet)
        {
            CommonLog.Error("Not Set NegativeButton");
            return;
        }
        else if (!isCreated)
        {
            CommonLog.Error("Create AlertDialog Failure");
            return;
        }
        builder.Call("show");
    }
}
