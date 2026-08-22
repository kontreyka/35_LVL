using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
	public void LoadScene(string sceneName)
	{
		LoadingSceneController.TargetSceneName = sceneName;
		SceneManager.LoadScene("LoadingScene");
	}
}