using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyInfoCardSpawner : MonoBehaviour
{
    [SerializeField] private UnitInfoCard cardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private EnemyUnitData[] enemies;
    [SerializeField] private TMP_Text enemyCountText;
    [SerializeField] private UnitInfoPopup enemyPopup;
    [SerializeField] private UISoundManager uiSoundManager;

    private void Start()
    {
        foreach (EnemyUnitData data in enemies)
        {
            UnitInfoCard card = Instantiate(cardPrefab, content);
            card.SetPopup(enemyPopup);
            card.Setup(data);

            if (uiSoundManager != null)
            {
                Button btn = card.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(uiSoundManager.PlayButtonClickSound);
            }
        }

        if (enemyCountText != null)
            enemyCountText.text = $"총 {enemies.Length}마리";
    }
}
