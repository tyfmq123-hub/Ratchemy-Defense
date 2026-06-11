using System.Collections;
using UnityEngine;

/// <summary>
/// 배틀씬에 들어왔을 때 카메라 인트로 연출을 담당합니다.
///
/// 흐름:
/// 우리 기지 잠깐 보기
/// → 오른쪽 보스 위치로 천천히 이동
/// → 보스 위치 잠깐 보기
/// → 다시 우리 기지로 복귀
/// → 잠깐 대기
///
/// 현재 단계에서는 카메라 이동만 테스트합니다.
/// 웨이브 시작 지연 연결은 다음 단계에서 추가합니다.
/// </summary>
public class BattleIntroCamera : MonoBehaviour
{
    [Header("연결할 카메라 컨트롤러")]
    [Tooltip("Main Camera에 붙어 있는 BattleCameraController를 연결하세요.")]
    public BattleCameraController battleCameraController;

    [Header("연결할 웨이브 매니저")]
    [Tooltip("카메라 인트로가 끝난 뒤 첫 번째 웨이브를 시작합니다.")]
    public WaveManager waveManager;

    [Header("연결할 코스트 매니저")]
    [Tooltip("인트로가 끝난 뒤 코스트 자동 회복을 시작합니다.")]
    public CostManager costManager;

    [Header("인트로 시간 설정")]
    [Tooltip("처음 우리 기지를 보여주는 시간입니다.")]
    public float playerBaseViewDuration = 0.8f;

    [Tooltip("우리 기지에서 보스 위치까지 이동하는 시간입니다.")]
    public float moveToBossDuration = 1.8f;

    [Tooltip("보스 위치에서 잠깐 멈추는 시간입니다.")]
    public float bossViewDuration = 1.0f;

    [Tooltip("보스 위치에서 우리 기지까지 돌아오는 시간입니다.")]
    public float returnToBaseDuration = 1.8f;

    [Tooltip("우리 기지로 돌아온 뒤 잠깐 멈추는 시간입니다.")]
    public float afterReturnWaitDuration = 0.8f;

    private void Start()
    {
        StartCoroutine(PlayIntroRoutine());
    }

    /// <summary>
    /// 카메라 인트로 연출 전체 흐름입니다.
    /// </summary>
    private IEnumerator PlayIntroRoutine()
    {
        if (battleCameraController == null)
        {
            Debug.LogError(
                "BattleIntroCamera: BattleCameraController가 연결되지 않았습니다."
            );

            yield break;
        }

        // 인트로 중에는 플레이어가 마우스로 카메라를 움직이지 못하게 합니다.
        battleCameraController.SetPlayerControlEnabled(false);

        // 카메라 연출 중에는 코스트가 차지 않게 합니다.
        if (costManager != null)
        {
            costManager.SetRecoveryEnabled(false);
        }

        Vector3 playerBasePosition =
            battleCameraController.GetPlayerBaseCameraPosition();

        Vector3 bossPosition =
            battleCameraController.GetBossCameraPosition();

        // 배틀씬 진입 직후에는 우리 기지를 바라봅니다.
        battleCameraController.SetCameraPosition(
            playerBasePosition
        );

        yield return new WaitForSeconds(
            playerBaseViewDuration
        );

        // 우리 기지에서 보스 위치까지 이동합니다.
        yield return MoveCameraRoutine(
            playerBasePosition,
            bossPosition,
            moveToBossDuration
        );

        // 보스를 잠깐 보여줍니다.
        yield return new WaitForSeconds(
            bossViewDuration
        );

        // 다시 우리 기지 쪽으로 돌아옵니다.
        yield return MoveCameraRoutine(
            bossPosition,
            playerBasePosition,
            returnToBaseDuration
        );

        // 복귀 직후 바로 전투가 시작되면 급해 보이므로
        // 짧게 숨을 고르는 시간을 둡니다.
        yield return new WaitForSeconds(
            afterReturnWaitDuration
        );

        // 인트로가 끝났으므로 플레이어의 카메라 조작을 다시 허용합니다.
        battleCameraController.SetPlayerControlEnabled(true);

        // WAVE 1 시작 시점부터 코스트가 차기 시작합니다.
        if (costManager != null)
        {
            costManager.SetRecoveryEnabled(true);
        }
        else
        {
            Debug.LogError(
                "BattleIntroCamera: CostManager가 연결되지 않았습니다."
            );
        }

        // 카메라가 우리 기지로 돌아오고 잠깐 대기한 뒤
        // 첫 번째 웨이브를 시작합니다.
        //
        // StartBattleWaves() 내부에는 중복 실행 방지 기능이 있으므로
        // 실수로 여러 번 호출되어도 첫 웨이브가 겹쳐서 시작되지 않습니다.
        if (waveManager != null)
        {
            waveManager.StartBattleWaves();
        }
        else
        {
            Debug.LogError(
                "BattleIntroCamera: WaveManager가 연결되지 않았습니다."
            );
        }
    }

    /// <summary>
    /// 카메라를 시작 위치에서 목표 위치까지 부드럽게 이동합니다.
    /// </summary>
    private IEnumerator MoveCameraRoutine(
        Vector3 startPosition,
        Vector3 targetPosition,
        float duration
    )
    {
        // 이동 시간이 0 이하라면 즉시 목표 위치로 보냅니다.
        if (duration <= 0f)
        {
            battleCameraController.SetCameraPosition(
                targetPosition
            );

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime / duration
                );

            // 시작과 끝에서 이동 속도를 부드럽게 줄입니다.
            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            Vector3 nextPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smoothProgress
                );

            battleCameraController.SetCameraPosition(
                nextPosition
            );

            yield return null;
        }

        // 마지막에는 정확히 목표 위치에 맞춥니다.
        battleCameraController.SetCameraPosition(
            targetPosition
        );
    }
}