using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [Header("연결할 UI")]
    public TextMeshProUGUI waveText;

    public void SetWave(int currentWave)
    {
        waveText.text =
            "WAVE." + currentWave;
    }

    public void ShowReady()
    {
        waveText.text = "READY";
    }

    public void ShowComplete()
    {
        waveText.text = "FINAL";
    }

    public void ShowBossStage()
    {
        waveText.text = "BOSS STAGE";
    }
}