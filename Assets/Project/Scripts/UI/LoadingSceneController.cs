using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
	public static string TargetSceneName;
	public static string TargetScenePath;

	public static void SetTargetScene(string scenePath)
	{
		TargetScenePath = scenePath;
		TargetSceneName = null;
	}

	[Header("Loading Images")]
	[SerializeField] private Image firstImage;
	[SerializeField] private Image secondImage;
	[SerializeField] private Image thirdImage;

	[Header("Scene")]
	[SerializeField] private string fallbackTargetSceneName;

	[Header("Timing")]
	[SerializeField] private float firstFrameTime = 0.8f;
	[SerializeField] private float secondFrameTime = 0.6f;
	[SerializeField] private float thirdFrameTime = 0.8f;
	[SerializeField] private float transitionDuration = 0.4f;
	[SerializeField, Min(0.1f)] private float animationSpeedMultiplier = 1.35f;
	[SerializeField, Min(0f)] private float sceneFadeOutDuration = 0.35f;
	[SerializeField, Min(0f)] private float sceneFadeInDuration = 0.35f;
	[SerializeField] private Color sceneFadeColor = Color.white;

	private void Start()
	{
		string sceneToLoad = !string.IsNullOrWhiteSpace(TargetScenePath)
			? TargetScenePath
			: string.IsNullOrWhiteSpace(TargetSceneName)
				? fallbackTargetSceneName
				: TargetSceneName;

		TargetScenePath = null;
		TargetSceneName = null;

		StartCoroutine(LoadSceneRoutine(sceneToLoad));
	}

	private IEnumerator LoadSceneRoutine(string sceneName)
	{
		SetAlpha(firstImage, 1f);
		SetAlpha(secondImage, 0f);
		SetAlpha(thirdImage, 0f);

		AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
		loading.allowSceneActivation = false;

		while (loading.progress < 0.9f)
		{
		// Кадр 1 — птица в клетке
		yield return new WaitForSecondsRealtime(firstFrameTime / animationSpeedMultiplier);
		if (loading.progress >= 0.9f)
			break;

		// Переход 1 -> 2
		yield return StartCoroutine(FadeBetween(firstImage, secondImage, transitionDuration / animationSpeedMultiplier));
		if (loading.progress >= 0.9f)
			break;

		// Кадр 2 — перья в клетке
		yield return new WaitForSecondsRealtime(secondFrameTime / animationSpeedMultiplier);
		if (loading.progress >= 0.9f)
			break;

		// Переход 2 -> 3
		yield return StartCoroutine(FadeBetween(secondImage, thirdImage, transitionDuration / animationSpeedMultiplier));
		if (loading.progress >= 0.9f)
			break;

		// Кадр 3 — перья упали
		yield return new WaitForSecondsRealtime(thirdFrameTime / animationSpeedMultiplier);
		if (loading.progress >= 0.9f)
			break;

		// Ждём, пока сцена будет почти готова
			yield return StartCoroutine(FadeBetween(thirdImage, firstImage, transitionDuration / animationSpeedMultiplier));
		}

		yield return SceneTransitionOverlay.FadeOutForSceneChange(
			sceneFadeOutDuration,
			sceneFadeInDuration,
			sceneFadeColor
		);

		loading.allowSceneActivation = true;
	}

	private IEnumerator FadeBetween(Image from, Image to, float duration)
	{
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);

			SetAlpha(from, 1f - t);
			SetAlpha(to, t);

			yield return null;
		}

		SetAlpha(from, 0f);
		SetAlpha(to, 1f);
	}

	private void SetAlpha(Image image, float alpha)
	{
		Color color = image.color;
		color.a = alpha;
		image.color = color;
	}
}
