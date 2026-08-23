using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class RoomPrototypeKeySpriteSizing
{
	public static Vector2 GetSizeForHeight(float sourceWidth, float sourceHeight, float height)
	{
		if (sourceWidth <= 0f || sourceHeight <= 0f || height <= 0f)
		{
			return Vector2.zero;
		}
		return new Vector2(height * sourceWidth / sourceHeight, height);
	}
}

public enum RoomPrototypePanelSlot
{
	TopLeft,
	TopRight,
	BottomLeft,
	BottomRight
}

public enum RoomPrototypePanelDirection
{
	Left,
	Right,
	Up,
	Down
}

public readonly struct RoomPrototypeViewport : IEquatable<RoomPrototypeViewport>
{
	public readonly int X;
	public readonly int Y;
	public readonly int Width;
	public readonly int Height;

	public RoomPrototypeViewport(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	public bool Equals(RoomPrototypeViewport other)
	{
		return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
	}

	public override bool Equals(object obj)
	{
		return obj is RoomPrototypeViewport other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			int hash = 17;
			hash = hash * 31 + X;
			hash = hash * 31 + Y;
			hash = hash * 31 + Width;
			hash = hash * 31 + Height;
			return hash;
		}
	}

	public override string ToString()
	{
		return RoomPrototypeLevelOnePanelModel.FormatViewport(this);
	}
}

public readonly struct RoomPrototypePanelState
{
	public readonly RoomPrototypePanelSlot Slot;
	public readonly RoomPrototypeViewport Viewport;
	public readonly bool IsZoomed;
	public readonly string Label;

	public RoomPrototypePanelState(
		RoomPrototypePanelSlot slot,
		RoomPrototypeViewport viewport,
		bool isZoomed,
		string label
	)
	{
		Slot = slot;
		Viewport = viewport;
		IsZoomed = isZoomed;
		Label = label;
	}
}

public static class RoomPrototypeLevelOnePanelModel
{
	public const int RoomColumns = 4;
	public const int RoomRows = 2;

	public static RoomPrototypeViewport GetInitialViewport(RoomPrototypePanelSlot slot)
	{
		switch (slot)
		{
			case RoomPrototypePanelSlot.TopLeft:
				return new RoomPrototypeViewport(0, 0, 2, 2);
			case RoomPrototypePanelSlot.BottomLeft:
				return new RoomPrototypeViewport(1, 0, 2, 2);
			case RoomPrototypePanelSlot.TopRight:
				return new RoomPrototypeViewport(2, 0, 2, 2);
			case RoomPrototypePanelSlot.BottomRight:
				return new RoomPrototypeViewport(3, 1, 1, 1);
			default:
				throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
		}
	}

	public static RoomPrototypePanelState GetInitialState(RoomPrototypePanelSlot slot)
	{
		return new RoomPrototypePanelState(slot, GetInitialViewport(slot), false, "ROOM");
	}

	public static RoomPrototypePanelState GetZoomState(RoomPrototypePanelSlot slot)
	{
		switch (slot)
		{
			case RoomPrototypePanelSlot.TopLeft:
				return new RoomPrototypePanelState(slot, new RoomPrototypeViewport(1, 0, 1, 1), true, "CUPBOARD");
			case RoomPrototypePanelSlot.BottomLeft:
				return new RoomPrototypePanelState(slot, new RoomPrototypeViewport(1, 0, 1, 1), true, "ROOM");
			case RoomPrototypePanelSlot.TopRight:
				return new RoomPrototypePanelState(slot, new RoomPrototypeViewport(3, 0, 1, 1), true, "CAGE");
			case RoomPrototypePanelSlot.BottomRight:
				return GetInitialState(slot);
			default:
				throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
		}
	}

	public static bool CanZoom(RoomPrototypePanelState state)
	{
		return !state.IsZoomed && state.Slot != RoomPrototypePanelSlot.BottomRight;
	}

	public static bool CanDropKeyIntoTruck(RoomPrototypePanelState keyState, RoomPrototypePanelState truckState)
	{
		return keyState.Slot == RoomPrototypePanelSlot.TopLeft
			&& keyState.IsZoomed
			&& keyState.Viewport.Equals(new RoomPrototypeViewport(0, 0, 1, 1))
			&& truckState.Slot == RoomPrototypePanelSlot.BottomLeft
			&& truckState.IsZoomed
			&& truckState.Viewport.Equals(new RoomPrototypeViewport(1, 1, 1, 1));
	}

	public static bool CanDropAppleIntoTruck(
		RoomPrototypePanelState appleState,
		RoomPrototypePanelState truckState,
		RoomPrototypePanelState cageState
	)
	{
		return appleState.Slot == RoomPrototypePanelSlot.BottomRight
			&& appleState.Viewport.Equals(new RoomPrototypeViewport(3, 1, 1, 1))
			&& truckState.Slot == RoomPrototypePanelSlot.BottomRight
			&& truckState.Viewport.Equals(new RoomPrototypeViewport(3, 1, 1, 1))
			&& cageState.Slot == RoomPrototypePanelSlot.TopRight
			&& cageState.IsZoomed
			&& cageState.Viewport.Equals(new RoomPrototypeViewport(3, 0, 1, 1));
	}

	public static bool TryMoveTruckToNextCell(RoomPrototypePanelState state, out RoomPrototypePanelState nextState)
	{
		nextState = state;
		if (state.Slot != RoomPrototypePanelSlot.BottomLeft
			|| !state.IsZoomed
			|| !state.Viewport.Equals(new RoomPrototypeViewport(1, 1, 1, 1)))
		{
			return false;
		}

		nextState = new RoomPrototypePanelState(
			RoomPrototypePanelSlot.BottomLeft,
			new RoomPrototypeViewport(2, 1, 1, 1),
			true,
			"TRUCK"
		);
		return true;
	}

	public static bool TryMoveTruckToTable(RoomPrototypePanelState state, out RoomPrototypePanelState nextState)
	{
		nextState = state;
		if (state.Slot != RoomPrototypePanelSlot.BottomLeft
			|| !state.IsZoomed
			|| !state.Viewport.Equals(new RoomPrototypeViewport(2, 1, 1, 1)))
		{
			return false;
		}

		nextState = new RoomPrototypePanelState(
			RoomPrototypePanelSlot.BottomRight,
			new RoomPrototypeViewport(3, 1, 1, 1),
			false,
			"TABLE"
		);
		return true;
	}

	public static float GetAlignedHandoffX(float sourceX, float sourcePanelWidth, float targetPanelWidth)
	{
		return sourcePanelWidth <= 0f ? sourceX : sourceX * targetPanelWidth / sourcePanelWidth;
	}

	public static bool TryNavigate(
		RoomPrototypePanelState state,
		RoomPrototypePanelDirection direction,
		out RoomPrototypePanelState nextState
	)
	{
		nextState = state;

		if (!state.IsZoomed)
		{
			return false;
		}

		if (state.Slot == RoomPrototypePanelSlot.TopLeft)
		{
			if (state.Viewport.Equals(new RoomPrototypeViewport(1, 0, 1, 1)) && direction == RoomPrototypePanelDirection.Left)
			{
				nextState = new RoomPrototypePanelState(state.Slot, new RoomPrototypeViewport(0, 0, 1, 1), true, "KEY RACK");
				return true;
			}

			if (state.Viewport.Equals(new RoomPrototypeViewport(0, 0, 1, 1)) && direction == RoomPrototypePanelDirection.Right)
			{
				nextState = GetZoomState(state.Slot);
				return true;
			}
		}

		if (state.Slot == RoomPrototypePanelSlot.BottomLeft)
		{
			int nextX = state.Viewport.X;
			int nextY = state.Viewport.Y;

			switch (direction)
			{
				case RoomPrototypePanelDirection.Left:
					nextX--;
					break;
				case RoomPrototypePanelDirection.Right:
					nextX++;
					break;
				case RoomPrototypePanelDirection.Up:
					nextY--;
					break;
				case RoomPrototypePanelDirection.Down:
					nextY++;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
			}

			if (nextX < 1 || nextX > 2 || nextY < 0 || nextY > 1)
			{
				return false;
			}

			RoomPrototypeViewport nextViewport = new RoomPrototypeViewport(nextX, nextY, 1, 1);
			nextState = new RoomPrototypePanelState(
				state.Slot,
				nextViewport,
				true,
				nextViewport.Equals(new RoomPrototypeViewport(1, 1, 1, 1)) ? "TRUCK" : "ROOM"
			);
			return true;
		}

		return false;
	}

	public static string FormatViewport(RoomPrototypeViewport viewport)
	{
		string start = FormatCell(viewport.X, viewport.Y);
		if (viewport.Width == 1 && viewport.Height == 1)
		{
			return start;
		}

		return $"{start}-{FormatCell(viewport.X + viewport.Width - 1, viewport.Y + viewport.Height - 1)}";
	}

	private static string FormatCell(int x, int y)
	{
		char row = (char)('A' + y);
		return $"{row}{x + 1}";
	}
}

public sealed class RoomPrototypeLevelOneController : MonoBehaviour
{
	public const string BuiltInFontResourceName = "LegacyRuntime.ttf";
	private const float PanelFrameThickness = 4f;

	[SerializeField] private Sprite backgroundSprite = null;
	[SerializeField] private Sprite keySprite = null;
	[SerializeField] private Sprite tableSprite = null;
	[SerializeField] private Sprite cageSprite = null;
	[SerializeField] private Vector2 referenceResolution = new Vector2(1674f, 942f);
	[SerializeField] private Vector2 boardSize = new Vector2(1674f, 942f);
	[SerializeField] private float panelGap = 8f;
	[SerializeField] private float animationDuration = 0.28f;
	[SerializeField] private Color frameColor = new Color(0.035f, 0.033f, 0.03f, 1f);
	[SerializeField] private Color panelTint = Color.white;
	[SerializeField] private Color controlColor = new Color(0.08f, 0.08f, 0.075f, 0.86f);
	[SerializeField] private AudioClip levelMusic = null;
	[Range(0f, 1f)] [SerializeField] private float levelMusicVolume = 0.45f;

	private readonly Dictionary<RoomPrototypePanelSlot, PanelView> panels = new Dictionary<RoomPrototypePanelSlot, PanelView>();
	private readonly List<RoomMarker> roomMarkers = new List<RoomMarker>();
	private Font interfaceFont;
	private Sprite circleSprite;
	private RectTransform canvasRoot;
	private Coroutine keyDropAnimation;
	private Coroutine truckMoveAnimation;
	private Coroutine appleDropAnimation;
	private bool keyIsFalling;
	private bool keyDeliveredToTruck;
	private bool keyDeliveredToCage;
	private bool appleIsFalling;
	private bool appleDelivered;
	private bool truckIsDriving;
	private bool truckIsTipping;
	private bool truckTipped;
	private bool truckMovedToNextCell;
	private bool truckReachedTable;

	private void Awake()
	{
		StartLevelMusic();
		BuildPrototype();
	}

	public static void ConfigureLevelMusic(AudioSource musicSource, AudioClip musicClip)
	{
		if (musicSource == null)
		{
			return;
		}

		musicSource.clip = musicClip;
		musicSource.loop = true;
		musicSource.spatialBlend = 0f;
		musicSource.playOnAwake = false;
	}

	private void StartLevelMusic()
	{
		if (levelMusic == null)
		{
			return;
		}

		AudioSource musicSource = GetComponent<AudioSource>();
		if (musicSource == null)
		{
			musicSource = gameObject.AddComponent<AudioSource>();
		}

		ConfigureLevelMusic(musicSource, levelMusic);
		musicSource.volume = Mathf.Clamp01(levelMusicVolume);
		musicSource.Play();
	}

	private void BuildPrototype()
	{
		interfaceFont = Resources.GetBuiltinResource<Font>(BuiltInFontResourceName);
		circleSprite = CreateCircleSprite();
		BuildMarkers();
		EnsureEventSystem();

		canvasRoot = CreateCanvas();
		RectTransform boardRoot = CreateRectTransform("Puzzle Board", canvasRoot);
		Vector2 squareBoardSize = CalculateSquareBoardSize(boardSize);
		boardRoot.anchorMin = new Vector2(0.5f, 0.5f);
		boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
		boardRoot.pivot = new Vector2(0.5f, 0.5f);
		boardRoot.sizeDelta = squareBoardSize;
		boardRoot.anchoredPosition = Vector2.zero;

		float panelSide = (squareBoardSize.x - panelGap) * 0.5f;
		Vector2 panelSize = new Vector2(panelSide, panelSide);
		CreatePanel(boardRoot, RoomPrototypePanelSlot.TopLeft, new Vector2(-(panelSize.x + panelGap) * 0.5f, (panelSize.y + panelGap) * 0.5f), panelSize);
		CreatePanel(boardRoot, RoomPrototypePanelSlot.TopRight, new Vector2((panelSize.x + panelGap) * 0.5f, (panelSize.y + panelGap) * 0.5f), panelSize);
		CreatePanel(boardRoot, RoomPrototypePanelSlot.BottomLeft, new Vector2(-(panelSize.x + panelGap) * 0.5f, -(panelSize.y + panelGap) * 0.5f), panelSize);
		CreatePanel(boardRoot, RoomPrototypePanelSlot.BottomRight, new Vector2((panelSize.x + panelGap) * 0.5f, -(panelSize.y + panelGap) * 0.5f), panelSize);
		CreateInternalDividers(boardRoot, squareBoardSize);
	}

	public static Vector2 CalculateSquareBoardSize(Vector2 configuredSize)
	{
		float side = Mathf.Max(0f, Mathf.Min(configuredSize.x, configuredSize.y));
		return new Vector2(side, side);
	}

	private RectTransform CreateCanvas()
	{
		GameObject canvasObject = new GameObject("Room Prototype Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasObject.transform.SetParent(transform, false);

		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 0;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = referenceResolution;
		scaler.matchWidthOrHeight = 0.5f;

		RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		return rectTransform;
	}

	private void CreatePanel(RectTransform boardRoot, RoomPrototypePanelSlot slot, Vector2 position, Vector2 size)
	{
		RectTransform root = CreateRectTransform($"{slot} Panel", boardRoot);
		root.sizeDelta = size;
		root.anchoredPosition = position;
		Image frame = root.gameObject.AddComponent<Image>();
		frame.color = frameColor;
		frame.raycastTarget = false;

		RectTransform content = CreateRectTransform("Viewport", root);
		content.anchorMin = Vector2.zero;
		content.anchorMax = Vector2.one;
		content.offsetMin = new Vector2(PanelFrameThickness, PanelFrameThickness);
		content.offsetMax = new Vector2(-PanelFrameThickness, -PanelFrameThickness);
		content.gameObject.AddComponent<RectMask2D>();

		Image background = CreateImage("Room Slice", content, Color.white);
		background.sprite = backgroundSprite;
		background.preserveAspect = false;
		background.color = backgroundSprite == null ? new Color(0.32f, 0.3f, 0.27f, 1f) : panelTint;
		RectTransform backgroundRect = background.rectTransform;
		backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
		backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
		backgroundRect.pivot = new Vector2(0.5f, 0.5f);

		PanelView panel = new PanelView
		{
			Slot = slot,
			Root = root,
			Content = content,
			Background = backgroundRect,
			State = RoomPrototypeLevelOnePanelModel.GetInitialState(slot)
		};

		foreach (RoomMarker marker in roomMarkers)
		{
			MarkerView markerView = CreateMarkerView(content, marker);
			panel.MarkerViews.Add(markerView);
			ConfigureMarkerInteraction(markerView);
		}

		Button tapButton = CreateButton("Tap Area", content, string.Empty, Color.clear, Color.clear, new Vector2(1f, 1f));
		RectTransform tapRect = tapButton.GetComponent<RectTransform>();
		tapRect.anchorMin = Vector2.zero;
		tapRect.anchorMax = Vector2.one;
		tapRect.offsetMin = Vector2.zero;
		tapRect.offsetMax = Vector2.zero;
		tapButton.onClick.AddListener(() => ZoomPanel(panel));
		panel.TapButton = tapButton;

		foreach (MarkerView markerView in panel.MarkerViews)
		{
			if (markerView.Button != null)
			{
				markerView.RectTransform.SetAsLastSibling();
			}
		}

		panels[slot] = panel;
		ApplyState(panel, panel.State, false);
	}

	private void CreateInternalDividers(RectTransform boardRoot, Vector2 squareBoardSize)
	{
		float dividerThickness = Mathf.Max(0f, panelGap) + PanelFrameThickness * 2f;
		CreateInternalDivider("Internal Vertical Divider", boardRoot, new Vector2(dividerThickness, squareBoardSize.y));
		CreateInternalDivider("Internal Horizontal Divider", boardRoot, new Vector2(squareBoardSize.x, dividerThickness));
	}

	private void CreateInternalDivider(string name, RectTransform boardRoot, Vector2 size)
	{
		Image divider = CreateImage(name, boardRoot, frameColor);
		divider.raycastTarget = false;
		divider.rectTransform.anchoredPosition = Vector2.zero;
		divider.rectTransform.sizeDelta = size;
		divider.rectTransform.SetAsLastSibling();
	}

	private MarkerView CreateMarkerView(RectTransform parent, RoomMarker marker)
	{
		Image image = CreateImage(marker.Label, parent, marker.Color);
		Sprite markerSprite = GetMarkerSprite(marker);
		bool usesMarkerSprite = markerSprite != null;
		image.sprite = usesMarkerSprite ? markerSprite : marker.Shape == MarkerShape.Circle ? circleSprite : null;
		image.preserveAspect = usesMarkerSprite;
		if (usesMarkerSprite)
		{
			image.color = Color.white;
		}
		else
		{
			Text label = CreateText($"{marker.Label} Label", image.rectTransform, marker.Label, 14, TextAnchor.MiddleCenter, Color.white);
			label.fontStyle = FontStyle.Bold;
			label.raycastTarget = false;
			RectTransform labelRect = label.rectTransform;
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.offsetMin = Vector2.zero;
			labelRect.offsetMax = Vector2.zero;
		}

		return new MarkerView
		{
			Marker = marker,
			RectTransform = image.rectTransform,
			Image = image
		};
	}

	private void ConfigureMarkerInteraction(MarkerView markerView)
	{
		if (markerView.Marker.Label != "KEY"
			&& markerView.Marker.Label != "ROPE"
			&& markerView.Marker.Label != "APPLE")
		{
			return;
		}

		Button button = markerView.Image.gameObject.AddComponent<Button>();
		button.targetGraphic = markerView.Image;
		if (markerView.Marker.Label == "KEY")
		{
			button.onClick.AddListener(TryDropKeyIntoTruck);
		}
		else if (markerView.Marker.Label == "ROPE")
		{
			button.onClick.AddListener(TryPullRopeToTable);
		}
		else
		{
			button.onClick.AddListener(TryDropAppleIntoTruck);
		}
		markerView.Button = button;
	}

	private void ZoomPanel(PanelView panel)
	{
		if (!RoomPrototypeLevelOnePanelModel.CanZoom(panel.State) || panel.Animation != null)
		{
			return;
		}

		ApplyState(panel, RoomPrototypeLevelOnePanelModel.GetZoomState(panel.Slot), true);
	}

	private void TryDropKeyIntoTruck()
	{
		if (!CanDropKeyIntoTruck())
		{
			return;
		}

		MarkerView keyMarker = FindMarkerView(RoomPrototypePanelSlot.TopLeft, "KEY");
		MarkerView truckMarker = FindMarkerView(RoomPrototypePanelSlot.BottomLeft, "TRUCK");
		if (keyMarker == null || truckMarker == null)
		{
			return;
		}

		PanelView keyPanel = panels[RoomPrototypePanelSlot.TopLeft];
		PanelView truckPanel = panels[RoomPrototypePanelSlot.BottomLeft];
		Vector2 startPosition = keyMarker.RectTransform.anchoredPosition;
		Vector2 targetPosition = truckMarker.RectTransform.anchoredPosition;
		Vector2 markerSize = keyMarker.RectTransform.rect.size;
		keyIsFalling = true;
		keyMarker.RectTransform.gameObject.SetActive(false);
		RefreshKeyInteraction();
		RefreshInteractionLock();
		keyDropAnimation = StartCoroutine(AnimateKeyDrop(keyMarker.Marker, keyPanel, truckPanel, startPosition, targetPosition, markerSize));
	}

	private bool CanDropKeyIntoTruck()
	{
		if (IsInteractionLocked() || keyDeliveredToTruck)
		{
			return false;
		}

		if (!panels.TryGetValue(RoomPrototypePanelSlot.TopLeft, out PanelView keyPanel)
			|| !panels.TryGetValue(RoomPrototypePanelSlot.BottomLeft, out PanelView truckPanel))
		{
			return false;
		}

		return keyPanel.Animation == null
			&& truckPanel.Animation == null
			&& RoomPrototypeLevelOnePanelModel.CanDropKeyIntoTruck(keyPanel.State, truckPanel.State);
	}

	private bool IsInteractionLocked()
	{
		return keyIsFalling || keyDropAnimation != null || truckMoveAnimation != null || appleIsFalling || appleDropAnimation != null;
	}

	private void RefreshKeyInteraction()
	{
		bool keyCanDrop = CanDropKeyIntoTruck();
		foreach (PanelView panel in panels.Values)
		{
			foreach (MarkerView markerView in panel.MarkerViews)
			{
				if (markerView.Marker.Label != "KEY" || markerView.Button == null)
				{
					continue;
				}

				bool isActiveKey = keyCanDrop
					&& panel.Slot == RoomPrototypePanelSlot.TopLeft
					&& markerView.RectTransform.gameObject.activeInHierarchy;
				markerView.Button.interactable = isActiveKey;
				markerView.Image.raycastTarget = isActiveKey;
			}
		}
	}

	private void RefreshRopeInteraction()
	{
		bool ropeCanPull = CanPullRopeToTable();
		foreach (PanelView panel in panels.Values)
		{
			foreach (MarkerView markerView in panel.MarkerViews)
			{
				if (markerView.Marker.Label != "ROPE" || markerView.Button == null)
				{
					continue;
				}

				bool isActiveRope = ropeCanPull
					&& panel.Slot == RoomPrototypePanelSlot.BottomLeft
					&& markerView.RectTransform.gameObject.activeInHierarchy;
				markerView.Button.interactable = isActiveRope;
				markerView.Image.raycastTarget = isActiveRope;
			}
		}
	}

	private void RefreshAppleInteraction()
	{
		bool appleCanDrop = CanDropAppleIntoTruck();
		foreach (PanelView panel in panels.Values)
		{
			foreach (MarkerView markerView in panel.MarkerViews)
			{
				if (markerView.Marker.Label != "APPLE" || markerView.Button == null)
				{
					continue;
				}

				bool isActiveApple = appleCanDrop
					&& panel.Slot == RoomPrototypePanelSlot.BottomRight
					&& markerView.RectTransform.gameObject.activeInHierarchy;
				markerView.Button.interactable = isActiveApple;
				markerView.Image.raycastTarget = isActiveApple;
			}
		}
	}

	private void RefreshInteractionLock()
	{
		bool isLocked = IsInteractionLocked();
		foreach (PanelView panel in panels.Values)
		{
			panel.TapButton.interactable = !isLocked && RoomPrototypeLevelOnePanelModel.CanZoom(panel.State);
			foreach (GameObject controlObject in panel.ControlObjects)
			{
				Button button = controlObject.GetComponent<Button>();
				if (button != null)
				{
					button.interactable = !isLocked;
				}
			}
		}
	}

	private MarkerView FindMarkerView(RoomPrototypePanelSlot slot, string label)
	{
		if (!panels.TryGetValue(slot, out PanelView panel))
		{
			return null;
		}

		foreach (MarkerView markerView in panel.MarkerViews)
		{
			if (markerView.Marker.Label == label)
			{
				return markerView;
			}
		}

		return null;
	}

	private void TryPullRopeToTable()
	{
		if (!CanPullRopeToTable())
		{
			return;
		}

		truckMoveAnimation = StartCoroutine(AnimateTruckToTable());
		RefreshInteractionLock();
		RefreshRopeInteraction();
	}

	private bool CanPullRopeToTable()
	{
		if (IsInteractionLocked() || !truckMovedToNextCell || truckReachedTable
			|| !panels.TryGetValue(RoomPrototypePanelSlot.BottomLeft, out PanelView truckPanel))
		{
			return false;
		}

		return truckPanel.Animation == null
			&& RoomPrototypeLevelOnePanelModel.TryMoveTruckToTable(truckPanel.State, out _);
	}

	private void TryDropAppleIntoTruck()
	{
		if (!CanDropAppleIntoTruck())
		{
			return;
		}

		MarkerView appleMarker = FindMarkerView(RoomPrototypePanelSlot.BottomRight, "APPLE");
		MarkerView truckMarker = FindMarkerView(RoomPrototypePanelSlot.BottomRight, "TRUCK");
		if (appleMarker == null || truckMarker == null)
		{
			return;
		}

		PanelView tablePanel = panels[RoomPrototypePanelSlot.BottomRight];
		appleMarker.RectTransform.gameObject.SetActive(false);
		truckMarker.RectTransform.gameObject.SetActive(false);
		appleIsFalling = true;
		RefreshAppleInteraction();
		appleDropAnimation = StartCoroutine(AnimateAppleDropAndLaunchKey(
			appleMarker.Marker,
			truckMarker.Marker,
			tablePanel,
			appleMarker.RectTransform.anchoredPosition,
			truckMarker.RectTransform.anchoredPosition,
			appleMarker.RectTransform.rect.size,
			truckMarker.RectTransform.rect.size
		));
		RefreshInteractionLock();
	}

	private bool CanDropAppleIntoTruck()
	{
		if (IsInteractionLocked() || !truckReachedTable || appleDelivered || keyDeliveredToCage
			|| !panels.TryGetValue(RoomPrototypePanelSlot.BottomRight, out PanelView tablePanel)
			|| !panels.TryGetValue(RoomPrototypePanelSlot.TopRight, out PanelView cagePanel))
		{
			return false;
		}

		return tablePanel.Animation == null
			&& cagePanel.Animation == null
			&& RoomPrototypeLevelOnePanelModel.CanDropAppleIntoTruck(
				tablePanel.State,
				RoomPrototypeLevelOnePanelModel.GetInitialState(RoomPrototypePanelSlot.BottomRight),
				cagePanel.State
			);
	}

	private IEnumerator AnimateAppleDropAndLaunchKey(
		RoomMarker appleMarker,
		RoomMarker truckMarker,
		PanelView tablePanel,
		Vector2 appleStart,
		Vector2 truckPosition,
		Vector2 appleSize,
		Vector2 truckSize
	)
	{
		Image fallingApple = CreateMovingMarker("Falling Apple", tablePanel.Content, appleMarker, appleSize, appleStart);
		RectTransform fallingAppleRect = fallingApple.rectTransform;
		const float appleDropDuration = 0.38f;
		float elapsed = 0f;
		while (elapsed < appleDropDuration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / appleDropDuration);
			float eased = progress * progress;
			fallingAppleRect.anchoredPosition = Vector2.Lerp(appleStart, truckPosition, eased);
			yield return null;
		}

		Destroy(fallingApple.gameObject);
		appleIsFalling = false;
		appleDelivered = true;
		truckIsTipping = true;
		Image tippedTruck = CreateMovingMarker("Tipped Truck", tablePanel.Content, truckMarker, truckSize, truckPosition);
		RectTransform tippedTruckRect = tippedTruck.rectTransform;
		const float tipDuration = 0.26f;
		elapsed = 0f;
		while (elapsed < tipDuration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / tipDuration);
			float eased = progress * progress * (3f - 2f * progress);
			tippedTruckRect.localRotation = Quaternion.Euler(0f, 0f, -22f * eased);
			yield return null;
		}

		truckIsTipping = false;
		truckTipped = true;
		MarkerView keyMarker = FindMarkerView(RoomPrototypePanelSlot.TopLeft, "KEY");
		MarkerView cageMarker = FindMarkerView(RoomPrototypePanelSlot.TopRight, "CAGE");
		if (keyMarker == null || cageMarker == null || !panels.TryGetValue(RoomPrototypePanelSlot.TopRight, out PanelView cagePanel))
		{
			appleDropAnimation = null;
			RefreshInteractionLock();
			yield break;
		}

		Vector2 keySize = keyMarker.RectTransform.rect.size;
		Vector2 keyExit = new Vector2(truckPosition.x, tablePanel.Content.rect.height * 0.5f + keySize.y);
		Image departingKey = CreateMovingMarker("Departing Key", tablePanel.Content, keyMarker.Marker, keySize, truckPosition);
		RectTransform departingKeyRect = departingKey.rectTransform;
		const float keyFlightDuration = 0.25f;
		elapsed = 0f;
		while (elapsed < keyFlightDuration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / keyFlightDuration);
			float eased = progress * progress;
			departingKeyRect.anchoredPosition = Vector2.Lerp(truckPosition, keyExit, eased);
			yield return null;
		}

		Destroy(departingKey.gameObject);
		float alignedEntryX = RoomPrototypeLevelOnePanelModel.GetAlignedHandoffX(
			truckPosition.x,
			tablePanel.Content.rect.width,
			cagePanel.Content.rect.width
		);
		Vector2 keyEntry = new Vector2(alignedEntryX, -cagePanel.Content.rect.height * 0.5f - keySize.y);
		Vector2 cagePosition = GetMarkerPositionInPanel(cageMarker.Marker, cagePanel);
		Image arrivingKey = CreateMovingMarker("Key To Cage", cagePanel.Content, keyMarker.Marker, keySize, keyEntry);
		RectTransform arrivingKeyRect = arrivingKey.rectTransform;
		elapsed = 0f;
		while (elapsed < keyFlightDuration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / keyFlightDuration);
			float eased = progress * progress;
			arrivingKeyRect.anchoredPosition = Vector2.Lerp(keyEntry, cagePosition, eased);
			yield return null;
		}

		Destroy(arrivingKey.gameObject);
		keyDeliveredToCage = true;
		ApplyViewport(tablePanel, ToFrame(tablePanel.State.Viewport));
		ApplyViewport(cagePanel, ToFrame(cagePanel.State.Viewport));
		appleDropAnimation = null;
		RefreshInteractionLock();
		RefreshAppleInteraction();
	}

	private IEnumerator AnimateKeyDrop(
		RoomMarker keyMarker,
		PanelView keyPanel,
		PanelView truckPanel,
		Vector2 startPosition,
		Vector2 targetPosition,
		Vector2 markerSize
	)
	{
		Image fallingKey = CreateMovingMarker("Falling Key", keyPanel.Content, keyMarker, markerSize, startPosition);
		RectTransform fallingKeyRect = fallingKey.rectTransform;
		Vector2 exitPosition = new Vector2(startPosition.x, -keyPanel.Content.rect.height * 0.5f - markerSize.y);
		const float duration = 0.24f;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / duration);
			float eased = progress * progress;
			fallingKeyRect.anchoredPosition = Vector2.Lerp(startPosition, exitPosition, eased);
			yield return null;
		}

		Destroy(fallingKey.gameObject);
		float alignedEntryX = RoomPrototypeLevelOnePanelModel.GetAlignedHandoffX(
			startPosition.x,
			keyPanel.Content.rect.width,
			truckPanel.Content.rect.width
		);
		Vector2 enterPosition = new Vector2(alignedEntryX, truckPanel.Content.rect.height * 0.5f + markerSize.y);
		Image arrivingKey = CreateMovingMarker("Arriving Key", truckPanel.Content, keyMarker, markerSize, enterPosition);
		RectTransform arrivingKeyRect = arrivingKey.rectTransform;
		elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / duration);
			float eased = progress * progress;
			arrivingKeyRect.anchoredPosition = Vector2.Lerp(enterPosition, targetPosition, eased);
			yield return null;
		}

		Destroy(arrivingKey.gameObject);
		keyIsFalling = false;
		keyDeliveredToTruck = true;
		keyDropAnimation = null;
		truckMoveAnimation = StartCoroutine(AnimateTruckToNextCell());
	}

	private IEnumerator AnimateTruckToNextCell()
	{
		if (!panels.TryGetValue(RoomPrototypePanelSlot.BottomLeft, out PanelView truckPanel)
			|| !RoomPrototypeLevelOnePanelModel.TryMoveTruckToNextCell(truckPanel.State, out _))
		{
			truckMoveAnimation = null;
			RefreshInteractionLock();
			yield break;
		}

		MarkerView truckMarker = FindMarkerView(RoomPrototypePanelSlot.BottomLeft, "TRUCK");
		if (truckMarker == null)
		{
			truckMoveAnimation = null;
			RefreshInteractionLock();
			yield break;
		}

		Vector2 startPosition = truckMarker.RectTransform.anchoredPosition;
		float travelDistance = Mathf.Max(80f, truckPanel.Content.rect.width * 0.7f);
		Vector2 targetPosition = startPosition + new Vector2(travelDistance, 0f);
		Vector2 markerSize = truckMarker.RectTransform.rect.size;
		truckIsDriving = true;
		truckMarker.RectTransform.gameObject.SetActive(false);

		Image movingTruck = CreateImage("Moving Truck", truckPanel.Content, truckMarker.Marker.Color);
		RectTransform movingTruckRect = movingTruck.rectTransform;
		movingTruckRect.anchorMin = new Vector2(0.5f, 0.5f);
		movingTruckRect.anchorMax = new Vector2(0.5f, 0.5f);
		movingTruckRect.pivot = new Vector2(0.5f, 0.5f);
		movingTruckRect.sizeDelta = markerSize;
		movingTruckRect.anchoredPosition = startPosition;
		movingTruckRect.SetAsLastSibling();

		Text label = CreateText("Text", movingTruckRect, truckMarker.Marker.Label, 14, TextAnchor.MiddleCenter, Color.white);
		label.fontStyle = FontStyle.Bold;
		label.raycastTarget = false;
		RectTransform labelRect = label.rectTransform;
		labelRect.anchorMin = Vector2.zero;
		labelRect.anchorMax = Vector2.one;
		labelRect.offsetMin = Vector2.zero;
		labelRect.offsetMax = Vector2.zero;

		const float duration = 0.5f;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / duration);
			float eased = progress * progress * (3f - 2f * progress);
			movingTruckRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, eased);
			yield return null;
		}

		Destroy(movingTruck.gameObject);
		truckIsDriving = false;
		truckMovedToNextCell = true;
		ApplyViewport(truckPanel, ToFrame(truckPanel.State.Viewport));

		truckMoveAnimation = null;
		RefreshInteractionLock();
		RefreshKeyInteraction();
		RefreshRopeInteraction();
	}

	private IEnumerator AnimateTruckToTable()
	{
		if (!panels.TryGetValue(RoomPrototypePanelSlot.BottomLeft, out PanelView truckPanel)
			|| !panels.TryGetValue(RoomPrototypePanelSlot.BottomRight, out PanelView tablePanel)
			|| !RoomPrototypeLevelOnePanelModel.TryMoveTruckToTable(truckPanel.State, out RoomPrototypePanelState tableState))
		{
			truckMoveAnimation = null;
			RefreshInteractionLock();
			yield break;
		}

		MarkerView truckMarker = FindMarkerView(RoomPrototypePanelSlot.BottomLeft, "TRUCK");
		if (truckMarker == null)
		{
			truckMoveAnimation = null;
			RefreshInteractionLock();
			yield break;
		}

		RoomMarker tableTruck = GetCurrentMarker(truckMarker.Marker).WithRoomPosition(new Vector2(3.44f, 1.63f)).WithDisplaySlot(RoomPrototypePanelSlot.BottomRight);
		Vector2 markerSize = truckMarker.RectTransform.rect.size;
		Vector2 sourceStart = truckMarker.RectTransform.anchoredPosition;
		Vector2 sourceExit = sourceStart + new Vector2(truckPanel.Content.rect.width, 0f);
		Vector2 targetPosition = GetMarkerPositionInPanel(tableTruck, tablePanel);
		Vector2 targetEnter = new Vector2(-tablePanel.Content.rect.width * 0.5f - markerSize.x, targetPosition.y);
		truckIsDriving = true;
		truckMarker.RectTransform.gameObject.SetActive(false);

		Image departingTruck = CreateMovingMarker("Departing Truck", truckPanel.Content, truckMarker.Marker, markerSize, sourceStart);
		Image arrivingTruck = CreateMovingMarker("Arriving Truck", tablePanel.Content, truckMarker.Marker, markerSize, targetEnter);
		RectTransform departingTruckRect = departingTruck.rectTransform;
		RectTransform arrivingTruckRect = arrivingTruck.rectTransform;
		const float duration = 0.5f;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / duration);
			float eased = progress * progress * (3f - 2f * progress);
			departingTruckRect.anchoredPosition = Vector2.Lerp(sourceStart, sourceExit, eased);
			arrivingTruckRect.anchoredPosition = Vector2.Lerp(targetEnter, targetPosition, eased);
			yield return null;
		}

		Destroy(departingTruck.gameObject);
		Destroy(arrivingTruck.gameObject);
		truckIsDriving = false;
		truckReachedTable = true;
		ApplyState(tablePanel, tableState, false);

		truckMoveAnimation = null;
		RefreshInteractionLock();
		RefreshRopeInteraction();
		RefreshAppleInteraction();
	}

	private void ApplyState(PanelView panel, RoomPrototypePanelState state, bool animated)
	{
		if (panel.Animation != null)
		{
			StopCoroutine(panel.Animation);
			panel.Animation = null;
		}

		ClearControls(panel);

		if (!animated)
		{
			panel.State = state;
			ApplyViewport(panel, ToFrame(state.Viewport));
			RefreshControls(panel);
			RefreshKeyInteraction();
			RefreshRopeInteraction();
			RefreshAppleInteraction();
			return;
		}

		panel.Animation = StartCoroutine(AnimateToState(panel, state));
	}

	private IEnumerator AnimateToState(PanelView panel, RoomPrototypePanelState targetState)
	{
		Vector4 start = ToFrame(panel.State.Viewport);
		Vector4 target = ToFrame(targetState.Viewport);
		float elapsed = 0f;

		while (elapsed < animationDuration)
		{
			elapsed += Time.deltaTime;
			float normalized = animationDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / animationDuration);
			float eased = normalized * normalized * (3f - 2f * normalized);
			ApplyViewport(panel, Vector4.Lerp(start, target, eased));
			yield return null;
		}

		panel.State = targetState;
		ApplyViewport(panel, target);
		panel.Animation = null;
		RefreshControls(panel);
		RefreshKeyInteraction();
		RefreshRopeInteraction();
		RefreshAppleInteraction();
	}

	private void ApplyViewport(PanelView panel, Vector4 viewport)
	{
		Vector2 panelSize = panel.Content.rect.size;
		if (panelSize.x <= 0f || panelSize.y <= 0f)
		{
			panelSize = panel.Content.sizeDelta;
		}

		float width = Mathf.Max(0.01f, viewport.z);
		float height = Mathf.Max(0.01f, viewport.w);
		Vector2 imageSize = new Vector2(
			panelSize.x * RoomPrototypeLevelOnePanelModel.RoomColumns / width,
			panelSize.y * RoomPrototypeLevelOnePanelModel.RoomRows / height
		);

		float centerX = (viewport.x + width * 0.5f) / RoomPrototypeLevelOnePanelModel.RoomColumns;
		float centerY = (viewport.y + height * 0.5f) / RoomPrototypeLevelOnePanelModel.RoomRows;
		Vector2 offset = new Vector2((centerX - 0.5f) * imageSize.x, (0.5f - centerY) * imageSize.y);

		panel.Background.sizeDelta = imageSize;
		panel.Background.anchoredPosition = -offset;
		UpdateMarkerViews(panel, viewport, panelSize);
	}

	private void UpdateMarkerViews(PanelView panel, Vector4 viewport, Vector2 panelSize)
	{
		foreach (MarkerView markerView in panel.MarkerViews)
		{
			RoomMarker marker = GetCurrentMarker(markerView.Marker);
			bool visible = (marker.Label != "KEY" || (!keyIsFalling && !keyDeliveredToTruck))
				&& (marker.Label != "CAGE KEY" || keyDeliveredToCage)
				&& (marker.Label != "APPLE" || (!appleIsFalling && !appleDelivered))
				&& (marker.Label != "TRUCK" || (!truckIsDriving && !truckIsTipping && !truckTipped))
				&& (marker.Label != "ROPE" || (truckMovedToNextCell && !truckReachedTable))
				&& (!marker.DisplaySlot.HasValue || marker.DisplaySlot.Value == panel.Slot)
				&& MarkerIntersectsViewport(marker, viewport);
			markerView.RectTransform.gameObject.SetActive(visible);
			if (!visible)
			{
				continue;
			}

			Vector2 normalized = new Vector2(
				(marker.RoomPosition.x - viewport.x) / viewport.z,
				1f - (marker.RoomPosition.y - viewport.y) / viewport.w
			);

			markerView.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			markerView.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			markerView.RectTransform.pivot = new Vector2(0.5f, 0.5f);
			markerView.RectTransform.sizeDelta = new Vector2(
				panelSize.x * marker.RoomSize.x / viewport.z,
				panelSize.y * marker.RoomSize.y / viewport.w
			);
			markerView.RectTransform.anchoredPosition = new Vector2(
				(normalized.x - 0.5f) * panelSize.x,
				(normalized.y - 0.5f) * panelSize.y
			);
		}
	}

	private static bool MarkerIntersectsViewport(RoomMarker marker, Vector4 viewport)
	{
		float markerMinX = marker.RoomPosition.x - marker.RoomSize.x * 0.5f;
		float markerMaxX = marker.RoomPosition.x + marker.RoomSize.x * 0.5f;
		float markerMinY = marker.RoomPosition.y - marker.RoomSize.y * 0.5f;
		float markerMaxY = marker.RoomPosition.y + marker.RoomSize.y * 0.5f;

		return markerMaxX >= viewport.x
			&& markerMinX <= viewport.x + viewport.z
			&& markerMaxY >= viewport.y
			&& markerMinY <= viewport.y + viewport.w;
	}

	private void RefreshControls(PanelView panel)
	{
		panel.TapButton.interactable = !IsInteractionLocked() && RoomPrototypeLevelOnePanelModel.CanZoom(panel.State);

		if (panel.State.IsZoomed)
		{
			Button minus = CreateButton("Zoom Out", panel.Root, "-", controlColor, Color.white, new Vector2(46f, 46f));
			PlaceControl(minus.transform as RectTransform, RoomPrototypePanelDirection.Left, true, panel.Root.sizeDelta);
			minus.onClick.AddListener(() => ApplyState(panel, RoomPrototypeLevelOnePanelModel.GetInitialState(panel.Slot), true));
			panel.ControlObjects.Add(minus.gameObject);
		}

		foreach (RoomPrototypePanelDirection direction in Enum.GetValues(typeof(RoomPrototypePanelDirection)))
		{
			RoomPrototypePanelDirection capturedDirection = direction;
			if (!RoomPrototypeLevelOnePanelModel.TryNavigate(panel.State, capturedDirection, out _))
			{
				continue;
			}

			Button arrow = CreateButton($"{capturedDirection} Arrow", panel.Root, GetArrowText(capturedDirection), controlColor, Color.white, new Vector2(46f, 46f));
			PlaceControl(arrow.transform as RectTransform, capturedDirection, false, panel.Root.sizeDelta);
			arrow.onClick.AddListener(() =>
			{
				if (RoomPrototypeLevelOnePanelModel.TryNavigate(panel.State, capturedDirection, out RoomPrototypePanelState nextState))
				{
					ApplyState(panel, nextState, true);
				}
			});
			panel.ControlObjects.Add(arrow.gameObject);
		}

		RefreshInteractionLock();
	}

	private void ClearControls(PanelView panel)
	{
		for (int i = 0; i < panel.ControlObjects.Count; i++)
		{
			Destroy(panel.ControlObjects[i]);
		}

		panel.ControlObjects.Clear();
	}

	private static void PlaceControl(RectTransform rectTransform, RoomPrototypePanelDirection direction, bool forceBottomLeft, Vector2 panelSize)
	{
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);

		if (forceBottomLeft)
		{
			rectTransform.anchoredPosition = new Vector2(-panelSize.x * 0.5f + 34f, -panelSize.y * 0.5f + 34f);
			return;
		}

		switch (direction)
		{
			case RoomPrototypePanelDirection.Left:
				rectTransform.anchoredPosition = new Vector2(-panelSize.x * 0.5f + 34f, 0f);
				break;
			case RoomPrototypePanelDirection.Right:
				rectTransform.anchoredPosition = new Vector2(panelSize.x * 0.5f - 34f, 0f);
				break;
			case RoomPrototypePanelDirection.Up:
				rectTransform.anchoredPosition = new Vector2(0f, panelSize.y * 0.5f - 34f);
				break;
			case RoomPrototypePanelDirection.Down:
				rectTransform.anchoredPosition = new Vector2(0f, -panelSize.y * 0.5f + 34f);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
		}
	}

	private static Vector4 ToFrame(RoomPrototypeViewport viewport)
	{
		return new Vector4(viewport.X, viewport.Y, viewport.Width, viewport.Height);
	}

	private void BuildMarkers()
	{
		roomMarkers.Clear();
		roomMarkers.Add(new RoomMarker("KEY", MarkerShape.Rectangle, new Vector2(0.39f, 0.62f), new Vector2(0.12f, 0.24f), new Color(0.96f, 0.78f, 0.2f, 0.92f), RoomPrototypePanelSlot.TopLeft));
		roomMarkers.Add(new RoomMarker("TRUCK", MarkerShape.Rectangle, new Vector2(1.44f, 1.63f), new Vector2(0.48f, 0.22f), new Color(0.1f, 0.38f, 0.78f, 0.9f), RoomPrototypePanelSlot.BottomLeft));
		roomMarkers.Add(new RoomMarker("ROPE", MarkerShape.Rectangle, new Vector2(2.76f, 1.29f), new Vector2(0.1f, 0.42f), new Color(0.26f, 0.16f, 0.09f, 0.85f), RoomPrototypePanelSlot.BottomLeft));
		roomMarkers.Add(new RoomMarker("APPLE", MarkerShape.Circle, new Vector2(3.39f, 1.14f), new Vector2(0.16f, 0.16f), new Color(0.82f, 0.08f, 0.08f, 0.94f), RoomPrototypePanelSlot.BottomRight));
		roomMarkers.Add(new RoomMarker("TABLE", MarkerShape.Rectangle, new Vector2(3.32f, 1.35f), new Vector2(0.54f, 0.91f), Color.white));
		roomMarkers.Add(new RoomMarker("CAGE", MarkerShape.Rectangle, new Vector2(3.32f, 0.55f), new Vector2(0.42f, 0.83f), Color.white));
		roomMarkers.Add(new RoomMarker("CAGE KEY", MarkerShape.Rectangle, new Vector2(3.32f, 0.58f), new Vector2(0.1f, 0.2f), new Color(0.96f, 0.78f, 0.2f, 0.92f), RoomPrototypePanelSlot.TopRight));
	}

	private static string GetArrowText(RoomPrototypePanelDirection direction)
	{
		switch (direction)
		{
			case RoomPrototypePanelDirection.Left:
				return "<";
			case RoomPrototypePanelDirection.Right:
				return ">";
			case RoomPrototypePanelDirection.Up:
				return "^";
			case RoomPrototypePanelDirection.Down:
				return "v";
			default:
				throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
		}
	}

	private void EnsureEventSystem()
	{
		EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
		if (eventSystem == null)
		{
			GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
			eventSystemObject.transform.SetParent(transform, false);
			eventSystem = eventSystemObject.GetComponent<EventSystem>();
		}

		InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
		if (inputModule == null)
		{
			inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
		}

		inputModule.enabled = true;
		inputModule.AssignDefaultActions();

		BaseInputModule[] inputModules = eventSystem.GetComponents<BaseInputModule>();
		foreach (BaseInputModule module in inputModules)
		{
			if (module != inputModule)
			{
				module.enabled = false;
			}
		}
	}

	private RoomMarker GetCurrentMarker(RoomMarker marker)
	{
		if (marker.Label != "TRUCK")
		{
			return marker;
		}

		if (truckReachedTable)
		{
			return marker
				.WithRoomPosition(new Vector2(3.44f, 1.63f))
				.WithDisplaySlot(RoomPrototypePanelSlot.BottomRight);
		}

		if (truckMovedToNextCell)
		{
			return marker.WithRoomPosition(new Vector2(2.44f, 1.63f));
		}

		return marker;
	}

	private Vector2 GetMarkerPositionInPanel(RoomMarker marker, PanelView panel)
	{
		Vector2 panelSize = panel.Content.rect.size;
		if (panelSize.x <= 0f || panelSize.y <= 0f)
		{
			panelSize = panel.Content.sizeDelta;
		}

		Vector4 viewport = ToFrame(panel.State.Viewport);
		Vector2 normalized = new Vector2(
			(marker.RoomPosition.x - viewport.x) / viewport.z,
			1f - (marker.RoomPosition.y - viewport.y) / viewport.w
		);
		return new Vector2(
			(normalized.x - 0.5f) * panelSize.x,
			(normalized.y - 0.5f) * panelSize.y
		);
	}

	private Image CreateMovingMarker(string name, RectTransform parent, RoomMarker marker, Vector2 size, Vector2 position)
	{
		Image image = CreateImage(name, parent, marker.Color);
		bool usesKeySprite = IsKeyMarker(marker) && keySprite != null;
		image.sprite = usesKeySprite ? keySprite : marker.Shape == MarkerShape.Circle ? circleSprite : null;
		image.preserveAspect = usesKeySprite;
		if (usesKeySprite)
		{
			image.color = Color.white;
		}
		RectTransform rectTransform = image.rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = size;
		rectTransform.anchoredPosition = position;
		rectTransform.SetAsLastSibling();

		if (!usesKeySprite)
		{
			Text label = CreateText("Text", rectTransform, marker.Label, 14, TextAnchor.MiddleCenter, Color.white);
			label.fontStyle = FontStyle.Bold;
			label.raycastTarget = false;
			RectTransform labelRect = label.rectTransform;
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.offsetMin = Vector2.zero;
			labelRect.offsetMax = Vector2.zero;
		}
		return image;
	}

	private static bool IsKeyMarker(RoomMarker marker)
	{
		return marker.Label == "KEY" || marker.Label == "CAGE KEY";
	}

	private Sprite GetMarkerSprite(RoomMarker marker)
	{
		if (IsKeyMarker(marker))
		{
			return keySprite;
		}

		switch (marker.Label)
		{
			case "TABLE":
				return tableSprite;
			case "CAGE":
				return cageSprite;
			default:
				return null;
		}
	}

	private Button CreateButton(string name, RectTransform parent, string text, Color background, Color foreground, Vector2 size)
	{
		Image image = CreateImage(name, parent, background);
		RectTransform rectTransform = image.rectTransform;
		rectTransform.sizeDelta = size;
		Button button = image.gameObject.AddComponent<Button>();
		image.raycastTarget = true;
		button.targetGraphic = image;
		ColorBlock colors = button.colors;
		colors.normalColor = background;
		colors.highlightedColor = new Color(background.r + 0.08f, background.g + 0.08f, background.b + 0.08f, background.a);
		colors.pressedColor = new Color(background.r * 0.7f, background.g * 0.7f, background.b * 0.7f, background.a);
		colors.selectedColor = colors.highlightedColor;
		button.colors = colors;

		if (!string.IsNullOrEmpty(text))
		{
			Text label = CreateText("Text", rectTransform, text, 30, TextAnchor.MiddleCenter, foreground);
			label.fontStyle = FontStyle.Bold;
			label.raycastTarget = false;
			RectTransform labelRect = label.rectTransform;
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.offsetMin = Vector2.zero;
			labelRect.offsetMax = Vector2.zero;
		}

		return button;
	}

	private Image CreateImage(string name, RectTransform parent, Color color)
	{
		RectTransform rectTransform = CreateRectTransform(name, parent);
		Image image = rectTransform.gameObject.AddComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		return image;
	}

	private Text CreateText(string name, RectTransform parent, string text, int fontSize, TextAnchor alignment, Color color)
	{
		RectTransform rectTransform = CreateRectTransform(name, parent);
		Text label = rectTransform.gameObject.AddComponent<Text>();
		label.text = text;
		label.font = interfaceFont;
		label.fontSize = fontSize;
		label.alignment = alignment;
		label.color = color;
		label.horizontalOverflow = HorizontalWrapMode.Overflow;
		label.verticalOverflow = VerticalWrapMode.Overflow;
		return label;
	}

	private static RectTransform CreateRectTransform(string name, Transform parent)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform));
		RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
		rectTransform.SetParent(parent, false);
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.localScale = Vector3.one;
		rectTransform.localRotation = Quaternion.identity;
		return rectTransform;
	}

	private static Sprite CreateCircleSprite()
	{
		const int size = 32;
		Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
		texture.name = "RoomPrototypeCircle";
		texture.wrapMode = TextureWrapMode.Clamp;

		Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
		float radius = (size - 2) * 0.5f;
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float distance = Vector2.Distance(new Vector2(x, y), center);
				texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
			}
		}

		texture.Apply();
		return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
	}

	private enum MarkerShape
	{
		Rectangle,
		Circle
	}

	private readonly struct RoomMarker
	{
		public readonly string Label;
		public readonly MarkerShape Shape;
		public readonly Vector2 RoomPosition;
		public readonly Vector2 RoomSize;
		public readonly Color Color;
		public readonly RoomPrototypePanelSlot? DisplaySlot;

		public RoomMarker(
			string label,
			MarkerShape shape,
			Vector2 roomPosition,
			Vector2 roomSize,
			Color color,
			RoomPrototypePanelSlot? displaySlot = null
		)
		{
			Label = label;
			Shape = shape;
			RoomPosition = roomPosition;
			RoomSize = roomSize;
			Color = color;
			DisplaySlot = displaySlot;
		}

		public RoomMarker WithRoomPosition(Vector2 roomPosition)
		{
			return new RoomMarker(Label, Shape, roomPosition, RoomSize, Color, DisplaySlot);
		}

		public RoomMarker WithDisplaySlot(RoomPrototypePanelSlot? displaySlot)
		{
			return new RoomMarker(Label, Shape, RoomPosition, RoomSize, Color, displaySlot);
		}
	}

	private sealed class MarkerView
	{
		public RoomMarker Marker;
		public RectTransform RectTransform;
		public Image Image;
		public Button Button;
	}

	private sealed class PanelView
	{
		public RoomPrototypePanelSlot Slot;
		public RectTransform Root;
		public RectTransform Content;
		public RectTransform Background;
		public Button TapButton;
		public RoomPrototypePanelState State;
		public Coroutine Animation;
		public readonly List<MarkerView> MarkerViews = new List<MarkerView>();
		public readonly List<GameObject> ControlObjects = new List<GameObject>();
	}
}
