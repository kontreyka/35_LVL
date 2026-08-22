using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public sealed class SceneReference
{
#if UNITY_EDITOR
	[SerializeField] private SceneAsset sceneAsset;
#endif
	[SerializeField, HideInInspector] private string scenePath;

	public string Path => scenePath;
	public bool IsAssigned => !string.IsNullOrWhiteSpace(scenePath);

#if UNITY_EDITOR
	public void SynchronizePath()
	{
		scenePath = sceneAsset == null ? string.Empty : AssetDatabase.GetAssetPath(sceneAsset);
	}
#endif
}

[DisallowMultipleComponent]
public sealed class LevelTransitionManager : MonoBehaviour
{
	[Header("Scenes")]
	[SerializeField] private SceneReference loadingScene = new SceneReference();
	[SerializeField] private SceneReference nextScene = new SceneReference();

	[Header("Fade Out")]
	[SerializeField, Min(0f)] private float fadeOutDuration = 0.45f;
	[SerializeField] private Color fadeColor = Color.black;

	private bool isTransitioning;

	public bool IsTransitioning => isTransitioning;

	public void LoadNextScene()
	{
		if (isTransitioning)
			return;

		if (!loadingScene.IsAssigned || !nextScene.IsAssigned)
		{
			Debug.LogWarning(
				$"{nameof(LevelTransitionManager)} requires both Loading Scene and Next Scene in the Inspector.",
				this
			);
			return;
		}

		isTransitioning = true;
		StartCoroutine(TransitionRoutine());
	}

	private IEnumerator TransitionRoutine()
	{
		CanvasGroup fadeGroup = CreateFadeOverlay();
		float elapsed = 0f;

		while (elapsed < fadeOutDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			fadeGroup.alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
			yield return null;
		}

		fadeGroup.alpha = 1f;
		LoadingSceneController.SetTargetScene(nextScene.Path);
		SceneManager.LoadScene(loadingScene.Path);
	}

	private CanvasGroup CreateFadeOverlay()
	{
		GameObject canvasObject = new GameObject(
			"Scene Fade Canvas",
			typeof(RectTransform),
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster),
			typeof(CanvasGroup)
		);
		canvasObject.transform.SetParent(transform, false);

		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 1000;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);

		CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
		group.alpha = 0f;
		group.blocksRaycasts = true;
		group.interactable = true;

		GameObject imageObject = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		imageObject.transform.SetParent(canvasObject.transform, false);

		RectTransform imageTransform = imageObject.GetComponent<RectTransform>();
		imageTransform.anchorMin = Vector2.zero;
		imageTransform.anchorMax = Vector2.one;
		imageTransform.offsetMin = Vector2.zero;
		imageTransform.offsetMax = Vector2.zero;

		Image image = imageObject.GetComponent<Image>();
		image.color = fadeColor;
		image.raycastTarget = true;

		return group;
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		loadingScene.SynchronizePath();
		nextScene.SynchronizePath();
	}
#endif
}
