using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Scene01AudioSettingsUI : MonoBehaviour
{
	[Header("Audio Settings")]
	[SerializeField] private AudioMixer audioMixer = null;

	[Header("Layout")]
	[SerializeField] private int sortingOrder = 120;
	[SerializeField] private Vector2 buttonSize = new Vector2(86f, 52f);
	[SerializeField] private Vector2 buttonOffset = new Vector2(22f, 0f);
	[SerializeField] private Vector2 panelSize = new Vector2(310f, 178f);
	[SerializeField] private Vector2 panelOffset = new Vector2(122f, 0f);

	[Header("Text")]
	[SerializeField] private string buttonLabel = "Звук";
	[SerializeField] private string titleLabel = "Настройки звука";
	[SerializeField] private string musicLabel = "Музыка";
	[SerializeField] private string sfxLabel = "Звуки";

	[Header("Colors")]
	[SerializeField] private Color buttonColor = new Color(0.16f, 0.14f, 0.12f, 0.88f);
	[SerializeField] private Color buttonHighlightColor = new Color(0.35f, 0.29f, 0.19f, 0.95f);
	[SerializeField] private Color panelColor = new Color(0.08f, 0.075f, 0.068f, 0.9f);
	[SerializeField] private Color textColor = new Color(0.92f, 0.84f, 0.68f, 1f);
	[SerializeField] private Color sliderTrackColor = new Color(0.2f, 0.17f, 0.13f, 1f);
	[SerializeField] private Color sliderFillColor = new Color(0.83f, 0.68f, 0.36f, 1f);
	[SerializeField] private Color sliderHandleColor = new Color(0.96f, 0.88f, 0.63f, 1f);

	public Button SoundButton { get; private set; }
	public GameObject SettingsPanel { get; private set; }
	public Slider MusicSlider { get; private set; }
	public Slider SfxSlider { get; private set; }

	private Canvas canvas;

	private void Awake()
	{
		BuildInterface();
	}

	public void BuildInterface()
	{
		if (SoundButton != null && SettingsPanel != null)
			return;

		canvas = CreateCanvas();
		EnsureEventSystem();

		SoundButton = CreateButton(
			canvas.transform,
			"SoundButton",
			buttonLabel,
			buttonSize,
			buttonOffset,
			0.5f
		);
		SoundButton.onClick.AddListener(TogglePanel);

		SettingsPanel = CreatePanel(canvas.transform);
		MusicSlider = CreateSliderRow(SettingsPanel.transform, "MusicRow", musicLabel, 32f);
		SfxSlider = CreateSliderRow(SettingsPanel.transform, "SfxRow", sfxLabel, -26f);

		Button closeButton = CreateButton(
			SettingsPanel.transform,
			"CloseButton",
			"X",
			new Vector2(32f, 28f),
			new Vector2(panelSize.x - 42f, panelSize.y * 0.5f - 26f),
			0.5f
		);
		closeButton.onClick.AddListener(HidePanel);

		CreateText(
			SettingsPanel.transform,
			"Title",
			titleLabel,
			18,
			TextAnchor.MiddleLeft,
			new Vector2(18f, panelSize.y * 0.5f - 28f),
			new Vector2(panelSize.x - 70f, 34f)
		);

		AudioSettingsController controller =
			SettingsPanel.AddComponent<AudioSettingsController>();
		controller.Initialize(audioMixer, MusicSlider, SfxSlider);

		SettingsPanel.SetActive(false);
	}

	public void TogglePanel()
	{
		if (SettingsPanel == null)
		{
			BuildInterface();
		}

		SettingsPanel.SetActive(!SettingsPanel.activeSelf);
	}

	public void HidePanel()
	{
		if (SettingsPanel != null)
		{
			SettingsPanel.SetActive(false);
		}
	}

	private Canvas CreateCanvas()
	{
		GameObject canvasObject = new GameObject("Scene01_AudioSettingsCanvas");
		canvasObject.layer = GetUILayer();
		canvasObject.transform.SetParent(transform, false);

		RectTransform canvasTransform = canvasObject.AddComponent<RectTransform>();
		canvasTransform.anchorMin = Vector2.zero;
		canvasTransform.anchorMax = Vector2.one;
		canvasTransform.offsetMin = Vector2.zero;
		canvasTransform.offsetMax = Vector2.zero;

		Canvas createdCanvas = canvasObject.AddComponent<Canvas>();
		createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
		createdCanvas.sortingOrder = sortingOrder;

		CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		canvasObject.AddComponent<GraphicRaycaster>();

		return createdCanvas;
	}

	private void EnsureEventSystem()
	{
		if (EventSystem.current != null)
			return;

		GameObject eventSystem = new GameObject("Scene01_AudioEventSystem");
		eventSystem.transform.SetParent(transform, false);
		eventSystem.AddComponent<EventSystem>();
		AddInputModule(eventSystem);
	}

	private static void AddInputModule(GameObject eventSystem)
	{
		Type inputSystemModuleType = Type.GetType(
			"UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"
		);

		if (inputSystemModuleType != null)
		{
			eventSystem.AddComponent(inputSystemModuleType);
			return;
		}

		eventSystem.AddComponent<StandaloneInputModule>();
	}

	private GameObject CreatePanel(Transform parent)
	{
		GameObject panel = CreateUIObject("SoundSettingsPanel", parent);
		RectTransform panelTransform = panel.GetComponent<RectTransform>();
		panelTransform.anchorMin = new Vector2(0f, 0.5f);
		panelTransform.anchorMax = new Vector2(0f, 0.5f);
		panelTransform.pivot = new Vector2(0f, 0.5f);
		panelTransform.anchoredPosition = panelOffset;
		panelTransform.sizeDelta = panelSize;

		Image panelImage = panel.AddComponent<Image>();
		panelImage.color = panelColor;
		panelImage.raycastTarget = true;

		return panel;
	}

	private Button CreateButton(
		Transform parent,
		string name,
		string label,
		Vector2 size,
		Vector2 anchoredPosition,
		float anchorY
	)
	{
		GameObject buttonObject = CreateUIObject(name, parent);
		RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
		buttonTransform.anchorMin = new Vector2(0f, anchorY);
		buttonTransform.anchorMax = new Vector2(0f, anchorY);
		buttonTransform.pivot = new Vector2(0f, 0.5f);
		buttonTransform.anchoredPosition = anchoredPosition;
		buttonTransform.sizeDelta = size;

		Image buttonImage = buttonObject.AddComponent<Image>();
		buttonImage.color = buttonColor;

		Button button = buttonObject.AddComponent<Button>();
		button.targetGraphic = buttonImage;
		button.colors = CreateButtonColors();

		CreateText(
			buttonObject.transform,
			"Label",
			label,
			17,
			TextAnchor.MiddleCenter,
			Vector2.zero,
			size
		);

		return button;
	}

	private Slider CreateSliderRow(
		Transform parent,
		string rowName,
		string label,
		float anchoredY
	)
	{
		GameObject row = CreateUIObject(rowName, parent);
		RectTransform rowTransform = row.GetComponent<RectTransform>();
		rowTransform.anchorMin = new Vector2(0f, 0.5f);
		rowTransform.anchorMax = new Vector2(0f, 0.5f);
		rowTransform.pivot = new Vector2(0f, 0.5f);
		rowTransform.anchoredPosition = new Vector2(18f, anchoredY);
		rowTransform.sizeDelta = new Vector2(panelSize.x - 36f, 42f);

		CreateText(
			row.transform,
			label + "Label",
			label,
			15,
			TextAnchor.MiddleLeft,
			new Vector2(0f, 0f),
			new Vector2(82f, 30f)
		);

		return CreateSlider(
			row.transform,
			label + "Slider",
			new Vector2(98f, 0f),
			new Vector2(panelSize.x - 154f, 24f)
		);
	}

	private Slider CreateSlider(
		Transform parent,
		string name,
		Vector2 anchoredPosition,
		Vector2 size
	)
	{
		GameObject sliderObject = CreateUIObject(name, parent);
		RectTransform sliderTransform = sliderObject.GetComponent<RectTransform>();
		sliderTransform.anchorMin = new Vector2(0f, 0.5f);
		sliderTransform.anchorMax = new Vector2(0f, 0.5f);
		sliderTransform.pivot = new Vector2(0f, 0.5f);
		sliderTransform.anchoredPosition = anchoredPosition;
		sliderTransform.sizeDelta = size;

		GameObject background = CreateUIObject("Background", sliderObject.transform);
		RectTransform backgroundTransform = background.GetComponent<RectTransform>();
		backgroundTransform.anchorMin = new Vector2(0f, 0.5f);
		backgroundTransform.anchorMax = new Vector2(1f, 0.5f);
		backgroundTransform.pivot = new Vector2(0.5f, 0.5f);
		backgroundTransform.anchoredPosition = Vector2.zero;
		backgroundTransform.sizeDelta = new Vector2(0f, 8f);
		background.AddComponent<Image>().color = sliderTrackColor;

		GameObject fillArea = CreateUIObject("Fill Area", sliderObject.transform);
		RectTransform fillAreaTransform = fillArea.GetComponent<RectTransform>();
		fillAreaTransform.anchorMin = new Vector2(0f, 0f);
		fillAreaTransform.anchorMax = new Vector2(1f, 1f);
		fillAreaTransform.offsetMin = new Vector2(8f, 0f);
		fillAreaTransform.offsetMax = new Vector2(-8f, 0f);

		GameObject fill = CreateUIObject("Fill", fillArea.transform);
		RectTransform fillTransform = fill.GetComponent<RectTransform>();
		fillTransform.anchorMin = new Vector2(0f, 0.5f);
		fillTransform.anchorMax = new Vector2(0f, 0.5f);
		fillTransform.pivot = new Vector2(0f, 0.5f);
		fillTransform.anchoredPosition = Vector2.zero;
		fillTransform.sizeDelta = new Vector2(0f, 8f);
		fill.AddComponent<Image>().color = sliderFillColor;

		GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObject.transform);
		RectTransform handleAreaTransform = handleArea.GetComponent<RectTransform>();
		handleAreaTransform.anchorMin = Vector2.zero;
		handleAreaTransform.anchorMax = Vector2.one;
		handleAreaTransform.offsetMin = new Vector2(8f, 0f);
		handleAreaTransform.offsetMax = new Vector2(-8f, 0f);

		GameObject handle = CreateUIObject("Handle", handleArea.transform);
		RectTransform handleTransform = handle.GetComponent<RectTransform>();
		handleTransform.sizeDelta = new Vector2(18f, 18f);
		Image handleImage = handle.AddComponent<Image>();
		handleImage.color = sliderHandleColor;

		Slider slider = sliderObject.AddComponent<Slider>();
		slider.minValue = 0f;
		slider.maxValue = 1f;
		slider.value = 0.8f;
		slider.direction = Slider.Direction.LeftToRight;
		slider.fillRect = fillTransform;
		slider.handleRect = handleTransform;
		slider.targetGraphic = handleImage;

		return slider;
	}

	private Text CreateText(
		Transform parent,
		string name,
		string content,
		int fontSize,
		TextAnchor alignment,
		Vector2 anchoredPosition,
		Vector2 size
	)
	{
		GameObject textObject = CreateUIObject(name, parent);
		RectTransform textTransform = textObject.GetComponent<RectTransform>();
		textTransform.anchorMin = new Vector2(0f, 0.5f);
		textTransform.anchorMax = new Vector2(0f, 0.5f);
		textTransform.pivot = new Vector2(0f, 0.5f);
		textTransform.anchoredPosition = anchoredPosition;
		textTransform.sizeDelta = size;

		Text text = textObject.AddComponent<Text>();
		text.text = content;
		text.font = GetDefaultFont();
		text.fontSize = fontSize;
		text.alignment = alignment;
		text.color = textColor;
		text.raycastTarget = false;

		return text;
	}

	private GameObject CreateUIObject(string name, Transform parent)
	{
		GameObject uiObject = new GameObject(name);
		uiObject.layer = GetUILayer();
		uiObject.transform.SetParent(parent, false);
		uiObject.AddComponent<RectTransform>();

		return uiObject;
	}

	private ColorBlock CreateButtonColors()
	{
		ColorBlock colors = ColorBlock.defaultColorBlock;
		colors.normalColor = Color.white;
		colors.highlightedColor = buttonHighlightColor;
		colors.pressedColor = new Color(0.08f, 0.07f, 0.06f, 1f);
		colors.selectedColor = buttonHighlightColor;
		colors.disabledColor = new Color(0.22f, 0.2f, 0.18f, 0.45f);
		colors.fadeDuration = 0.08f;

		return colors;
	}

	private static Font GetDefaultFont()
	{
		Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

		if (font != null)
			return font;

		return Resources.GetBuiltinResource<Font>("Arial.ttf");
	}

	private static int GetUILayer()
	{
		int uiLayer = LayerMask.NameToLayer("UI");
		return uiLayer >= 0 ? uiLayer : 5;
	}
}
