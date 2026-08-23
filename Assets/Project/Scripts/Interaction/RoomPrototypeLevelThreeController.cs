using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public enum RoomPrototypeLevelThreeWindowState
{
	Room,
	Window,
	Sky
}

public static class RoomPrototypeLevelThreePuzzleModel
{
	public static RoomPrototypeLevelThreeWindowState AdvanceWindow(RoomPrototypeLevelThreeWindowState state)
	{
		switch (state)
		{
			case RoomPrototypeLevelThreeWindowState.Room:
				return RoomPrototypeLevelThreeWindowState.Window;
			case RoomPrototypeLevelThreeWindowState.Window:
				return RoomPrototypeLevelThreeWindowState.Sky;
			default:
				return RoomPrototypeLevelThreeWindowState.Sky;
		}
	}

	public static bool CanDropKey(
		RoomPrototypeLevelThreeWindowState windowState,
		int windowSlot,
		bool flowerZoomed,
		int flowerSlot,
		bool keyAlreadyCaught)
	{
		return windowState == RoomPrototypeLevelThreeWindowState.Sky
			&& flowerZoomed
			&& !keyAlreadyCaught
			&& IsDirectlyAbove(windowSlot, flowerSlot);
	}

	public static bool CanGrowFlower(bool keyCaught, int flowerSlot, bool cageZoomed, int cageSlot)
	{
		return keyCaught && cageZoomed && IsDirectlyAbove(cageSlot, flowerSlot);
	}

	public static float GetFlowerPullProgress(float startY, float currentY, float requiredDistance)
	{
		if (requiredDistance <= 0f)
		{
			return 0f;
		}
		return Mathf.Clamp01(Mathf.Max(0f, currentY - startY) / requiredDistance);
	}

	public static bool IsFlowerPullComplete(float progress, float completionThreshold)
	{
		return progress >= Mathf.Clamp01(completionThreshold);
	}

	private static bool IsDirectlyAbove(int upperSlot, int lowerSlot)
	{
		return (upperSlot == RoomPrototypeLevelTwoSlot.TopLeft && lowerSlot == RoomPrototypeLevelTwoSlot.BottomLeft)
			|| (upperSlot == RoomPrototypeLevelTwoSlot.TopRight && lowerSlot == RoomPrototypeLevelTwoSlot.BottomRight);
	}
}

[ExecuteAlways]
public sealed class RoomPrototypeLevelThreeController : MonoBehaviour
{
	private const int RoomColumns = 4;
	private const int RoomRows = 2;
	private const string BuiltInFontResourceName = "LegacyRuntime.ttf";
	private const string EditorPreviewRootName = "Level 03 Editor Preview";

	[SerializeField] private Sprite roomSprite = null;
	[SerializeField] private Sprite skySprite = null;
	[SerializeField] private Sprite keySprite = null;
	[Header("Level 03 Room Object Layout")]
	[SerializeField] private Sprite tableSprite = null;
	[SerializeField] private Sprite birdSprite = null;
	[SerializeField] private Sprite cageSprite = null;
	[SerializeField] private Vector2 tableWorldPosition = new Vector2(3.42f, 1.5f);
	[SerializeField] private Vector2 tableWorldSize = new Vector2(0.55f, 0.95f);
	[SerializeField] private Vector2 birdWorldPosition = new Vector2(3.38f, 0.71f);
	[SerializeField] private Vector2 birdWorldSize = new Vector2(17.35f, 0.61f);
	[SerializeField] private Vector2 cageWorldPosition = new Vector2(3.42f, 0.7f);
	[SerializeField] private Vector2 cageWorldSize = new Vector2(0.9f, 0.9f);
	[Header("Level 03 Interactive Object Layout")]
	[SerializeField] private Vector2 flowerWorldPosition = new Vector2(1.55f, 0.42f);
	[SerializeField] private Vector2 flowerWorldSize = new Vector2(0.34f, 0.24f);
	[SerializeField] private Vector2 caughtKeyWorldPosition = new Vector2(1.63f, 0.34f);
	[SerializeField] private float caughtKeyWorldHeight = 0.28f;
	[SerializeField] private AudioClip levelMusic = null;
	[Range(0f, 1f)] [SerializeField] private float levelMusicVolume = 0.45f;
	[Header("Prototype SFX")]
	[SerializeField] private AudioClip interactionClickSound = null;
	[SerializeField] private AudioClip zoomSound = null;
	[Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
	[SerializeField] private Vector2 referenceResolution = new Vector2(1674f, 942f);
	[SerializeField] private Vector2 boardSize = new Vector2(1674f, 942f);
	[SerializeField] private float panelGap = 8f;
	[SerializeField] private float animationDuration = 0.3f;
	[SerializeField] private float dragStartDistance = 14f;
	[SerializeField] private float keyFallDuration = 1.25f;
	[SerializeField] private float flowerGrowDuration = 1.1f;
	[SerializeField, Range(0.5f, 1f)] private float flowerPullCompletionThreshold = 0.82f;
	[SerializeField] private float flowerReturnDuration = 0.28f;
	[SerializeField] private Color frameColor = new Color(0.035f, 0.033f, 0.03f, 1f);
	[SerializeField] private Color flowerColor = new Color(0.36f, 0.78f, 0.26f, 0.95f);
	[SerializeField] private Color keyColor = new Color(0.96f, 0.75f, 0.13f, 0.98f);

	private readonly List<PanelView> panels = new List<PanelView>();
	private Font interfaceFont;
	private RectTransform boardRoot;
	private Vector2 panelSize;
	private PanelView pressedPanel;
	private Vector2 pointerDownScreenPosition;
	private bool isDragging;
	private bool interactionLocked;
	private bool keyDropStarted;
	private bool keyCaught;
	private bool flowerPullActive;
	private bool flowerPullAnimating;
	private bool cageOpened;
	private RectTransform keyOverlay;
	private RectTransform growthOverlay;
	private RectTransform flowerHeadOverlay;
	private Vector2 flowerPullStart;
	private Vector2 flowerPullTarget;
	private float flowerPointerStartY;
	private float flowerPullProgress;
	private AudioSource sfxSource;

	private void Awake()
	{
		if (Application.isPlaying)
		{
			ClearEditorPreview();
			StartLevelMusic();
			return;
		}

		BuildEditorPreview();
	}

	private void Start()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		BuildPrototype(transform);
	}

	#if UNITY_EDITOR
	private bool editorPreviewRefreshQueued;

	private void OnEnable()
	{
		if (!Application.isPlaying)
		{
			QueueEditorPreviewRefresh();
		}
	}

	private void OnValidate()
	{
		if (!Application.isPlaying)
		{
			QueueEditorPreviewRefresh();
		}
	}

	private void QueueEditorPreviewRefresh()
	{
		if (editorPreviewRefreshQueued || !gameObject.scene.IsValid())
		{
			return;
		}

		editorPreviewRefreshQueued = true;
		UnityEditor.EditorApplication.delayCall += RefreshEditorPreview;
	}

	private void RefreshEditorPreview()
	{
		editorPreviewRefreshQueued = false;
		if (this == null || Application.isPlaying || !isActiveAndEnabled)
		{
			return;
		}

		BuildEditorPreview();
		UnityEditor.SceneView.RepaintAll();
		UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
	}
	#endif

	private void BuildEditorPreview()
	{
		ClearEditorPreview();
		GameObject previewRoot = new GameObject(EditorPreviewRootName, typeof(RectTransform));
		previewRoot.transform.SetParent(transform, false);
		previewRoot.hideFlags = HideFlags.DontSaveInEditor;
		BuildPrototype(previewRoot.transform);
	}

	private void ClearEditorPreview()
	{
		Transform previewRoot = transform.Find(EditorPreviewRootName);
		if (previewRoot != null)
		{
			if (Application.isPlaying)
			{
				Destroy(previewRoot.gameObject);
			}
			else
			{
				DestroyImmediate(previewRoot.gameObject);
			}
		}

		panels.Clear();
		boardRoot = null;
		keyOverlay = null;
		growthOverlay = null;
		flowerHeadOverlay = null;
	}

	private void BuildPrototype(Transform parent)
	{
		interfaceFont = Resources.GetBuiltinResource<Font>(BuiltInFontResourceName);
		if (Application.isPlaying)
		{
			EnsureEventSystem(parent);
		}

		RectTransform canvasRoot = CreateCanvas(parent);
		boardRoot = CreateRectTransform("Level Three Board", canvasRoot);
		float boardSide = Mathf.Max(0f, Mathf.Min(boardSize.x, boardSize.y));
		boardRoot.sizeDelta = new Vector2(boardSide, boardSide);
		float side = (boardSide - panelGap) * 0.5f;
		panelSize = new Vector2(side, side);

		CreatePanel(0, PanelRole.Cage, RoomPrototypeLevelTwoSlot.TopLeft, 2, 0);
		CreatePanel(1, PanelRole.Flower, RoomPrototypeLevelTwoSlot.TopRight, 0, 0);
		CreatePanel(2, PanelRole.Window, RoomPrototypeLevelTwoSlot.BottomLeft, 2, 0);
		CreateTransitionOverlays();
		RefreshAllVisuals();
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

		RoomPrototypeLoopingMusic.ConfigureAndPlay(musicSource, levelMusic, levelMusicVolume);
	}

	private void PlayInteractionSound()
	{
		RoomPrototypeLoopingMusic.PlaySfx(this, ref sfxSource, interactionClickSound, sfxVolume);
	}

	private void PlayZoomSound()
	{
		RoomPrototypeLoopingMusic.PlaySfx(this, ref sfxSource, zoomSound, sfxVolume);
	}

	public void OnPanelPointerDown(int panelId, PointerEventData eventData)
	{
		if (interactionLocked || isDragging || flowerPullAnimating || cageOpened)
		{
			return;
		}

		pressedPanel = FindPanel(panelId);
		if (pressedPanel != null)
		{
			pointerDownScreenPosition = eventData.position;
			if (CanStartFlowerPull(pressedPanel, eventData.position))
			{
				BeginFlowerPull(pressedPanel, eventData.position);
			}
		}
	}

	public void OnPanelDrag(int panelId, PointerEventData eventData)
	{
		if (interactionLocked || pressedPanel == null || pressedPanel.Id != panelId)
		{
			return;
		}

		if (flowerPullActive)
		{
			UpdateFlowerPull(eventData.position);
			return;
		}

		if (!isDragging && (eventData.position - pointerDownScreenPosition).sqrMagnitude >= dragStartDistance * dragStartDistance)
		{
			isDragging = true;
			pressedPanel.Root.SetAsLastSibling();
			pressedPanel.Root.localScale = Vector3.one * 1.045f;
			pressedPanel.CanvasGroup.alpha = 0.94f;
		}

		if (isDragging && RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, eventData.position, null, out Vector2 localPosition))
		{
			pressedPanel.Root.anchoredPosition = localPosition;
		}
	}

	public void OnPanelPointerUp(int panelId, PointerEventData eventData)
	{
		if (pressedPanel != null && pressedPanel.Id == panelId)
		{
			FinishPointer(eventData.position);
		}
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
		if (flowerPullActive)
		{
			flowerPullActive = false;
			StartCoroutine(FinishFlowerPull());
			return;
		}

		if (!isDragging)
		{
			HandlePanelTap(panel, screenPosition);
			return;
		}

		isDragging = false;
		panel.Root.localScale = Vector3.one;
		panel.CanvasGroup.alpha = 1f;
		MovePanelToSlot(panel, GetClosestSlot(screenPosition));
	}

	private void HandlePanelTap(PanelView panel, Vector2 screenPosition)
	{
		if (interactionLocked || panel == null)
		{
			return;
		}

		switch (panel.Role)
		{
			case PanelRole.Cage:
				ToggleCageZoom(panel);
				break;
			case PanelRole.Flower:
				ToggleFlowerZoom(panel);
				break;
			case PanelRole.Window:
				AdvanceWindow(panel);
				break;
		}
	}

	private void ToggleCageZoom(PanelView panel)
	{
		Viewport target = panel.IsZoomed ? new Viewport(2f, 0f, 2f, 2f) : new Viewport(3f, 1f, 1f, 1f);
		panel.IsZoomed = !panel.IsZoomed;
		PlayZoomSound();
		StartViewportAnimation(panel, target);
	}

	private void ToggleFlowerZoom(PanelView panel)
	{
		Viewport target = panel.IsZoomed ? new Viewport(0f, 0f, 2f, 2f) : new Viewport(1f, 0f, 1f, 1f);
		panel.IsZoomed = !panel.IsZoomed;
		PlayZoomSound();
		StartViewportAnimation(panel, target);
	}

	private void AdvanceWindow(PanelView panel)
	{
		if (panel.WindowState == RoomPrototypeLevelThreeWindowState.Room)
		{
			panel.WindowState = RoomPrototypeLevelThreeWindowState.Window;
			panel.IsZoomed = true;
			PlayZoomSound();
			StartViewportAnimation(panel, new Viewport(3f, 0f, 1f, 1f));
			return;
		}

		if (panel.WindowState == RoomPrototypeLevelThreeWindowState.Window
			&& Mathf.Approximately(panel.Viewport.X, 3f)
			&& Mathf.Approximately(panel.Viewport.Y, 0f))
		{
			panel.WindowState = RoomPrototypeLevelThreePuzzleModel.AdvanceWindow(panel.WindowState);
			RefreshAllVisuals();
			CheckPuzzleTransitions();
		}
	}

	private void StartViewportAnimation(PanelView panel, Viewport target)
	{
		if (panel.Animation != null)
		{
			return;
		}
		ClearControls(panel);
		panel.Animation = StartCoroutine(AnimateViewport(panel, target));
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
		CheckPuzzleTransitions();
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

		if (!IsAllowedCell(panel.Role, x, y))
		{
			return;
		}

		StartViewportAnimation(panel, new Viewport(x, y, 1f, 1f));
	}

	private static bool IsAllowedCell(PanelRole role, float x, float y)
	{
		if (role == PanelRole.Flower)
		{
			return x >= 0f && x <= 1f && y >= 0f && y <= 1f;
		}
		if (role == PanelRole.Window)
		{
			return (Mathf.Approximately(x, 2f) && (Mathf.Approximately(y, 0f) || Mathf.Approximately(y, 1f)))
				|| (Mathf.Approximately(x, 3f) && Mathf.Approximately(y, 0f));
		}
		return false;
	}

	private void MovePanelToSlot(PanelView draggedPanel, int targetSlot)
	{
		int originSlot = draggedPanel.Slot;
		PanelView occupiedPanel = FindPanelInSlot(targetSlot);
		if (occupiedPanel == null)
		{
			draggedPanel.Slot = targetSlot;
			StartCoroutine(AnimatePanelsToSlots(new[] { draggedPanel }));
			return;
		}
		if (targetSlot == originSlot)
		{
			StartCoroutine(AnimatePanelsToSlots(new[] { draggedPanel }));
			return;
		}

		int displacementSlot = RoomPrototypeLevelTwoLayoutModel.ChooseDisplacementSlot(targetSlot, originSlot, FindEmptySlot());
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
		CheckPuzzleTransitions();
	}

	private void CreatePanel(int id, PanelRole role, int slot, int regionX, int regionY)
	{
		RectTransform root = CreateRectTransform($"{role} Panel", boardRoot);
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

		Image room = CreateImage("Room", content, Color.white);
		room.sprite = roomSprite;
		room.preserveAspect = false;
		RectTransform roomRect = room.rectTransform;
		roomRect.anchorMin = new Vector2(0.5f, 0.5f);
		roomRect.anchorMax = new Vector2(0.5f, 0.5f);

		Image sky = CreateImage("Sky", content, Color.white);
		sky.sprite = skySprite;
		sky.preserveAspect = false;
		sky.rectTransform.anchorMin = Vector2.zero;
		sky.rectTransform.anchorMax = Vector2.one;
		sky.rectTransform.offsetMin = Vector2.zero;
		sky.rectTransform.offsetMax = Vector2.zero;
		sky.gameObject.SetActive(false);

		PanelView panel = new PanelView
		{
			Id = id,
			Role = role,
			Slot = slot,
			RegionX = regionX,
			RegionY = regionY,
			Root = root,
			Content = content,
			Room = roomRect,
			Sky = sky,
			CanvasGroup = canvasGroup,
			Viewport = new Viewport(regionX, regionY, 2f, 2f),
			WindowState = RoomPrototypeLevelThreeWindowState.Room
		};
		panels.Add(panel);
		CreateRoomFurnitureViews(panel);

		if (role == PanelRole.Window)
		{
			CreateBird(panel);
		}
		if (role == PanelRole.Flower)
		{
			CreateFlower(panel);
		}
		if (role == PanelRole.Cage)
		{
			CreateCageOpenMarker(panel);
		}

		RoomPrototypeLevelThreePanelDrag drag = root.gameObject.AddComponent<RoomPrototypeLevelThreePanelDrag>();
		drag.Initialize(this, id);
		ApplyViewport(panel, panel.Viewport);
	}

	private void CreateBird(PanelView panel)
	{
		Image bird = CreateImage("Bird", panel.Content, new Color(0.96f, 0.97f, 1f, 0.96f));
		bird.rectTransform.sizeDelta = new Vector2(86f, 38f);
		Text label = CreateText("Bird Label", bird.rectTransform, "BIRD", 17, new Color(0.12f, 0.2f, 0.36f, 1f));
		Stretch(label.rectTransform);
		panel.SkyBird = bird.rectTransform;
		bird.gameObject.SetActive(false);
	}

	private void CreateRoomFurnitureViews(PanelView panel)
	{
		panel.Table = CreateRoomFurnitureView("Table", panel.Content, tableSprite);
		panel.CageBird = CreateRoomFurnitureView("Cage Bird", panel.Content, birdSprite);
		panel.Cage = CreateRoomFurnitureView("Cage", panel.Content, cageSprite);
	}

	private static Image CreateRoomFurnitureView(string name, RectTransform parent, Sprite sprite)
	{
		Image image = CreateImage(name, parent, Color.white);
		image.sprite = sprite;
		image.preserveAspect = true;
		return image;
	}

	private void CreateFlower(PanelView panel)
	{
		Image flower = CreateImage("Flower", panel.Content, flowerColor);
		flower.rectTransform.sizeDelta = new Vector2(88f, 76f);
		Text label = CreateText("Flower Label", flower.rectTransform, "FLOWER", 15, Color.white);
		Stretch(label.rectTransform);
		panel.Flower = flower.rectTransform;
	}

	private void CreateCageOpenMarker(PanelView panel)
	{
		Text marker = CreateText("Cage Open Marker", panel.Content, "OPEN", 42, new Color(0.44f, 1f, 0.42f, 1f));
		marker.fontStyle = FontStyle.Bold;
		marker.rectTransform.sizeDelta = new Vector2(180f, 70f);
		marker.gameObject.SetActive(false);
		panel.CageOpenMarker = marker;
	}

	private void CreateTransitionOverlays()
	{
		bool usesKeySprite = keySprite != null;
		Image key = CreateImage("Falling Key", boardRoot, usesKeySprite ? Color.white : keyColor);
		key.sprite = keySprite;
		key.preserveAspect = usesKeySprite;
		key.rectTransform.sizeDelta = GetKeyOverlaySize();
		key.gameObject.SetActive(false);
		keyOverlay = key.rectTransform;

		Image growth = CreateImage("Growing Flower", boardRoot, flowerColor);
		growth.rectTransform.sizeDelta = new Vector2(32f, 0f);
		growth.rectTransform.pivot = new Vector2(0.5f, 0f);
		growth.gameObject.SetActive(false);
		growthOverlay = growth.rectTransform;

		Image flowerHead = CreateImage("Pulled Flower", boardRoot, flowerColor);
		flowerHead.rectTransform.sizeDelta = new Vector2(88f, 76f);
		Text flowerLabel = CreateText("Pulled Flower Label", flowerHead.rectTransform, "FLOWER", 15, Color.white);
		Stretch(flowerLabel.rectTransform);
		flowerHead.gameObject.SetActive(false);
		flowerHeadOverlay = flowerHead.rectTransform;
	}

	private void ApplyViewport(PanelView panel, Viewport viewport)
	{
		Vector2 contentSize = GetContentSize(panel);
		Vector2 imageSize = new Vector2(contentSize.x * RoomColumns / viewport.Width, contentSize.y * RoomRows / viewport.Height);
		float centerX = (viewport.X + viewport.Width * 0.5f) / RoomColumns;
		float centerY = (viewport.Y + viewport.Height * 0.5f) / RoomRows;
		Vector2 offset = new Vector2((centerX - 0.5f) * imageSize.x, (0.5f - centerY) * imageSize.y);
		panel.Room.sizeDelta = imageSize;
		panel.Room.anchoredPosition = -offset;
		RefreshPanelVisuals(panel, viewport);
	}

	private void RefreshAllVisuals()
	{
		for (int i = 0; i < panels.Count; i++)
		{
			RefreshPanelVisuals(panels[i], panels[i].Viewport);
			RefreshControls(panels[i]);
		}
	}

	private void RefreshPanelVisuals(PanelView panel, Viewport viewport)
	{
		bool isSky = panel.Role == PanelRole.Window && panel.WindowState == RoomPrototypeLevelThreeWindowState.Sky;
		panel.Room.gameObject.SetActive(!isSky);
		panel.Sky.gameObject.SetActive(isSky);
		UpdateRoomFurnitureViews(panel, viewport, !isSky);
		if (panel.SkyBird != null)
		{
			panel.SkyBird.gameObject.SetActive(isSky);
		}

		if (panel.Flower != null)
		{
			PlaceWorldMarker(panel.Flower, panel, viewport, flowerWorldPosition, flowerWorldSize);
		}
		if (panel.CaughtKey != null)
		{
			PlaceWorldMarker(panel.CaughtKey, panel, viewport, caughtKeyWorldPosition, GetCaughtKeyWorldSize());
			panel.CaughtKey.gameObject.SetActive(keyCaught && panel.CaughtKey.gameObject.activeSelf);
		}
		if (panel.CageOpenMarker != null)
		{
			panel.CageOpenMarker.gameObject.SetActive(cageOpened && panel.IsZoomed);
			panel.CageOpenMarker.rectTransform.anchoredPosition = new Vector2(0f, 40f);
		}
	}

	private void UpdateRoomFurnitureViews(PanelView panel, Viewport viewport, bool showRoomFurniture)
	{
		UpdateRoomFurnitureView(panel.Table, panel, viewport, tableWorldPosition, tableWorldSize, showRoomFurniture);
		UpdateRoomFurnitureView(panel.CageBird, panel, viewport, birdWorldPosition, birdWorldSize, showRoomFurniture);
		UpdateRoomFurnitureView(panel.Cage, panel, viewport, cageWorldPosition, cageWorldSize, showRoomFurniture);
	}

	private void UpdateRoomFurnitureView(
		Image furniture,
		PanelView panel,
		Viewport viewport,
		Vector2 worldPosition,
		Vector2 worldSize,
		bool showRoomFurniture
	)
	{
		if (furniture == null)
		{
			return;
		}

		bool visible = showRoomFurniture && furniture.sprite != null;
		furniture.gameObject.SetActive(visible);
		if (visible)
		{
			PlaceWorldMarker(furniture.rectTransform, panel, viewport, worldPosition, worldSize);
		}
	}

	private void PlaceWorldMarker(RectTransform marker, PanelView panel, Viewport viewport, Vector2 worldPosition, Vector2 worldSize)
	{
		Rect visible = new Rect(viewport.X, viewport.Y, viewport.Width, viewport.Height);
		Rect markerBounds = new Rect(worldPosition - worldSize * 0.5f, worldSize);
		bool active = markerBounds.xMax >= visible.xMin && markerBounds.xMin <= visible.xMax
			&& markerBounds.yMax >= visible.yMin && markerBounds.yMin <= visible.yMax;
		marker.gameObject.SetActive(active);
		if (!active)
		{
			return;
		}

		Vector2 contentSize = GetContentSize(panel);
		float normalizedX = (worldPosition.x - visible.xMin) / visible.width;
		float normalizedY = (worldPosition.y - visible.yMin) / visible.height;
		marker.anchoredPosition = new Vector2((normalizedX - 0.5f) * contentSize.x, (0.5f - normalizedY) * contentSize.y);
		marker.sizeDelta = new Vector2(worldSize.x / visible.width * contentSize.x, worldSize.y / visible.height * contentSize.y);
	}

	private void Update()
	{
		PanelView window = FindPanelByRole(PanelRole.Window);
		if (window == null || window.SkyBird == null || window.WindowState != RoomPrototypeLevelThreeWindowState.Sky)
		{
			return;
		}

		float width = GetContentSize(window).x;
		float normalized = Mathf.PingPong(Time.unscaledTime * 0.22f, 1f);
		window.SkyBird.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.38f, width * 0.38f, normalized), width * 0.12f + Mathf.Sin(Time.unscaledTime * 2f) * 18f);
	}

	private void CheckPuzzleTransitions()
	{
		PanelView window = FindPanelByRole(PanelRole.Window);
		PanelView flower = FindPanelByRole(PanelRole.Flower);
		if (!keyDropStarted && RoomPrototypeLevelThreePuzzleModel.CanDropKey(
			window.WindowState,
			window.Slot,
			IsFlowerZoomed(flower),
			flower.Slot,
			keyCaught))
		{
			keyDropStarted = true;
			StartCoroutine(AnimateKeyDrop(window, flower));
		}
	}

	private IEnumerator AnimateKeyDrop(PanelView window, PanelView flower)
	{
		interactionLocked = true;
		keyOverlay.SetAsLastSibling();
		Vector2 flowerBoardPosition = GetBoardPositionForWorld(flower, flowerWorldPosition);
		Vector2 start = new Vector2(flowerBoardPosition.x, GetPanelContentBoardPosition(window, new Vector2(0.5f, 0.45f)).y);
		Vector2 target = flowerBoardPosition;
		keyOverlay.anchoredPosition = start;
		keyOverlay.localEulerAngles = new Vector3(0f, 0f, -75f);
		keyOverlay.gameObject.SetActive(true);

		float elapsed = 0f;
		while (elapsed < keyFallDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			float normalized = Mathf.Clamp01(elapsed / keyFallDuration);
			float fall = normalized * normalized;
			keyOverlay.anchoredPosition = Vector2.LerpUnclamped(start, target, fall);
			keyOverlay.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-75f, 0f, normalized));
			yield return null;
		}

		keyOverlay.gameObject.SetActive(false);
		keyCaught = true;
		EnsureCaughtKey(flower);
		RefreshPanelVisuals(flower, flower.Viewport);
		interactionLocked = false;
	}

	private void EnsureCaughtKey(PanelView flower)
	{
		if (flower.CaughtKey != null)
		{
			return;
		}
		bool usesKeySprite = keySprite != null;
		Image key = CreateImage("Caught Key", flower.Content, usesKeySprite ? Color.white : keyColor);
		key.sprite = keySprite;
		key.preserveAspect = usesKeySprite;
		flower.CaughtKey = key.rectTransform;
	}

	private Vector2 GetKeyOverlaySize()
	{
		return keySprite == null
			? new Vector2(70f, 20f)
			: RoomPrototypeKeySpriteSizing.GetSizeForHeight(keySprite.rect.width, keySprite.rect.height, 84f);
	}

	private Vector2 GetCaughtKeyWorldSize()
	{
		return keySprite == null
			? new Vector2(0.24f, 0.07f)
			: RoomPrototypeKeySpriteSizing.GetSizeForHeight(keySprite.rect.width, keySprite.rect.height, caughtKeyWorldHeight);
	}

	private bool CanStartFlowerPull(PanelView flower, Vector2 screenPosition)
	{
		PanelView cage = FindPanelByRole(PanelRole.Cage);
		if (!RoomPrototypeLevelThreePuzzleModel.CanGrowFlower(
			keyCaught,
			flower.Slot,
			IsCageZoomed(cage),
			cage.Slot))
		{
			return false;
		}

		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(flower.Content, screenPosition, null, out Vector2 local))
		{
			return false;
		}
		Vector2 markerPosition = flower.Flower.anchoredPosition;
		return Mathf.Abs(local.x - markerPosition.x) <= flower.Flower.sizeDelta.x * 0.7f
			&& Mathf.Abs(local.y - markerPosition.y) <= flower.Flower.sizeDelta.y * 0.8f;
	}

	private void BeginFlowerPull(PanelView flower, Vector2 screenPosition)
	{
		PanelView cage = FindPanelByRole(PanelRole.Cage);
		flowerPullStart = GetBoardPositionForWorld(flower, flowerWorldPosition);
		flowerPullTarget = GetPanelContentBoardPosition(cage, new Vector2(0.5f, 0.66f));
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPosition, null, out Vector2 pointerPosition))
		{
			return;
		}

		flowerPointerStartY = pointerPosition.y;
		flowerPullProgress = 0f;
		flowerPullActive = true;
		PlayInteractionSound();
		growthOverlay.SetAsLastSibling();
		flowerHeadOverlay.SetAsLastSibling();
		keyOverlay.SetAsLastSibling();
		growthOverlay.gameObject.SetActive(true);
		flowerHeadOverlay.gameObject.SetActive(true);
		keyOverlay.gameObject.SetActive(true);
		flower.Flower.gameObject.SetActive(false);
		flower.CaughtKey.gameObject.SetActive(false);
		UpdateFlowerPullVisuals(0f);
	}

	private void UpdateFlowerPull(Vector2 screenPosition)
	{
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPosition, null, out Vector2 pointerPosition))
		{
			return;
		}

		float requiredDistance = Mathf.Max(1f, flowerPullTarget.y - flowerPullStart.y);
		flowerPullProgress = RoomPrototypeLevelThreePuzzleModel.GetFlowerPullProgress(
			flowerPointerStartY,
			pointerPosition.y,
			requiredDistance);
		UpdateFlowerPullVisuals(flowerPullProgress);
	}

	private void UpdateFlowerPullVisuals(float progress)
	{
		float height = Mathf.Max(0f, flowerPullTarget.y - flowerPullStart.y) * Mathf.Clamp01(progress);
		Vector2 tip = flowerPullStart + Vector2.up * height;
		growthOverlay.anchoredPosition = flowerPullStart;
		growthOverlay.localScale = Vector3.one;
		growthOverlay.sizeDelta = new Vector2(34f, height);
		flowerHeadOverlay.anchoredPosition = tip;
		keyOverlay.anchoredPosition = tip + new Vector2(48f, -8f);
		keyOverlay.localEulerAngles = Vector3.zero;
	}

	private IEnumerator FinishFlowerPull()
	{
		flowerPullAnimating = true;
		interactionLocked = true;
		bool completed = RoomPrototypeLevelThreePuzzleModel.IsFlowerPullComplete(
			flowerPullProgress,
			flowerPullCompletionThreshold);
		float startProgress = flowerPullProgress;
		float targetProgress = completed ? 1f : 0f;
		float duration = completed ? Mathf.Min(0.24f, flowerGrowDuration) : flowerReturnDuration;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
			flowerPullProgress = Mathf.Lerp(startProgress, targetProgress, t);
			UpdateFlowerPullVisuals(flowerPullProgress);
			yield return null;
		}

		flowerPullProgress = targetProgress;
		UpdateFlowerPullVisuals(targetProgress);
		PanelView flower = FindPanelByRole(PanelRole.Flower);
		PanelView cage = FindPanelByRole(PanelRole.Cage);
		if (completed)
		{
			cageOpened = true;
			RefreshPanelVisuals(cage, cage.Viewport);
		}
		else
		{
			growthOverlay.gameObject.SetActive(false);
			flowerHeadOverlay.gameObject.SetActive(false);
			keyOverlay.gameObject.SetActive(false);
			RefreshPanelVisuals(flower, flower.Viewport);
		}

		flowerPullAnimating = false;
		interactionLocked = false;
	}

	private static bool IsFlowerZoomed(PanelView panel)
	{
		return panel != null && panel.IsZoomed && Mathf.Approximately(panel.Viewport.X, 1f) && Mathf.Approximately(panel.Viewport.Y, 0f);
	}

	private static bool IsCageZoomed(PanelView panel)
	{
		return panel != null && panel.IsZoomed && Mathf.Approximately(panel.Viewport.X, 3f) && Mathf.Approximately(panel.Viewport.Y, 1f);
	}

	private void RefreshControls(PanelView panel)
	{
		ClearControls(panel);
		if (!panel.IsZoomed || panel.Role == PanelRole.Cage || panel.WindowState == RoomPrototypeLevelThreeWindowState.Sky)
		{
			if (panel.Role == PanelRole.Window && panel.WindowState == RoomPrototypeLevelThreeWindowState.Sky)
			{
				CreateBackFromSkyButton(panel);
			}
			return;
		}

		TryCreateArrow(panel, Direction.Left, IsAllowedCell(panel.Role, panel.Viewport.X - 1f, panel.Viewport.Y));
		TryCreateArrow(panel, Direction.Right, IsAllowedCell(panel.Role, panel.Viewport.X + 1f, panel.Viewport.Y));
		TryCreateArrow(panel, Direction.Up, IsAllowedCell(panel.Role, panel.Viewport.X, panel.Viewport.Y - 1f));
		TryCreateArrow(panel, Direction.Down, IsAllowedCell(panel.Role, panel.Viewport.X, panel.Viewport.Y + 1f));
	}

	private void TryCreateArrow(PanelView panel, Direction direction, bool allowed)
	{
		if (!allowed)
		{
			return;
		}
		Image image = CreateImage($"{direction} Arrow", panel.Root, new Color(0.08f, 0.08f, 0.075f, 0.86f));
		image.raycastTarget = true;
		image.rectTransform.sizeDelta = new Vector2(46f, 46f);
		PlaceArrow(image.rectTransform, direction);
		Button button = image.gameObject.AddComponent<Button>();
		button.targetGraphic = image;
		button.onClick.AddListener(() => Navigate(panel, direction));
		Text label = CreateText("Label", image.rectTransform, GetArrowText(direction), 30, Color.white);
		Stretch(label.rectTransform);
		panel.Controls.Add(image.gameObject);
	}

	private void CreateBackFromSkyButton(PanelView panel)
	{
		Image image = CreateImage("Back From Sky", panel.Root, new Color(0.08f, 0.08f, 0.075f, 0.86f));
		image.raycastTarget = true;
		image.rectTransform.sizeDelta = new Vector2(46f, 46f);
		PlaceArrow(image.rectTransform, Direction.Left);
		Button button = image.gameObject.AddComponent<Button>();
		button.targetGraphic = image;
		button.onClick.AddListener(() =>
		{
			panel.WindowState = RoomPrototypeLevelThreeWindowState.Window;
			PlayZoomSound();
			RefreshAllVisuals();
		});
		Text label = CreateText("Label", image.rectTransform, "<", 30, Color.white);
		Stretch(label.rectTransform);
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
		int closest = 0;
		float distance = float.MaxValue;
		for (int slot = 0; slot <= 3; slot++)
		{
			float current = (GetSlotPosition(slot) - localPosition).sqrMagnitude;
			if (current < distance)
			{
				distance = current;
				closest = slot;
			}
		}
		return closest;
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
		for (int slot = 0; slot <= 3; slot++)
		{
			if (FindPanelInSlot(slot) == null)
			{
				return slot;
			}
		}
		throw new InvalidOperationException("Level three must keep one slot empty.");
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

	private PanelView FindPanelByRole(PanelRole role)
	{
		for (int i = 0; i < panels.Count; i++)
		{
			if (panels[i].Role == role)
			{
				return panels[i];
			}
		}
		return null;
	}

	private Vector2 GetContentSize(PanelView panel)
	{
		Vector2 size = panel.Content.rect.size;
		return size.x <= 0f || size.y <= 0f ? panel.Content.sizeDelta : size;
	}

	private Vector2 GetBoardPositionForWorld(PanelView panel, Vector2 worldPosition)
	{
		Rect viewport = new Rect(panel.Viewport.X, panel.Viewport.Y, panel.Viewport.Width, panel.Viewport.Height);
		Vector2 size = GetContentSize(panel);
		float x = ((worldPosition.x - viewport.xMin) / viewport.width - 0.5f) * size.x;
		float y = (0.5f - (worldPosition.y - viewport.yMin) / viewport.height) * size.y;
		return boardRoot.InverseTransformPoint(panel.Content.TransformPoint(new Vector2(x, y)));
	}

	private Vector2 GetPanelContentBoardPosition(PanelView panel, Vector2 normalizedTopLeft)
	{
		Vector2 size = GetContentSize(panel);
		Vector2 local = new Vector2((normalizedTopLeft.x - 0.5f) * size.x, (0.5f - normalizedTopLeft.y) * size.y);
		return boardRoot.InverseTransformPoint(panel.Content.TransformPoint(local));
	}

	private RectTransform CreateCanvas(Transform parent)
	{
		GameObject canvasObject = new GameObject("Room Prototype Level Three Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasObject.transform.SetParent(parent, false);
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

	private void EnsureEventSystem(Transform parent)
	{
		EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
		if (eventSystem == null)
		{
			GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
			eventSystemObject.transform.SetParent(parent, false);
			eventSystem = eventSystemObject.GetComponent<EventSystem>();
		}
		InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
		if (inputModule == null)
		{
			inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
		}
		inputModule.AssignDefaultActions();
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

	private static Image CreateImage(string name, Transform parent, Color color)
	{
		RectTransform rect = CreateRectTransform(name, parent);
		Image image = rect.gameObject.AddComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		return image;
	}

	private Text CreateText(string name, Transform parent, string text, int fontSize, Color color)
	{
		RectTransform rect = CreateRectTransform(name, parent);
		Text label = rect.gameObject.AddComponent<Text>();
		label.font = interfaceFont;
		label.text = text;
		label.fontSize = fontSize;
		label.alignment = TextAnchor.MiddleCenter;
		label.color = color;
		label.raycastTarget = false;
		return label;
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
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
			default: return string.Empty;
		}
	}

	private enum PanelRole { Cage, Flower, Window }
	private enum Direction { Left, Right, Up, Down }

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
				Mathf.Lerp(from.Height, to.Height, t));
		}
	}

	private sealed class PanelView
	{
		public int Id;
		public PanelRole Role;
		public int Slot;
		public int RegionX;
		public int RegionY;
		public bool IsZoomed;
		public RectTransform Root;
		public RectTransform Content;
		public RectTransform Room;
		public Image Sky;
		public Image Table;
		public Image CageBird;
		public Image Cage;
		public RectTransform SkyBird;
		public RectTransform Flower;
		public RectTransform CaughtKey;
		public Text CageOpenMarker;
		public CanvasGroup CanvasGroup;
		public Viewport Viewport;
		public Coroutine Animation;
		public RoomPrototypeLevelThreeWindowState WindowState;
		public readonly List<GameObject> Controls = new List<GameObject>();
	}
}
