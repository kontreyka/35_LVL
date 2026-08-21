using UnityEngine;

public class AudioManager : MonoBehaviour
{
	public static AudioManager Instance { get; private set; }

	private void Awake()
	{
		// Если AudioManager уже существует —
		// второй экземпляр уничтожаем.
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;

		// Не уничтожать при смене сцены.
		DontDestroyOnLoad(gameObject);
	}
}