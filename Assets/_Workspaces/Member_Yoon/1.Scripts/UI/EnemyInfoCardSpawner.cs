using UnityEngine;
using TMPro;

public class EnemyInfoCardSpawner : MonoBehaviour
{
    [SerializeField] private UnitInfoCard cardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private EnemyUnitData[] enemies;
    [SerializeField] private TMP_Text enemyCountText;

    private void Start()
    {
        foreach (EnemyUnitData data in enemies)
        {
            UnitInfoCard card = Instantiate(cardPrefab, content);
            card.Setup(data);
        }

        if (enemyCountText != null)
            enemyCountText.text = $"총 {enemies.Length}마리";
    }
}
