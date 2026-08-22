using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class TitleIntroController : MonoBehaviour
{
	[Header("Title")]
	[SerializeField] private RectTransform title;
	[SerializeField] private CanvasGroup titleGroup;
	[SerializeField] private TMP_Text titleText;
	[SerializeField] private bool showTitle = true;

	[Header("Menu")]
	[SerializeField] private CanvasGroup menuButtons;
	[SerializeField] private CanvasGroup rightVisual;

	[Header("Button Feedback")]
	[SerializeField] private AudioSource uiSfxSource;
	[SerializeField] private AudioClip hoverClip;
	[SerializeField] private AudioClip confirmClip;

	[Header("Iris")]
	[SerializeField] private Material irisMaterial;
	[SerializeField] private GameObject introOverlay;

	[Header("Timing")]
	[SerializeField] private float blackScreenDelay = 0.4f;
	[SerializeField] private float irisDuration = 1.2f;
	[SerializeField] private float titleHoldDuration = 1.8f;
	[SerializeField] private float titleDisappearDuration = 1.2f;

	[Header("Wind")]
	[SerializeField] private float windDistance = 180f;
	[SerializeField] private float windLift = 15f;
	[SerializeField] private float characterSpacingAmount = 18f;

	[SerializeField]
	private AnimationCurve windCurve = AnimationCurve.EaseInOut(
		0f, 0f,
		1f, 1f
	);

	[SerializeField]
	private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(
		0f, 0f,
		1f, 1f
	);

	[Header("Iris Settings")]
	[SerializeField] private float irisFinalRadius = 1.2f;

	[SerializeField]
	private AnimationCurve irisCurve = AnimationCurve.EaseInOut(
		0f, 0f,
		1f, 1f
	);

	private Vector2 titleStartPosition;
	private Vector3 titleStartScale;
	private float titleStartCharacterSpacing;

	private static readonly int RadiusID =
		Shader.PropertyToID("_Radius");

	private void Start()
	{
		ConfigureMenuButtonFeedback();

		if (!showTitle)
		{
			title.gameObject.SetActive(false);
		}

		// Запоминаем исходное состояние названия
		titleStartPosition = title.anchoredPosition;
		titleStartScale = title.localScale;
		titleStartCharacterSpacing = titleText.characterSpacing;

		titleGroup.alpha = 1f;

		// Меню изначально скрыто
		menuButtons.alpha = 0f;
		menuButtons.interactable = false;
		menuButtons.blocksRaycasts = false;

		rightVisual.alpha = 0f;

		// Полностью закрытый iris
		irisMaterial.SetFloat(RadiusID, 0f);

		StartCoroutine(PlayIntro());
	}

	private IEnumerator PlayIntro()
	{
		// ------------------------------------------------
		// 1. Чёрный экран
		// ------------------------------------------------

		yield return new WaitForSecondsRealtime(blackScreenDelay);

		// ------------------------------------------------
		// 2. Круговое раскрытие
		// ------------------------------------------------

		float time = 0f;

		while (time < irisDuration)
		{
			time += Time.unscaledDeltaTime;

			float t = Mathf.Clamp01(time / irisDuration);
			float irisT = irisCurve.Evaluate(t);

			float radius = Mathf.Lerp(
				0f,
				irisFinalRadius,
				irisT
			);

			irisMaterial.SetFloat(
				RadiusID,
				radius
			);

			yield return null;
		}

		irisMaterial.SetFloat(
			RadiusID,
			irisFinalRadius
		);

		// После полного раскрытия чёрная маска больше не нужна
		if (introOverlay != null)
		{
			introOverlay.SetActive(false);
		}

		// ------------------------------------------------
		// 3. Название остаётся в центре
		// ------------------------------------------------

		if (!showTitle)
		{
			RevealMenu();
			yield break;
		}

		yield return new WaitForSecondsRealtime(titleHoldDuration);

		// ------------------------------------------------
		// 4. Название начинает "уносить ветром"
		// ------------------------------------------------

		time = 0f;

		while (time < titleDisappearDuration)
		{
			time += Time.unscaledDeltaTime;

			float t = Mathf.Clamp01(
				time / titleDisappearDuration
			);

			float windT = windCurve.Evaluate(t);
			float fadeT = fadeCurve.Evaluate(t);

			// Лёгкое движение вправо и вверх
			title.anchoredPosition =
				titleStartPosition +
				new Vector2(
					windDistance * windT,
					windLift * windT
				);

			// Размер букв не деформируем
			title.localScale = titleStartScale;

			// Расстояние между буквами увеличивается
			titleText.characterSpacing = Mathf.Lerp(
				titleStartCharacterSpacing,
				titleStartCharacterSpacing +
				characterSpacingAmount,
				windT
			);

			// Название растворяется
			titleGroup.alpha = 1f - fadeT;

			// ------------------------------------------------
			// Меню начинает проявляться после начала исчезновения
			// ------------------------------------------------

			float menuStart = 0.18f;

			float menuT = Mathf.Clamp01(
				(t - menuStart) /
				(1f - menuStart)
			);

			// Немного сглаживаем появление
			menuT = Mathf.SmoothStep(
				0f,
				1f,
				menuT
			);

			menuButtons.alpha = menuT;
			rightVisual.alpha = menuT;

			yield return null;
		}

		RevealMenu();
	}

	private void RevealMenu()
	{
		titleGroup.alpha = 0f;
		menuButtons.alpha = 1f;
		rightVisual.alpha = 1f;
		menuButtons.interactable = true;
		menuButtons.blocksRaycasts = true;
	}

	private void ConfigureMenuButtonFeedback()
	{
		foreach (Button button in GetComponentsInChildren<Button>(true))
		{
			AudioManager.ConfigureMainMenuButtonColors(button);
			ColorBlock colors = button.colors;
			EventTrigger trigger = button.GetComponent<EventTrigger>();
			if (trigger == null)
			{
				trigger = button.gameObject.AddComponent<EventTrigger>();
			}

			if (trigger.triggers == null)
			{
				trigger.triggers = new List<EventTrigger.Entry>();
			}

			trigger.triggers.RemoveAll(entry => entry.eventID == EventTriggerType.PointerEnter
				|| entry.eventID == EventTriggerType.PointerExit
				|| entry.eventID == EventTriggerType.PointerDown
				|| entry.eventID == EventTriggerType.PointerUp);
			AddPointerEvent(trigger, EventTriggerType.PointerEnter, () =>
			{
				SetButtonColor(button, colors.highlightedColor);
				PlayUiSfx(hoverClip);
			});
			AddPointerEvent(trigger, EventTriggerType.PointerExit, () => SetButtonColor(button, colors.normalColor));
			AddPointerEvent(trigger, EventTriggerType.PointerDown, () => SetButtonColor(button, colors.pressedColor));
			AddPointerEvent(trigger, EventTriggerType.PointerUp, () => SetButtonColor(button, colors.highlightedColor));

			button.onClick.RemoveListener(PlayConfirm);
			button.onClick.AddListener(PlayConfirm);
		}
	}

	private static void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction action)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
		entry.callback.AddListener(_ => action());
		trigger.triggers.Add(entry);
	}

	private static void SetButtonColor(Button button, Color color)
	{
		if (button != null && button.targetGraphic != null)
		{
			button.targetGraphic.color = color;
		}
	}

	private void PlayConfirm()
	{
		PlayUiSfx(confirmClip);
	}

	private void PlayUiSfx(AudioClip clip)
	{
		if (uiSfxSource != null && clip != null)
		{
			uiSfxSource.PlayOneShot(clip);
		}
	}
}
