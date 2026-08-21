using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
	public void StartGame()
	{
		SceneManager.LoadScene("Scene01");
	}

	public void ExitGame()
	{
		Application.Quit();

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}