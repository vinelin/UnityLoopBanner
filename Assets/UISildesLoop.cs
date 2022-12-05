using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]  //不可重复
[RequireComponent(typeof(RectTransform))] //依赖于RectTransform组件
public class UISildesLoop : MonoBehaviour, IEndDragHandler, IBeginDragHandler, IDragHandler
{
    //运动方向
    public enum MoveDir
    {
        Left,
        Right,
    }
    [SerializeField]
    private RectTransform m_content;
    public RectTransform Content { get { return m_content; } set { m_content = value; } }

    /// <summary>
    /// 自动轮播时长
    /// </summary>
    [SerializeField]
    private float m_showTime = 2.0f;
    public float ShowTime { get { return m_showTime; } set { m_showTime = value; } }

    /// <summary>
    /// 是否自动轮播
    /// </summary>
    [SerializeField]
    private bool m_autoSlide = false;
    public bool AutoSlide { get { return m_autoSlide; } set { m_autoSlide = value; } }

    /// <summary>
    /// 自动轮播方向，0表示向左，1表示向右
    /// </summary>
    public MoveDir m_autoSlideDir = MoveDir.Right;

    /// <summary>
    /// 是否允许拖动切页
    /// </summary>
    [SerializeField]
    private bool m_allowDrag = true;
    public bool AllowDrag { get { return m_allowDrag; } set { m_allowDrag = value; } }

    /// <summary>
    /// 当前显示页的页码，下标从0开始
    /// </summary>
    private int m_curPageIndex = 0;
    public int CurPageIndex
    {
        get
        {
            var index = (m_curPageIndex) % m_itemNum;
            if (index == 0)
                index = m_itemNum;
            return index;
        }
    }

    /// <summary>
    /// item数量（真实数量）
    /// </summary>
    public int m_itemNum = 0;
    public int ItemNum { get { return m_itemNum; } }

    //是否在移动中
    private bool m_isMoving = false;

    //自动滑动计时
    public float m_time;

    private List<Transform> m_childItemTrans = new List<Transform>();

    public GameObject prefab;

    private int[] posArray;

    public float movingTime = 0.2f;
    private float  leftLimit;
    private float rightLimit;
    //图片宽度
    private int width;
    //滑动切换界限
    public float switchLimit = 300f;

    private  void Awake()
    {
        var objNum = 4;
        if (objNum == 1)
            m_allowDrag = false;


        m_time = 2;
        m_showTime = 2;

        float unit_color = 1f / objNum;
        Debug.Log(unit_color);

        //真正的物体        生成物体
        for (int i = 0; i < objNum; i++)
        {
            GameObject go = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, m_content);
            go.name = (i + 1).ToString();
            go.GetComponentInChildren<Text>().text = go.name;
            go.GetComponent<Image>().color = new Color(unit_color * i, unit_color * i, 255);
            Debug.Log(go.GetComponent<Image>().color);
            m_childItemTrans.Add(go.transform);
            go.SetActive(true);
        }

        m_itemNum = m_childItemTrans.Count;

        //无限循环需要特殊处理，需要一个假1
        if (objNum > 1)
        {
            GameObject go = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, m_content);
            go.name = "Fake end";
            m_childItemTrans.Insert(0, go.transform);
            go.SetActive(true);
            go.GetComponentInChildren<Text>().text = "4";
            go.GetComponent<Image>().color = new Color(unit_color * 3, unit_color * 3, 255);
            go.transform.SetAsFirstSibling();

            go = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, m_content);
            go.name = "Fake Begin";
            m_childItemTrans.Add(go.transform);
            go.SetActive(true);
            go.GetComponentInChildren<Text>().text ="1";
            go.GetComponent<Image>().color = new Color(unit_color * 0, unit_color * 0, 255);
            posArray = new int[m_itemNum + 2];
        }
        else
        {
            posArray = new int[m_itemNum];
        }


        InitChildItemPos();
    }

    private void InitChildItemPos()
    {
        int childCount = m_content.transform.childCount;
        var childRect = m_content.GetChild(0).GetComponent<RectTransform>();
        width = (int)childRect.rect.width;
        m_content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * childCount);

        for (int i = 0; i < childCount; i++)
        {
            m_childItemTrans[i].localPosition = new Vector2(i * width, 0);

            posArray[i] = (int)m_content.localPosition.x - i * width;
        }
        if(childCount > 1)
        {
            m_content.transform.localPosition = new Vector2(m_content.localPosition.x - width, m_content.localPosition.y);
            m_curPageIndex = 1;
        }
        leftLimit = 0f;
        rightLimit = -width * (childCount - 1);

    }

    /// <summary>
    /// 切换至某页
    /// </summary>
    /// <param name="pageNum">页码</param>
    private void SwitchToPageNum(int pageNum)
    {
        if (pageNum < 0)
        {
            pageNum = m_itemNum+1;
            m_content.localPosition = new Vector2(posArray[m_itemNum], m_content.localPosition.y);
        }
        //假图1 向右翻页
        else if (pageNum > m_itemNum + 1)
        {
            pageNum = 2;
            m_content.localPosition = new Vector2(posArray[1], m_content.localPosition.y);
        }

        m_curPageIndex = pageNum;

        //暂时实现有缝的无限循环
        m_isMoving = true;

        m_content.DOLocalMoveX(posArray[m_curPageIndex], movingTime).OnComplete(() => {
            m_isMoving = false;
        });

        m_time = m_showTime;
        Debug.Log($"现在真下标   {m_curPageIndex}，假下标{CurPageIndex}");
        if (m_onValueChanged != null)
        {
            //执行回调
            m_onValueChanged.Invoke(CurPageIndex);
        }
    }

    private void OnNext()
    {
        SwitchToPageNum(m_curPageIndex + 1);
    }

    private void OnPrev()
    {
        SwitchToPageNum(m_curPageIndex - 1);
    }

    private bool m_isDrag = false;
    private float beginX = 0f;//开始拖动的X位置
    private float endX = 0f;//结束拖动的X位置


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!m_allowDrag)
        {
            return;
        }
        if (m_isMoving)
        {
            return;
        }
        if (m_itemNum <= 1)
        {
            return;
        }
        beginX = eventData.position.x;
        m_isDrag = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_isDrag)
        {
            var dis = eventData.position.x - beginX;
            var contentX = m_content.localPosition.x;
            m_content.localPosition = new Vector2(contentX + dis - endX, m_content.localPosition.y);
            //记录上一次位移，下次计算抵消 因为已经移动过了
            endX = dis;
            if(m_content.localPosition.x <= rightLimit)
            {
                m_content.localPosition = new Vector2(posArray[1],m_content.localPosition.y);
            }
            else if (m_content.localPosition.x >= leftLimit)
            {
                m_content.localPosition = new Vector2(posArray[m_itemNum], m_content.localPosition.y);
            }
        }
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (!m_isDrag)
            return;
        selfChange();
        endX = 0f;
        beginX = 0f;
        m_isDrag = false;
    }

    //如果要检测是否离开区域，里面的函数和onEndDrag一样
    //public void OnPointerExit(PointerEventData eventData)
    //{
    //    Debug.Log("离开范围");
    //}

    public void selfChange()
    {
        if (endX == 0)
            return;
        //Debug.Log(Mathf.Abs(m_content.localPosition.x % width));
        //取余数,判断这个是否超过了宽度的一半
        //Debug.Log(-m_content.localPosition.x + switchLimit);
        //Debug.Log("当前位置.." + Mathf.FloorToInt((-m_content.localPosition.x+ switchLimit) / width));
        var newPageIndex = m_curPageIndex;
        if (endX < 0)
        {
            newPageIndex = Mathf.FloorToInt((-m_content.localPosition.x + (width - switchLimit)) / width);
        }
        else
        {
            newPageIndex = Mathf.FloorToInt((-m_content.localPosition.x + switchLimit) / width);
        }
        Debug.Log(newPageIndex);


        SwitchToPageNum(newPageIndex);
    }




    /// <summary>
    /// 切页后回调函数
    /// </summary>
    [Serializable]
    public class SlideshowEvent : UnityEvent<int> { }

    [SerializeField]
    private SlideshowEvent m_onValueChanged = new SlideshowEvent();
    public SlideshowEvent OnValueChanged { get { return m_onValueChanged; } set { m_onValueChanged = value; } }

    private void LateUpdate()
    {
        if (m_autoSlide && !m_isDrag && !m_isMoving && (m_childItemTrans.Count > 1))
        {
            if (m_time > 0)
            {
                m_time -= Time.deltaTime;
            }
            else
            {
                switch (m_autoSlideDir)
                {
                    case MoveDir.Left:
                        OnPrev();
                        break;
                    case MoveDir.Right:
                        OnNext();
                        break;
                }
            }
        }
    }
}