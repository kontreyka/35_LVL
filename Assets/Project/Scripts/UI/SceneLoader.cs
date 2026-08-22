using UnityEngine;

public class SceneLoader : MonoBehaviour
{
	public void LoadScene(string sceneName)
	{
		LoadingSceneController.SetTargetScene(sceneName);
		SceneTransitionOverlay.FadeOutAndLoad(
			"LoadingScene",
			0.35f,
			0.35f,
			Color.white
		);
	}
}
