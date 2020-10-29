using SignalTools;
using TimerUtils;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 这是一个用来分离UI事件和UI触发代码的类，并且可以设置Retian时间避免重复点击多次处理
/// Example usage: UIEventListener.Get(gameObject).onClick += MyClickFunction;
/// </summary>


public class UIEventSignal
{
    private EventSignalObject s;
    private object parobj;

    private EventSignalObjects ss;
    private object[] parobjs;
    //不是Release的事件将会被抛弃
    public bool IsRelease = true;

    private float StartRetainTime;

    private float AutoReleaseTime = 0;
    /// <summary>
    /// 额外的Invoke事件监听，用于各个需要的拖拽的效果系统,如果是Retain不会被触发，此外不会被Reset
    /// </summary>
    public FixedLinkedSignal EffectInvokeSignal = new FixedLinkedSignal();

    public Button FollowedBtn { get; private set; }
    public void SetFollowedUIButton(Button btn)
    {
        if (btn == null && FollowedBtn != null) { FollowedBtn.onClick.RemoveListener(OnButtonClick); }
        FollowedBtn = btn;
        btn.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        Invoke();
    }


    public UIEventSignal SetListener(EventSignalObject callback, object par = null)
    {
        s = callback;
        parobj = par;

        ss = null;
        return this;
    }

    public UIEventSignal SetListener(EventSignalObjects callbacks, params object[] par)
    {
        ss = callbacks;
        parobjs = par;

        s = null;
        return this;
    }

    public void RemoveListener()
    {
        s = null;
        parobj = null;
        parobjs = null;
    }

    public void Invoke()
    {
        if (IsRelease)
        {
            if (s != null)
            {
                s(parobj, this);
            }
            if (ss != null)
            {
                ss(this, parobjs);
            }
            EffectInvokeSignal.Dispatch();
        }
        else
        {//如果是到时间自动释放
            if (AutoReleaseTime > 0)
            {
                if (Time.realtimeSinceStartup - StartRetainTime > AutoReleaseTime)
                {
                    IsRelease = true; 
                    if (s != null)
                    {
                        s(parobj, this);
                    }
                    if (ss != null)
                    {
                        ss(this, parobjs);
                    }
                    EffectInvokeSignal.Dispatch();
                }
            }
        }
    }

    public void Retain(float releaseTime = 0)
    {
        IsRelease = false;
        StartRetainTime = Time.realtimeSinceStartup;
        AutoReleaseTime = releaseTime;
        if (FollowedBtn != null)
        {
            FollowedBtn.interactable = IsRelease;
            //Btn得释放走时间
            TimerUtils.Timers.TimerStart(releaseTime , BtnRetainRelease
                , FollowedBtn, FollowTargetMode.TargetDisactiveDoNothing); 
        }
    }

    private void BtnRetainRelease()
    {
        FollowedBtn.interactable = true;
    }

    public void Release()
    {
        IsRelease = true;
        if (FollowedBtn != null)
        {
            FollowedBtn.interactable = IsRelease;
        }
    }

    public UIEventSignal Reset()
    {
        s = null;
        parobj = null;

        ss = null;
        parobjs = null;
        //不是Release的事件将会被抛弃
        IsRelease = true;

        StartRetainTime = 0;

        AutoReleaseTime = 0;


        if (FollowedBtn != null)
        {
            FollowedBtn.interactable = IsRelease;
        }
        return this;
    }
}
