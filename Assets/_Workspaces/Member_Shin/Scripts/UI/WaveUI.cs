using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [Header("연결할 UI")]
    public TextMeshProUGUI waveText;

    public void SetWave(int currentWave)
    {
        // WaveManager가 현재 웨이브 숫자를 전달하면
        // 화면에 WAVE.1, WAVE.2 형태로 표시합니다.
        // 이 스크립트는 시간 계산이나 적 생성을 담당하지 않습니다.
        waveText.text =
            "WAVE." + currentWave;
    }

    public void ShowReady()
    {
        // 첫 웨이브가 시작되기 전 대기 상태입니다.
        // 게임 시작 직후 화면에 WAVE.0 대신 READY를 보여줍니다.
        waveText.text = "READY";
    }

    public void ShowComplete()
    {
        // 모든 웨이브의 적 생성이 끝나면 표시합니다.
        // 추후 실제 승리 조건이 추가되면 문구를 변경해도 됩니다.
        waveText.text = "FINAL";
    }
}