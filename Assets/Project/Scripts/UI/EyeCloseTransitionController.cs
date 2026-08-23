using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class EyeCloseTransitionController : MonoBehaviour
{
	private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");

	[SerializeField] private SceneReference targetScene = new SceneReference();
	[SerializeField] private Material eyeVignetteMaterial;
	[SerializeField] private TMP_FontAsset hintFont;

	[Header("Prompt")]
	[SerializeField] private string promptText = "Нажмите Enter";
	[SerializeField] private float promptFontSize = 42f;
	[SerializeField] private Vector2 promptOffset = new Vector2(-56f, 42f);

	[Header("Transition")]
	[SerializeField, Min(0.05f)] private float closeDuration = 1.25f;
	[SerializeField, Min(0f)] private float closedHoldDuration = 0.12f;
	[SerializeField, Min(0.05f)] private float openDuration = 0.8f;

	private TMP_Text promptLabel;
	private CanvasGroup vignetteCanvasGroup;
	private Material runtimeVignetteMaterial;
	private bool transitionStarted;

	private void Awake()
	{
		BuildPrompt();
		BuildVignette();
		SetEyeProgress(0f);
	}

	private void Update()
	{
		if (ModalSettingsPanel.IsOpen || transitionStarted || !WasEnterPressed())
			return;

		if (!targetScene.IsAssigned)
		{
			Debug.LogWarning($"{nameof(EyeCloseTransitionController)} requires a Target Scene.", this);
			return;
		}

		transitionStarted = true;

		if (promptLabel != null)
			promptLabel.gameObject.SetActive(false);

		DontDestroyOnLoad(gameObject);
		StartCoroutine(PlayTransition());
	}

	private void OnDestroy()
	{
		if (runtimeVignetteMaterial != null)
			Destroy(runtimeVignetteMaterial);
	}

	private IEnumerator PlayTransition()
	{
		vignetteCanvasGroup.blocksRaycasts = true;
		vignetteCanvasGroup.interactable = true;

		yield return AnimateEye(0f, 1f, closeDuration);

		AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetScene.Path);

		while (loadOperation != null && !loadOperation.isDone)
			yield return null;

		yield return new WaitForEndOfFrame();

		if (closedHoldDuration > 0f)
			yield return new WaitForSecondsRealtime(closedHoldDuration);

		yield return AnimateEye(1f, 0f, openDuration);

		Destroy(gameObject);
	}

	private IEnumerator AnimateEye(float from, float to, float duration)
	{
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float normalizedTime = Mathf.Clamp01(elapsed / duration);
			float smoothTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
			SetEyeProgress(Mathf.Lerp(from, to, smoothTime));
			yield return null;
		}

		SetEyeProgress(to);
	}

	private void SetEyeProgress(float progress)
	{
		if (runtimeVignetteMaterial != null)
			runtimeVignetteMaterial.SetFloat(ProgressProperty, Mathf.Clamp01(progress));
	}

	private void BuildPrompt()
	{
		GameObject canvasObject = CreateCanvas("Eye Transition Prompt Canvas", 450);

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

	private void BuildVignette()
	{
		GameObject canvasObject = CreateCanvas("Eye Closing Vignette Canvas", 2000);
		vignetteCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
		vignetteCanvasGroup.blocksRaycasts = false;
		vignetteCanvasGroup.interactable = false;

		GameObject imageObject = new GameObject(
			"Eye Closing Vignette",
			typeof(RectTransform),
			typeof(CanvasRenderer),
			typeof(RawImage)
		);
		imageObject.transform.SetParent(canvasObject.transform, false);

		RectTransform rect = imageObject.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		RawImage image = imageObject.GetComponent<RawImage>();
		image.texture = Texture2D.whiteTexture;
		image.color = Color.white;
		image.raycastTarget = true;

		if (eyeVignetteMaterial != null)
		{
			runtimeVignetteMaterial = Instantiate(eyeVignetteMaterial);
			runtimeVignetteMaterial.name = $"{eyeVignetteMaterial.name} (Runtime)";
			image.material = runtimeVignetteMaterial;
		}
	}

	private GameObject CreateCanvas(string objectName, int sortingOrder)
	{
		GameObject canvasObject = new GameObject(
			objectName,
			typeof(RectTransform),
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster)
		);
		canvasObject.transform.SetParent(transform, false);

		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = sortingOrder;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		return canvasObject;
	}

	private static bool WasEnterPressed()
	{
#if ENABLE_INPUT_SYSTEM
		Keyboard keyboard = Keyboard.current;
		return keyboard != null &&
			(keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
#else
		return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		targetScene.SynchronizePath();
	}
#endif
}
