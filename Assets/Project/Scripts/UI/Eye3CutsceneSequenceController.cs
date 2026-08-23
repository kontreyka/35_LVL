using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class Eye3CutsceneSequenceController : MonoBehaviour
{
	[SerializeField] private GameObject[] frames;
	[SerializeField] private DialogueSequence dialogue;
	[SerializeField] private DialogueSystem dialogueSystem;
	[SerializeField] private EyeCloseTransitionController eyeTransition;
	[SerializeField] private TMP_FontAsset hintFont;

	[Header("Frame Transition")]
	[SerializeField, Min(0f)] private float framePromptDelay = 3f;
	[SerializeField, Min(0.05f)] private float framePromptFadeDuration = 0.3f;
	[SerializeField, Min(0.05f)] private float whiteFadeDuration = 0.35f;

	private CanvasGroup whiteOverlay;
	private CanvasGroup framePrompt;
	private bool dialogueStarted;

	private void Awake()
	{
		BuildWhiteOverlay();
		BuildFramePrompt();
		ShowOnlyFrame(0);
	}

	private void Start()
	{
		ResolveDependencies();
		StartCoroutine(PlaySequence());
	}

	private void OnDestroy()
	{
		if (dialogueSystem != null)
			dialogueSystem.DialogueFinished -= HandleDialogueFinished;
	}

	private IEnumerator PlaySequence()
	{
		for (int frameIndex = 1; frameIndex < frames.Length; frameIndex++)
		{
			yield return WaitForFrameAdvance();
			yield return FadeWhite(0f, 1f);
			ShowOnlyFrame(frameIndex);
			yield return FadeWhite(1f, 0f);
		}

		yield return WaitForFrameAdvance();
		StartDialogue();
	}

	private IEnumerator WaitForFrameAdvance()
	{
		if (framePromptDelay > 0f)
			yield return new WaitForSeconds(framePromptDelay);

		yield return FadeFramePrompt(0f, 1f);

		while (ModalSettingsPanel.IsOpen || !WasAdvancePressed())
			yield return null;

		yield return FadeFramePrompt(1f, 0f);
	}

	private void StartDialogue()
	{
		if (dialogueStarted)
			return;

		dialogueStarted = true;
		ResolveDependencies();

		if (dialogueSystem == null || dialogue == null)
		{
			Debug.LogWarning($"{nameof(Eye3CutsceneSequenceController)} is missing its dialogue setup.", this);
			ShowTransitionPrompt();
			return;
		}

		dialogueSystem.DialogueFinished -= HandleDialogueFinished;
		dialogueSystem.DialogueFinished += HandleDialogueFinished;
		dialogueSystem.StartDialogue(dialogue);
	}

	private void HandleDialogueFinished(DialogueSequence finishedDialogue)
	{
		if (finishedDialogue != dialogue)
			return;

		dialogueSystem.DialogueFinished -= HandleDialogueFinished;
		StartCoroutine(ShowTransitionPromptNextFrame());
	}

	private IEnumerator ShowTransitionPromptNextFrame()
	{
		yield return null;
		ShowTransitionPrompt();
	}

	private void ShowTransitionPrompt()
	{
		ResolveDependencies();

		if (eyeTransition != null)
		{
			eyeTransition.ShowPromptAndWait();
		}
		else
		{
			Debug.LogWarning($"{nameof(Eye3CutsceneSequenceController)} could not find an eye transition.", this);
		}
	}

	private IEnumerator FadeWhite(float from, float to)
	{
		whiteOverlay.blocksRaycasts = true;
		float elapsed = 0f;

		while (elapsed < whiteFadeDuration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / whiteFadeDuration);
			whiteOverlay.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
			yield return null;
		}

		whiteOverlay.alpha = to;
		whiteOverlay.blocksRaycasts = to > 0f;
	}

	private IEnumerator FadeFramePrompt(float from, float to)
	{
		float elapsed = 0f;

		while (elapsed < framePromptFadeDuration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / framePromptFadeDuration);
			framePrompt.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
			yield return null;
		}

		framePrompt.alpha = to;
	}

	private void ShowOnlyFrame(int activeIndex)
	{
		if (frames == null)
			return;

		for (int index = 0; index < frames.Length; index++)
		{
			if (frames[index] != null)
				frames[index].SetActive(index == activeIndex);
		}
	}

	private void ResolveDependencies()
	{
		if (dialogueSystem == null)
			dialogueSystem = DialogueSystem.Instance ?? FindFirstObjectByType<DialogueSystem>();

		if (eyeTransition == null)
			eyeTransition = FindFirstObjectByType<EyeCloseTransitionController>();
	}

	private void BuildWhiteOverlay()
	{
		GameObject canvasObject = new GameObject(
			"Eye 3 Frame Transition Canvas",
			typeof(RectTransform),
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster),
			typeof(CanvasGroup)
		);
		canvasObject.transform.SetParent(transform, false);

		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 400;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		whiteOverlay = canvasObject.GetComponent<CanvasGroup>();
		whiteOverlay.alpha = 0f;
		whiteOverlay.blocksRaycasts = false;
		whiteOverlay.interactable = false;

		GameObject imageObject = new GameObject(
			"White Frame Transition",
			typeof(RectTransform),
			typeof(CanvasRenderer),
			typeof(Image)
		);
		imageObject.transform.SetParent(canvasObject.transform, false);

		RectTransform rect = imageObject.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		Image image = imageObject.GetComponent<Image>();
		image.color = Color.white;
		image.raycastTarget = true;
	}

	private void BuildFramePrompt()
	{
		GameObject canvasObject = new GameObject(
			"Eye 3 Continue Prompt Canvas",
			typeof(RectTransform),
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster),
			typeof(CanvasGroup)
		);
		canvasObject.transform.SetParent(transform, false);

		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 450;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		framePrompt = canvasObject.GetComponent<CanvasGroup>();
		framePrompt.alpha = 0f;
		framePrompt.interactable = false;
		framePrompt.blocksRaycasts = false;

		GameObject promptObject = new GameObject(
			"Continue Prompt",
			typeof(RectTransform),
			typeof(CanvasRenderer),
			typeof(TextMeshProUGUI)
		);
		promptObject.transform.SetParent(canvasObject.transform, false);

		RectTransform rect = promptObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(1f, 0f);
		rect.anchorMax = new Vector2(1f, 0f);
		rect.pivot = new Vector2(1f, 0f);
		rect.anchoredPosition = new Vector2(-56f, 42f);
		rect.sizeDelta = new Vector2(720f, 90f);

		TMP_Text text = promptObject.GetComponent<TMP_Text>();
		text.text = "Нажмите Enter";
		text.font = hintFont;
		text.fontSize = 42f;
		text.color = Color.white;
		text.alignment = TextAlignmentOptions.BottomRight;
		text.fontStyle = FontStyles.Bold;
		text.outlineColor = new Color32(0, 0, 0, 220);
		text.outlineWidth = 0.22f;
		text.raycastTarget = false;
	}

	private static bool WasAdvancePressed()
	{
#if ENABLE_INPUT_SYSTEM
		Keyboard keyboard = Keyboard.current;
		Mouse mouse = Mouse.current;

		return (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
			(keyboard != null &&
			(keyboard.enterKey.wasPressedThisFrame ||
			keyboard.numpadEnterKey.wasPressedThisFrame ||
			keyboard.eKey.wasPressedThisFrame));
#else
		return Input.GetMouseButtonDown(0) ||
			Input.GetKeyDown(KeyCode.Return) ||
			Input.GetKeyDown(KeyCode.KeypadEnter) ||
			Input.GetKeyDown(KeyCode.E);
#endif
	}
}
