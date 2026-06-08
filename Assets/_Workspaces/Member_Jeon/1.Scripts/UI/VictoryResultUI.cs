using UnityEngine;

public class VictoryResultUI : MonoBehaviour
{
    private ResultUICanvas resultCanvas;

    private void Awake()
    {
        resultCanvas = GetComponentInParent<ResultUICanvas>();
    }

    public void Show()
    {
        if (resultCanvas == null)
            resultCanvas = GetComponentInParent<ResultUICanvas>();

        resultCanvas?.BringToFront();
        gameObject.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("[VictoryResultUI] 승리 화면 표시");
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
