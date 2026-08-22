using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
	public static AudioManager Instance { get; private set; }

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
		ConfigureMainMenuButtons();
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
		colors.disabledColor = new Color(0.3f, 0.32f, 0.36f, 0.5f);
		colors.fadeDuration = 0.04f;
		button.colors = colors;
	}

	private void ConfigureMainMenuButtons()
	{
		Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (Button button in buttons)
		{
			ConfigureMainMenuButtonColors(button);
			EventTrigger trigger = button.GetComponent<EventTrigger>();
			if (trigger == null)
			{
				trigger = button.gameObject.AddComponent<EventTrigger>();
			}

			if (trigger.triggers == null)
			{
				trigger.triggers = new List<EventTrigger.Entry>();
			}

			trigger.triggers.RemoveAll(entry => entry.eventID == EventTriggerType.PointerEnter);
			EventTrigger.Entry hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
			hoverEntry.callback.AddListener(_ => PlayHover());
			trigger.triggers.Add(hoverEntry);

			button.onClick.RemoveListener(PlayConfirm);
			button.onClick.AddListener(PlayConfirm);
		}
	}

	private static void PreloadUiClip(AudioClip clip)
	{
		if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
		{
			clip.LoadAudioData();
		}
	}

	private void PlayHover()
	{
		if (uiSfxSource != null && hoverClip != null)
		{
			uiSfxSource.PlayOneShot(hoverClip);
		}
	}

	private void PlayConfirm()
	{
		if (uiSfxSource != null && confirmClip != null)
		{
			uiSfxSource.PlayOneShot(confirmClip);
		}
	}
}
