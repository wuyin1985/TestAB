using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlertDialogClickListener : AndroidJavaProxy
{
    public delegate void OnClickDelegate(AndroidJavaObject dialog, int which);

    public OnClickDelegate onClickDelegate;

    public AlertDialogClickListener(OnClickDelegate clickDelegate) : base("android.content.DialogInterface$OnClickListener")
    {
        this.onClickDelegate = clickDelegate;
    }

    public void onClick(AndroidJavaObject dialog, int which)
    {
        onClickDelegate?.Invoke(dialog, which);
    }
}
