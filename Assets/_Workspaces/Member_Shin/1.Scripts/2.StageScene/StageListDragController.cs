using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스테이지 목록을 마우스 클릭 드래그로 위아래 이동시키는 스크립트입니다.
///
/// Scroll View 프리팹이나 Scroll Rect 컴포넌트를 사용하지 않고,
/// StageListViewport 영역에서 직접 드래그 입력을 받아
/// StageListContent의 위치만 움직입니다.
/// </summary>
public class StageListDragController : MonoBehaviour, IDragHandler
{
    [Header("연결할 UI")]
    [Tooltip("버튼 목록이 들어 있는 StageListContent를 연결합니다.")]
    public RectTransform stageListContent;

    [Tooltip("목록이 실제로 보이는 영역인 StageListViewport를 연결합니다.")]
    public RectTransform stageListViewport;

    [Header("드래그 감도")]
    [Tooltip("마우스를 움직인 거리 대비 목록이 이동하는 정도입니다.")]
    public float dragSpeed = 1f;

    /// <summary>
    /// 마우스를 누른 상태로 움직일 때 자동으로 호출됩니다.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // Inspector 연결이 빠졌다면 오류가 나지 않도록 중단합니다.
        if (stageListContent == null || stageListViewport == null)
        {
            return;
        }

        // 마우스가 위로 움직이면 delta.y가 양수입니다.
        // 목록도 위로 올라가면서 아래쪽 스테이지가 보이도록 합니다.
        float nextY =
            stageListContent.anchoredPosition.y
            + eventData.delta.y * dragSpeed;

        // 전체 목록 높이에서 화면에 보이는 영역 높이를 뺍니다.
        // 이 값이 목록을 위로 올릴 수 있는 최대 거리입니다.
        float maxScrollY =
            Mathf.Max(
                0f,
                stageListContent.rect.height
                - stageListViewport.rect.height
            );

        // 목록이 위쪽이나 아래쪽 경계를 넘어가지 않도록 제한합니다.
        nextY = Mathf.Clamp(
            nextY,
            0f,
            maxScrollY
        );

        // 계산한 위치를 실제 Content에 적용합니다.
        stageListContent.anchoredPosition =
            new Vector2(
                stageListContent.anchoredPosition.x,
                nextY
            );
    }
}