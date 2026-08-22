using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
	public void LoadScene(string sceneName)
	{
		LoadingSceneController.SetTargetScene(sceneName);
		SceneManager.LoadScene("LoadingScene");
	}
}
