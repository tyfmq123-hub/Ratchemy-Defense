using UnityEngine;

/// <summary>
/// 버튼 클릭 같은 UI 효과음을 재생합니다.
///
/// 여러 버튼이 같은 효과음을 사용할 수 있도록
/// UISoundManager 오브젝트 하나에서 관리합니다.
/// </summary>
public class UISoundManager : MonoBehaviour
{
    [Header("연결할 Audio Source")]
    [Tooltip("UISoundManager 오브젝트의 Audio Source를 연결하세요.")]
    public AudioSource audioSource;

    [Header("버튼 클릭 효과음")]
    [Tooltip("버튼을 눌렀을 때 재생할 효과음을 연결하세요.")]
    public AudioClip buttonClickSound;

    /// <summary>
    /// 버튼의 On Click 이벤트에서 호출합니다.
    /// </summary>
    public void PlayButtonClickSound()
    {
        if (audioSource == null)
        {
            Debug.LogError(
                "UISoundManager: AudioSource가 연결되지 않았습니다."
            );

            return;
        }

        if (buttonClickSound == null)
        {
            Debug.LogError(
                "UISoundManager: Button Click Sound가 연결되지 않았습니다."
            );

            return;
        }

        // 버튼을 빠르게 여러 번 눌러도
        // 앞의 사운드를 끊지 않고 겹쳐서 재생할 수 있습니다.
        audioSource.PlayOneShot(
            buttonClickSound
        );
    }
}