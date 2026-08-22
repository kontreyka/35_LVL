using UnityEngine;

public class MainMenuController : MonoBehaviour
{
	[SerializeField] private LevelTransitionManager levelTransitionManager;

	public void StartGame()
	{
		if (levelTransitionManager == null)
		{
			levelTransitionManager = FindFirstObjectByType<LevelTransitionManager>();
		}

		if (levelTransitionManager != null)
		{
			levelTransitionManager.LoadNextScene();
		}
		else
		{
			Debug.LogWarning($"{nameof(MainMenuController)} could not find a {nameof(LevelTransitionManager)}.", this);
		}
	}

	public void ExitGame()
	{
		Application.Quit();

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}
