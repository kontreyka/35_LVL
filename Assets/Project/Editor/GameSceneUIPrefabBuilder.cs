using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class GameSceneUIPrefabBuilder
{
	private const string SourceScenePath = "Assets/Scenes/Scene01.unity";
	private const string PrefabPath = "Assets/Resources/GameSceneUI.prefab";

	static GameSceneUIPrefabBuilder()
	{
		EditorApplication.delayCall += EnsurePrefabExists;
	}

	[MenuItem("Tools/Game UI/Rebuild Game Scene UI Prefab")]
	private static void RebuildPrefab()
	{
		BuildPrefab(true);
	}

	private static void EnsurePrefabExists()
	{
		BuildPrefab(false);
	}

	private static void BuildPrefab(bool overwrite)
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
			return;

		if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
			return;

		if (!AssetDatabase.IsValidFolder("Assets/Resources"))
			AssetDatabase.CreateFolder("Assets", "Resources");

		Scene previewScene = EditorSceneManager.OpenPreviewScene(SourceScenePath);
		try
		{
			GameObject sourceCanvas = previewScene
				.GetRootGameObjects()
				.FirstOrDefault(root => root.name == "GameCanvas");

			if (sourceCanvas == null)
			{
				Debug.LogError($"GameCanvas was not found in {SourceScenePath}.");
				return;
			}

			GameObject prefabRoot = Object.Instantiate(sourceCanvas);
			prefabRoot.name = "GameSceneUI";
			SceneManager.MoveGameObjectToScene(prefabRoot, previewScene);

			if (prefabRoot.GetComponent<GameSceneUIController>() == null)
				prefabRoot.AddComponent<GameSceneUIController>();

			PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
			Object.DestroyImmediate(prefabRoot);
			AssetDatabase.SaveAssets();
		}
		finally
		{
			EditorSceneManager.ClosePreviewScene(previewScene);
		}
	}
}
