using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Eye3CutsceneSequenceController : MonoBehaviour
{
	[SerializeField] private GameObject[] frames;
	[SerializeField] private DialogueSequence dialogue;
	[SerializeField] private DialogueSystem dialogueSystem;
	[SerializeField] private EyeCloseTransitionController eyeTransition;

	[Header("Frame Transition")]
	[SerializeField, Min(0.1f)] private float frameHoldDuration = 1.6f;
	[SerializeField, Min(0.05f)] private float whiteFadeDuration = 0.35f;

	private CanvasGroup whiteOverlay;
	private bool dialogueStarted;

	private void Awake()
	{
		BuildWhiteOverlay();
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
			yield return new WaitForSeconds(frameHoldDuration);
			yield return FadeWhite(0f, 1f);
			ShowOnlyFrame(frameIndex);
			yield return FadeWhite(1f, 0f);
		}

		StartDialogue();
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
}
