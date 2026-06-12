using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 전투 화면의 공용 설명 말풍선을 관리합니다.
///
/// 길게 누른 UI에 맞춰 제목, 설명, 박스 위치, 꼬리 위치를 바꿔 재사용합니다.
/// 말풍선은 마우스를 떼어도 유지되며, 이후 새 클릭 또는 터치로 닫힙니다.
///
/// 현재 표시 중인 UI의 Inspector 값을 매 프레임 다시 읽으므로,
/// Play 중 Tooltip Offset과 Tail Offset을 수정하면 화면에서 즉시 확인할 수 있습니다.
/// </summary>
public class HoldTooltipManager : MonoBehaviour
{
    public static HoldTooltipManager Instance { get; private set; }

    [Header("말풍선 UI 연결")]
    [Tooltip("Canvas 아래에 만든 TooltipBubble 오브젝트를 연결하세요.")]
    public GameObject tooltipBubble;

    [Tooltip("말풍선 박스 TooltipBubble의 RectTransform을 연결하세요.")]
    public RectTransform tooltipRect;

    [Tooltip("말풍선 제목 TextMeshPro를 연결하세요.")]
    public TextMeshProUGUI titleText;

    [Tooltip("말풍선 설명 TextMeshPro를 연결하세요.")]
    public TextMeshProUGUI descriptionText;

    [Tooltip("TooltipBubble 자식으로 만든 삼각형 Tail의 RectTransform을 연결하세요.")]
    public RectTransform tailRect;

    [Header("말풍선 화면 경계 설정")]
    [Tooltip("켜면 말풍선이 화면 밖으로 나가지 않도록 자동 제한합니다. 직접 위치를 세밀하게 조절하려면 끄세요.")]
    public bool keepInsideCanvas = false;

    private RectTransform canvasRect;
    private Canvas canvas;

    private bool isTooltipVisible;
    private bool waitForInitialRelease;

    // 현재 말풍선을 띄운 UI와 설정 컴포넌트를 기억합니다.
    // Play 중 Inspector 값을 조절할 때 즉시 반영하기 위해 필요합니다.
    private RectTransform activeTarget;
    private HoldTooltipTrigger activeTrigger;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }

        HideTooltip();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!isTooltipVisible)
        {
            return;
        }

        // 현재 표시 중인 UI의 최신 Inspector 값을 계속 읽습니다.
        // Play 중 Tooltip Offset 또는 Tail Offset을 바꾸면 바로 움직입니다.
        if (activeTarget != null && activeTrigger != null)
        {
            RefreshTooltipLayout(
                activeTarget,
                activeTrigger.tooltipOffset,
                activeTrigger.tailSide,
                activeTrigger.tailOffset
            );
        }

        // 말풍선을 띄우는 데 사용한 입력을 아직 누르고 있다면
        // 닫기 입력으로 처리하지 않고 손을 뗄 때까지 기다립니다.
        if (waitForInitialRelease)
        {
            if (!IsPointerPressed())
            {
                waitForInitialRelease = false;
            }

            return;
        }

        // 길게 누르기 입력이 끝난 뒤 발생한 새 클릭 또는 터치로 닫습니다.
        if (IsNewPointerDown())
        {
            HideTooltip();
        }
    }

    public void ShowTooltip(
        RectTransform target,
        string title,
        string description,
        HoldTooltipTrigger trigger)
    {
        if (tooltipBubble == null ||
            tooltipRect == null ||
            target == null ||
            trigger == null ||
            canvasRect == null ||
            canvas == null)
        {
            Debug.LogWarning("HoldTooltipManager 연결 상태를 확인하세요.");
            return;
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        activeTarget = target;
        activeTrigger = trigger;

        RefreshTooltipLayout(
            activeTarget,
            activeTrigger.tooltipOffset,
            activeTrigger.tailSide,
            activeTrigger.tailOffset
        );

        tooltipBubble.SetActive(true);
        isTooltipVisible = true;

        // 현재 길게 누르는 중인 입력은 닫기 클릭으로 처리하지 않습니다.
        waitForInitialRelease = true;
    }

    public void HideTooltip()
    {
        if (tooltipBubble != null)
        {
            tooltipBubble.SetActive(false);
        }

        isTooltipVisible = false;
        waitForInitialRelease = false;
        activeTarget = null;
        activeTrigger = null;
    }

    /// <summary>
    /// 이 UI에 대한 설명 말풍선이 현재 표시 중인지 확인합니다.
    /// 같은 카드에서 말풍선을 닫는 탭이 소환 클릭으로 이어지지 않도록 할 때 사용합니다.
    /// </summary>
    public bool IsTooltipVisibleFor(HoldTooltipTrigger trigger)
    {
        return isTooltipVisible && activeTrigger == trigger;
    }

    /// <summary>
    /// 말풍선 박스 전체 위치와 꼬리 위치를 최신 설정값으로 갱신합니다.
    /// </summary>
    private void RefreshTooltipLayout(
        RectTransform target,
        Vector2 tooltipOffset,
        TooltipTailSide tailSide,
        Vector2 tailOffset)
    {
        Vector3 targetWorldPosition = target.TransformPoint(target.rect.center);

        Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera,
            targetWorldPosition
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetScreenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera,
            out Vector2 localPosition
        );

        localPosition += tooltipOffset;

        // 필요할 때만 화면 안쪽으로 제한합니다.
        // 직접 위치를 세밀하게 조절하려면 Inspector에서 Keep Inside Canvas를 끄세요.
        if (keepInsideCanvas)
        {
            float halfWidth = tooltipRect.rect.width * 0.5f;
            float halfHeight = tooltipRect.rect.height * 0.5f;

            float minX = canvasRect.rect.xMin + halfWidth;
            float maxX = canvasRect.rect.xMax - halfWidth;
            float minY = canvasRect.rect.yMin + halfHeight;
            float maxY = canvasRect.rect.yMax - halfHeight;

            localPosition.x = Mathf.Clamp(localPosition.x, minX, maxX);
            localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);
        }

        tooltipRect.anchoredPosition = localPosition;

        UpdateTailPosition(tailSide, tailOffset);
    }

    private bool IsPointerPressed()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        return false;
    }

    private bool IsNewPointerDown()
    {
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 꼬리를 말풍선의 선택한 가장자리에 붙인 뒤 X, Y 값으로 미세 조정합니다.
    /// Tail 이미지의 기본 방향은 아래쪽 삼각형(▼) 기준입니다.
    /// </summary>
    private void UpdateTailPosition(
        TooltipTailSide tailSide,
        Vector2 tailOffset)
    {
        if (tailRect == null || tooltipRect == null)
        {
            return;
        }

        if (tailRect.parent != tooltipRect)
        {
            tailRect.SetParent(tooltipRect, false);
        }

        tailRect.pivot = new Vector2(0.5f, 0.5f);

        switch (tailSide)
        {
            case TooltipTailSide.Top:
                tailRect.anchorMin = new Vector2(0.5f, 1f);
                tailRect.anchorMax = new Vector2(0.5f, 1f);
                tailRect.anchoredPosition = tailOffset;
                tailRect.localRotation = Quaternion.Euler(0f, 0f, 180f);
                break;

            case TooltipTailSide.Bottom:
                tailRect.anchorMin = new Vector2(0.5f, 0f);
                tailRect.anchorMax = new Vector2(0.5f, 0f);
                tailRect.anchoredPosition = tailOffset;
                tailRect.localRotation = Quaternion.identity;
                break;

            case TooltipTailSide.Left:
                tailRect.anchorMin = new Vector2(0f, 0.5f);
                tailRect.anchorMax = new Vector2(0f, 0.5f);
                tailRect.anchoredPosition = tailOffset;
                tailRect.localRotation = Quaternion.Euler(0f, 0f, -90f);
                break;

            case TooltipTailSide.Right:
                tailRect.anchorMin = new Vector2(1f, 0.5f);
                tailRect.anchorMax = new Vector2(1f, 0.5f);
                tailRect.anchoredPosition = tailOffset;
                tailRect.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
        }
    }
}
