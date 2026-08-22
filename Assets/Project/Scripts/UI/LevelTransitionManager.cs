using System;
using UnityEngine;

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

	[Header("Screen Fade")]
	[SerializeField, Min(0f)] private float fadeOutDuration = 0.45f;
	[SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
	[SerializeField] private Color fadeColor = Color.white;

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
		LoadingSceneController.SetTargetScene(nextScene.Path);
		SceneTransitionOverlay.FadeOutAndLoad(
			loadingScene.Path,
			fadeOutDuration,
			fadeInDuration,
			fadeColor
		);
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		loadingScene.SynchronizePath();
		nextScene.SynchronizePath();
	}
#endif
}
