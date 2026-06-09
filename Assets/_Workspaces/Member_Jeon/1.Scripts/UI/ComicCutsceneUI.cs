using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ComicCutsceneUI : MonoBehaviour
{
    [Header("만화 이미지")]
    [SerializeField] private Image comicImage;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button skipButton;

    [Header("크기 기준")]
    [Tooltip("비우면 ComicImage RectTransform 크기를 그대로 사용합니다. 결과 UI는 VictoryResultUI > ResultImage를 연결하세요.")]
    [SerializeField] private RectTransform sizeReference;
    [SerializeField] private Image sizeReferenceImage;
    [SerializeField] private bool preserveAspect = true;

    [Header("승리 4컷")]
    [SerializeField] private Sprite[] victoryComicSprites;

    [Header("패배 4컷")]
    [SerializeField] private Sprite[] defeatComicSprites;

    [Header("인트로 씬 (선택)")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private Sprite[] introComicSprites;
    [SerializeField] private string nextSceneName = "BattleScene";

    [Header("자동 넘김")]
    [SerializeField] private float autoAdvanceDelay = 15f;

    [Header("타이머 UI")]
    [SerializeField] private Animator timerAnimator;
    [SerializeField] private float timerClipLength = 1.0833334f;
    [SerializeField] private string timerAnimationState = "Timer";

    private Sprite[] comicSprites;
    private int currentIndex;
    private Action onFinished;
    private ResultUICanvas resultCanvas;
    private bool isInitialized;
    private bool pausedTime;
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

    private void Start()
    {
        if (!playOnStart)
            return;

        if (introComicSprites == null || introComicSprites.Length == 0)
        {
            Debug.LogWarning("[ComicCutsceneUI] 인트로 스프라이트가 비어 있습니다.");
            FinishCutscene();
            return;
        }

        Show(introComicSprites, "인트로", OnIntroFinished, pauseTime: false);
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;
        resultCanvas = GetComponentInParent<ResultUICanvas>();

        if (nextButton != null)
            nextButton.onClick.AddListener(NextCut);

        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousCut);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipCutscene);
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

    public void SkipCutscene()
    {
        if (!isActiveAndEnabled)
            return;

        FinishCutscene();
    }

    private void Show(Sprite[] sprites, string label, Action onFinished, bool pauseTime = true)
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
        SetNavigationButtonsActive(true);
        comicSprites = sprites;
        currentIndex = 0;

        DisplaySprite(comicSprites[currentIndex]);
        UpdateNavigationButtons();

        if (pauseTime)
        {
            Time.timeScale = 0f;
            pausedTime = true;
        }
    }

    private void OnIntroFinished()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
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
        ApplyImageDisplaySettings();

        Debug.Log($"[ComicCutsceneUI] 만화 표시: {sprite.name}");
        StartAutoAdvance();
    }

    private void StartAutoAdvance()
    {
        StopAutoAdvance();

        if (autoAdvanceDelay <= 0f || !isActiveAndEnabled)
            return;

        StartTimerAnimation();
        autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
    }

    private void StopAutoAdvance()
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        StopTimerAnimation();
    }

    private void StartTimerAnimation()
    {
        if (timerAnimator == null)
            return;

        timerAnimator.gameObject.SetActive(true);
        timerAnimator.speed = timerClipLength > 0f && autoAdvanceDelay > 0f
            ? timerClipLength / autoAdvanceDelay
            : 1f;
        timerAnimator.Play(timerAnimationState, 0, 0f);
    }

    private void StopTimerAnimation()
    {
        if (timerAnimator == null)
            return;

        timerAnimator.gameObject.SetActive(false);
    }

    private IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSecondsRealtime(autoAdvanceDelay);
        autoAdvanceCoroutine = null;
        NextCut();
    }

    private void ApplyLayoutFromReference()
    {
        if (comicImage == null)
            return;

        var rect = comicImage.rectTransform;

        if (sizeReference != null && sizeReference != rect)
        {
            rect.anchorMin = sizeReference.anchorMin;
            rect.anchorMax = sizeReference.anchorMax;
            rect.pivot = sizeReference.pivot;
            rect.anchoredPosition = sizeReference.anchoredPosition;
            rect.sizeDelta = sizeReference.sizeDelta;
            rect.localRotation = sizeReference.localRotation;
            rect.localScale = sizeReference.localScale;
        }

        ApplyImageDisplaySettings();
    }

    private void ApplyImageDisplaySettings()
    {
        if (comicImage == null)
            return;

        comicImage.preserveAspect = sizeReferenceImage != null
            ? sizeReferenceImage.preserveAspect
            : preserveAspect;
    }

    private void NextCut()
    {
        if (!isActiveAndEnabled)
            return;

        if (comicSprites == null || comicSprites.Length == 0)
            return;

        currentIndex++;

        if (currentIndex >= comicSprites.Length)
        {
            FinishCutscene();
            return;
        }

        DisplaySprite(comicSprites[currentIndex]);
        UpdateNavigationButtons();
    }

    private void PreviousCut()
    {
        if (!isActiveAndEnabled)
            return;

        if (comicSprites == null || currentIndex <= 0)
            return;

        currentIndex--;
        DisplaySprite(comicSprites[currentIndex]);
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        if (previousButton != null)
            previousButton.interactable = currentIndex > 0;
    }

    private void SetNavigationButtonsActive(bool active)
    {
        if (nextButton != null)
            nextButton.gameObject.SetActive(active);

        if (previousButton != null)
            previousButton.gameObject.SetActive(active);

        if (skipButton != null)
            skipButton.gameObject.SetActive(active);
    }

    private void FinishCutscene()
    {
        var callback = onFinished;
        onFinished = null;

        StopAutoAdvance();
        SetNavigationButtonsActive(false);

        if (pausedTime)
        {
            Time.timeScale = 1f;
            pausedTime = false;
        }

        gameObject.SetActive(false);

        callback?.Invoke();
    }
}
