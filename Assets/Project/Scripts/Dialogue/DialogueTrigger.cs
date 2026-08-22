using UnityEngine;
using UnityEngine.Events;

public sealed class DialogueTrigger : MonoBehaviour
{
	[SerializeField] private DialogueSequence dialogue;
	[SerializeField] private DialogueSystem dialogueSystem;
	[SerializeField] private bool playOnStart;
	[SerializeField] private bool playOnce = true;
	[SerializeField] private UnityEvent onDialogueStarted;
	[SerializeField] private UnityEvent onDialogueFinished;

	private DialogueSystem activeSystem;
	private bool hasPlayed;
	private bool waitsForCurrentDialogue;

	private void Start()
	{
		if (playOnStart)
		{
			PlayDialogue();
		}
	}

	private void OnDisable()
	{
		UnsubscribeFromActiveSystem();
	}

	public void PlayDialogue()
	{
		if (playOnce && hasPlayed)
			return;

		if (dialogue == null)
		{
			Debug.LogWarning($"{nameof(DialogueTrigger)} has no dialogue assigned.", this);
			return;
		}

		DialogueSystem system = ResolveDialogueSystem();

		if (system == null)
		{
			Debug.LogWarning($"{nameof(DialogueTrigger)} could not find {nameof(DialogueSystem)}.", this);
			return;
		}

		hasPlayed = true;
		waitsForCurrentDialogue = true;
		activeSystem = system;
		activeSystem.DialogueFinished -= HandleDialogueFinished;
		activeSystem.DialogueFinished += HandleDialogueFinished;

		onDialogueStarted?.Invoke();
		activeSystem.StartDialogue(dialogue);
	}

	public void ResetPlayOnce()
	{
		hasPlayed = false;
	}

	private DialogueSystem ResolveDialogueSystem()
	{
		if (dialogueSystem != null)
			return dialogueSystem;

		if (DialogueSystem.Instance != null)
			return DialogueSystem.Instance;

		return FindFirstObjectByType<DialogueSystem>();
	}

	private void HandleDialogueFinished(DialogueSequence finishedDialogue)
	{
		if (!waitsForCurrentDialogue || finishedDialogue != dialogue)
			return;

		waitsForCurrentDialogue = false;
		onDialogueFinished?.Invoke();
		UnsubscribeFromActiveSystem();
	}

	private void UnsubscribeFromActiveSystem()
	{
		if (activeSystem == null)
			return;

		activeSystem.DialogueFinished -= HandleDialogueFinished;
		activeSystem = null;
	}
}
