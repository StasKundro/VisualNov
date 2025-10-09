using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class SceneData
{
    public bool isTransitionScene; // ���� true - ���������� �����
    public Sprite backgroundSprite; // ��� ��� ������� ���� ��� �������� ��� ���������� ����
    public Sprite heroSprite;      // ������ ������ ��� ���� �����
    public Sprite interlocutorSprite; // ������ ����������� ��� ���� �����
    public CharacterSide speakingCharacter; // ��� ������� � ���� �����
    public string dialogueText;
}

public enum CharacterSide
{
    None,       // ��� ���������� ����
    Hero,       // ����� (�����)
    Interlocutor // ���������� (������)
}

public class VisualNovelManager : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image heroImage;          // �������� �����
    public Image interlocutorImage;  // �������� ������
    public TextMeshProUGUI dialogueText;     // ����� ��� ��������
    public TextMeshProUGUI transitionText;   // ����� ��� ���������� ����
    public Image dialogueTextBackground;     // ����� ��� ������ ��������

    [Header("Scene Settings")]
    [SerializeField] private List<SceneData> scenes;

    [Header("Transition Settings")]
    public float fadeDuration = 1.0f;

    [Header("Text Colors")]
    public Color heroTextColor = new Color(0.2f, 0.6f, 1f);     // �������
    public Color interlocutorTextColor = new Color(1f, 0.4f, 0.8f); // �������
    public Color transitionTextColor = Color.white;

    private int currentSceneIndex = 0;
    private Color blackColor = Color.black;
    private Color whiteColor = Color.white;
    private Sprite currentBackgroundSprite;
    private Sprite currentHeroSprite;
    private Sprite currentInterlocutorSprite;

    void Start()
    {
        // ������������� - �������� � ������� ���� � ������� ����������
        InitializeFirstScene();

        if (scenes.Count == 0)
        {
            Debug.LogError("No scenes configured!");
            return;
        }

        // �������� � ������ �����
        StartCoroutine(LoadScene(0));
    }

    void InitializeFirstScene()
    {
        // ������������� ��������� ��������� - ������ ��� � ������� ���������
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

        // �������� ��� ������ � �����
        HideAllTextsAndBackground();
    }

    void Update()
    {
        // ������� � ��������� ����� �� �����
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
            Debug.Log("����� �������!");
            // ����� ����� �������� ������ ���������� ����
        }
    }

    private IEnumerator LoadScene(int sceneIndex)
    {
        SceneData currentScene = scenes[sceneIndex];

        // ������ �������� ������� ����� ����� ������ �����
        yield return StartCoroutine(FadeOutTexts());

        // ���� ��� ���������� �����
        if (currentScene.isTransitionScene)
        {
            yield return StartCoroutine(HideCharacters());

            // ���� ������� �������� ��� ����������� ������ - ���������� ��
            if (currentScene.backgroundSprite != null)
            {
                yield return StartCoroutine(ChangeToTransitionBackground(currentScene.backgroundSprite));
            }
            else
            {
                // ����� ���������� ������ ���
                yield return StartCoroutine(ChangeToBlackBackground());
            }

            // ���������� ����� �������� � ������� ����������
            if (transitionText != null)
            {
                transitionText.color = transitionTextColor;
                transitionText.text = currentScene.dialogueText;
                transitionText.gameObject.SetActive(true);
                yield return StartCoroutine(FadeInText(transitionText));
            }
        }
        else // ������� �����
        {
            // ���������, ����� �� ������ ��� (������ ���� ������ ���������)
            bool backgroundChanged = currentScene.backgroundSprite != currentBackgroundSprite;
            if (backgroundChanged)
            {
                yield return StartCoroutine(ChangeBackground(currentScene.backgroundSprite));
            }
            else
            {
                // ���� ��� �� �������, ������ ��������� ������
                currentBackgroundSprite = currentScene.backgroundSprite;
            }

            // ��������� ������� ���������� (������ ���� ��� ����������)
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

            // ���������� ����� ������� � ���������� ������ � ������
            if (dialogueText != null)
            {
                // ������������� ���� ������
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

                // ���������� ����� ��� ��������
                if (dialogueTextBackground != null)
                {
                    dialogueTextBackground.gameObject.SetActive(true);
                    dialogueTextBackground.color = new Color(1, 1, 1, 0);
                }

                // ������� ��������� ������ � �����
                yield return StartCoroutine(FadeInDialogueText());
            }
        }
    }

    private IEnumerator ChangeToTransitionBackground(Sprite transitionSprite)
    {
        if (backgroundImage == null) yield break;

        // ���� ��� ���������� ���� �� ������, �� ������ ��������
        if (backgroundImage.sprite == transitionSprite && backgroundImage.color.a >= 0.9f)
        {
            yield break;
        }

        // ������� ��������� Image ��� �������� ��������
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

        // ������������� ����� ��� ��� ��������
        backgroundImage.sprite = transitionSprite;
        backgroundImage.color = new Color(1, 1, 1, 0);

        // ������� ��������� ���� ��������
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
        // ������� ������ ���� �������� ������� ��� �������� ������������
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

        // ���� ��� �������� �������, �������
        if (textsToFade.Count == 0 && backgroundsToFade.Count == 0)
            yield break;

        // ������� ������������
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

        // ��������� �������� ���
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

            // ������� ��������� ������
            dialogueText.color = new Color(textStartColor.r, textStartColor.g, textStartColor.b, alpha);

            // ������� ��������� �����
            if (dialogueTextBackground != null)
            {
                dialogueTextBackground.color = new Color(backgroundStartColor.r, backgroundStartColor.g, backgroundStartColor.b, alpha);
            }

            yield return null;
        }

        // ������������� ��������� �����
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

        // ���� ����� ��� �� ������, ���������� �������
        if (newBackground == null)
        {
            backgroundImage.color = whiteColor;
            currentBackgroundSprite = null;
            yield break;
        }

        // ���� ��� ������ ��� ��� ������� � ������� ����
        if (currentBackgroundSprite == null || backgroundImage.color == blackColor)
        {
            backgroundImage.sprite = newBackground;
            backgroundImage.color = whiteColor;
            currentBackgroundSprite = newBackground;
            yield break;
        }

        // ������� ��������� Image ��� �������� �������� ������ ���� ��� ������������� ��������
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

        // ������������� ����� ���
        backgroundImage.sprite = newBackground;
        backgroundImage.color = new Color(1, 1, 1, 0);

        // ������� ��������� ������ ����
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

        // ���� ��� ������ ���, �� ������ ��������
        if (backgroundImage.color == blackColor && backgroundImage.sprite == null)
        {
            yield break;
        }

        // ������� ��������� Image ��� �������� ��������
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

        // ������������� ������ ���
        backgroundImage.sprite = null;
        backgroundImage.color = new Color(0, 0, 0, 0);

        // ������� ��������� ������� ����
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
        // ��������� ������� ���������� ������ ���� ��� ����������
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
        // ���������, �� ������ �� ��� ���������
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
        // ����������, ����� ���������� ������������
        bool highlightHero = (speakingCharacter == CharacterSide.Hero);
        bool highlightInterlocutor = (speakingCharacter == CharacterSide.Interlocutor);

        float elapsedTime = 0f;
        Color heroStartColor = heroImage.color;
        Color interlocutorStartColor = interlocutorImage.color;

        Color heroTargetColor = highlightHero ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        Color interlocutorTargetColor = highlightInterlocutor ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);

        // ���� ��������� ��������� ����������, ������� ������ �� ��������
        if (heroStartColor.a < 0.1f)
        {
            heroStartColor = new Color(heroTargetColor.r, heroTargetColor.g, heroTargetColor.b, 0f);
        }

        if (interlocutorStartColor.a < 0.1f)
        {
            interlocutorStartColor = new Color(interlocutorTargetColor.r, interlocutorTargetColor.g, interlocutorTargetColor.b, 0f);
        }

        // ���������, ����� �� ������ ��������
        bool needsAnimation =
            heroStartColor != heroTargetColor ||
            interlocutorStartColor != interlocutorTargetColor ||
            heroStartColor.a < 0.9f ||
            interlocutorStartColor.a < 0.9f;

        if (!needsAnimation)
        {
            // ���� ��� � ������� ���������, ������ ������������� �����
            heroImage.color = heroTargetColor;
            interlocutorImage.color = interlocutorTargetColor;
            yield break;
        }

        // ��������� ������� ��������
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

    // ����� ��� �������� � ���������� ����� �� �������
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

    // ����� ��� ����������� �������
    public void RestartNovel()
    {
        currentSceneIndex = 0;
        InitializeFirstScene();
        StartCoroutine(LoadScene(currentSceneIndex));
    }

    // ����� ��� ��������� �������� ������ �����
    public int GetCurrentSceneIndex()
    {
        return currentSceneIndex;
    }

    // ����� ��� ��������� ������ ���������� ����
    public int GetTotalScenes()
    {
        return scenes.Count;
    }
}