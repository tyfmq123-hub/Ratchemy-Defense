using TMPro;          // TextMeshPro 텍스트를 사용하기 위해 필요합니다.
using UnityEngine;    // Unity 기본 기능을 사용하기 위해 필요합니다.
using UnityEngine.UI; // UI Image를 사용하기 위해 필요합니다.

public class CostManager : MonoBehaviour
{
    [Header("코스트 설정")]
    public int maxCost = 20;        // 가질 수 있는 최대 코스트입니다.
    public int startCost = 0;       // 게임 시작 시 보유할 코스트입니다.
    public float recoveryTime = 1f; // 코스트 1개가 회복되는 간격입니다.

    [Header("연결할 UI")]
    public Image[] costFillImages;       // 노란색 코스트 이미지 20개를 연결합니다.
    public TextMeshProUGUI costText;     // 현재 코스트 숫자를 표시합니다.

    // 현재 플레이어가 가지고 있는 코스트입니다.
    private int currentCost;

    // 다음 코스트 회복까지 흐른 시간을 저장합니다.
    private float recoveryTimer;

    // 다른 스크립트에서 현재 코스트를 읽을 수 있게 공개합니다.
    // 외부에서 값을 직접 수정하지는 못하게 막습니다.
    public int CurrentCost => currentCost;

    void Start()
    {
        // 시작 코스트가 0보다 작거나 최대값을 넘지 않도록 제한합니다.
        currentCost = Mathf.Clamp(
            startCost,
            0,
            maxCost
        );

        // 게임 시작 시 UI를 한 번 갱신합니다.
        UpdateCostUI();
    }

    void Update()
    {
        // 이미 최대 코스트라면 더 이상 회복하지 않습니다.
        if (currentCost >= maxCost)
        {
            recoveryTimer = 0f;
            return;
        }

        // 실제 경과 시간을 누적합니다.
        recoveryTimer += Time.deltaTime;

        // 설정한 시간이 지나면 코스트를 1개 회복합니다.
        if (recoveryTimer >= recoveryTime)
        {
            recoveryTimer -= recoveryTime;

            AddCost(1);
        }
    }

    public void AddCost(int amount)
    {
        // 음수를 전달해도 잘못 증가하거나 감소하지 않도록 막습니다.
        if (amount <= 0)
        {
            return;
        }

        // 현재 코스트에 회복량을 더합니다.
        currentCost += amount;

        // 최대 코스트 20을 넘지 않도록 제한합니다.
        currentCost = Mathf.Clamp(
            currentCost,
            0,
            maxCost
        );

        // 변경된 값을 화면에 표시합니다.
        UpdateCostUI();
    }

    public bool TrySpendCost(int amount)
    {
        // 비용이 0 이하라면 잘못된 요청이므로 사용하지 않습니다.
        if (amount <= 0)
        {
            return false;
        }

        // 현재 코스트가 카드 비용보다 적으면
        // 유닛을 생성하지 못하도록 false를 반환합니다.
        if (currentCost < amount)
        {
            Debug.Log(
                "코스트가 부족합니다. 필요 코스트: "
                + amount
                + ", 현재 코스트: "
                + currentCost
            );

            return false;
        }

        // 충분한 코스트가 있다면 카드 비용만큼 차감합니다.
        currentCost -= amount;

        // 혹시라도 0보다 작아지지 않도록 제한합니다.
        currentCost = Mathf.Clamp(
            currentCost,
            0,
            maxCost
        );

        // 변경된 값을 화면에 표시합니다.
        UpdateCostUI();

        // 카드 사용이 가능하다는 의미로 true를 반환합니다.
        return true;
    }

    private void UpdateCostUI()
    {
        // 노란색 이미지 배열이 연결되어 있다면
        // 현재 코스트 수만큼만 이미지를 표시합니다.
        if (costFillImages != null)
        {
            for (
                int i = 0;
                i < costFillImages.Length;
                i++
            )
            {
                // 예:
                // 현재 코스트가 3이라면
                // 배열의 0, 1, 2번 이미지만 표시합니다.
                // 나머지 이미지는 숨깁니다.
                costFillImages[i].enabled =
                    i < currentCost;
            }
        }

        // 숫자 UI가 연결되어 있다면 현재 코스트를 표시합니다.
        if (costText != null)
        {
            costText.text =
                currentCost.ToString();
        }
    }

    [ContextMenu("테스트: 코스트 1 추가")]
    private void TestAddOneCost()
    {
        // Inspector에서 테스트할 때 사용하는 임시 기능입니다.
        AddCost(1);
    }

    [ContextMenu("테스트: 코스트 3 사용")]
    private void TestSpendThreeCost()
    {
        // Inspector에서 테스트할 때 사용하는 임시 기능입니다.
        TrySpendCost(3);
    }
}