using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ComicCutsceneUI : MonoBehaviour
{
    [Header("만화 이미지")]
    [SerializeField] private Image comicImage;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button formerButton;

    [Header("크기 기준 (VictoryResultUI > ResultImage)")]
    [SerializeField] private RectTransform sizeReference;
    [SerializeField] private Image sizeReferenceImage;

    [Header("승리 4컷")]
    [SerializeField] private Sprite[] victoryComicSprites;

    [Header("패배 4컷")]
    [SerializeField] private Sprite[] defeatComicSprites;

    [Header("자동 넘김")]
    [SerializeField] private float autoAdvanceDelay = 15f;

    private Sprite[] comicSprites;
    private int currentIndex;
    private Action onFinished;
    private ResultUICanvas resultCanvas;
    private bool isInitialized;
    private Coroutine autoAdvanceCoroutine;

    private void OnDisable()
    {
        StopAutoAdvance();
    }

    private void Awake()
    {
        Initialize();
        ApplyLayoutFromReference();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;
        resultCanvas = GetComponentInParent<ResultUICanvas>();

        if (nextButton != null)
            nextButton.onClick.AddListener(NextCut);

        if (formerButton != null)
            formerButton.onClick.AddListener(PreviousCut);
    }

    public void ShowVictory(Action onFinished = null)
    {
        Show(victoryComicSprites, "승리", onFinished);
    }

    public void ShowDefeat(Action onFinished = null)
    {
        Show(defeatComicSprites, "패배", onFinished);
    }

    public void Show(Sprite[] sprites, Action onFinished = null)
    {
        Show(sprites, null, onFinished);
    }

    private void Show(Sprite[] sprites, string label, Action onFinished)
    {
        this.onFinished = onFinished;

        if (comicImage == null)
        {
            Debug.LogError("[ComicCutsceneUI] Comic Image가 연결되지 않았습니다.");
            return;
        }

        if (sprites == null || sprites.Length == 0)
        {
            var name = string.IsNullOrEmpty(label) ? "만화" : label;
            Debug.LogError($"[ComicCutsceneUI] {name} 스프라이트가 비어 있습니다.");
            return;
        }

        Initialize();
        resultCanvas?.BringToFront();
        ApplyLayoutFromReference();

        gameObject.SetActive(true);
        comicSprites = sprites;
        currentIndex = 0;

        DisplaySprite(comicSprites[currentIndex]);
        UpdateNavigationButtons();

        Time.timeScale = 0f;
    }

    private void DisplaySprite(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError($"[ComicCutsceneUI] {currentIndex}번째 스프라이트가 null입니다.");
            return;
        }

        comicImage.enabled = true;
        comicImage.color = Color.white;
        comicImage.sprite = sprite;

        Debug.Log($"[ComicCutsceneUI] 만화 표시: {sprite.name}");
        StartAutoAdvance();
    }

    private void StartAutoAdvance()
    {
        StopAutoAdvance();

        if (autoAdvanceDelay <= 0f)
            return;

        autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
    }

    private void StopAutoAdvance()
    {
        if (autoAdvanceCoroutine == null)
            return;

        StopCoroutine(autoAdvanceCoroutine);
        autoAdvanceCoroutine = null;
    }

    private IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSecondsRealtime(autoAdvanceDelay);
        autoAdvanceCoroutine = null;
        NextCut();
    }

    private void ApplyLayoutFromReference()
    {
        if (comicImage == null || sizeReference == null)
            return;

        var rect = comicImage.rectTransform;

        rect.anchorMin = sizeReference.anchorMin;
        rect.anchorMax = sizeReference.anchorMax;
        rect.pivot = sizeReference.pivot;
        rect.anchoredPosition = sizeReference.anchoredPosition;
        rect.sizeDelta = sizeReference.sizeDelta;
        rect.localRotation = sizeReference.localRotation;
        rect.localScale = sizeReference.localScale;

        if (sizeReferenceImage != null)
            comicImage.preserveAspect = sizeReferenceImage.preserveAspect;
    }

    private void NextCut()
    {
        if (comicSprites == null || comicSprites.Length == 0)
            return;

        currentIndex++;

        if (currentIndex >= comicSprites.Length)
        {
            var callback = onFinished;
            onFinished = null;
            Hide();
            callback?.Invoke();
            return;
        }

        DisplaySprite(comicSprites[currentIndex]);
        UpdateNavigationButtons();
    }

    private void PreviousCut()
    {
        if (comicSprites == null || currentIndex <= 0)
            return;

        currentIndex--;
        DisplaySprite(comicSprites[currentIndex]);
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        if (formerButton != null)
            formerButton.interactable = currentIndex > 0;
    }

    private void Hide()
    {
        StopAutoAdvance();
        gameObject.SetActive(false);
    }
}
