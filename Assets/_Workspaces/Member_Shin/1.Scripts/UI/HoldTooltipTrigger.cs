using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
///
/// 같은 오브젝트에 Unity Button이 있으면 길게 누른 뒤 손을 뗄 때
/// Button.onClick이 함께 발생하지 않도록 클릭을 차단합니다.
/// </summary>
[DefaultExecutionOrder(-100)]
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
    private Button cachedButton;
    private bool isPointerDown;
    private bool isHolding;
    private bool tooltipShownThisPress;
    private bool tooltipWasVisibleOnPress;
    private bool disabledButtonForSuppress;
    private bool cachedButtonInteractable;
    private float pressStartUnscaledTime;

    private void Awake()
    {
        cachedButton = GetComponent<Button>();
        if (cachedButton == null)
        {
            cachedButton = GetComponentInParent<Button>();
        }
    }

    private void Update()
    {
        // 말풍선이 떠 있는 동안은 이 카드 소환 버튼을 계속 막습니다.
        if (IsTooltipVisible())
        {
            DisableButtonForSuppress();
            return;
        }

        if (!isPointerDown)
        {
            RestoreButtonInteractable();
            return;
        }

        if (!disabledButtonForSuppress &&
            Time.unscaledTime - pressStartUnscaledTime >= holdDuration)
        {
            DisableButtonForSuppress();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        isHolding = true;
        tooltipShownThisPress = false;
        pressStartUnscaledTime = Time.unscaledTime;
        tooltipWasVisibleOnPress =
            HoldTooltipManager.Instance != null &&
            HoldTooltipManager.Instance.IsTooltipVisibleFor(this);

        if (tooltipWasVisibleOnPress)
        {
            DisableButtonForSuppress();
        }

        StopHoldCoroutine();
        holdCoroutine = StartCoroutine(ShowTooltipAfterDelay());
    }

    /// <summary>
    /// 마우스를 떼더라도 이미 표시된 말풍선은 유지합니다.
    /// 아직 말풍선이 나오기 전이라면 대기 중인 코루틴만 정리합니다.
    /// 길게 누르기로 말풍선을 연 입력, 또는 말풍선을 닫기 위한 탭은
    /// 같은 오브젝트의 Button 클릭(유닛 소환 등)으로 이어지지 않게 막습니다.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (ShouldSuppressButtonClick())
        {
            DisableButtonForSuppress();
        }

        isPointerDown = false;
        isHolding = false;
        tooltipShownThisPress = false;
        tooltipWasVisibleOnPress = false;
        StopHoldCoroutine();

        // PointerUp 직후 OnPointerClick이 오므로, 말풍선이 아직 있으면 복구하지 않습니다.
        if (!IsTooltipVisible())
        {
            RestoreButtonInteractable();
        }
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

        if (!isPointerDown)
        {
            yield break;
        }

        if (HoldTooltipManager.Instance == null)
        {
            Debug.LogWarning("화면에 HoldTooltipManager가 없습니다.");
            yield break;
        }

        RectTransform targetRect = GetComponent<RectTransform>();

        HoldTooltipManager.Instance.ShowTooltip(
            targetRect,
            tooltipTitle,
            tooltipDescription,
            this
        );

        tooltipShownThisPress = true;
        DisableButtonForSuppress();
        holdCoroutine = null;
    }

    private bool IsTooltipVisible()
    {
        return HoldTooltipManager.Instance != null &&
               HoldTooltipManager.Instance.IsTooltipVisibleFor(this);
    }

    private bool ShouldSuppressButtonClick()
    {
        if (tooltipShownThisPress || tooltipWasVisibleOnPress || IsTooltipVisible())
        {
            return true;
        }

        return Time.unscaledTime - pressStartUnscaledTime >= holdDuration;
    }

    /// <summary>
    /// Input System UI Input Module은 PointerUp 전에 클릭 여부를 확정하므로
    /// eligibleForClick만으로는 Button.onClick을 막을 수 없습니다.
    /// 손을 떼기 전에 interactable을 끄면 OnPointerClick이 무시됩니다.
    /// </summary>
    private void DisableButtonForSuppress()
    {
        if (cachedButton == null || disabledButtonForSuppress)
        {
            return;
        }

        cachedButtonInteractable = cachedButton.interactable;
        disabledButtonForSuppress = true;
        cachedButton.interactable = false;
    }

    private void RestoreButtonInteractable()
    {
        if (!disabledButtonForSuppress || cachedButton == null)
        {
            return;
        }

        cachedButton.interactable = cachedButtonInteractable;
        disabledButtonForSuppress = false;
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
        isPointerDown = false;
        isHolding = false;
        StopHoldCoroutine();
        RestoreButtonInteractable();
    }
}
