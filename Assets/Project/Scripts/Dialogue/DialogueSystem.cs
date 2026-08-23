using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public sealed class DialogueSystem : MonoBehaviour
{
	public static DialogueSystem Instance { get; private set; }

	public event Action<DialogueSequence> DialogueFinished;

	[Header("Start")]
	[SerializeField] private DialogueSequence startSequence;
	[SerializeField] private bool playOnStart;

	[Header("View References")]
	[SerializeField] private Canvas canvas;
	[SerializeField] private CanvasGroup canvasGroup;
	[SerializeField] private Image backgroundImage;
	[SerializeField] private TMP_Text speakerNameText;
	[SerializeField] private TMP_Text bodyText;
	[SerializeField] private Image portraitImage;
	[SerializeField] private Button advanceButton;

	[Header("Art")]
	[SerializeField] private Sprite dialogueWindowSprite;
	[SerializeField] private TMP_FontAsset dialogueFont;
	[SerializeField] private Color speakerNameColor = new Color(0.95f, 0.87f, 0.64f, 1f);
	[SerializeField] private Color bodyTextColor = new Color(0.23f, 0.16f, 0.1f, 1f);

	[Header("Layout")]
	[SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
	[SerializeField] private Vector2 windowSize = new Vector2(1220f, 467f);
	[SerializeField] private Vector2 windowOffset = new Vector2(0f, 56f);
	[SerializeField] private Vector2 textPaddingMin = new Vector2(185f, 110f);
	[SerializeField] private Vector2 textPaddingMax = new Vector2(120f, 112f);
	[SerializeField] private float bodyTextVerticalOffset = -18f;
	[SerializeField] private float bodyTextHorizontalOffset;
	[SerializeField] private float speakerFontSize = 46f;
	[SerializeField] private float bodyFontSize = 42f;
	[SerializeField] private bool usePortraits = true;
	[SerializeField] private Vector2 portraitSize = new Vector2(120f, 120f);
	[SerializeField] private Vector2 portraitOffset = new Vector2(104f, 0f);

	[Header("Behaviour")]
	[SerializeField, Min(0f)] private float showFadeDuration = 0.18f;
	[SerializeField, Min(0f)] private float hideFadeDuration = 0.14f;
	[SerializeField, Min(0f)] private float typewriterCharactersPerSecond = 55f;
	[SerializeField] private bool advanceWithKeyboard = true;
	[SerializeField] private bool hideWhenFinished = true;
	[SerializeField] private bool useUnscaledTime;

	[Header("Advance Hint")]
	[SerializeField] private string advanceHintText = "Нажмите Enter";
	[SerializeField, Min(0f)] private float advanceHintDelay = 3f;
	[SerializeField, Min(0f)] private float advanceHintFadeDuration = 0.35f;
	[SerializeField] private float advanceHintFontSize = 30f;
	[SerializeField] private Vector2 advanceHintOffset = new Vector2(-84f, 44f);

	private DialogueSequence currentSequence;
	private Coroutine fadeCoroutine;
	private Coroutine typewriterCoroutine;
	private Coroutine advanceHintCoroutine;
	private int currentLineIndex = -1;
	private bool isRunning;
	private bool isTyping;
	private string fullCurrentText;
	private TMP_Text advanceHintLabel;
	private CanvasGroup advanceHintGroup;

	public bool IsRunning => isRunning;
	public float HideFadeDuration => hideFadeDuration;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}

		BuildRuntimeViewIfNeeded();
		ApplyArtSettings();
		BindAdvanceButton();
		HideInstant();
	}

	private void Start()
	{
		if (playOnStart && startSequence != null)
		{
			StartDialogue(startSequence);
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Update()
	{
		if (ModalSettingsPanel.IsOpen || !isRunning || !WasAdvancePressed())
			return;

		Advance();
	}

	public void StartDialogue(DialogueSequence sequence)
	{
		StartDialogue(sequence, false);
	}

	public void StartDialogue(DialogueSequence sequence, bool revealTextInstantly)
	{
		if (sequence == null || !sequence.HasLines)
		{
			Debug.LogWarning($"{nameof(DialogueSystem)} got empty dialogue sequence.", this);
			return;
		}

		StopTypewriter();

		currentSequence = sequence;
		currentLineIndex = -1;
		isRunning = true;

		HideAdvanceHintInstant();
		Show();
		Advance();

		if (revealTextInstantly && isTyping)
		{
			FinishTypewriter();
		}
	}

	public void Advance()
	{
		if (!isRunning)
			return;

		if (isTyping)
		{
			FinishTypewriter();
			return;
		}

		currentLineIndex++;

		if (currentSequence == null || currentLineIndex >= currentSequence.LineCount)
		{
			FinishDialogue();
			return;
		}

		ShowLine(currentSequence.GetLine(currentLineIndex));
	}

	public void StopDialogue()
	{
		if (!isRunning)
			return;

		FinishDialogue();
	}

	private void ShowLine(DialogueLine line)
	{
		RestartAdvanceHint();

		if (line == null)
		{
			fullCurrentText = string.Empty;
			SetTextInstant(string.Empty);
			return;
		}

		SetSpeaker(line.SpeakerName);
		SetPortrait(line.Portrait);
		ApplyLineLayout(line);

		fullCurrentText = line.Text ?? string.Empty;

		StopTypewriter();

		if (typewriterCharactersPerSecond <= 0f || fullCurrentText.Length == 0)
		{
			SetTextInstant(fullCurrentText);
			return;
		}

		typewriterCoroutine = StartCoroutine(TypeText(fullCurrentText));
	}

	private IEnumerator TypeText(string text)
	{
		isTyping = true;
		bodyText.text = string.Empty;

		float visibleCharacters = 0f;

		while (visibleCharacters < text.Length)
		{
			visibleCharacters += typewriterCharactersPerSecond * GetDeltaTime();
			int count = Mathf.Clamp(
				Mathf.FloorToInt(visibleCharacters),
				0,
				text.Length
			);

			bodyText.text = text.Substring(0, count);
			yield return null;
		}

		FinishTypewriter();
	}

	private void FinishTypewriter()
	{
		StopTypewriter();
		SetTextInstant(fullCurrentText);
	}

	private void StopTypewriter()
	{
		if (typewriterCoroutine != null)
		{
			StopCoroutine(typewriterCoroutine);
			typewriterCoroutine = null;
		}

		isTyping = false;
	}

	private void FinishDialogue()
	{
		DialogueSequence finishedSequence = currentSequence;

		StopTypewriter();

		isRunning = false;
		currentSequence = null;
		currentLineIndex = -1;

		if (hideWhenFinished)
		{
			Hide();
		}

		HideAdvanceHintInstant();
		DialogueFinished?.Invoke(finishedSequence);
	}

	private void Show()
	{
		SetCanvasActive(true);
		FadeTo(1f, showFadeDuration, false);
	}

	private void Hide()
	{
		FadeTo(0f, hideFadeDuration, true);
	}

	private void HideInstant()
	{
		if (canvasGroup == null)
			return;

		canvasGroup.alpha = 0f;
		canvasGroup.interactable = false;
		canvasGroup.blocksRaycasts = false;
		HideAdvanceHintInstant();
		SetCanvasActive(false);
	}

	private void FadeTo(float targetAlpha, float duration, bool disableAfterFade)
	{
		if (canvasGroup == null)
			return;

		if (fadeCoroutine != null)
		{
			StopCoroutine(fadeCoroutine);
		}

		fadeCoroutine = StartCoroutine(FadeCanvas(targetAlpha, duration, disableAfterFade));
	}

	private IEnumerator FadeCanvas(float targetAlpha, float duration, bool disableAfterFade)
	{
		float startAlpha = canvasGroup.alpha;

		canvasGroup.interactable = targetAlpha > 0f;
		canvasGroup.blocksRaycasts = targetAlpha > 0f;

		if (duration <= 0f)
		{
			canvasGroup.alpha = targetAlpha;
		}
		else
		{
			float time = 0f;

			while (time < duration)
			{
				time += GetDeltaTime();
				float t = Mathf.Clamp01(time / duration);
				canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
				yield return null;
			}

			canvasGroup.alpha = targetAlpha;
		}

		if (disableAfterFade)
		{
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
			SetCanvasActive(false);
		}

		fadeCoroutine = null;
	}

	private void SetTextInstant(string text)
	{
		isTyping = false;

		if (bodyText != null)
		{
			bodyText.text = text ?? string.Empty;
		}
	}

	private void RestartAdvanceHint()
	{
		if (advanceHintGroup == null)
			return;

		if (advanceHintCoroutine != null)
			StopCoroutine(advanceHintCoroutine);

		advanceHintCoroutine = StartCoroutine(ShowAdvanceHintAfterDelay());
	}

	private IEnumerator ShowAdvanceHintAfterDelay()
	{
		advanceHintGroup.alpha = 0f;

		if (advanceHintDelay > 0f)
		{
			float elapsedDelay = 0f;
			while (elapsedDelay < advanceHintDelay)
			{
				elapsedDelay += GetDeltaTime();
				yield return null;
			}
		}

		if (advanceHintFadeDuration <= 0f)
		{
			advanceHintGroup.alpha = 1f;
		}
		else
		{
			float elapsed = 0f;
			while (elapsed < advanceHintFadeDuration)
			{
				elapsed += GetDeltaTime();
				float t = Mathf.Clamp01(elapsed / advanceHintFadeDuration);
				advanceHintGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
				yield return null;
			}

			advanceHintGroup.alpha = 1f;
		}

		advanceHintCoroutine = null;
	}

	private void HideAdvanceHintInstant()
	{
		if (advanceHintCoroutine != null)
		{
			StopCoroutine(advanceHintCoroutine);
			advanceHintCoroutine = null;
		}

		if (advanceHintGroup != null)
			advanceHintGroup.alpha = 0f;
	}

	private void SetSpeaker(string speakerName)
	{
		if (speakerNameText == null)
			return;

		bool hasSpeaker = !string.IsNullOrWhiteSpace(speakerName);
		speakerNameText.gameObject.SetActive(hasSpeaker);
		speakerNameText.text = hasSpeaker ? speakerName : string.Empty;
	}

	private void SetPortrait(Sprite portrait)
	{
		if (portraitImage == null)
			return;

		bool shouldShowPortrait = usePortraits && portrait != null;
		portraitImage.gameObject.SetActive(shouldShowPortrait);
		portraitImage.sprite = portrait;
	}

	private void ApplyLineLayout(DialogueLine line)
	{
		if (bodyText == null)
			return;

		bool centerText = line != null && line.CenterText;
		bodyText.alignment = centerText
			? TextAlignmentOptions.Center
			: TextAlignmentOptions.MidlineLeft;
		bodyText.fontStyle = centerText ? FontStyles.Bold : FontStyles.Normal;
		bodyText.fontSize = line != null && line.FontSizeOverride > 0f
			? line.FontSizeOverride
			: bodyFontSize;
	}

	private void SetCanvasActive(bool active)
	{
		if (canvas != null && canvas.gameObject.activeSelf != active)
		{
			canvas.gameObject.SetActive(active);
		}
	}

	private float GetDeltaTime()
	{
		return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
	}

	private bool WasAdvancePressed()
	{
		return WasLeftMousePressed() ||
			(advanceWithKeyboard && WasKeyboardAdvancePressed());
	}

	private static bool WasKeyboardAdvancePressed()
	{
#if ENABLE_INPUT_SYSTEM
		Keyboard keyboard = Keyboard.current;

		return keyboard != null &&
			(keyboard.enterKey.wasPressedThisFrame ||
			keyboard.numpadEnterKey.wasPressedThisFrame ||
			keyboard.eKey.wasPressedThisFrame);
#else
		return Input.GetKeyDown(KeyCode.Return) ||
			Input.GetKeyDown(KeyCode.KeypadEnter) ||
			Input.GetKeyDown(KeyCode.E);
#endif
	}

	private static bool WasLeftMousePressed()
	{
#if ENABLE_INPUT_SYSTEM
		return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
		return Input.GetMouseButtonDown(0);
#endif
	}

	private void BuildRuntimeViewIfNeeded()
	{
		if (canvas != null &&
			canvasGroup != null &&
			backgroundImage != null &&
			speakerNameText != null &&
			bodyText != null)
		{
			if (advanceHintGroup == null && backgroundImage != null)
				CreateAdvanceHint(backgroundImage.rectTransform);

			return;
		}

		EnsureEventSystem();

		GameObject canvasObject = new GameObject(
			"Dialogue Canvas",
			typeof(RectTransform),
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster),
			typeof(CanvasGroup)
		);
		canvasObject.transform.SetParent(transform, false);

		canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 500;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = referenceResolution;
		scaler.matchWidthOrHeight = 0.5f;

		canvasGroup = canvasObject.GetComponent<CanvasGroup>();

		RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
		Stretch(canvasRect);

		RectTransform window = CreateRectTransform("Dialogue Window", canvasRect);
		window.anchorMin = new Vector2(0.5f, 0f);
		window.anchorMax = new Vector2(0.5f, 0f);
		window.pivot = new Vector2(0.5f, 0f);
		window.anchoredPosition = windowOffset;
		window.sizeDelta = windowSize;

		backgroundImage = window.gameObject.AddComponent<Image>();
		backgroundImage.raycastTarget = true;

		advanceButton = window.gameObject.AddComponent<Button>();
		advanceButton.transition = Selectable.Transition.None;
		advanceButton.targetGraphic = backgroundImage;

		RectTransform portrait = CreateRectTransform("Portrait", window);
		portrait.anchorMin = new Vector2(0f, 0.5f);
		portrait.anchorMax = new Vector2(0f, 0.5f);
		portrait.pivot = new Vector2(0.5f, 0.5f);
		portrait.anchoredPosition = portraitOffset;
		portrait.sizeDelta = portraitSize;

		portraitImage = portrait.gameObject.AddComponent<Image>();
		portraitImage.preserveAspect = true;
		portraitImage.raycastTarget = false;

		RectTransform speaker = CreateTextRect("Speaker Name", window);
		speaker.anchorMin = new Vector2(0f, 1f);
		speaker.anchorMax = new Vector2(1f, 1f);
		speaker.pivot = new Vector2(0.5f, 1f);
		speaker.offsetMin = new Vector2(textPaddingMin.x, -textPaddingMin.y - 54f);
		speaker.offsetMax = new Vector2(-textPaddingMax.x, -textPaddingMin.y);

		speakerNameText = speaker.GetComponent<TMP_Text>();
		speakerNameText.fontSize = speakerFontSize;
		speakerNameText.fontStyle = FontStyles.Bold;
		speakerNameText.alignment = TextAlignmentOptions.Left;

		RectTransform body = CreateTextRect("Body Text", window);
		body.anchorMin = new Vector2(0f, 0f);
		body.anchorMax = new Vector2(1f, 1f);
		body.pivot = new Vector2(0.5f, 0.5f);
		Vector2 bodyTextOffset = new Vector2(
			bodyTextHorizontalOffset,
			bodyTextVerticalOffset
		);
		body.offsetMin = textPaddingMin + bodyTextOffset;
		body.offsetMax = -textPaddingMax + bodyTextOffset;

		bodyText = body.GetComponent<TMP_Text>();
		bodyText.fontSize = bodyFontSize;
		bodyText.alignment = TextAlignmentOptions.MidlineLeft;
		bodyText.textWrappingMode = TextWrappingModes.Normal;
		bodyText.overflowMode = TextOverflowModes.Overflow;

		CreateAdvanceHint(window);
	}

	private void ApplyArtSettings()
	{
		if (backgroundImage != null)
		{
			backgroundImage.sprite = dialogueWindowSprite;
			backgroundImage.type = dialogueWindowSprite != null &&
				dialogueWindowSprite.border.sqrMagnitude > 0f
				? Image.Type.Sliced
				: Image.Type.Simple;
			backgroundImage.preserveAspect = false;
		}

		ApplyTextStyle(speakerNameText, speakerNameColor);
		ApplyTextStyle(bodyText, bodyTextColor);
		ApplyAdvanceHintStyle();
	}

	private void ApplyTextStyle(TMP_Text text, Color color)
	{
		if (text == null)
			return;

		if (dialogueFont != null)
		{
			text.font = dialogueFont;
		}

		text.color = color;
		text.raycastTarget = false;
		text.richText = true;
	}

	private void CreateAdvanceHint(RectTransform parent)
	{
		RectTransform hint = CreateTextRect("Advance Hint", parent);
		hint.anchorMin = new Vector2(1f, 0f);
		hint.anchorMax = new Vector2(1f, 0f);
		hint.pivot = new Vector2(1f, 0f);
		hint.anchoredPosition = advanceHintOffset;
		hint.sizeDelta = new Vector2(360f, 48f);

		advanceHintGroup = hint.gameObject.AddComponent<CanvasGroup>();
		advanceHintGroup.alpha = 0f;
		advanceHintGroup.interactable = false;
		advanceHintGroup.blocksRaycasts = false;

		advanceHintLabel = hint.GetComponent<TMP_Text>();
		advanceHintLabel.alignment = TextAlignmentOptions.BottomRight;
		advanceHintLabel.fontSize = advanceHintFontSize;
		advanceHintLabel.fontStyle = FontStyles.Bold;
		advanceHintLabel.raycastTarget = false;
		ApplyAdvanceHintStyle();
	}

	private void ApplyAdvanceHintStyle()
	{
		if (advanceHintLabel == null)
			return;

		if (dialogueFont != null)
			advanceHintLabel.font = dialogueFont;

		advanceHintLabel.text = advanceHintText;
		advanceHintLabel.color = new Color(0.23f, 0.16f, 0.1f, 0.88f);
	}

	private void BindAdvanceButton()
	{
		if (advanceButton == null)
			return;

		advanceButton.onClick.RemoveListener(Advance);
	}

	private static RectTransform CreateRectTransform(string objectName, RectTransform parent)
	{
		GameObject gameObject = new GameObject(
			objectName,
			typeof(RectTransform),
			typeof(CanvasRenderer)
		);
		RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
		rectTransform.SetParent(parent, false);
		return rectTransform;
	}

	private static RectTransform CreateTextRect(string objectName, RectTransform parent)
	{
		RectTransform rectTransform = CreateRectTransform(objectName, parent);
		rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
		return rectTransform;
	}

	private static void Stretch(RectTransform rectTransform)
	{
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
	}

	private static void EnsureEventSystem()
	{
		if (EventSystem.current != null)
			return;

		GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
		eventSystem.AddComponent<InputSystemUIInputModule>();
#else
		eventSystem.AddComponent<StandaloneInputModule>();
#endif
	}
}
