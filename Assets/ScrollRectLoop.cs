using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ScrollRectLoop : ScrollRect
{

    public override void OnDrag(PointerEventData data)
    {
        base.OnScroll(data);
        Debug.Log(data);
    }

    public override void StopMovement()
    {
        base.StopMovement();
    }
}