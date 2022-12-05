using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class UIScrollRectAdjustor : MonoBehaviour, IEndDragHandler, IBeginDragHandler
{
    private ScrollRect _scrollRect;
    private RectTransform _contentRectTrans;
    private float _minX;
    private float _baseX;
    private float _xOffset;
    [SerializeField, Header("自动移动时的移动速度")]
    private float _moveSpeed = 3000;
    private IEnumerator _autoMoveCoroutine;
    [SerializeField, Header("方向优先")]
    private bool _shouldBaseOnMoveDir;
    private void Start()
    {
        Init();
    }

    private void OnDestroy()
    {
        Destroy();
    }

    public void Init()
    {
        _minX = 0;
        _scrollRect = GetComponent<ScrollRect>();
        _contentRectTrans = _scrollRect.content;
        var gridLayoutGroup = _contentRectTrans.GetComponent<GridLayoutGroup>();
        _baseX = gridLayoutGroup.cellSize.x / 2 + gridLayoutGroup.padding.left;
        _xOffset = gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x;
    }

    public void Destroy()
    {
        StopAutoMove();
        _scrollRect = null;
        _contentRectTrans = null;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_minX == 0)
        {
            var gridLayoutGroup = _contentRectTrans.GetComponent<GridLayoutGroup>();
            _minX = _contentRectTrans.sizeDelta.x - gridLayoutGroup.padding.right - gridLayoutGroup.cellSize.x - gridLayoutGroup.spacing.x / 2;
            _minX *= -1;
        }
        StopAutoMove();
    }

    private bool _isLeftMove;

    public void OnEndDrag(PointerEventData eventData)
    {
        float curX = _contentRectTrans.localPosition.x;
        if (curX < 0)
        {
            _isLeftMove = _scrollRect.velocity.x < 0;

            float suitableX = GetSuitableX(curX);
            StartAutoMove(curX, suitableX);
        }
    }

    private float GetSuitableX(float curX)
    {
        float suitableX = curX;
        int index = 0;
        float leftX;
        float rightX;
        while (suitableX >= _minX)
        {
            leftX = GetX(index);
            rightX = GetX(index + 1);

            if (leftX >= curX && rightX <= curX)
            {
                var leftXOffset = Mathf.Abs(leftX - curX);
                var rightXOffset = Mathf.Abs(rightX - curX);

                if (_shouldBaseOnMoveDir)
                {
                    suitableX = _isLeftMove ? rightX : leftX;
                }
                else
                {
                    suitableX = leftXOffset < rightXOffset ? leftX : rightX;
                }
                break;
            }
            index++;
        }

        return Mathf.Max(suitableX, _minX);

        float GetX(int i)
        {
            float value = 0;
            if (i > 0)
            {
                value = (_baseX + _xOffset * i) * -1 + _baseX;
            }
            return value;
        }
    }


    private void StartAutoMove(float beginX, float endX)
    {
        StopAutoMove();

        _autoMoveCoroutine = AutoMoveCoroutine(beginX, endX);
        StartCoroutine(_autoMoveCoroutine);
    }
    private void StopAutoMove()
    {
        if (_autoMoveCoroutine != null)
        {
            StopCoroutine(_autoMoveCoroutine);
            _autoMoveCoroutine = null;
        }
    }

    private IEnumerator AutoMoveCoroutine(float beginX, float endX)
    {
        float timer = 0f;
        float moveTime = Mathf.Abs(beginX - endX) / _moveSpeed;
        while (timer < moveTime)
        {
            _contentRectTrans.localPosition = new Vector3(Mathf.Lerp(beginX, endX, timer / moveTime),
                                                          _contentRectTrans.localPosition.y,
                                                          _contentRectTrans.localPosition.z);
            timer += Time.deltaTime;
            yield return null;
        }

        _contentRectTrans.localPosition = new Vector3(endX, _contentRectTrans.localPosition.y, _contentRectTrans.localPosition.z);
        _scrollRect.StopMovement();
    }
}