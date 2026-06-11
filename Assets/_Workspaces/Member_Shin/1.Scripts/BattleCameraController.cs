using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BattleCameraController : MonoBehaviour
{
    [Header("배경 오브젝트")]
    public SpriteRenderer backgroundRenderer;

    [Header("카메라 기본 설정")]
    public float fixedY = 0f;
    public float fixedZ = -10f;
    public float cameraSize = 3.5f;

    private Camera cam;
    private float minX;
    private float maxX;
    private Vector3 dragStartWorldPosition;

    // 카드나 버튼 위에서 클릭했을 때 카메라 이동이 시작되지 않도록
    // 실제로 카메라 드래그를 허용한 상태인지 저장합니다.
    private bool isCameraDragging = false;

    // true이면 플레이어가 마우스로 카메라를 움직일 수 있습니다.
    // 배틀 시작 연출 중에는 false로 바꿔서 직접 조작을 막습니다.
    private bool isPlayerControlEnabled = true;

    void Awake()
    {
        cam = GetComponent<Camera>();

        cam.orthographic = true;
        cam.orthographicSize = cameraSize;

        CalculateCameraLimit();

        transform.position = new Vector3(minX, fixedY, fixedZ);
    }

    void Update()
    {
        // 배틀 시작 카메라 연출이 진행되는 동안에는
        // 플레이어의 마우스 드래그 입력을 받지 않습니다.
        if (!isPlayerControlEnabled)
        {
            isCameraDragging = false;
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        // 마우스를 처음 눌렀을 때만 드래그 시작 여부를 결정합니다.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 유닛 카드, 버튼 등 UI 위에서 클릭했다면
            // 마우스를 계속 누른 채 바깥으로 이동해도 카메라가 움직이지 않습니다.
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                isCameraDragging = false;
                return;
            }

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            dragStartWorldPosition = cam.ScreenToWorldPoint(mouseScreenPosition);
            isCameraDragging = true;
        }

        // 마우스를 놓으면 카메라 드래그 상태를 종료합니다.
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isCameraDragging = false;
            return;
        }

        // UI가 아닌 전투 화면에서 드래그를 시작한 경우에만 카메라를 이동합니다.
        if (Mouse.current.leftButton.isPressed && isCameraDragging)
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 currentWorldPosition = cam.ScreenToWorldPoint(mouseScreenPosition);

            Vector3 dragDifference = dragStartWorldPosition - currentWorldPosition;

            Vector3 nextPosition = transform.position;
            nextPosition.x += dragDifference.x;
            nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
            nextPosition.y = fixedY;
            nextPosition.z = fixedZ;

            transform.position = nextPosition;
        }
    }

    private void CalculateCameraLimit()
    {
        if (backgroundRenderer == null)
        {
            minX = transform.position.x;
            maxX = transform.position.x;
            return;
        }

        Bounds bgBounds = backgroundRenderer.bounds;
        float cameraHalfHeight = cam.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * cam.aspect;

        minX = bgBounds.min.x + cameraHalfWidth;
        maxX = bgBounds.max.x - cameraHalfWidth;

        if (minX > maxX)
        {
            float centerX = bgBounds.center.x;
            minX = centerX;
            maxX = centerX;
        }
    }
    /// <summary>
    /// 플레이어가 마우스로 카메라를 움직일 수 있는지 설정합니다.
    /// 배틀 시작 연출 중에는 false, 연출 종료 후에는 true를 전달합니다.
    /// </summary>
    public void SetPlayerControlEnabled(bool enabled)
    {
        isPlayerControlEnabled = enabled;

        // 마우스를 누르고 있던 도중 조작이 잠기더라도
        // 기존 드래그 상태가 남지 않도록 초기화합니다.
        if (!enabled)
        {
            isCameraDragging = false;
        }
    }

    /// <summary>
    /// 우리 기지가 보이는 왼쪽 끝 위치를 반환합니다.
    /// </summary>
    public Vector3 GetPlayerBaseCameraPosition()
    {
        return new Vector3(
            minX,
            fixedY,
            fixedZ
        );
    }

    /// <summary>
    /// 보스가 있는 오른쪽 끝 위치를 반환합니다.
    /// </summary>
    public Vector3 GetBossCameraPosition()
    {
        return new Vector3(
            maxX,
            fixedY,
            fixedZ
        );
    }

    /// <summary>
    /// 자동 연출에서 사용할 함수입니다.
    /// 카메라 위치를 전달받아 허용된 범위 안에서 이동합니다.
    /// </summary>
    public void SetCameraPosition(Vector3 targetPosition)
    {
        targetPosition.x =
            Mathf.Clamp(
                targetPosition.x,
                minX,
                maxX
            );

        targetPosition.y = fixedY;
        targetPosition.z = fixedZ;

        transform.position = targetPosition;
    }
}
