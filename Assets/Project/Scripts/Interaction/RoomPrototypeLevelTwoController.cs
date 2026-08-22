using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RoomPrototypeLevelTwoSlot
{
	public const int TopLeft = 0;
	public const int TopRight = 1;
	public const int BottomLeft = 2;
	public const int BottomRight = 3;
}

public static class RoomPrototypeLevelTwoLayoutModel
{
	public static bool AreOrthogonallyAdjacent(int firstSlot, int secondSlot)
	{
		int firstX = firstSlot % 2;
		int firstY = firstSlot / 2;
		int secondX = secondSlot % 2;
		int secondY = secondSlot / 2;
		return Mathf.Abs(firstX - secondX) + Mathf.Abs(firstY - secondY) == 1;
	}

	public static int ChooseDisplacementSlot(int occupiedSlot, int draggedOriginSlot, int emptySlot)
	{
		if (AreOrthogonallyAdjacent(occupiedSlot, draggedOriginSlot))
		{
			return draggedOriginSlot;
		}

		if (AreOrthogonallyAdjacent(occupiedSlot, emptySlot))
		{
			return emptySlot;
		}

		throw new ArgumentException("An occupied slot must have an orthogonally adjacent free slot.");
	}
}

public static class RoomPrototypeLevelTwoWorldProjection
{
	public static Vector2 GetPanelAnchoredPosition(Vector2 worldPosition, Vector2 panelContentSize, Rect viewport)
	{
		float normalizedX = (worldPosition.x - viewport.xMin) / viewport.width;
		float normalizedY = (worldPosition.y - viewport.yMin) / viewport.height;
		return new Vector2(
			(normalizedX - 0.5f) * panelContentSize.x,
			(0.5f - normalizedY) * panelContentSize.y
		);
	}

	public static Vector2 GetPanelSize(Vector2 worldSize, Vector2 panelContentSize, Rect viewport)
	{
		return new Vector2(
			worldSize.x / viewport.width * panelContentSize.x,
			worldSize.y / viewport.height * panelContentSize.y
		);
	}

	public static bool IsVisibleInViewport(Vector2 worldPosition, Rect viewport)
	{
		return worldPosition.x >= viewport.xMin
			&& worldPosition.x <= viewport.xMax
			&& worldPosition.y >= viewport.yMin
			&& worldPosition.y <= viewport.yMax;
	}

	public static Vector2 GetWorldPosition(Vector2 normalizedViewportPosition, Rect viewport)
	{
		return new Vector2(
			viewport.xMin + normalizedViewportPosition.x * viewport.width,
			viewport.yMin + normalizedViewportPosition.y * viewport.height
		);
	}
}

public static class RoomPrototypeLevelTwoClockPuzzleModel
{
	public static bool ArePortraitAndCageVerticallyAligned(int portraitSlot, int cageSlot)
	{
		return (portraitSlot == RoomPrototypeLevelTwoSlot.TopLeft && cageSlot == RoomPrototypeLevelTwoSlot.BottomLeft)
			|| (portraitSlot == RoomPrototypeLevelTwoSlot.TopRight && cageSlot == RoomPrototypeLevelTwoSlot.BottomRight);
	}

	public static float GetAcceleratedFallProgress(float normalizedTime)
	{
		float clampedTime = Mathf.Clamp01(normalizedTime);
		return clampedTime * clampedTime;
	}

	public static Vector2 GetStraightDropTarget(Vector2 startPosition, Vector2 tablePosition)
	{
		return new Vector2(startPosition.x, tablePosition.y);
	}
}

public sealed class RoomPrototypeLevelTwoController : MonoBehaviour
{
	private const int RoomColumns = 4;
	private const int RoomRows = 2;
	private const string BuiltInFontResourceName = "LegacyRuntime.ttf";

	[SerializeField] private Sprite backgroundSprite = null;
	[SerializeField] private Vector2 referenceResolution = new Vector2(1674f, 942f);
	[SerializeField] private Vector2 boardSize = new Vector2(1674f, 942f);
	[SerializeField] private float panelGap = 8f;
	[SerializeField] private float animationDuration = 0.28f;
	[SerializeField] private float dragStartDistance = 14f;
	[SerializeField] private Color frameColor = new Color(0.035f, 0.033f, 0.03f, 1f);
	[SerializeField] private Vector2 sharedTruckWorldPosition = new Vector2(1.75f, 1.5f);
	[SerializeField] private Vector2 sharedTruckWorldSize = new Vector2(0.5f, 0.22f);
	[SerializeField] private Color sharedTruckPlaceholderColor = new Color(0.08f, 0.42f, 0.92f, 0.92f);
	[SerializeField] private float sharedTruckMoveDuration = 0.8f;
	[SerializeField] private Sprite portraitHand1 = null;
	[SerializeField] private Sprite portraitHand2 = null;
	[SerializeField] private Sprite portraitHand3 = null;
	[SerializeField] private Vector2 portraitWorldPosition = new Vector2(2.53f, 0.47f);
	[SerializeField] private Vector2 portraitWorldSize = new Vector2(0.68f, 0.9f);
	[SerializeField] private Vector2 clockWorldPosition = new Vector2(0.73f, 0.43f);
	[SerializeField] private Vector2 clockWorldSize = new Vector2(0.42f, 0.66f);
	[SerializeField] private Vector2 releasedKeyStartWorldPosition = new Vector2(2.66f, 0.67f);
	[SerializeField] private Color releasedKeyPlaceholderColor = new Color(0.95f, 0.76f, 0.16f, 0.96f);
	[SerializeField] private Vector2 releasedKeyWorldSize = new Vector2(0.24f, 0.08f);
	[SerializeField] private float releasedKeyFallDuration = 1.5f;
	[SerializeField] private Vector2 releasedKeyOverlaySize = new Vector2(78f, 22f);
	[SerializeField] private float releasedKeyLandingContentY = 0.18f;

	private readonly List<PanelView> panels = new List<PanelView>();
	private Font interfaceFont;
	private RectTransform boardRoot;
	private Vector2 panelSize;
	private bool interactionLocked;
	private PanelView pressedPanel;
	private Vector2 pointerDownScreenPosition;
	private bool isDragging;
	private SharedWorldObject sharedTruck;
	private Coroutine sharedTruckAnimation;
	private Coroutine releasedKeyAnimation;
	private RectTransform releasedKeyOverlay;
	private SharedWorldObject landedKey;
	private int portraitStageIndex;
	private int clockPressCount;
	private bool keyReleased;
	private bool keyLanded;

	private void Start()
	{
		ResolvePortraitSpritesForEditor();
		interfaceFont = Resources.GetBuiltinResource<Font>(BuiltInFontResourceName);
		EnsureEventSystem();

		RectTransform canvasRoot = CreateCanvas();
		boardRoot = CreateRectTransform("Puzzle Board", canvasRoot);
		Vector2 squareBoardSize = CalculateSquareBoardSize(boardSize);
		boardRoot.sizeDelta = squareBoardSize;
		boardRoot.anchoredPosition = Vector2.zero;

		float side = (squareBoardSize.x - panelGap) * 0.5f;
		panelSize = new Vector2(side, side);
		sharedTruck = new SharedWorldObject(sharedTruckWorldPosition, sharedTruckWorldSize);
		landedKey = new SharedWorldObject(Vector2.zero, releasedKeyWorldSize);

		CreatePanel(0, RoomPrototypeLevelTwoSlot.TopRight, 0, 0);
		CreatePanel(1, RoomPrototypeLevelTwoSlot.TopLeft, 2, 0);
		CreatePanel(2, RoomPrototypeLevelTwoSlot.BottomLeft, 1, 0);
		CreateReleasedKeyOverlay();
		RefreshClockPuzzleState();
	}

	private void ResolvePortraitSpritesForEditor()
	{
#if UNITY_EDITOR
		if (portraitHand1 == null)
		{
			portraitHand1 = LoadPortraitSprite("Assets/Project/ART/потретруки1.png");
		}
		if (portraitHand2 == null)
		{
			portraitHand2 = LoadPortraitSprite("Assets/Project/ART/потретруки2.png");
		}
		if (portraitHand3 == null)
		{
			portraitHand3 = LoadPortraitSprite("Assets/Project/ART/потретруки3.png");
		}
#endif
	}

#if UNITY_EDITOR
	private static Sprite LoadPortraitSprite(string assetPath)
	{
		UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
		for (int i = 0; i < assets.Length; i++)
		{
			if (assets[i] is Sprite sprite)
			{
				return sprite;
			}
		}
		return null;
	}
#endif

	public void OnPanelPointerDown(int panelId, PointerEventData eventData)
	{
		if (interactionLocked || isDragging)
		{
			return;
		}

		pressedPanel = FindPanel(panelId);
		if (pressedPanel == null)
		{
			return;
		}

		pointerDownScreenPosition = eventData.position;
	}

	public void OnPanelDrag(int panelId, PointerEventData eventData)
	{
		if (interactionLocked || pressedPanel == null || pressedPanel.Id != panelId)
		{
			return;
		}

		if (!isDragging && (eventData.position - pointerDownScreenPosition).sqrMagnitude >= dragStartDistance * dragStartDistance)
		{
			isDragging = true;
			pressedPanel.Root.SetAsLastSibling();
			pressedPanel.Root.localScale = Vector3.one * 1.045f;
			pressedPanel.CanvasGroup.alpha = 0.94f;
		}

		if (!isDragging)
		{
			return;
		}

		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, eventData.position, null, out Vector2 localPosition))
		{
			pressedPanel.Root.anchoredPosition = localPosition;
		}
	}

	public void OnPanelPointerUp(int panelId, PointerEventData eventData)
	{
		if (pressedPanel == null || pressedPanel.Id != panelId)
		{
			return;
		}

		FinishPointer(eventData.position);
	}

	public void OnPanelEndDrag(int panelId, PointerEventData eventData)
	{
		if (pressedPanel != null && pressedPanel.Id == panelId)
		{
			FinishPointer(eventData.position);
		}
	}

	private void FinishPointer(Vector2 screenPosition)
	{
		PanelView panel = pressedPanel;
		pressedPanel = null;

		if (!isDragging)
		{
			if (TryPressClock(panel, screenPosition))
			{
				return;
			}

			ToggleZoom(panel);
			return;
		}

		isDragging = false;
		panel.Root.localScale = Vector3.one;
		panel.CanvasGroup.alpha = 1f;
		int targetSlot = GetClosestSlot(screenPosition);
		MovePanelToSlot(panel, targetSlot);
	}

	private void ToggleZoom(PanelView panel)
	{
		if (interactionLocked || panel.Animation != null)
		{
			return;
		}

		Viewport target = panel.IsZoomed
			? new Viewport(panel.RegionX, panel.RegionY, 2, 2)
			: new Viewport(panel.RegionX, panel.RegionY, 1, 1);
		panel.IsZoomed = !panel.IsZoomed;
		ClearControls(panel);
		panel.Animation = StartCoroutine(AnimateViewport(panel, target));
	}

	private void Navigate(PanelView panel, Direction direction)
	{
		if (interactionLocked || !panel.IsZoomed || panel.Animation != null)
		{
			return;
		}

		float x = panel.Viewport.X;
		float y = panel.Viewport.Y;
		switch (direction)
		{
			case Direction.Left: x--; break;
			case Direction.Right: x++; break;
			case Direction.Up: y--; break;
			case Direction.Down: y++; break;
		}

		if (x < panel.RegionX || x > panel.RegionX + 1 || y < panel.RegionY || y > panel.RegionY + 1)
		{
			return;
		}

		ClearControls(panel);
		panel.Animation = StartCoroutine(AnimateViewport(panel, new Viewport(x, y, 1, 1)));
	}

	private void MovePanelToSlot(PanelView draggedPanel, int targetSlot)
	{
		int originSlot = draggedPanel.Slot;
		if (targetSlot == originSlot)
		{
			StartCoroutine(AnimatePanelsToSlots(new[] { draggedPanel }));
			return;
		}

		PanelView occupiedPanel = FindPanelInSlot(targetSlot);
		if (occupiedPanel == null)
		{
			draggedPanel.Slot = targetSlot;
			StartCoroutine(AnimatePanelsToSlots(new[] { draggedPanel }));
			return;
		}

		int permanentEmptySlot = FindEmptySlot();
		int displacementSlot = RoomPrototypeLevelTwoLayoutModel.ChooseDisplacementSlot(
			targetSlot,
			originSlot,
			permanentEmptySlot
		);
		draggedPanel.Slot = targetSlot;
		occupiedPanel.Slot = displacementSlot;
		StartCoroutine(AnimatePanelsToSlots(new[] { draggedPanel, occupiedPanel }));
	}

	private IEnumerator AnimatePanelsToSlots(IReadOnlyList<PanelView> movingPanels)
	{
		interactionLocked = true;
		Vector2[] starts = new Vector2[movingPanels.Count];
		Vector2[] targets = new Vector2[movingPanels.Count];
		for (int i = 0; i < movingPanels.Count; i++)
		{
			starts[i] = movingPanels[i].Root.anchoredPosition;
			targets[i] = GetSlotPosition(movingPanels[i].Slot);
		}

		float elapsed = 0f;
		while (elapsed < animationDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animationDuration));
			for (int i = 0; i < movingPanels.Count; i++)
			{
				movingPanels[i].Root.anchoredPosition = Vector2.LerpUnclamped(starts[i], targets[i], t);
			}
			yield return null;
		}

		for (int i = 0; i < movingPanels.Count; i++)
		{
			movingPanels[i].Root.anchoredPosition = targets[i];
		}

		interactionLocked = false;
		RefreshClockPuzzleState();
	}

	private IEnumerator AnimateViewport(PanelView panel, Viewport target)
	{
		interactionLocked = true;
		Viewport start = panel.Viewport;
		float elapsed = 0f;
		while (elapsed < animationDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animationDuration));
			ApplyViewport(panel, Viewport.Lerp(start, target, t));
			yield return null;
		}

		panel.Viewport = target;
		ApplyViewport(panel, target);
		panel.Animation = null;
		interactionLocked = false;
		RefreshControls(panel);
		RefreshClockPuzzleState();
	}

	private void CreatePanel(int id, int slot, int regionX, int regionY)
	{
		RectTransform root = CreateRectTransform($"Panel {id}", boardRoot);
		root.sizeDelta = panelSize;
		root.anchoredPosition = GetSlotPosition(slot);
		Image frame = root.gameObject.AddComponent<Image>();
		frame.color = frameColor;
		frame.raycastTarget = true;

		CanvasGroup canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
		RectTransform content = CreateRectTransform("Content", root);
		content.anchorMin = Vector2.zero;
		content.anchorMax = Vector2.one;
		content.offsetMin = new Vector2(4f, 4f);
		content.offsetMax = new Vector2(-4f, -4f);
		content.gameObject.AddComponent<RectMask2D>();

		Image roomSlice = CreateImage("Room Slice", content, Color.white);
		roomSlice.sprite = backgroundSprite;
		roomSlice.preserveAspect = false;
		RectTransform background = roomSlice.rectTransform;
		background.anchorMin = new Vector2(0.5f, 0.5f);
		background.anchorMax = new Vector2(0.5f, 0.5f);
		background.pivot = new Vector2(0.5f, 0.5f);
		roomSlice.raycastTarget = false;

		PanelView panel = new PanelView
		{
			Id = id,
			Slot = slot,
			RegionX = regionX,
			RegionY = regionY,
			Root = root,
			Content = content,
			Background = background,
			CanvasGroup = canvasGroup,
			Viewport = new Viewport(regionX, regionY, 2, 2)
		};
		panels.Add(panel);

		RoomPrototypeLevelTwoPanelDrag dragHandler = root.gameObject.AddComponent<RoomPrototypeLevelTwoPanelDrag>();
		dragHandler.Initialize(this, id);
		ApplyViewport(panel, panel.Viewport);
		CreateSharedTruckView(panel);
		CreatePortraitView(panel);
		CreateLandedKeyView(panel);
		UpdateWorldViews(panel, panel.Viewport);
	}

	private void ApplyViewport(PanelView panel, Viewport viewport)
	{
		Vector2 contentSize = panel.Content.rect.size;
		if (contentSize.x <= 0f || contentSize.y <= 0f)
		{
			contentSize = panel.Content.sizeDelta;
		}

		Vector2 imageSize = new Vector2(
			contentSize.x * RoomColumns / viewport.Width,
			contentSize.y * RoomRows / viewport.Height
		);
		float centerX = (viewport.X + viewport.Width * 0.5f) / RoomColumns;
		float centerY = (viewport.Y + viewport.Height * 0.5f) / RoomRows;
		Vector2 offset = new Vector2((centerX - 0.5f) * imageSize.x, (0.5f - centerY) * imageSize.y);
		panel.Background.sizeDelta = imageSize;
		panel.Background.anchoredPosition = -offset;
		UpdateWorldViews(panel, viewport);
	}

	private void UpdateWorldViews(PanelView panel, Viewport viewport)
	{
		UpdateSharedTruckView(panel, viewport);
		UpdatePortraitView(panel, viewport);
		UpdateLandedKeyView(panel, viewport);
	}

	private void CreateSharedTruckView(PanelView panel)
	{
		Image placeholder = CreateImage("Shared Truck Placeholder", panel.Content, sharedTruckPlaceholderColor);
		placeholder.raycastTarget = true;
		Button button = placeholder.gameObject.AddComponent<Button>();
		button.targetGraphic = placeholder;
		button.onClick.AddListener(RunSharedTruckDemo);
		panel.SharedTruckPlaceholder = placeholder.rectTransform;
	}

	private void UpdateSharedTruckView(PanelView panel, Viewport viewport)
	{
		if (sharedTruck == null || panel.SharedTruckPlaceholder == null)
		{
			return;
		}

		Vector2 contentSize = panel.Content.rect.size;
		if (contentSize.x <= 0f || contentSize.y <= 0f)
		{
			contentSize = panel.Content.sizeDelta;
		}

		Rect visibleWorldRect = new Rect(viewport.X, viewport.Y, viewport.Width, viewport.Height);
		panel.SharedTruckPlaceholder.gameObject.SetActive(
			RoomPrototypeLevelTwoWorldProjection.IsVisibleInViewport(sharedTruck.Position, visibleWorldRect)
		);
		panel.SharedTruckPlaceholder.sizeDelta = RoomPrototypeLevelTwoWorldProjection.GetPanelSize(
			sharedTruck.Size,
			contentSize,
			visibleWorldRect
		);
		panel.SharedTruckPlaceholder.anchoredPosition = RoomPrototypeLevelTwoWorldProjection.GetPanelAnchoredPosition(
			sharedTruck.Position,
			contentSize,
			visibleWorldRect
		);
	}

	private void UpdateSharedTruckViews()
	{
		for (int i = 0; i < panels.Count; i++)
		{
			UpdateSharedTruckView(panels[i], panels[i].Viewport);
		}
	}

	private void CreatePortraitView(PanelView panel)
	{
		Image portrait = CreateImage("Portrait Stage", panel.Content, Color.white);
		portrait.preserveAspect = true;
		panel.PortraitStage = portrait;
	}

	private void UpdatePortraitView(PanelView panel, Viewport viewport)
	{
		if (panel.PortraitStage == null)
		{
			return;
		}

		Sprite portraitSprite = GetCurrentPortraitSprite();
		Rect visibleWorldRect = new Rect(viewport.X, viewport.Y, viewport.Width, viewport.Height);
		panel.PortraitStage.sprite = portraitSprite;
		panel.PortraitStage.gameObject.SetActive(
			portraitSprite != null
			&& RoomPrototypeLevelTwoWorldProjection.IsVisibleInViewport(portraitWorldPosition, visibleWorldRect)
		);
		if (!panel.PortraitStage.gameObject.activeSelf)
		{
			return;
		}

		Vector2 contentSize = GetContentSize(panel);
		panel.PortraitStage.rectTransform.sizeDelta = RoomPrototypeLevelTwoWorldProjection.GetPanelSize(
			portraitWorldSize,
			contentSize,
			visibleWorldRect
		);
		panel.PortraitStage.rectTransform.anchoredPosition = RoomPrototypeLevelTwoWorldProjection.GetPanelAnchoredPosition(
			portraitWorldPosition,
			contentSize,
			visibleWorldRect
		);
	}

	private Sprite GetCurrentPortraitSprite()
	{
		switch (portraitStageIndex)
		{
			case 0: return portraitHand1;
			case 1: return portraitHand2;
			default: return portraitHand3;
		}
	}

	private void CreateReleasedKeyOverlay()
	{
		Image keyOverlay = CreateImage("Released Key Screen Overlay", boardRoot, releasedKeyPlaceholderColor);
		keyOverlay.rectTransform.sizeDelta = releasedKeyOverlaySize;
		keyOverlay.rectTransform.SetAsLastSibling();
		keyOverlay.gameObject.SetActive(false);
		releasedKeyOverlay = keyOverlay.rectTransform;
	}

	private void CreateLandedKeyView(PanelView panel)
	{
		Image keyView = CreateImage("Landed Key", panel.Content, releasedKeyPlaceholderColor);
		panel.LandedKey = keyView.rectTransform;
	}

	private void UpdateLandedKeyView(PanelView panel, Viewport viewport)
	{
		if (landedKey == null || panel.LandedKey == null)
		{
			return;
		}

		Rect visibleWorldRect = new Rect(viewport.X, viewport.Y, viewport.Width, viewport.Height);
		bool shouldShow = keyLanded && RoomPrototypeLevelTwoWorldProjection.IsVisibleInViewport(landedKey.Position, visibleWorldRect);
		panel.LandedKey.gameObject.SetActive(shouldShow);
		if (!shouldShow)
		{
			return;
		}

		Vector2 contentSize = GetContentSize(panel);
		panel.LandedKey.sizeDelta = RoomPrototypeLevelTwoWorldProjection.GetPanelSize(landedKey.Size, contentSize, visibleWorldRect);
		panel.LandedKey.anchoredPosition = RoomPrototypeLevelTwoWorldProjection.GetPanelAnchoredPosition(
			landedKey.Position,
			contentSize,
			visibleWorldRect
		);
		panel.LandedKey.localEulerAngles = Vector3.zero;
	}

	private Vector2 GetContentSize(PanelView panel)
	{
		Vector2 contentSize = panel.Content.rect.size;
		return contentSize.x <= 0f || contentSize.y <= 0f ? panel.Content.sizeDelta : contentSize;
	}

	private void RefreshClockPuzzleState()
	{
		bool puzzleReady = IsClockPuzzleReady();
		if (!puzzleReady && !keyReleased && clockPressCount > 0)
		{
			clockPressCount = 0;
			portraitStageIndex = 0;
			RefreshPortraitViews();
		}
	}

	private bool IsClockPuzzleReady()
	{
		PanelView clockPanel = FindPanel(0);
		PanelView portraitPanel = FindPanel(2);
		PanelView cagePanel = FindPanel(1);
		return IsZoomedAt(clockPanel, 0f, 0f)
			&& IsZoomedAt(portraitPanel, 2f, 0f)
			&& IsZoomedAt(cagePanel, 3f, 1f)
			&& RoomPrototypeLevelTwoClockPuzzleModel.ArePortraitAndCageVerticallyAligned(portraitPanel.Slot, cagePanel.Slot);
	}

	private static bool IsZoomedAt(PanelView panel, float worldX, float worldY)
	{
		return panel != null
			&& panel.IsZoomed
			&& Mathf.Approximately(panel.Viewport.X, worldX)
			&& Mathf.Approximately(panel.Viewport.Y, worldY)
			&& Mathf.Approximately(panel.Viewport.Width, 1f)
			&& Mathf.Approximately(panel.Viewport.Height, 1f);
	}

	private void OnClockPressed()
	{
		if (interactionLocked || keyReleased || !IsClockPuzzleReady())
		{
			return;
		}

		clockPressCount++;
		portraitStageIndex = Mathf.Min(clockPressCount, 2);
		RefreshPortraitViews();
		if (clockPressCount == 2)
		{
			keyReleased = true;
			releasedKeyAnimation = StartCoroutine(AnimateReleasedKey());
		}
	}

	private bool TryPressClock(PanelView panel, Vector2 screenPosition)
	{
		if (panel == null || panel.Id != 0 || !IsClockPuzzleReady())
		{
			return false;
		}

		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panel.Content, screenPosition, null, out Vector2 localPosition))
		{
			return false;
		}

		Vector2 contentSize = GetContentSize(panel);
		Vector2 normalizedPosition = new Vector2(
			localPosition.x / contentSize.x + 0.5f,
			0.5f - localPosition.y / contentSize.y
		);
		Vector2 worldPosition = new Vector2(
			panel.Viewport.X + normalizedPosition.x * panel.Viewport.Width,
			panel.Viewport.Y + normalizedPosition.y * panel.Viewport.Height
		);
		bool isInsideClock = Mathf.Abs(worldPosition.x - clockWorldPosition.x) <= clockWorldSize.x * 0.5f
			&& Mathf.Abs(worldPosition.y - clockWorldPosition.y) <= clockWorldSize.y * 0.5f;
		if (isInsideClock)
		{
			OnClockPressed();
		}
		return isInsideClock;
	}

	private void RefreshPortraitViews()
	{
		for (int i = 0; i < panels.Count; i++)
		{
			UpdatePortraitView(panels[i], panels[i].Viewport);
		}
	}

	private IEnumerator AnimateReleasedKey()
	{
		PanelView portraitPanel = FindPanel(2);
		PanelView cagePanel = FindPanel(1);
		if (releasedKeyOverlay == null || portraitPanel == null || cagePanel == null)
		{
			yield break;
		}

		interactionLocked = true;
		Vector2 startPosition = GetBoardPositionForWorld(portraitPanel, releasedKeyStartWorldPosition);
		Vector2 tablePosition = GetBoardPositionForPanelContent(cagePanel, new Vector2(0.5f, releasedKeyLandingContentY));
		Vector2 targetPosition = RoomPrototypeLevelTwoClockPuzzleModel.GetStraightDropTarget(startPosition, tablePosition);
		releasedKeyOverlay.SetAsLastSibling();
		releasedKeyOverlay.gameObject.SetActive(true);
		releasedKeyOverlay.anchoredPosition = startPosition;
		releasedKeyOverlay.localEulerAngles = new Vector3(0f, 0f, -82f);
		float elapsed = 0f;
		while (elapsed < releasedKeyFallDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			float normalizedTime = Mathf.Clamp01(elapsed / releasedKeyFallDuration);
			float fallProgress = RoomPrototypeLevelTwoClockPuzzleModel.GetAcceleratedFallProgress(normalizedTime);
			releasedKeyOverlay.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, fallProgress);
			releasedKeyOverlay.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-82f, 0f, normalizedTime));
			yield return null;
		}

		releasedKeyOverlay.anchoredPosition = targetPosition;
		releasedKeyOverlay.localEulerAngles = Vector3.zero;
		landedKey.Position = GetWorldPositionForBoardPosition(cagePanel, targetPosition);
		keyLanded = true;
		RefreshLandedKeyViews();
		releasedKeyOverlay.gameObject.SetActive(false);
		releasedKeyAnimation = null;
		interactionLocked = false;
	}

	private Vector2 GetBoardPositionForWorld(PanelView panel, Vector2 worldPosition)
	{
		Vector2 localPanelPosition = RoomPrototypeLevelTwoWorldProjection.GetPanelAnchoredPosition(
			worldPosition,
			GetContentSize(panel),
			new Rect(panel.Viewport.X, panel.Viewport.Y, panel.Viewport.Width, panel.Viewport.Height)
		);
		return boardRoot.InverseTransformPoint(panel.Content.TransformPoint(localPanelPosition));
	}

	private Vector2 GetBoardPositionForPanelContent(PanelView panel, Vector2 normalizedTopLeftPosition)
	{
		Vector2 contentSize = GetContentSize(panel);
		Vector2 localPanelPosition = new Vector2(
			(normalizedTopLeftPosition.x - 0.5f) * contentSize.x,
			(0.5f - normalizedTopLeftPosition.y) * contentSize.y
		);
		return boardRoot.InverseTransformPoint(panel.Content.TransformPoint(localPanelPosition));
	}

	private Vector2 GetWorldPositionForBoardPosition(PanelView panel, Vector2 boardPosition)
	{
		Vector2 contentSize = GetContentSize(panel);
		Vector2 contentLocalPosition = panel.Content.InverseTransformPoint(boardRoot.TransformPoint(boardPosition));
		Vector2 normalizedPosition = new Vector2(
			contentLocalPosition.x / contentSize.x + 0.5f,
			0.5f - contentLocalPosition.y / contentSize.y
		);
		return RoomPrototypeLevelTwoWorldProjection.GetWorldPosition(
			normalizedPosition,
			new Rect(panel.Viewport.X, panel.Viewport.Y, panel.Viewport.Width, panel.Viewport.Height)
		);
	}

	private void RefreshLandedKeyViews()
	{
		for (int i = 0; i < panels.Count; i++)
		{
			UpdateLandedKeyView(panels[i], panels[i].Viewport);
		}
	}

	private void RunSharedTruckDemo()
	{
		if (interactionLocked || sharedTruckAnimation != null)
		{
			return;
		}

		Vector2 targetPosition = sharedTruck.Position.x < 2.5f
			? new Vector2(2.75f, sharedTruck.Position.y)
			: new Vector2(1.75f, sharedTruck.Position.y);
		sharedTruckAnimation = StartCoroutine(AnimateSharedTruck(targetPosition));
	}

	private IEnumerator AnimateSharedTruck(Vector2 targetPosition)
	{
		interactionLocked = true;
		Vector2 startPosition = sharedTruck.Position;
		float elapsed = 0f;
		while (elapsed < sharedTruckMoveDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / sharedTruckMoveDuration));
			sharedTruck.Position = Vector2.LerpUnclamped(startPosition, targetPosition, t);
			UpdateSharedTruckViews();
			yield return null;
		}

		sharedTruck.Position = targetPosition;
		UpdateSharedTruckViews();
		sharedTruckAnimation = null;
		interactionLocked = false;
	}

	private void RefreshControls(PanelView panel)
	{
		ClearControls(panel);
		if (!panel.IsZoomed)
		{
			return;
		}

		TryCreateArrow(panel, Direction.Left, panel.Viewport.X > panel.RegionX);
		TryCreateArrow(panel, Direction.Right, panel.Viewport.X < panel.RegionX + 1);
		TryCreateArrow(panel, Direction.Up, panel.Viewport.Y > panel.RegionY);
		TryCreateArrow(panel, Direction.Down, panel.Viewport.Y < panel.RegionY + 1);
	}

	private void TryCreateArrow(PanelView panel, Direction direction, bool allowed)
	{
		if (!allowed)
		{
			return;
		}

		Image image = CreateImage($"{direction} Arrow", panel.Root, new Color(0.08f, 0.08f, 0.075f, 0.86f));
		image.raycastTarget = true;
		RectTransform rect = image.rectTransform;
		rect.sizeDelta = new Vector2(46f, 46f);
		PlaceArrow(rect, direction);
		Button button = image.gameObject.AddComponent<Button>();
		button.targetGraphic = image;
		button.onClick.AddListener(() => Navigate(panel, direction));
		Text label = CreateText("Label", rect, GetArrowText(direction), 30, Color.white);
		label.raycastTarget = false;
		label.rectTransform.anchorMin = Vector2.zero;
		label.rectTransform.anchorMax = Vector2.one;
		label.rectTransform.offsetMin = Vector2.zero;
		label.rectTransform.offsetMax = Vector2.zero;
		panel.Controls.Add(image.gameObject);
	}

	private void ClearControls(PanelView panel)
	{
		for (int i = 0; i < panel.Controls.Count; i++)
		{
			Destroy(panel.Controls[i]);
		}
		panel.Controls.Clear();
	}

	private int GetClosestSlot(Vector2 screenPosition)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPosition, null, out Vector2 localPosition);
		int closestSlot = RoomPrototypeLevelTwoSlot.TopLeft;
		float closestDistance = float.MaxValue;
		for (int slot = RoomPrototypeLevelTwoSlot.TopLeft; slot <= RoomPrototypeLevelTwoSlot.BottomRight; slot++)
		{
			float distance = (GetSlotPosition(slot) - localPosition).sqrMagnitude;
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestSlot = slot;
			}
		}
		return closestSlot;
	}

	private Vector2 GetSlotPosition(int slot)
	{
		float halfStep = (panelSize.x + panelGap) * 0.5f;
		switch (slot)
		{
			case RoomPrototypeLevelTwoSlot.TopLeft: return new Vector2(-halfStep, halfStep);
			case RoomPrototypeLevelTwoSlot.TopRight: return new Vector2(halfStep, halfStep);
			case RoomPrototypeLevelTwoSlot.BottomLeft: return new Vector2(-halfStep, -halfStep);
			case RoomPrototypeLevelTwoSlot.BottomRight: return new Vector2(halfStep, -halfStep);
			default: throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
		}
	}

	private int FindEmptySlot()
	{
		for (int slot = RoomPrototypeLevelTwoSlot.TopLeft; slot <= RoomPrototypeLevelTwoSlot.BottomRight; slot++)
		{
			if (FindPanelInSlot(slot) == null)
			{
				return slot;
			}
		}
		throw new InvalidOperationException("The level two board must have one free slot.");
	}

	private PanelView FindPanelInSlot(int slot)
	{
		for (int i = 0; i < panels.Count; i++)
		{
			if (panels[i].Slot == slot)
			{
				return panels[i];
			}
		}
		return null;
	}

	private PanelView FindPanel(int id)
	{
		for (int i = 0; i < panels.Count; i++)
		{
			if (panels[i].Id == id)
			{
				return panels[i];
			}
		}
		return null;
	}

	private RectTransform CreateCanvas()
	{
		GameObject canvasObject = new GameObject("Room Prototype Level Two Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasObject.transform.SetParent(transform, false);
		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = referenceResolution;
		scaler.matchWidthOrHeight = 0.5f;
		RectTransform rect = canvasObject.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		return rect;
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
		inputModule.AssignDefaultActions();
	}

	private static Vector2 CalculateSquareBoardSize(Vector2 configuredSize)
	{
		float side = Mathf.Max(0f, Mathf.Min(configuredSize.x, configuredSize.y));
		return new Vector2(side, side);
	}

	private static RectTransform CreateRectTransform(string name, Transform parent)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform));
		RectTransform rect = gameObject.GetComponent<RectTransform>();
		rect.SetParent(parent, false);
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		return rect;
	}

	private static Image CreateImage(string name, RectTransform parent, Color color)
	{
		RectTransform rect = CreateRectTransform(name, parent);
		Image image = rect.gameObject.AddComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		return image;
	}

	private Text CreateText(string name, RectTransform parent, string text, int fontSize, Color color)
	{
		RectTransform rect = CreateRectTransform(name, parent);
		Text label = rect.gameObject.AddComponent<Text>();
		label.font = interfaceFont;
		label.text = text;
		label.fontSize = fontSize;
		label.alignment = TextAnchor.MiddleCenter;
		label.color = color;
		return label;
	}

	private void PlaceArrow(RectTransform rect, Direction direction)
	{
		float edge = panelSize.x * 0.5f - 34f;
		switch (direction)
		{
			case Direction.Left: rect.anchoredPosition = new Vector2(-edge, 0f); break;
			case Direction.Right: rect.anchoredPosition = new Vector2(edge, 0f); break;
			case Direction.Up: rect.anchoredPosition = new Vector2(0f, edge); break;
			case Direction.Down: rect.anchoredPosition = new Vector2(0f, -edge); break;
		}
	}

	private static string GetArrowText(Direction direction)
	{
		switch (direction)
		{
			case Direction.Left: return "<";
			case Direction.Right: return ">";
			case Direction.Up: return "^";
			case Direction.Down: return "v";
			default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
		}
	}

	private enum Direction
	{
		Left,
		Right,
		Up,
		Down
	}

	private readonly struct Viewport
	{
		public readonly float X;
		public readonly float Y;
		public readonly float Width;
		public readonly float Height;

		public Viewport(float x, float y, float width, float height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public static Viewport Lerp(Viewport from, Viewport to, float t)
		{
			return new Viewport(
				Mathf.Lerp(from.X, to.X, t),
				Mathf.Lerp(from.Y, to.Y, t),
				Mathf.Lerp(from.Width, to.Width, t),
				Mathf.Lerp(from.Height, to.Height, t)
			);
		}
	}

	private sealed class PanelView
	{
		public int Id;
		public int Slot;
		public int RegionX;
		public int RegionY;
		public bool IsZoomed;
		public RectTransform Root;
		public RectTransform Content;
		public RectTransform Background;
		public RectTransform SharedTruckPlaceholder;
		public Image PortraitStage;
		public RectTransform LandedKey;
		public CanvasGroup CanvasGroup;
		public Viewport Viewport;
		public Coroutine Animation;
		public readonly List<GameObject> Controls = new List<GameObject>();
	}

	private sealed class SharedWorldObject
	{
		public Vector2 Position;
		public readonly Vector2 Size;

		public SharedWorldObject(Vector2 position, Vector2 size)
		{
			Position = position;
			Size = size;
		}
	}
}
