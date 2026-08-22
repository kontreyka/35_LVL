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

	private void ConfigureMainMenuButtons()
	{
		Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (Button button in buttons)
		{
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
