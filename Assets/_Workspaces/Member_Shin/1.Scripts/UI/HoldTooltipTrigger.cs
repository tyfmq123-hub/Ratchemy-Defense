using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 말풍선 꼬리를 박스의 어느 방향에 붙일지 정합니다.
/// </summary>
public enum TooltipTailSide
{
    Top,
    Bottom,
    Left,
    Right
}

/// <summary>
/// 이 스크립트를 붙인 UI를 일정 시간 이상 누르면
/// HoldTooltipManager를 통해 설명 말풍선을 표시합니다.
///
/// 마우스 클릭과 모바일 터치를 함께 지원합니다.
/// 각 UI마다 말풍선 위치와 꼬리 방향을 Inspector에서 직접 설정합니다.
/// </summary>
public class HoldTooltipTrigger : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [Header("말풍선 내용")]
    [Tooltip("말풍선 첫 줄에 표시할 제목입니다.")]
    public string tooltipTitle;

    [TextArea(2, 4)]
    [Tooltip("UI 기능을 설명하는 문구입니다.")]
    public string tooltipDescription;

    [Header("길게 누르기 설정")]
    [Tooltip("몇 초간 누르면 말풍선을 표시할지 결정합니다.")]
    public float holdDuration = 2f;

    [Header("말풍선 위치 직접 조정")]
    [Tooltip("말풍선 박스 전체 위치를 조정합니다. X는 좌우, Y는 상하 이동입니다.")]
    public Vector2 tooltipOffset = new Vector2(0f, 85f);

    [Header("말풍선 꼬리 설정")]
    [Tooltip("꼬리를 말풍선 박스의 어느 방향에 붙일지 선택합니다.")]
    public TooltipTailSide tailSide = TooltipTailSide.Top;

    [Tooltip("꼬리만 미세 조정합니다. X는 좌우, Y는 상하 이동입니다.")]
    public Vector2 tailOffset = Vector2.zero;

    private Coroutine holdCoroutine;
    private bool isHolding;

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;

        StopHoldCoroutine();
        holdCoroutine = StartCoroutine(ShowTooltipAfterDelay());
    }

    /// <summary>
    /// 마우스를 떼더라도 이미 표시된 말풍선은 유지합니다.
    /// 아직 말풍선이 나오기 전이라면 대기 중인 코루틴만 정리합니다.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        StopHoldCoroutine();
    }

    /// <summary>
    /// 길게 누르기 완료 전에 UI 밖으로 벗어나면 대기만 취소합니다.
    /// 이미 표시된 말풍선은 유지합니다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHolding = false;
        StopHoldCoroutine();
    }

    private IEnumerator ShowTooltipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(holdDuration);

        if (!isHolding)
        {
            yield break;
        }

        if (HoldTooltipManager.Instance == null)
        {
            Debug.LogWarning("화면에 HoldTooltipManager가 없습니다.");
            yield break;
        }

        RectTransform targetRect = GetComponent<RectTransform>();

        // this를 함께 전달합니다.
        // 덕분에 말풍선 표시 중 Inspector 값을 바꾸면
        // Manager가 최신 tooltipOffset, tailOffset 값을 계속 읽을 수 있습니다.
        HoldTooltipManager.Instance.ShowTooltip(
            targetRect,
            tooltipTitle,
            tooltipDescription,
            this
        );

        holdCoroutine = null;
    }

    private void StopHoldCoroutine()
    {
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private void OnDisable()
    {
        isHolding = false;
        StopHoldCoroutine();
    }
}
