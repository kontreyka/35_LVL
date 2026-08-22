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

		// Кадр 1 — птица в клетке
		yield return new WaitForSecondsRealtime(firstFrameTime);

		// Переход 1 -> 2
		yield return StartCoroutine(FadeBetween(firstImage, secondImage, transitionDuration));

		// Кадр 2 — перья в клетке
		yield return new WaitForSecondsRealtime(secondFrameTime);

		// Переход 2 -> 3
		yield return StartCoroutine(FadeBetween(secondImage, thirdImage, transitionDuration));

		// Кадр 3 — перья упали
		yield return new WaitForSecondsRealtime(thirdFrameTime);

		// Ждём, пока сцена будет почти готова
		while (loading.progress < 0.9f)
		{
			yield return null;
		}

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
