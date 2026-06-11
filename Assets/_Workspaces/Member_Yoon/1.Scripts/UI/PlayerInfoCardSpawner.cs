using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInfoCardSpawner : MonoBehaviour
{
    [SerializeField] private UnitInfoCard cardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private PlayerUnitDisplayData[] units;
    [SerializeField] private TMP_Text unitCountText;
    [SerializeField] private UnitInfoPopup allyPopup;
    [SerializeField] private UISoundManager uiSoundManager;

    private void Start()
    {
        foreach (PlayerUnitDisplayData data in units)
        {
            UnitInfoCard card = Instantiate(cardPrefab, content);
            card.SetPopup(allyPopup);
            card.Setup(data);

            if (uiSoundManager != null)
            {
                Button btn = card.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(uiSoundManager.PlayButtonClickSound);
            }
        }

        if (unitCountText != null)
            unitCountText.text = $"총 {units.Length}마리";
    }
}
