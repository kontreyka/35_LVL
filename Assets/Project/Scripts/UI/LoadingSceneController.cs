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

	[Header("Scene")]
	[SerializeField] private string fallbackTargetSceneName;

	[Header("Timing")]
	[SerializeField] private float minimumFirstFrameTime = 1f;
	[SerializeField] private float transitionDuration = 0.6f;
	[SerializeField] private float secondFrameTime = 0.8f;

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

		yield return new WaitForSecondsRealtime(minimumFirstFrameTime);

		AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
		loading.allowSceneActivation = false;

		while (loading.progress < 0.9f)
		{
			yield return null;
		}

		// Плавный переход между двумя кадрами
		float elapsed = 0f;

		while (elapsed < transitionDuration)
		{
			elapsed += Time.unscaledDeltaTime;

			float t = Mathf.Clamp01(elapsed / transitionDuration);

			SetAlpha(firstImage, 1f - t);
			SetAlpha(secondImage, t);

			yield return null;
		}

		SetAlpha(firstImage, 0f);
		SetAlpha(secondImage, 1f);

		yield return new WaitForSecondsRealtime(secondFrameTime);

		loading.allowSceneActivation = true;
	}

	private void SetAlpha(Image image, float alpha)
	{
		Color color = image.color;
		color.a = alpha;
		image.color = color;
	}
}
