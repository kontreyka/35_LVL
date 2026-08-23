using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class BirdWalkDialogueProgression : MonoBehaviour
{
	[SerializeField] private BirdWalkController birdWalkController;
	[SerializeField] private DialogueSystem dialogueSystem;
	[SerializeField] private LevelTransitionManager levelTransitionManager;
	[SerializeField] private SceneReference nextScene = new SceneReference();
	[SerializeField] private DialogueSequence ellipsisDialogue;

	[Header("Progress")]
	[SerializeField, Min(1)] private int stepsPerDialogue = 3;
	[SerializeField, Min(1)] private int dialoguesBeforeTransition = 3;

	private int stepsSinceLastDialogue;
	private int dialoguesShown;
	private bool waitsForDialogue;
	private bool waitsForTransitionInput;

	private void Awake()
	{
		if (birdWalkController == null)
			birdWalkController = GetComponent<BirdWalkController>();

		if (dialogueSystem == null)
			dialogueSystem = DialogueSystem.Instance ?? FindFirstObjectByType<DialogueSystem>();

		if (levelTransitionManager == null)
			levelTransitionManager = FindFirstObjectByType<LevelTransitionManager>();
	}

	private void OnEnable()
	{
		if (birdWalkController != null)
			birdWalkController.StepMade += HandleStepMade;
	}

	private void OnDisable()
	{
		if (birdWalkController != null)
			birdWalkController.StepMade -= HandleStepMade;

		UnsubscribeFromDialogue();
	}

	private void Update()
	{
		if (!waitsForTransitionInput || ModalSettingsPanel.IsOpen || !WasMovementPressed())
			return;

		waitsForTransitionInput = false;

		if (birdWalkController != null)
			birdWalkController.enabled = false;

		LoadNextScene();
	}

	private void HandleStepMade()
	{
		if (waitsForDialogue || waitsForTransitionInput || ++stepsSinceLastDialogue < stepsPerDialogue)
			return;

		stepsSinceLastDialogue = 0;
		ShowEllipsisDialogue();
	}

	private void ShowEllipsisDialogue()
	{
		if (dialogueSystem == null || ellipsisDialogue == null)
		{
			Debug.LogWarning($"{nameof(BirdWalkDialogueProgression)} is missing its dialogue setup.", this);
			return;
		}

		waitsForDialogue = true;
		dialoguesShown++;

		if (birdWalkController != null)
			birdWalkController.enabled = false;

		dialogueSystem.DialogueFinished += HandleDialogueFinished;
		dialogueSystem.StartDialogue(ellipsisDialogue, true);
	}

	private void HandleDialogueFinished(DialogueSequence finishedDialogue)
	{
		if (!waitsForDialogue || finishedDialogue != ellipsisDialogue)
			return;

		UnsubscribeFromDialogue();

		if (dialoguesShown >= dialoguesBeforeTransition)
		{
			StartCoroutine(WaitForTransitionInputAfterDialogueFade());
			return;
		}

		StartCoroutine(ResumeWalkingAfterDialogueFade());
	}

	private IEnumerator ResumeWalkingAfterDialogueFade()
	{
		yield return new WaitForSecondsRealtime(dialogueSystem.HideFadeDuration);
		waitsForDialogue = false;

		if (birdWalkController != null)
			birdWalkController.enabled = true;
	}

	private IEnumerator WaitForTransitionInputAfterDialogueFade()
	{
		yield return new WaitForSecondsRealtime(dialogueSystem.HideFadeDuration);
		waitsForDialogue = false;
		waitsForTransitionInput = true;

		if (birdWalkController != null)
			birdWalkController.enabled = true;
	}

	private void LoadNextScene()
	{
		if (nextScene.IsAssigned)
		{
			SceneManager.LoadScene(nextScene.Path);
			return;
		}

		if (levelTransitionManager == null)
			levelTransitionManager = FindFirstObjectByType<LevelTransitionManager>();

		if (levelTransitionManager != null)
			levelTransitionManager.LoadNextScene();
		else
			Debug.LogWarning($"{nameof(BirdWalkDialogueProgression)} could not find a {nameof(LevelTransitionManager)}.", this);
	}

	private void UnsubscribeFromDialogue()
	{
		if (dialogueSystem != null)
			dialogueSystem.DialogueFinished -= HandleDialogueFinished;
	}

	private static bool WasMovementPressed()
	{
#if ENABLE_INPUT_SYSTEM
		Keyboard keyboard = Keyboard.current;
		return keyboard != null &&
			(keyboard.aKey.wasPressedThisFrame ||
			keyboard.dKey.wasPressedThisFrame ||
			keyboard.leftArrowKey.wasPressedThisFrame ||
			keyboard.rightArrowKey.wasPressedThisFrame);
#else
		return Input.GetKeyDown(KeyCode.A) ||
			Input.GetKeyDown(KeyCode.D) ||
			Input.GetKeyDown(KeyCode.LeftArrow) ||
			Input.GetKeyDown(KeyCode.RightArrow);
#endif
	}
}
