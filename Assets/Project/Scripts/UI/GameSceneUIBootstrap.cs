using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneUIBootstrap
{
	private const string PrefabResourcePath = "GameSceneUI";
	private const string MainMenuSceneName = "MainMenu";
	private const string LoadingSceneName = "LoadingScene";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Register()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name == MainMenuSceneName || scene.name == LoadingSceneName)
			return;

		if (Object.FindFirstObjectByType<GameSceneUIController>() != null)
			return;

		GameObject existingCanvas = GameObject.Find("GameCanvas");
		if (existingCanvas != null)
		{
			existingCanvas.AddComponent<GameSceneUIController>();
			return;
		}

		GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
		if (prefab == null)
		{
			Debug.LogWarning($"Could not load Resources/{PrefabResourcePath}.prefab.");
			return;
		}

		Object.Instantiate(prefab);
	}
}
