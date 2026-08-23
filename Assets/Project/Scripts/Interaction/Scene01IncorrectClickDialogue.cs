using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class Scene01IncorrectClickDialogue : MonoBehaviour
{
	[SerializeField] private Camera sceneCamera;
	[SerializeField] private Collider2D correctCageHotspot;
	[SerializeField] private DialogueSequence dialogue;
	[SerializeField] private DialogueSystem dialogueSystem;
	[SerializeField] private bool disableAfterCorrectCageClick = true;

	private bool correctCageClicked;
	private bool isStartingDialogue;

	private void Awake()
	{
		ResolveDependencies();
	}

	private void Update()
	{
		if ((disableAfterCorrectCageClick && correctCageClicked) ||
			isStartingDialogue ||
			ModalSettingsPanel.IsOpen ||
			(dialogueSystem != null && dialogueSystem.IsRunning) ||
			!WasLeftMousePressed() ||
			IsPointerOverUi())
		{
			return;
		}

		ResolveDependencies();

		if (sceneCamera == null || dialogue == null)
			return;

		Vector3 worldPosition = sceneCamera.ScreenToWorldPoint(GetPointerPosition());

		if (correctCageHotspot != null && correctCageHotspot.OverlapPoint(worldPosition))
		{
			correctCageClicked = true;
			return;
		}

		isStartingDialogue = true;
		StartCoroutine(StartDialogueNextFrame());
	}

	private IEnumerator StartDialogueNextFrame()
	{
		yield return null;
		ResolveDependencies();

		if (dialogueSystem != null && dialogue != null && !dialogueSystem.IsRunning)
			dialogueSystem.StartDialogue(dialogue);

		isStartingDialogue = false;
	}

	private void ResolveDependencies()
	{
		if (sceneCamera == null)
			sceneCamera = Camera.main;

		if (dialogueSystem == null)
			dialogueSystem = DialogueSystem.Instance ?? FindFirstObjectByType<DialogueSystem>();
	}

	private static bool IsPointerOverUi()
	{
		return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
	}

	private static bool WasLeftMousePressed()
	{
#if ENABLE_INPUT_SYSTEM
		return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
		return Input.GetMouseButtonDown(0);
#endif
	}

	private static Vector2 GetPointerPosition()
	{
#if ENABLE_INPUT_SYSTEM
		return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
		return Input.mousePosition;
#endif
	}
}
