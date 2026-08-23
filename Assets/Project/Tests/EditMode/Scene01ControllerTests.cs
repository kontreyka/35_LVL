using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class SpriteContourSamplerTests
{
	[Test]
	public void FindContourPixels_UsesAlphaBorderWhenTransparentPixelsExist()
	{
		Texture2D texture = CreateTexture(5, 5, Color.clear);

		for (int y = 1; y <= 3; y++)
		{
			for (int x = 1; x <= 3; x++)
			{
				texture.SetPixel(x, y, Color.white);
			}
		}

		texture.Apply();

		Vector2Int[] contour = SpriteContourSampler.FindContourPixels(
			texture,
			new Rect(0f, 0f, 5f, 5f),
			1,
			0.5f
		).ToArray();

		Assert.That(contour, Has.Member(new Vector2Int(1, 1)));
		Assert.That(contour, Has.Member(new Vector2Int(3, 2)));
		Assert.That(contour, Has.No.Member(new Vector2Int(2, 2)));
		Assert.That(contour, Has.Length.EqualTo(8));
	}

	[Test]
	public void FindContourPixels_UsesSpritePerimeterWhenAlphaIsOpaque()
	{
		Texture2D texture = CreateTexture(5, 5, Color.black);

		for (int y = 1; y <= 3; y++)
		{
			for (int x = 1; x <= 3; x++)
			{
				texture.SetPixel(x, y, Color.white);
			}
		}

		texture.Apply();

		Vector2Int[] contour = SpriteContourSampler.FindContourPixels(
			texture,
			new Rect(0f, 0f, 5f, 5f),
			1,
			0.5f
		).ToArray();

		Assert.That(contour, Has.Member(new Vector2Int(0, 0)));
		Assert.That(contour, Has.Member(new Vector2Int(0, 2)));
		Assert.That(contour, Has.Member(new Vector2Int(4, 4)));
		Assert.That(contour, Has.No.Member(new Vector2Int(1, 1)));
		Assert.That(contour, Has.No.Member(new Vector2Int(2, 2)));
		Assert.That(contour, Has.Length.EqualTo(16));
	}

	[Test]
	public void FindContourPixels_IgnoresTransparentHolesInsideSilhouette()
	{
		Texture2D texture = CreateTexture(7, 7, Color.clear);

		for (int y = 1; y <= 5; y++)
		{
			for (int x = 1; x <= 5; x++)
			{
				texture.SetPixel(x, y, Color.white);
			}
		}

		texture.SetPixel(3, 3, Color.clear);
		texture.Apply();

		Vector2Int[] contour = SpriteContourSampler.FindContourPixels(
			texture,
			new Rect(0f, 0f, 7f, 7f),
			1,
			0.5f
		).ToArray();

		Assert.That(contour, Has.No.Member(new Vector2Int(2, 3)));
		Assert.That(contour, Has.No.Member(new Vector2Int(3, 2)));
		Assert.That(contour, Has.No.Member(new Vector2Int(3, 4)));
		Assert.That(contour, Has.No.Member(new Vector2Int(4, 3)));
		Assert.That(contour, Has.Member(new Vector2Int(1, 3)));
		Assert.That(contour, Has.Member(new Vector2Int(5, 3)));
	}

	[Test]
	public void BuildGlowTexture_AddsPaddingSoOpaqueSpriteAuraCanExtendPastEdges()
	{
		Texture2D texture = CreateTexture(7, 7, Color.black);

		texture.Apply();

		Texture2D glowTexture = SpriteContourGlowTextureBuilder.BuildGlowTexture(
			texture,
			new Rect(0f, 0f, 7f, 7f),
			new Color(1f, 0.86f, 0.48f, 1f),
			1,
			0.5f,
			1,
			1f
		);

		Assert.That(glowTexture.width, Is.EqualTo(9));
		Assert.That(glowTexture.height, Is.EqualTo(9));
		Assert.That(glowTexture.GetPixel(4, 4).a, Is.EqualTo(0f).Within(0.001f));
		Assert.That(glowTexture.GetPixel(4, 8).a, Is.GreaterThan(0f));
	}

	[Test]
	public void BuildGlowTexture_KeepsEmptyPixelsTransparent()
	{
		Texture2D texture = CreateTexture(7, 7, Color.clear);

		for (int y = 2; y <= 4; y++)
		{
			for (int x = 2; x <= 4; x++)
			{
				texture.SetPixel(x, y, Color.white);
			}
		}

		texture.Apply();

		Texture2D glowTexture = SpriteContourGlowTextureBuilder.BuildGlowTexture(
			texture,
			new Rect(0f, 0f, 7f, 7f),
			new Color(1f, 0.86f, 0.48f, 1f),
			1,
			0.5f,
			1,
			1f
		);

		Assert.That(glowTexture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(0.001f));
		Assert.That(glowTexture.GetPixel(2, 4).a, Is.GreaterThan(0f));
		Assert.That(glowTexture.GetPixel(4, 4).a, Is.GreaterThan(0f));
	}

	private static Texture2D CreateTexture(int width, int height, Color color)
	{
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				texture.SetPixel(x, y, color);
			}
		}

		return texture;
	}
}

public sealed class Scene01ControllerBackgroundScaleTests
{
	[Test]
	public void ShouldFadeCageAuraForClickStepIndex_FadesForFirstThreeClicks()
	{
		Assert.That(Scene01Controller.ShouldFadeCageAuraForClickStepIndex(0), Is.True);
		Assert.That(Scene01Controller.ShouldFadeCageAuraForClickStepIndex(1), Is.True);
		Assert.That(Scene01Controller.ShouldFadeCageAuraForClickStepIndex(2), Is.True);
		Assert.That(Scene01Controller.ShouldFadeCageAuraForClickStepIndex(3), Is.False);
	}

	[Test]
	public void ShouldFadeCageAuraForTriggerCount_UsesInspectorCount()
	{
		Assert.That(Scene01Controller.ShouldFadeCageAuraForTriggerCount(0, 0), Is.False);
		Assert.That(Scene01Controller.ShouldFadeCageAuraForTriggerCount(0, 2), Is.True);
		Assert.That(Scene01Controller.ShouldFadeCageAuraForTriggerCount(1, 2), Is.True);
		Assert.That(Scene01Controller.ShouldFadeCageAuraForTriggerCount(2, 2), Is.False);
	}

	[Test]
	public void CalculateZoomResponsiveBackgroundScale_FullyCompensatesCameraZoom()
	{
		Vector3 scale = Scene01Controller.CalculateZoomResponsiveBackgroundScale(
			new Vector3(2f, 3f, 1f),
			5f,
			2.5f,
			1f
		);

		Assert.That(scale.x, Is.EqualTo(1f).Within(0.001f));
		Assert.That(scale.y, Is.EqualTo(1.5f).Within(0.001f));
		Assert.That(scale.z, Is.EqualTo(1f).Within(0.001f));
	}

	[Test]
	public void CalculateZoomResponsiveBackgroundScale_HalfCompensatesCameraZoom()
	{
		Vector3 scale = Scene01Controller.CalculateZoomResponsiveBackgroundScale(
			new Vector3(2f, 3f, 1f),
			5f,
			2.5f,
			0.5f
		);

		Assert.That(scale.x, Is.EqualTo(1.5f).Within(0.001f));
		Assert.That(scale.y, Is.EqualTo(2.25f).Within(0.001f));
		Assert.That(scale.z, Is.EqualTo(1f).Within(0.001f));
	}
}

public sealed class RoomPrototypeNavigationTests
{
	[Test]
	public void PrototypeMusic_ConfiguresAnImmediateSeamlessTwoDimensionalLoop()
	{
		GameObject musicObject = new GameObject("Prototype Music Test");
		AudioSource musicSource = musicObject.AddComponent<AudioSource>();
		AudioClip musicClip = AudioClip.Create("Loop", 256, 1, 44100, false);

		try
		{
			RoomPrototypeLoopingMusic.ConfigureAndPlay(musicSource, musicClip, 0.4f);

			Assert.That(musicSource.clip, Is.SameAs(musicClip));
			Assert.That(musicSource.loop, Is.True);
			Assert.That(musicSource.spatialBlend, Is.EqualTo(0f));
			Assert.That(musicSource.volume, Is.EqualTo(0.4f));
			Assert.That(musicSource.playOnAwake, Is.False);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(musicObject);
			UnityEngine.Object.DestroyImmediate(musicClip);
		}
	}

	[Test]
	public void KeySpriteSizing_PreservesThePortraitKeyProportions()
	{
		Vector2 size = RoomPrototypeKeySpriteSizing.GetSizeForHeight(168f, 643f, 84f);

		Assert.That(size.x, Is.EqualTo(21.95f).Within(0.01f));
		Assert.That(size.y, Is.EqualTo(84f));
	}

	[Test]
	public void LevelThreeWindow_UsesRoomWindowAndSkyAsThreeConsecutiveStates()
	{
		Assert.That(RoomPrototypeLevelThreePuzzleModel.AdvanceWindow(RoomPrototypeLevelThreeWindowState.Room),
			Is.EqualTo(RoomPrototypeLevelThreeWindowState.Window));
		Assert.That(RoomPrototypeLevelThreePuzzleModel.AdvanceWindow(RoomPrototypeLevelThreeWindowState.Window),
			Is.EqualTo(RoomPrototypeLevelThreeWindowState.Sky));
		Assert.That(RoomPrototypeLevelThreePuzzleModel.AdvanceWindow(RoomPrototypeLevelThreeWindowState.Sky),
			Is.EqualTo(RoomPrototypeLevelThreeWindowState.Sky));
	}

	[Test]
	public void LevelThreeBird_DropsKeyOnlyWhenSkyIsDirectlyAboveZoomedFlower()
	{
		Assert.That(RoomPrototypeLevelThreePuzzleModel.CanDropKey(
			RoomPrototypeLevelThreeWindowState.Sky,
			RoomPrototypeLevelTwoSlot.TopRight,
			true,
			RoomPrototypeLevelTwoSlot.BottomRight,
			false
		), Is.True);
		Assert.That(RoomPrototypeLevelThreePuzzleModel.CanDropKey(
			RoomPrototypeLevelThreeWindowState.Window,
			RoomPrototypeLevelTwoSlot.TopRight,
			true,
			RoomPrototypeLevelTwoSlot.BottomRight,
			false
		), Is.False);
		Assert.That(RoomPrototypeLevelThreePuzzleModel.CanDropKey(
			RoomPrototypeLevelThreeWindowState.Sky,
			RoomPrototypeLevelTwoSlot.TopLeft,
			true,
			RoomPrototypeLevelTwoSlot.BottomRight,
			false
		), Is.False);
	}

	[Test]
	public void LevelThreeFlower_GrowsOnlyBelowZoomedCageAfterCatchingKey()
	{
		Assert.That(RoomPrototypeLevelThreePuzzleModel.CanGrowFlower(
			true,
			RoomPrototypeLevelTwoSlot.BottomLeft,
			true,
			RoomPrototypeLevelTwoSlot.TopLeft
		), Is.True);
		Assert.That(RoomPrototypeLevelThreePuzzleModel.CanGrowFlower(
			false,
			RoomPrototypeLevelTwoSlot.BottomLeft,
			true,
			RoomPrototypeLevelTwoSlot.TopLeft
		), Is.False);
		Assert.That(RoomPrototypeLevelThreePuzzleModel.CanGrowFlower(
			true,
			RoomPrototypeLevelTwoSlot.BottomRight,
			true,
			RoomPrototypeLevelTwoSlot.TopLeft
		), Is.False);
	}

	[Test]
	public void LevelThreeFlowerPull_OnlyUsesUpwardDistanceAndRequiresTheThreshold()
	{
		Assert.That(RoomPrototypeLevelThreePuzzleModel.GetFlowerPullProgress(100f, 40f, 200f), Is.EqualTo(0f));
		Assert.That(RoomPrototypeLevelThreePuzzleModel.GetFlowerPullProgress(100f, 200f, 200f), Is.EqualTo(0.5f).Within(0.001f));
		Assert.That(RoomPrototypeLevelThreePuzzleModel.GetFlowerPullProgress(100f, 400f, 200f), Is.EqualTo(1f));
		Assert.That(RoomPrototypeLevelThreePuzzleModel.IsFlowerPullComplete(0.79f, 0.8f), Is.False);
		Assert.That(RoomPrototypeLevelThreePuzzleModel.IsFlowerPullComplete(0.8f, 0.8f), Is.True);
	}

	[Test]
	public void LevelTwoWorldObject_UsesOneWorldPositionAcrossOverlappingPanels()
	{
		Vector2 sharedWorldPosition = new Vector2(1.75f, 1.5f);
		Vector2 panelSize = new Vector2(400f, 400f);

		Vector2 leftPanelPosition = RoomPrototypeLevelTwoWorldProjection.GetPanelAnchoredPosition(
			sharedWorldPosition,
			panelSize,
			new Rect(0f, 0f, 2f, 2f)
		);
		Vector2 rightPanelPosition = RoomPrototypeLevelTwoWorldProjection.GetPanelAnchoredPosition(
			sharedWorldPosition,
			panelSize,
			new Rect(1f, 0f, 2f, 2f)
		);

		Assert.That(leftPanelPosition, Is.EqualTo(new Vector2(150f, -100f)));
		Assert.That(rightPanelPosition, Is.EqualTo(new Vector2(-50f, -100f)));
		Assert.That(RoomPrototypeLevelTwoWorldProjection.IsVisibleInViewport(
			sharedWorldPosition,
			new Rect(0f, 0f, 2f, 2f)
		), Is.True);
		Assert.That(RoomPrototypeLevelTwoWorldProjection.IsVisibleInViewport(
			sharedWorldPosition,
			new Rect(2f, 0f, 2f, 2f)
		), Is.False);
	}

	[Test]
	public void LevelTwoClockPuzzle_RequiresPortraitDirectlyAboveCage()
	{
		Assert.That(RoomPrototypeLevelTwoClockPuzzleModel.ArePortraitAndCageVerticallyAligned(
			RoomPrototypeLevelTwoSlot.TopLeft,
			RoomPrototypeLevelTwoSlot.BottomLeft
		), Is.True);
		Assert.That(RoomPrototypeLevelTwoClockPuzzleModel.ArePortraitAndCageVerticallyAligned(
			RoomPrototypeLevelTwoSlot.TopRight,
			RoomPrototypeLevelTwoSlot.BottomRight
		), Is.True);
		Assert.That(RoomPrototypeLevelTwoClockPuzzleModel.ArePortraitAndCageVerticallyAligned(
			RoomPrototypeLevelTwoSlot.TopLeft,
			RoomPrototypeLevelTwoSlot.BottomRight
		), Is.False);
	}

	[Test]
	public void LevelTwoKeyFall_AcceleratesAfterRelease()
	{
		Assert.That(RoomPrototypeLevelTwoClockPuzzleModel.GetAcceleratedFallProgress(0.5f), Is.EqualTo(0.25f).Within(0.001f));
		Assert.That(RoomPrototypeLevelTwoClockPuzzleModel.GetAcceleratedFallProgress(1f), Is.EqualTo(1f).Within(0.001f));
	}

	[Test]
	public void LevelTwoKeyFallOverlay_DropsVerticallyOverTheBoard()
	{
		Vector2 start = new Vector2(120f, 260f);
		Vector2 tablePosition = new Vector2(420f, -180f);

		Assert.That(RoomPrototypeLevelTwoClockPuzzleModel.GetStraightDropTarget(start, tablePosition),
			Is.EqualTo(new Vector2(120f, -180f)));
	}

	[Test]
	public void LevelTwoKeyLanding_ConvertsOverlayPositionBackIntoB4WorldCoordinates()
	{
		Vector2 worldPosition = RoomPrototypeLevelTwoWorldProjection.GetWorldPosition(
			new Vector2(0.66f, 0.18f),
			new Rect(3f, 1f, 1f, 1f)
		);

		Assert.That(worldPosition.x, Is.EqualTo(3.66f).Within(0.001f));
		Assert.That(worldPosition.y, Is.EqualTo(1.18f).Within(0.001f));
	}

	[Test]
	public void CalculateSquareBoardSize_UsesShorterConfiguredSide()
	{
		Vector2 squareBoardSize = RoomPrototypeLevelOneController.CalculateSquareBoardSize(new Vector2(1320f, 742f));

		Assert.That(squareBoardSize, Is.EqualTo(new Vector2(742f, 742f)));
	}

	[Test]
	public void BuiltInFontResourceName_UsesUnity6000RuntimeFont()
	{
		Assert.That(RoomPrototypeLevelOneController.BuiltInFontResourceName, Is.EqualTo("LegacyRuntime.ttf"));
	}

	[Test]
	public void BuiltPrototype_HidesSectorLabelsAndLeavesOnlyButtonsBlockingRaycasts()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.AddComponent<RoomPrototypeLevelOneController>();

		try
		{
			Text[] textLabels = root.GetComponentsInChildren<Text>(true);
			Assert.That(textLabels.Any(label => label.name == "Panel Label"), Is.False);

			Button[] tapButtons = root.GetComponentsInChildren<Button>(true)
				.Where(button => button.name == "Tap Area")
				.ToArray();

			Assert.That(tapButtons, Has.Length.EqualTo(4));
			Assert.That(tapButtons.All(button => button.targetGraphic != null && button.targetGraphic.raycastTarget), Is.True);

			Graphic[] nonButtonRaycastBlockers = root.GetComponentsInChildren<Graphic>(true)
				.Where(graphic => graphic.raycastTarget && graphic.GetComponent<Button>() == null)
				.ToArray();

			Assert.That(nonButtonRaycastBlockers, Is.Empty);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_CreatesOneStraightNonInteractiveCrossAbovePanels()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.AddComponent<RoomPrototypeLevelOneController>();

		try
		{
			RectTransform vertical = root.GetComponentsInChildren<RectTransform>(true)
				.Single(rect => rect.name == "Internal Vertical Divider");
			RectTransform horizontal = root.GetComponentsInChildren<RectTransform>(true)
				.Single(rect => rect.name == "Internal Horizontal Divider");

			Assert.That(vertical.anchoredPosition, Is.EqualTo(Vector2.zero));
			Assert.That(horizontal.anchoredPosition, Is.EqualTo(Vector2.zero));
			Assert.That(vertical.sizeDelta, Is.EqualTo(new Vector2(16f, 942f)));
			Assert.That(horizontal.sizeDelta, Is.EqualTo(new Vector2(942f, 16f)));
			Assert.That(vertical.GetComponent<Graphic>().raycastTarget, Is.False);
			Assert.That(horizontal.GetComponent<Graphic>().raycastTarget, Is.False);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_TableSpansTheTopAndBottomRightRoomViews()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.AddComponent<RoomPrototypeLevelOneController>();

		try
		{
			RectTransform[] visibleTables = root.GetComponentsInChildren<RectTransform>(true)
				.Where(rect => rect.name == "TABLE" && rect.gameObject.activeInHierarchy)
				.ToArray();

			Assert.That(visibleTables, Has.Length.EqualTo(2));
			Assert.That(visibleTables.Select(GetOwningPanelName), Is.EquivalentTo(new[]
			{
				"TopRight Panel",
				"BottomRight Panel"
			}));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_CreatesInputSystemEventModuleForPanelClicks()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.AddComponent<RoomPrototypeLevelOneController>();

		try
		{
			EventSystem eventSystem = root.GetComponentInChildren<EventSystem>(true);
			Assert.That(eventSystem, Is.Not.Null);

			InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
			Assert.That(inputModule, Is.Not.Null);
			Assert.That(inputModule.enabled, Is.True);
			Assert.That(inputModule.actionsAsset, Is.Not.Null);
			Assert.That(inputModule.point?.action, Is.Not.Null);
			Assert.That(inputModule.leftClick?.action, Is.Not.Null);

			BaseInputModule[] enabledNonInputSystemModules = eventSystem.GetComponents<BaseInputModule>()
				.Where(module => module.enabled && !(module is InputSystemUIInputModule))
				.ToArray();

			Assert.That(enabledNonInputSystemModules, Is.Empty);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static string GetOwningPanelName(RectTransform rectTransform)
	{
		Transform current = rectTransform.transform;
		while (current != null && !current.name.EndsWith(" Panel"))
		{
			current = current.parent;
		}

		return current == null ? string.Empty : current.name;
	}

	[Test]
	public void RoomPrototypeBackground_DoesNotContainFullSectorGuideLines()
	{
		string path = Path.Combine(Application.dataPath, "Project/ART/UI/интерьер_с_правильным_делением_на_8.png");
		Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

		try
		{
			Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), Is.True);

			int fullHeightRedColumns = 0;
			for (int x = 0; x < texture.width; x++)
			{
				int redPixels = 0;
				for (int y = 0; y < texture.height; y++)
				{
					if (IsSectorGuidePixel(texture.GetPixel(x, y)))
					{
						redPixels++;
					}
				}

				if (redPixels > texture.height * 0.85f)
				{
					fullHeightRedColumns++;
				}
			}

			int fullWidthRedRows = 0;
			for (int y = 0; y < texture.height; y++)
			{
				int redPixels = 0;
				for (int x = 0; x < texture.width; x++)
				{
					if (IsSectorGuidePixel(texture.GetPixel(x, y)))
					{
						redPixels++;
					}
				}

				if (redPixels > texture.width * 0.85f)
				{
					fullWidthRedRows++;
				}
			}

			Assert.That(fullHeightRedColumns, Is.EqualTo(0));
			Assert.That(fullWidthRedRows, Is.EqualTo(0));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(texture);
		}
	}

	private static bool IsSectorGuidePixel(Color pixel)
	{
		return pixel.r > 0.5f
			&& pixel.g < 0.45f
			&& pixel.b < 0.45f
			&& pixel.r > pixel.g + 0.15f
			&& pixel.r > pixel.b + 0.15f;
	}

	[Test]
	public void GetInitialViewport_ReturnsExpectedLevelOnePanels()
	{
		Assert.That(RoomPrototypeLevelOnePanelModel.GetInitialViewport(RoomPrototypePanelSlot.TopLeft), Is.EqualTo(new RoomPrototypeViewport(0, 0, 2, 2)));
		Assert.That(RoomPrototypeLevelOnePanelModel.GetInitialViewport(RoomPrototypePanelSlot.BottomLeft), Is.EqualTo(new RoomPrototypeViewport(1, 0, 2, 2)));
		Assert.That(RoomPrototypeLevelOnePanelModel.GetInitialViewport(RoomPrototypePanelSlot.TopRight), Is.EqualTo(new RoomPrototypeViewport(2, 0, 2, 2)));
		Assert.That(RoomPrototypeLevelOnePanelModel.GetInitialViewport(RoomPrototypePanelSlot.BottomRight), Is.EqualTo(new RoomPrototypeViewport(3, 1, 1, 1)));
	}

	[Test]
	public void TryNavigate_AllowsOnlyConfiguredPrototypeArrows()
	{
		RoomPrototypePanelState topLeftZoom = RoomPrototypeLevelOnePanelModel.GetZoomState(RoomPrototypePanelSlot.TopLeft);
		RoomPrototypePanelState bottomLeftZoom = RoomPrototypeLevelOnePanelModel.GetZoomState(RoomPrototypePanelSlot.BottomLeft);

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(topLeftZoom, RoomPrototypePanelDirection.Left, out RoomPrototypePanelState keyRackState), Is.True);
		Assert.That(keyRackState.Viewport, Is.EqualTo(new RoomPrototypeViewport(0, 0, 1, 1)));

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(bottomLeftZoom, RoomPrototypePanelDirection.Right, out RoomPrototypePanelState rightState), Is.True);
		Assert.That(rightState.Viewport, Is.EqualTo(new RoomPrototypeViewport(2, 0, 1, 1)));

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(topLeftZoom, RoomPrototypePanelDirection.Down, out _), Is.False);
		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(RoomPrototypeLevelOnePanelModel.GetInitialState(RoomPrototypePanelSlot.BottomRight), RoomPrototypePanelDirection.Left, out _), Is.False);
	}

	[Test]
	public void CanDropKeyIntoTruck_RequiresKeyAndTruckToBeZoomedToMatchingCells()
	{
		RoomPrototypePanelState keyState = new RoomPrototypePanelState(
			RoomPrototypePanelSlot.TopLeft,
			new RoomPrototypeViewport(0, 0, 1, 1),
			true,
			"KEY RACK"
		);
		RoomPrototypePanelState truckState = new RoomPrototypePanelState(
			RoomPrototypePanelSlot.BottomLeft,
			new RoomPrototypeViewport(1, 1, 1, 1),
			true,
			"TRUCK"
		);

		Assert.That(RoomPrototypeLevelOnePanelModel.CanDropKeyIntoTruck(keyState, truckState), Is.True);
		Assert.That(RoomPrototypeLevelOnePanelModel.CanDropKeyIntoTruck(
			RoomPrototypeLevelOnePanelModel.GetZoomState(RoomPrototypePanelSlot.TopLeft),
			truckState
		), Is.False);
		Assert.That(RoomPrototypeLevelOnePanelModel.CanDropKeyIntoTruck(
			keyState,
			RoomPrototypeLevelOnePanelModel.GetZoomState(RoomPrototypePanelSlot.BottomLeft)
		), Is.False);
	}

	[Test]
	public void TryMoveTruckToNextCell_MovesOnlyFromB2ToB3()
	{
		RoomPrototypePanelState truckInB2 = new RoomPrototypePanelState(
			RoomPrototypePanelSlot.BottomLeft,
			new RoomPrototypeViewport(1, 1, 1, 1),
			true,
			"TRUCK"
		);

		Assert.That(RoomPrototypeLevelOnePanelModel.TryMoveTruckToNextCell(truckInB2, out RoomPrototypePanelState truckInB3), Is.True);
		Assert.That(truckInB3.Viewport, Is.EqualTo(new RoomPrototypeViewport(2, 1, 1, 1)));
		Assert.That(RoomPrototypeLevelOnePanelModel.TryMoveTruckToNextCell(truckInB3, out _), Is.False);
	}

	[Test]
	public void TryMoveTruckToTable_MovesOnlyFromB3ToB4()
	{
		RoomPrototypePanelState truckInB3 = new RoomPrototypePanelState(
			RoomPrototypePanelSlot.BottomLeft,
			new RoomPrototypeViewport(2, 1, 1, 1),
			true,
			"TRUCK"
		);
		RoomPrototypePanelState truckInB2 = new RoomPrototypePanelState(
			RoomPrototypePanelSlot.BottomLeft,
			new RoomPrototypeViewport(1, 1, 1, 1),
			true,
			"TRUCK"
		);

		Assert.That(RoomPrototypeLevelOnePanelModel.TryMoveTruckToTable(truckInB3, out RoomPrototypePanelState truckAtTable), Is.True);
		Assert.That(truckAtTable.Slot, Is.EqualTo(RoomPrototypePanelSlot.BottomRight));
		Assert.That(truckAtTable.Viewport, Is.EqualTo(new RoomPrototypeViewport(3, 1, 1, 1)));
		Assert.That(RoomPrototypeLevelOnePanelModel.TryMoveTruckToTable(truckInB2, out _), Is.False);
	}

	[Test]
	public void CanDropAppleIntoTruck_RequiresTruckToReachB4()
	{
		RoomPrototypePanelState appleAtTable = RoomPrototypeLevelOnePanelModel.GetInitialState(RoomPrototypePanelSlot.BottomRight);
		RoomPrototypePanelState cageZoom = RoomPrototypeLevelOnePanelModel.GetZoomState(RoomPrototypePanelSlot.TopRight);
		RoomPrototypePanelState cageOverview = RoomPrototypeLevelOnePanelModel.GetInitialState(RoomPrototypePanelSlot.TopRight);
		RoomPrototypePanelState truckInB3 = new RoomPrototypePanelState(
			RoomPrototypePanelSlot.BottomLeft,
			new RoomPrototypeViewport(2, 1, 1, 1),
			true,
			"TRUCK"
		);
		RoomPrototypeLevelOnePanelModel.TryMoveTruckToTable(truckInB3, out RoomPrototypePanelState truckAtTable);

		Assert.That(RoomPrototypeLevelOnePanelModel.CanDropAppleIntoTruck(appleAtTable, truckAtTable, cageZoom), Is.True);
		Assert.That(RoomPrototypeLevelOnePanelModel.CanDropAppleIntoTruck(appleAtTable, truckAtTable, cageOverview), Is.False);
		Assert.That(RoomPrototypeLevelOnePanelModel.CanDropAppleIntoTruck(appleAtTable, truckInB3, cageZoom), Is.False);
	}

	[Test]
	public void GetAlignedHandoffX_PreservesTheRelativeHorizontalPositionBetweenPanels()
	{
		Assert.That(RoomPrototypeLevelOnePanelModel.GetAlignedHandoffX(-50f, 200f, 300f), Is.EqualTo(-75f));
		Assert.That(RoomPrototypeLevelOnePanelModel.GetAlignedHandoffX(0f, 200f, 300f), Is.EqualTo(0f));
		Assert.That(RoomPrototypeLevelOnePanelModel.GetAlignedHandoffX(50f, 200f, 300f), Is.EqualTo(75f));
	}

	[Test]
	public void ConfigureLevelMusic_UsesASeamlessTwoDimensionalLoop()
	{
		GameObject musicObject = new GameObject("Test Music");
		AudioSource musicSource = musicObject.AddComponent<AudioSource>();
		AudioClip musicClip = AudioClip.Create("Test Clip", 44100, 1, 44100, false);

		RoomPrototypeLevelOneController.ConfigureLevelMusic(musicSource, musicClip);

		Assert.That(musicSource.clip, Is.EqualTo(musicClip));
		Assert.That(musicSource.loop, Is.True);
		Assert.That(musicSource.spatialBlend, Is.EqualTo(0f));
		Assert.That(musicSource.playOnAwake, Is.False);
		Object.DestroyImmediate(musicObject);
		Object.DestroyImmediate(musicClip);
	}

	[Test]
	public void ConfigureUiSfxSource_UsesTwoDimensionalOneShotPlayback()
	{
		GameObject audioObject = new GameObject("Test UI SFX");
		AudioSource audioSource = audioObject.AddComponent<AudioSource>();
		audioSource.loop = true;
		audioSource.playOnAwake = true;
		audioSource.spatialBlend = 1f;

		AudioManager.ConfigureUiSfxSource(audioSource);

		Assert.That(audioSource.loop, Is.False);
		Assert.That(audioSource.playOnAwake, Is.False);
		Assert.That(audioSource.spatialBlend, Is.EqualTo(0f));
		Object.DestroyImmediate(audioObject);
	}

	[Test]
	public void ConfigureMainMenuButtonColors_DesaturatesBrushesBeforeApplyingButtonTint()
	{
		GameObject buttonObject = new GameObject("Test Menu Button", typeof(Image), typeof(Button));
		Button button = buttonObject.GetComponent<Button>();
		Image image = buttonObject.GetComponent<Image>();
		button.targetGraphic = image;
		button.transition = Selectable.Transition.ColorTint;
		image.color = Color.magenta;

		AudioManager.ConfigureMainMenuButtonColors(button);

		ColorBlock colors = button.colors;
		Assert.That(colors.normalColor, Is.EqualTo(new Color(0.55f, 0.58f, 0.64f, 1f)));
		Assert.That(colors.highlightedColor, Is.EqualTo(new Color(0.12f, 0.42f, 0.92f, 1f)));
		Assert.That(colors.pressedColor, Is.EqualTo(new Color(0.05f, 0.17f, 0.48f, 1f)));
		Assert.That(colors.selectedColor, Is.EqualTo(colors.normalColor));
		Assert.That(colors.disabledColor, Is.EqualTo(new Color(0.42f, 0.44f, 0.48f, 1f)));
		Assert.That(button.transition, Is.EqualTo(Selectable.Transition.ColorTint));
		Assert.That(image.color, Is.EqualTo(Color.white));
		Assert.That(image.material.shader.name, Is.EqualTo("UI/Grayscale Button Tint"));
		Object.DestroyImmediate(buttonObject);
	}

	[Test]
	public void TryNavigate_BottomLeftZoomMovesOneCellAtATimeInEveryValidDirection()
	{
		RoomPrototypePanelState topLeftState = RoomPrototypeLevelOnePanelModel.GetZoomState(RoomPrototypePanelSlot.BottomLeft);

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(topLeftState, RoomPrototypePanelDirection.Right, out RoomPrototypePanelState topRightState), Is.True);
		Assert.That(topRightState.Viewport, Is.EqualTo(new RoomPrototypeViewport(2, 0, 1, 1)));

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(topLeftState, RoomPrototypePanelDirection.Down, out RoomPrototypePanelState bottomLeftState), Is.True);
		Assert.That(bottomLeftState.Viewport, Is.EqualTo(new RoomPrototypeViewport(1, 1, 1, 1)));

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(topRightState, RoomPrototypePanelDirection.Left, out RoomPrototypePanelState topLeftFromRightState), Is.True);
		Assert.That(topLeftFromRightState.Viewport, Is.EqualTo(new RoomPrototypeViewport(1, 0, 1, 1)));
		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(topRightState, RoomPrototypePanelDirection.Down, out RoomPrototypePanelState bottomRightState), Is.True);
		Assert.That(bottomRightState.Viewport, Is.EqualTo(new RoomPrototypeViewport(2, 1, 1, 1)));

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(bottomLeftState, RoomPrototypePanelDirection.Up, out RoomPrototypePanelState topLeftFromBottomState), Is.True);
		Assert.That(topLeftFromBottomState.Viewport, Is.EqualTo(new RoomPrototypeViewport(1, 0, 1, 1)));
		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(bottomLeftState, RoomPrototypePanelDirection.Right, out RoomPrototypePanelState bottomRightFromLeftState), Is.True);
		Assert.That(bottomRightFromLeftState.Viewport, Is.EqualTo(new RoomPrototypeViewport(2, 1, 1, 1)));

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(bottomRightState, RoomPrototypePanelDirection.Left, out RoomPrototypePanelState bottomLeftFromRightState), Is.True);
		Assert.That(bottomLeftFromRightState.Viewport, Is.EqualTo(new RoomPrototypeViewport(1, 1, 1, 1)));
		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(bottomRightState, RoomPrototypePanelDirection.Up, out RoomPrototypePanelState topRightFromBottomState), Is.True);
		Assert.That(topRightFromBottomState.Viewport, Is.EqualTo(new RoomPrototypeViewport(2, 0, 1, 1)));
	}

	[Test]
	public void LevelTwoDisplacement_UsesAnOrthogonalFreeSlotInsteadOfADiagonal()
	{
		int displacedSlot = RoomPrototypeLevelTwoLayoutModel.ChooseDisplacementSlot(
			RoomPrototypeLevelTwoSlot.TopRight,
			RoomPrototypeLevelTwoSlot.BottomLeft,
			RoomPrototypeLevelTwoSlot.BottomRight
		);

		Assert.That(displacedSlot, Is.EqualTo(RoomPrototypeLevelTwoSlot.BottomRight));
		Assert.That(RoomPrototypeLevelTwoLayoutModel.AreOrthogonallyAdjacent(
			RoomPrototypeLevelTwoSlot.TopRight,
			displacedSlot
		), Is.True);
	}
}
