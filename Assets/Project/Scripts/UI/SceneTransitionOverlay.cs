using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SceneTransitionOverlay : MonoBehaviour
{
	private static SceneTransitionOverlay instance;

	private CanvasGroup canvasGroup;
	private Image fadeImage;
	private Coroutine fadeCoroutine;
	private float revealDuration;
	private bool revealAfterSceneLoad;

	public static void FadeOutAndLoad(
		string scenePath,
		float fadeOutDuration,
		float fadeInDuration,
		Color fadeColor
	)
	{
		SceneTransitionOverlay overlay = GetOrCreate(fadeColor);
		overlay.revealDuration = Mathf.Max(0f, fadeInDuration);
		overlay.revealAfterSceneLoad = true;
		overlay.StartFadeOutThenLoad(scenePath, fadeOutDuration);
	}

	public static Coroutine FadeOutForSceneChange(
		float fadeOutDuration,
		float fadeInDuration,
		Color fadeColor
	)
	{
		SceneTransitionOverlay overlay = GetOrCreate(fadeColor);
		overlay.revealDuration = Mathf.Max(0f, fadeInDuration);
		overlay.revealAfterSceneLoad = true;
		return overlay.StartCoroutine(overlay.FadeTo(1f, fadeOutDuration));
	}

	private static SceneTransitionOverlay GetOrCreate(Color fadeColor)
	{
		if (instance == null)
		{
			GameObject overlayObject = new GameObject(
				"Scene Transition Overlay",
				typeof(RectTransform)
			);
			instance = overlayObject.AddComponent<SceneTransitionOverlay>();
		}

		instance.SetColor(fadeColor);
		return instance;
	}

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(gameObject);
			return;
		}

		instance = this;
		DontDestroyOnLoad(gameObject);
		SceneManager.sceneLoaded += HandleSceneLoaded;
		BuildView();
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;

		if (instance == this)
			instance = null;
	}

	private void StartFadeOutThenLoad(string scenePath, float duration)
	{
		if (fadeCoroutine != null)
			StopCoroutine(fadeCoroutine);

		fadeCoroutine = StartCoroutine(FadeOutThenLoadRoutine(scenePath, duration));
	}

	private IEnumerator FadeOutThenLoadRoutine(string scenePath, float duration)
	{
		yield return FadeTo(1f, duration);
		SceneManager.LoadScene(scenePath);
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!revealAfterSceneLoad)
			return;

		revealAfterSceneLoad = false;

		if (fadeCoroutine != null)
			StopCoroutine(fadeCoroutine);

		canvasGroup.alpha = 1f;
		fadeCoroutine = StartCoroutine(RevealAfterSceneLoad());
	}

	private IEnumerator RevealAfterSceneLoad()
	{
		yield return new WaitForEndOfFrame();
		yield return FadeTo(0f, revealDuration);
		Destroy(gameObject);
	}

	private IEnumerator FadeTo(float targetAlpha, float duration)
	{
		float startAlpha = canvasGroup.alpha;
		canvasGroup.blocksRaycasts = true;
		canvasGroup.interactable = true;

		if (duration <= 0f)
		{
			canvasGroup.alpha = targetAlpha;
			yield break;
		}

		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
			yield return null;
		}

		canvasGroup.alpha = targetAlpha;
	}

	private void SetColor(Color color)
	{
		if (fadeImage != null)
			fadeImage.color = color;
	}

	private void BuildView()
	{
		gameObject.AddComponent<Canvas>();
		gameObject.AddComponent<CanvasScaler>();
		gameObject.AddComponent<GraphicRaycaster>();
		canvasGroup = gameObject.AddComponent<CanvasGroup>();

		Canvas canvas = GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 2000;

		CanvasScaler scaler = GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		canvasGroup.alpha = 0f;
		canvasGroup.blocksRaycasts = true;
		canvasGroup.interactable = true;

		GameObject imageObject = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		imageObject.transform.SetParent(transform, false);

		RectTransform rect = imageObject.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		fadeImage = imageObject.GetComponent<Image>();
		fadeImage.color = Color.white;
		fadeImage.raycastTarget = true;
	}
}
