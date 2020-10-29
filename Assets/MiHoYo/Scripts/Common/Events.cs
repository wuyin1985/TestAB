using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//全部基础类型Event
public delegate void EventVoid();
public delegate void EventObject(object par);
public delegate void EventInt(int par);
public delegate void EventUInt(uint par);
public delegate void EventLong(long par);
public delegate void EventFloat(float par);
public delegate void EventString(string par);
public delegate void EventBoolean(bool par);
public delegate void EventKeyCode(KeyCode key);
public delegate void EventVector3(Vector3 par);
public delegate void EventVector2(Vector2 par);
public delegate void EventTransform(Transform par);
public delegate void EventGameObject(GameObject par);
public delegate void EventPointerEvent(GameObject par);
public delegate void EventBaseEvent(GameObject par);
public delegate void EventObjectBoolean(object par, bool boo);

public delegate void EventSignalObject(object par, UIEventSignal signal);
public delegate void EventSignalObjects(UIEventSignal signal, params object[] par);
public delegate void EventSignalItemObject(int itemIndex, object itemData, object par, UIEventSignal signal);
public delegate void EventSignalItemObjects(int itemIndex, object itemData, UIEventSignal signal, params object[] par);


public delegate void EventInt2(int n1, int n2);
public delegate void EventInt3(int n1, int n2, int n3);
public delegate void EventInt4(int n1, int n2, int n3, int n4);
public delegate void EventLong2(long n1, long n2);
public delegate void EventLong3(long n1, long n2, long n3);
public delegate void EventFloat2(float f1, float f2);
public delegate void EventFloat3(float f1, float f2, float f3);
public delegate void EventBool(bool b);
public delegate void EventBool2(bool b1, bool b2);
public delegate void EventBool3(bool b1, bool b2, bool b3);
public delegate void EventString2(string str1, string str2);
public delegate void EventString3(string str1, string str2, string str3);
public delegate void EventObject2(object obj1, object obj2);
public delegate void EventObject3(object obj1, object obj2, object obj3);
public delegate void EventObject4(object obj1, object obj2, object obj3, object obj4);

//全部泛型类型Event
public delegate void EventT<T>(T t);
public delegate void EventT<T, U>(T t, U u);
public delegate void EventT<T, U, V>(T t, U u, V v);
public delegate void EventT<T, U, V, W>(T t, U u, V v, W w);
public delegate void EventT<T, U, V, W, X>(T t, U u, V v, W w, X x);
public delegate void EventT<T, U, V, W, X, Y>(T t, U u, V v, W w, X x, Y y);
public delegate void EventT<T, U, V, W, X, Y, Z>(T t, U u, V v, W w, X x, Y y, Z z);
public delegate void EventListT<T>(List<T> listT);
public delegate void EventListTU<T, U>(List<T> listT, List<U> listU);
public delegate void EventListTUV<T, U, V>(List<T> listT, List<U> listU, List<V> listV);

/// <summary>
/// 只会触发一次的Callback，用于UI居多
/// </summary>
public class OnceCallBackButton
{
    private EventVoid cb;
    //点击过了
    private bool Clicked = false;

    public void Dispatch()
    {
        if (Clicked) return;
        if (cb != null)
        {
            Clicked = true;
            cb();
        }
    }

    public void ResetClickAgain()
    {
        Clicked = false;
    }

    public OnceCallBackButton() { }

    public OnceCallBackButton(EventVoid callback)
    {
        SetCallBack(callback);
    }

    public void SetCallBack(EventVoid callback)
    {
        cb = callback;
    }
}
