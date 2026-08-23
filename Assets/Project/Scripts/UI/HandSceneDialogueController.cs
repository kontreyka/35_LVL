using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class HandSceneDialogueController : MonoBehaviour
{
	[SerializeField] private DialogueSequence dialogue;
	[SerializeField] private DialogueSystem dialogueSystem;
	[SerializeField] private LevelTransitionManager levelTransitionManager;
	[SerializeField] private SceneReference nextScene = new SceneReference();
	[SerializeField] private TMP_FontAsset hintFont;

	[Header("Prompt")]
	[SerializeField] private string promptText = "Нажмите Enter";
	[SerializeField] private float promptFontSize = 42f;
	[SerializeField] private Vector2 promptOffset = new Vector2(-56f, 42f);

	private TMP_Text promptLabel;
	private bool dialogueStarted;
	private bool transitionStarted;

	private void Awake()
	{
		CreatePrompt();
	}

	private void Start()
	{
		ResolveDependencies();
	}

	private void Update()
	{
		if (ModalSettingsPanel.IsOpen || dialogueStarted || !WasStartPressed())
			return;

		dialogueStarted = true;

		if (promptLabel != null)
			promptLabel.gameObject.SetActive(false);

		StartCoroutine(StartDialogueNextFrame());
	}

	private void OnDisable()
	{
		if (dialogueSystem != null)
			dialogueSystem.DialogueFinished -= HandleDialogueFinished;
	}

	private IEnumerator StartDialogueNextFrame()
	{
		yield return null;
		ResolveDependencies();

		if (dialogueSystem == null || dialogue == null)
		{
			Debug.LogWarning($"{nameof(HandSceneDialogueController)} is missing its dialogue setup.", this);
			yield break;
		}

		dialogueSystem.DialogueFinished -= HandleDialogueFinished;
		dialogueSystem.DialogueFinished += HandleDialogueFinished;
		dialogueSystem.StartDialogue(dialogue);
	}

	private void HandleDialogueFinished(DialogueSequence finishedDialogue)
	{
		if (transitionStarted || finishedDialogue != dialogue)
			return;

		transitionStarted = true;
		dialogueSystem.DialogueFinished -= HandleDialogueFinished;
		if (nextScene.IsAssigned)
		{
			SceneManager.LoadScene(nextScene.Path);
			return;
		}

		if (levelTransitionManager == null)
			levelTransitionManager = FindFirstObjectByType<LevelTransitionManager>();

		if (levelTransitionManager != null)
		{
			levelTransitionManager.LoadNextScene();
		}
		else
		{
			Debug.LogWarning($"{nameof(HandSceneDialogueController)} could not find a {nameof(LevelTransitionManager)}.", this);
		}
	}

	private void ResolveDependencies()
	{
		if (dialogueSystem == null)
			dialogueSystem = DialogueSystem.Instance ?? FindFirstObjectByType<DialogueSystem>();

		if (levelTransitionManager == null)
			levelTransitionManager = FindFirstObjectByType<LevelTransitionManager>();
	}

	private void CreatePrompt()
	{
		GameObject canvasObject = new GameObject(
			"Hand Scene Prompt Canvas",
			typeof(RectTransform),
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster)
		);
		canvasObject.transform.SetParent(transform, false);

		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 450;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

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
		rect.anchoredPosition = promptOffset;
		rect.sizeDelta = new Vector2(720f, 90f);

		promptLabel = promptObject.GetComponent<TMP_Text>();
		promptLabel.text = promptText;
		promptLabel.font = hintFont;
		promptLabel.fontSize = promptFontSize;
		promptLabel.color = Color.white;
		promptLabel.alignment = TextAlignmentOptions.BottomRight;
		promptLabel.fontStyle = FontStyles.Bold;
		promptLabel.outlineColor = new Color32(0, 0, 0, 220);
		promptLabel.outlineWidth = 0.22f;
		promptLabel.raycastTarget = false;
	}

	private static bool WasStartPressed()
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
