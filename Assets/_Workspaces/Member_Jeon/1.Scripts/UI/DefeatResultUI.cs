using UnityEngine;

public class DefeatResultUI : MonoBehaviour
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
        Debug.Log("[DefeatResultUI] 패배 화면 표시");
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
