using UnityEngine;
using TMPro;

public class PlayerInfoCardSpawner : MonoBehaviour
{
    [SerializeField] private UnitInfoCard cardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private PlayerUnitDisplayData[] units;
    [SerializeField] private TMP_Text unitCountText;

    private void Start()
    {
        foreach (PlayerUnitDisplayData data in units)
        {
            UnitInfoCard card = Instantiate(cardPrefab, content);
            card.Setup(data);
        }

        if (unitCountText != null)
            unitCountText.text = $"총 {units.Length}마리";
    }
}
