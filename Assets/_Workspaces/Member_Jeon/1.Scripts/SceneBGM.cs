using UnityEngine;

public class SceneBGM : MonoBehaviour
{
    [SerializeField] private AudioClip clip;

    private void Start()
    {
        if (clip == null)
            return;

        if (BGMManager.Instance == null)
        {
            Debug.LogWarning("[SceneBGM] BGMManager.Instance가 없습니다. 0.App 씬에 BGMManager를 배치했는지 확인하세요.");
            return;
        }

        BGMManager.Instance.Play(clip);
    }
}
