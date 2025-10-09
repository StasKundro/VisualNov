using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class SceneData
{
    public bool isTransitionScene; // Если true - переходная сцена
    public Sprite backgroundSprite; // Фон для обычных сцен ИЛИ картинка для переходных сцен
    public Sprite heroSprite;      // Спрайт игрока для этой сцены
    public Sprite interlocutorSprite; // Спрайт собеседника для этой сцены
    public CharacterSide speakingCharacter; // Кто говорит в этой сцене
    public string dialogueText;
}

public enum CharacterSide
{
    None,       // Для переходных сцен
    Hero,       // Игрок (слева)
    Interlocutor // Собеседник (справа)
}

public class VisualNovelManager : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image heroImage;          // Персонаж слева
    public Image interlocutorImage;  // Персонаж справа
    public TextMeshProUGUI dialogueText;     // Текст для диалогов
    public TextMeshProUGUI transitionText;   // Текст для переходных сцен
    public Image dialogueTextBackground;     // Рамка для текста диалогов

    [Header("Scene Settings")]
    [SerializeField] private List<SceneData> scenes;

    [Header("Transition Settings")]
    public float fadeDuration = 1.0f;

    [Header("Text Colors")]
    public Color heroTextColor = new Color(0.2f, 0.6f, 1f);     // Голубой
    public Color interlocutorTextColor = new Color(1f, 0.4f, 0.8f); // Розовый
    public Color transitionTextColor = Color.white;

    private int currentSceneIndex = 0;
    private Color blackColor = Color.black;
    private Color whiteColor = Color.white;
    private Sprite currentBackgroundSprite;
    private Sprite currentHeroSprite;
    private Sprite currentInterlocutorSprite;

    void Start()
    {
        // Инициализация - начинаем с черного фона и скрытых персонажей
        InitializeFirstScene();

        if (scenes.Count == 0)
        {
            Debug.LogError("No scenes configured!");
            return;
        }

        // Начинаем с первой сцены
        StartCoroutine(LoadScene(0));
    }

    void InitializeFirstScene()
    {
        // Устанавливаем начальное состояние - черный фон и скрытые персонажи
        if (backgroundImage != null)
        {
            backgroundImage.sprite = null;
            backgroundImage.color = blackColor;
            currentBackgroundSprite = null;
        }

        if (heroImage != null)
        {
            heroImage.color = new Color(1, 1, 1, 0);
            currentHeroSprite = null;
        }

        if (interlocutorImage != null)
        {
            interlocutorImage.color = new Color(1, 1, 1, 0);
            currentInterlocutorSprite = null;
        }

        // Скрываем все тексты и рамку
        HideAllTextsAndBackground();
    }

    void Update()
    {
        // Переход к следующей сцене по клику
        if (Input.GetMouseButtonDown(0))
        {
            NextScene();
        }
    }

    public void NextScene()
    {
        if (currentSceneIndex < scenes.Count - 1)
        {
            currentSceneIndex++;
            StartCoroutine(LoadScene(currentSceneIndex));
        }
        else
        {
            Debug.Log("Конец новеллы!");
            // Здесь можно добавить логику завершения игры
        }
    }

    private IEnumerator LoadScene(int sceneIndex)
    {
        SceneData currentScene = scenes[sceneIndex];

        // Плавно скрываем текущий текст перед сменой сцены
        yield return StartCoroutine(FadeOutTexts());

        // Если это переходная сцена
        if (currentScene.isTransitionScene)
        {
            yield return StartCoroutine(HideCharacters());

            // Если указана картинка для переходного экрана - используем ее
            if (currentScene.backgroundSprite != null)
            {
                yield return StartCoroutine(ChangeToTransitionBackground(currentScene.backgroundSprite));
            }
            else
            {
                // Иначе используем черный фон
                yield return StartCoroutine(ChangeToBlackBackground());
            }

            // Показываем текст перехода с плавным появлением
            if (transitionText != null)
            {
                transitionText.color = transitionTextColor;
                transitionText.text = currentScene.dialogueText;
                transitionText.gameObject.SetActive(true);
                yield return StartCoroutine(FadeInText(transitionText));
            }
        }
        else // Обычная сцена
        {
            // Проверяем, нужно ли менять фон (только если спрайт изменился)
            bool backgroundChanged = currentScene.backgroundSprite != currentBackgroundSprite;
            if (backgroundChanged)
            {
                yield return StartCoroutine(ChangeBackground(currentScene.backgroundSprite));
            }
            else
            {
                // Если фон не менялся, просто обновляем ссылку
                currentBackgroundSprite = currentScene.backgroundSprite;
            }

            // Обновляем спрайты персонажей (только если они изменились)
            bool heroSpriteChanged = currentScene.heroSprite != currentHeroSprite;
            bool interlocutorSpriteChanged = currentScene.interlocutorSprite != currentInterlocutorSprite;

            if (heroSpriteChanged || interlocutorSpriteChanged)
            {
                yield return StartCoroutine(UpdateCharacterSprites(
                    currentScene.heroSprite,
                    currentScene.interlocutorSprite,
                    heroSpriteChanged,
                    interlocutorSpriteChanged
                ));
            }

            yield return StartCoroutine(ShowCharacters(currentScene.speakingCharacter));

            // Показываем текст диалога с правильным цветом и рамкой
            if (dialogueText != null)
            {
                // Устанавливаем цвет текста
                switch (currentScene.speakingCharacter)
                {
                    case CharacterSide.Hero:
                        dialogueText.color = new Color(heroTextColor.r, heroTextColor.g, heroTextColor.b, 0);
                        break;
                    case CharacterSide.Interlocutor:
                        dialogueText.color = new Color(interlocutorTextColor.r, interlocutorTextColor.g, interlocutorTextColor.b, 0);
                        break;
                    default:
                        dialogueText.color = new Color(transitionTextColor.r, transitionTextColor.g, transitionTextColor.b, 0);
                        break;
                }

                dialogueText.text = currentScene.dialogueText;
                dialogueText.gameObject.SetActive(true);

                // Показываем рамку для диалогов
                if (dialogueTextBackground != null)
                {
                    dialogueTextBackground.gameObject.SetActive(true);
                    dialogueTextBackground.color = new Color(1, 1, 1, 0);
                }

                // Плавное появление текста и рамки
                yield return StartCoroutine(FadeInDialogueText());
            }
        }
    }

    private IEnumerator ChangeToTransitionBackground(Sprite transitionSprite)
    {
        if (backgroundImage == null) yield break;

        // Если уже установлен этот же спрайт, не делаем анимацию
        if (backgroundImage.sprite == transitionSprite && backgroundImage.color.a >= 0.9f)
        {
            yield break;
        }

        // Создаем временный Image для плавного перехода
        GameObject tempBackgroundObj = new GameObject("TempBackground");
        Image tempBackground = tempBackgroundObj.AddComponent<Image>();
        tempBackground.sprite = currentBackgroundSprite;
        tempBackground.color = backgroundImage.color;
        tempBackground.rectTransform.SetParent(backgroundImage.transform.parent);
        tempBackground.rectTransform.anchorMin = Vector2.zero;
        tempBackground.rectTransform.anchorMax = Vector2.one;
        tempBackground.rectTransform.offsetMin = Vector2.zero;
        tempBackground.rectTransform.offsetMax = Vector2.zero;
        tempBackground.transform.SetAsFirstSibling();

        // Устанавливаем новый фон для перехода
        backgroundImage.sprite = transitionSprite;
        backgroundImage.color = new Color(1, 1, 1, 0);

        // Плавное появление фона перехода
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeDuration;
            backgroundImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        backgroundImage.color = whiteColor;
        currentBackgroundSprite = transitionSprite;
        Destroy(tempBackgroundObj);
    }

    private IEnumerator FadeOutTexts()
    {
        // Создаем список всех активных текстов для плавного исчезновения
        List<TextMeshProUGUI> textsToFade = new List<TextMeshProUGUI>();
        List<Image> backgroundsToFade = new List<Image>();

        if (dialogueText != null && dialogueText.gameObject.activeSelf)
        {
            textsToFade.Add(dialogueText);
        }

        if (transitionText != null && transitionText.gameObject.activeSelf)
        {
            textsToFade.Add(transitionText);
        }

        if (dialogueTextBackground != null && dialogueTextBackground.gameObject.activeSelf)
        {
            backgroundsToFade.Add(dialogueTextBackground);
        }

        // Если нет активных текстов, выходим
        if (textsToFade.Count == 0 && backgroundsToFade.Count == 0)
            yield break;

        // Плавное исчезновение
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / fadeDuration);

            foreach (var text in textsToFade)
            {
                Color color = text.color;
                text.color = new Color(color.r, color.g, color.b, alpha);
            }

            foreach (var background in backgroundsToFade)
            {
                Color color = background.color;
                background.color = new Color(color.r, color.g, color.b, alpha);
            }

            yield return null;
        }

        // Полностью скрываем все
        HideAllTextsAndBackground();
    }

    private IEnumerator FadeInText(TextMeshProUGUI text)
    {
        if (text == null) yield break;

        float elapsedTime = 0f;
        Color startColor = text.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeDuration;
            text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        text.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
    }

    private IEnumerator FadeInDialogueText()
    {
        if (dialogueText == null) yield break;

        float elapsedTime = 0f;
        Color textStartColor = dialogueText.color;
        Color backgroundStartColor = dialogueTextBackground != null ? dialogueTextBackground.color : Color.white;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeDuration;

            // Плавное появление текста
            dialogueText.color = new Color(textStartColor.r, textStartColor.g, textStartColor.b, alpha);

            // Плавное появление рамки
            if (dialogueTextBackground != null)
            {
                dialogueTextBackground.color = new Color(backgroundStartColor.r, backgroundStartColor.g, backgroundStartColor.b, alpha);
            }

            yield return null;
        }

        // Устанавливаем финальные цвета
        dialogueText.color = new Color(textStartColor.r, textStartColor.g, textStartColor.b, 1f);
        if (dialogueTextBackground != null)
        {
            dialogueTextBackground.color = new Color(backgroundStartColor.r, backgroundStartColor.g, backgroundStartColor.b, 1f);
        }
    }

    private void HideAllTextsAndBackground()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.gameObject.SetActive(false);
        }

        if (transitionText != null)
        {
            transitionText.text = "";
            transitionText.gameObject.SetActive(false);
        }

        if (dialogueTextBackground != null)
        {
            dialogueTextBackground.gameObject.SetActive(false);
        }
    }

    private IEnumerator ChangeBackground(Sprite newBackground)
    {
        if (backgroundImage == null) yield break;

        // Если новый фон не указан, используем текущий
        if (newBackground == null)
        {
            backgroundImage.color = whiteColor;
            currentBackgroundSprite = null;
            yield break;
        }

        // Если это первый фон или переход с черного фона
        if (currentBackgroundSprite == null || backgroundImage.color == blackColor)
        {
            backgroundImage.sprite = newBackground;
            backgroundImage.color = whiteColor;
            currentBackgroundSprite = newBackground;
            yield break;
        }

        // Создаем временный Image для плавного перехода только если фон действительно меняется
        GameObject tempBackgroundObj = new GameObject("TempBackground");
        Image tempBackground = tempBackgroundObj.AddComponent<Image>();
        tempBackground.sprite = currentBackgroundSprite;
        tempBackground.color = backgroundImage.color;
        tempBackground.rectTransform.SetParent(backgroundImage.transform.parent);
        tempBackground.rectTransform.anchorMin = Vector2.zero;
        tempBackground.rectTransform.anchorMax = Vector2.one;
        tempBackground.rectTransform.offsetMin = Vector2.zero;
        tempBackground.rectTransform.offsetMax = Vector2.zero;
        tempBackground.transform.SetAsFirstSibling();

        // Устанавливаем новый фон
        backgroundImage.sprite = newBackground;
        backgroundImage.color = new Color(1, 1, 1, 0);

        // Плавное появление нового фона
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeDuration;
            backgroundImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        backgroundImage.color = whiteColor;
        currentBackgroundSprite = newBackground;
        Destroy(tempBackgroundObj);
    }

    private IEnumerator ChangeToBlackBackground()
    {
        if (backgroundImage == null) yield break;

        // Если уже черный фон, не делаем анимацию
        if (backgroundImage.color == blackColor && backgroundImage.sprite == null)
        {
            yield break;
        }

        // Создаем временный Image для плавного перехода
        GameObject tempBackgroundObj = new GameObject("TempBackground");
        Image tempBackground = tempBackgroundObj.AddComponent<Image>();
        tempBackground.sprite = currentBackgroundSprite;
        tempBackground.color = backgroundImage.color;
        tempBackground.rectTransform.SetParent(backgroundImage.transform.parent);
        tempBackground.rectTransform.anchorMin = Vector2.zero;
        tempBackground.rectTransform.anchorMax = Vector2.one;
        tempBackground.rectTransform.offsetMin = Vector2.zero;
        tempBackground.rectTransform.offsetMax = Vector2.zero;
        tempBackground.transform.SetAsFirstSibling();

        // Устанавливаем черный фон
        backgroundImage.sprite = null;
        backgroundImage.color = new Color(0, 0, 0, 0);

        // Плавное появление черного фона
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeDuration;
            backgroundImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        backgroundImage.color = blackColor;
        currentBackgroundSprite = null;
        Destroy(tempBackgroundObj);
    }

    private IEnumerator UpdateCharacterSprites(Sprite heroSprite, Sprite interlocutorSprite, bool updateHero, bool updateInterlocutor)
    {
        // Обновляем спрайты персонажей только если они изменились
        if (updateHero && heroSprite != null)
        {
            heroImage.sprite = heroSprite;
            currentHeroSprite = heroSprite;
        }

        if (updateInterlocutor && interlocutorSprite != null)
        {
            interlocutorImage.sprite = interlocutorSprite;
            currentInterlocutorSprite = interlocutorSprite;
        }

        yield return null;
    }

    private IEnumerator HideCharacters()
    {
        // Проверяем, не скрыты ли уже персонажи
        if (heroImage.color.a <= 0.1f && interlocutorImage.color.a <= 0.1f)
        {
            yield break;
        }

        float elapsedTime = 0f;
        Color heroStartColor = heroImage.color;
        Color interlocutorStartColor = interlocutorImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / fadeDuration);

            heroImage.color = new Color(heroStartColor.r, heroStartColor.g, heroStartColor.b, alpha);
            interlocutorImage.color = new Color(interlocutorStartColor.r, interlocutorStartColor.g, interlocutorStartColor.b, alpha);

            yield return null;
        }

        heroImage.color = new Color(heroStartColor.r, heroStartColor.g, heroStartColor.b, 0);
        interlocutorImage.color = new Color(interlocutorStartColor.r, interlocutorStartColor.g, interlocutorStartColor.b, 0);
    }

    private IEnumerator ShowCharacters(CharacterSide speakingCharacter)
    {
        // Определяем, каких персонажей подсвечивать
        bool highlightHero = (speakingCharacter == CharacterSide.Hero);
        bool highlightInterlocutor = (speakingCharacter == CharacterSide.Interlocutor);

        float elapsedTime = 0f;
        Color heroStartColor = heroImage.color;
        Color interlocutorStartColor = interlocutorImage.color;

        Color heroTargetColor = highlightHero ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        Color interlocutorTargetColor = highlightInterlocutor ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);

        // Если персонажи полностью прозрачные, сначала делаем их видимыми
        if (heroStartColor.a < 0.1f)
        {
            heroStartColor = new Color(heroTargetColor.r, heroTargetColor.g, heroTargetColor.b, 0f);
        }

        if (interlocutorStartColor.a < 0.1f)
        {
            interlocutorStartColor = new Color(interlocutorTargetColor.r, interlocutorTargetColor.g, interlocutorTargetColor.b, 0f);
        }

        // Проверяем, нужна ли вообще анимация
        bool needsAnimation =
            heroStartColor != heroTargetColor ||
            interlocutorStartColor != interlocutorTargetColor ||
            heroStartColor.a < 0.9f ||
            interlocutorStartColor.a < 0.9f;

        if (!needsAnimation)
        {
            // Если уже в целевом состоянии, просто устанавливаем цвета
            heroImage.color = heroTargetColor;
            interlocutorImage.color = interlocutorTargetColor;
            yield break;
        }

        // Выполняем плавную анимацию
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            heroImage.color = Color.Lerp(heroStartColor, heroTargetColor, t);
            interlocutorImage.color = Color.Lerp(interlocutorStartColor, interlocutorTargetColor, t);

            yield return null;
        }

        heroImage.color = heroTargetColor;
        interlocutorImage.color = interlocutorTargetColor;
    }

    // Метод для перехода к конкретной сцене по индексу
    public void GoToScene(int sceneIndex)
    {
        if (sceneIndex >= 0 && sceneIndex < scenes.Count)
        {
            currentSceneIndex = sceneIndex;
            StartCoroutine(LoadScene(currentSceneIndex));
        }
        else
        {
            Debug.LogWarning($"Scene index {sceneIndex} is out of range!");
        }
    }

    // Метод для перезапуска новеллы
    public void RestartNovel()
    {
        currentSceneIndex = 0;
        InitializeFirstScene();
        StartCoroutine(LoadScene(currentSceneIndex));
    }

    // Метод для получения текущего номера сцены
    public int GetCurrentSceneIndex()
    {
        return currentSceneIndex;
    }

    // Метод для получения общего количества сцен
    public int GetTotalScenes()
    {
        return scenes.Count;
    }
}