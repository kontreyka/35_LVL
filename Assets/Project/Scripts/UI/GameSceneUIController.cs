using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DisallowMultipleComponent]
public sealed class GameSceneUIController : MonoBehaviour
{
	private const string LoadingScenePath = "Assets/Scenes/LoadingScene.unity";
	private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

	[SerializeField] private GameObject settingsPanel;
	[SerializeField] private Button settingsButton;
	[SerializeField] private Button exitButton;
	[SerializeField] private int canvasSortingOrder = 100;
	[SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
	[SerializeField, Min(0f)] private float fadeInDuration = 0.35f;

	private static bool isReturningToMenu;

	private void Awake()
	{
		isReturningToMenu = false;
		EnsureEventSystem();
		ResolveViewReferences();
		ConfigureCanvas();
		BindButtons();
	}

	public void OpenSettings()
	{
		if (settingsPanel != null)
			settingsPanel.SetActive(true);
	}

	public void ExitGame()
	{
		if (isReturningToMenu)
			return;

		isReturningToMenu = true;
		Time.timeScale = 1f;
		LoadingSceneController.SetTargetScene(MainMenuScenePath);
		SceneTransitionOverlay.FadeOutAndLoad(
			LoadingScenePath,
			fadeOutDuration,
			fadeInDuration,
			Color.white
		);
	}

	private void ResolveViewReferences()
	{
		if (settingsPanel == null)
		{
			Transform settingsTransform = FindChild(transform, "SettingsPanel");
			if (settingsTransform != null)
				settingsPanel = settingsTransform.gameObject;
		}

		if (settingsButton == null)
		{
			Transform buttonTransform = FindChild(transform, "SettingsButton");
			if (buttonTransform != null)
				settingsButton = buttonTransform.GetComponent<Button>();
		}

		if (exitButton == null)
		{
			Transform buttonTransform = FindChild(transform, "ExitButton");
			if (buttonTransform != null)
				exitButton = buttonTransform.GetComponent<Button>();
		}
	}

	private void ConfigureCanvas()
	{
		Canvas canvas = GetComponentInChildren<Canvas>(true);
		if (canvas == null)
			return;

		canvas.overrideSorting = true;
		canvas.sortingOrder = canvasSortingOrder;
	}

	private void BindButtons()
	{
		if (settingsButton != null)
			settingsButton.onClick.AddListener(OpenSettings);

		if (exitButton != null)
			exitButton.onClick.AddListener(ExitGame);
	}

	private static Transform FindChild(Transform root, string objectName)
	{
		if (root.name == objectName)
			return root;

		for (int i = 0; i < root.childCount; i++)
		{
			Transform found = FindChild(root.GetChild(i), objectName);
			if (found != null)
				return found;
		}

		return null;
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
