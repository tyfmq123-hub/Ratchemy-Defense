using UnityEngine;
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
        if (Mouse.current == null)
        {
            return;
        }
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            dragStartWorldPosition = cam.ScreenToWorldPoint(mouseScreenPosition);
        }
        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 currentWorldPosition = cam.ScreenToWorldPoint(mouseScreenPosition);
        }

        if (Mouse.current.leftButton.isPressed)
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
            Debug.LogWarning("BackgroundRenderer가 연결되지 않았습니다.");
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
}
