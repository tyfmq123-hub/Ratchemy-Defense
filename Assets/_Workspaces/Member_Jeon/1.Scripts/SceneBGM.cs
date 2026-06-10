using UnityEngine;

public class SceneBGM : MonoBehaviour
{
    [SerializeField] private AudioClip clip;

    private void Start()
    {
        if (clip == null)
            return;

        if (BgmManager.Instance == null)
        {
            Debug.LogWarning("[SceneBGM] BgmManager.Instance가 없습니다. 0.App 씬에 BgmManager를 배치했는지 확인하세요.");
            return;
        }

        BgmManager.Instance.Play(clip);
    }
}
