using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
	private const string GrayscaleButtonShaderName = "UI/Grayscale Button Tint";
	private static Material grayscaleButtonMaterial;

	public static AudioManager Instance { get; private set; }

	[SerializeField] private AudioSource backgroundMusicSource = null;
	[SerializeField] private AudioSource uiSfxSource = null;
	[SerializeField] private AudioClip hoverClip = null;
	[SerializeField] private AudioClip confirmClip = null;

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
		PreloadUiClip(hoverClip);
		PreloadUiClip(confirmClip);

		// Не уничтожать при смене сцены.
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		ConfigureUiSfxSource(uiSfxSource);
	}

	public static void ConfigureUiSfxSource(AudioSource audioSource)
	{
		if (audioSource == null)
		{
			return;
		}

		audioSource.loop = false;
		audioSource.playOnAwake = false;
		audioSource.spatialBlend = 0f;
	}

	public static bool TryKeepBackgroundMusicPlaying(AudioClip fallbackClip, float fallbackVolume)
	{
		if (Instance == null || Instance.backgroundMusicSource == null)
		{
			return false;
		}

		AudioSource source = Instance.backgroundMusicSource;
		source.loop = true;
		source.spatialBlend = 0f;
		if (source.clip == null && fallbackClip != null)
		{
			source.clip = fallbackClip;
			source.volume = Mathf.Clamp01(fallbackVolume);
		}

		if (!source.isPlaying && source.clip != null)
		{
			source.Play();
		}

		return source.clip != null;
	}

	public static void ConfigureMainMenuButtonColors(Button button)
	{
		if (button == null)
		{
			return;
		}

		ColorBlock colors = button.colors;
		colors.normalColor = new Color(0.55f, 0.58f, 0.64f, 1f);
		colors.highlightedColor = new Color(0.12f, 0.42f, 0.92f, 1f);
		colors.pressedColor = new Color(0.05f, 0.17f, 0.48f, 1f);
		colors.selectedColor = colors.normalColor;
		colors.disabledColor = new Color(0.42f, 0.44f, 0.48f, 1f);
		colors.fadeDuration = 0.04f;
		button.colors = colors;
		button.transition = Selectable.Transition.ColorTint;

		if (button.targetGraphic != null)
		{
			Material material = GetGrayscaleButtonMaterial();
			if (material != null)
			{
				button.targetGraphic.material = material;
			}

			button.targetGraphic.color = Color.white;
			button.targetGraphic.CrossFadeColor(colors.normalColor, 0f, true, true);
		}
	}

	private static Material GetGrayscaleButtonMaterial()
	{
		if (grayscaleButtonMaterial != null)
		{
			return grayscaleButtonMaterial;
		}

		Shader shader = Shader.Find(GrayscaleButtonShaderName);
		if (shader == null)
		{
			return null;
		}

		grayscaleButtonMaterial = new Material(shader)
		{
			hideFlags = HideFlags.DontSave
		};
		return grayscaleButtonMaterial;
	}

	private static void PreloadUiClip(AudioClip clip)
	{
		if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
		{
			clip.LoadAudioData();
		}
	}

}
