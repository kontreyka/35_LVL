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
	public void LevelThreeFlow_WiresEyeThreeToTheRoomThenTheFinalScene()
	{
		string eyeThreeScene = File.ReadAllText(Path.Combine(Application.dataPath, "Scenes/Eye_3.unity"));
		string levelThreeScene = File.ReadAllText(Path.Combine(Application.dataPath, "Scenes/RoomPrototype_Level03.unity"));
		string buildSettings = File.ReadAllText(Path.Combine(Application.dataPath, "../ProjectSettings/EditorBuildSettings.asset"));

		Assert.That(eyeThreeScene, Does.Contain("scenePath: Assets/Scenes/RoomPrototype_Level03.unity"));
		Assert.That(levelThreeScene, Does.Contain("scenePath: Assets/Scenes/Last_Scene.unity"));
		Assert.That(buildSettings, Does.Contain("path: Assets/Scenes/RoomPrototype_Level03.unity"));
		Assert.That(buildSettings, Does.Contain("path: Assets/Scenes/Last_Scene.unity"));
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
	public void LevelThreeBird_MirrorsOnlyWhileFlyingRight()
	{
		Assert.That(RoomPrototypeLevelThreePuzzleModel.GetSkyBirdHorizontalScale(true), Is.EqualTo(-1f));
		Assert.That(RoomPrototypeLevelThreePuzzleModel.GetSkyBirdHorizontalScale(false), Is.EqualTo(1f));
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
	public void LevelThreeFlowerPull_MovesAConstantSizePlantInsteadOfStretchingIt()
	{
		Vector2 originalBottom = new Vector2(83f, -120f);
		Vector2 plantSize = new Vector2(32f, 160f);
		Vector2 start = RoomPrototypeLevelThreePuzzleModel.GetPlantPullStartTip(originalBottom, plantSize);
		Vector2 targetTip = new Vector2(start.x, 700f);

		Rect startLayout = RoomPrototypeLevelThreePuzzleModel.GetPlantPullLayout(start, targetTip, plantSize, 0f);
		Rect halfwayLayout = RoomPrototypeLevelThreePuzzleModel.GetPlantPullLayout(start, targetTip, plantSize, 0.5f);
		Rect completedLayout = RoomPrototypeLevelThreePuzzleModel.GetPlantPullLayout(start, targetTip, plantSize, 1f);

		Assert.That(startLayout.position, Is.EqualTo(originalBottom));
		Assert.That(startLayout.size, Is.EqualTo(plantSize));
		Assert.That(halfwayLayout.size, Is.EqualTo(plantSize));
		Assert.That(completedLayout.size, Is.EqualTo(plantSize));
		Assert.That(halfwayLayout.x, Is.EqualTo(originalBottom.x).Within(0.001f));
		Assert.That(completedLayout.x, Is.EqualTo(originalBottom.x).Within(0.001f));
	}

	[Test]
	public void LevelThreePlantDisplaySize_ScalesTheOriginalSpriteProportionally()
	{
		Vector2 size = RoomPrototypeLevelThreePuzzleModel.GetPlantDisplaySize(
			new Vector2(80f, 200f),
			0.14f,
			2f
		);

		Assert.That(size.x, Is.EqualTo(56f).Within(0.001f));
		Assert.That(size.y, Is.EqualTo(400f).Within(0.001f));
	}

	[Test]
	public void LevelThreeFlowerPull_PlacesKeyOnTableWhilePlantTipFinishesAboveIt()
	{
		float tableSurfaceY = 620f;
		float plantTipY = RoomPrototypeLevelThreePuzzleModel.GetPlantTipTargetY(tableSurfaceY, 28f);

		Assert.That(plantTipY, Is.EqualTo(648f).Within(0.001f));
	}

	[Test]
	public void LevelThreeFlowerPull_MaskStartsAtThePotThroat()
	{
		Rect mask = RoomPrototypeLevelThreePuzzleModel.GetPlantPullMaskLayout(
			new Vector2(100f, 50f),
			400f,
			800f
		);

		Assert.That(mask.xMin, Is.EqualTo(-300f).Within(0.001f));
		Assert.That(mask.xMax, Is.EqualTo(500f).Within(0.001f));
		Assert.That(mask.yMin, Is.EqualTo(50f).Within(0.001f));
		Assert.That(mask.yMax, Is.EqualTo(400f).Within(0.001f));
	}

	[Test]
	public void LevelThreeFlowerPull_KeyKeepsItsOffsetFromThePlantTipUntilCompletion()
	{
		Vector2 offset = new Vector2(26f, -8f);

		Assert.That(
			RoomPrototypeLevelThreePuzzleModel.GetKeyPositionOnPlant(new Vector2(100f, 200f), offset),
			Is.EqualTo(new Vector2(126f, 192f))
		);
		Assert.That(
			RoomPrototypeLevelThreePuzzleModel.GetKeyPositionOnPlant(new Vector2(100f, 700f), offset),
			Is.EqualTo(new Vector2(126f, 692f))
		);
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
	public void BuiltPrototype_UsesInspectorConfiguredTablePosition()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.SetActive(false);
		RoomPrototypeLevelOneController controller = root.AddComponent<RoomPrototypeLevelOneController>();
		System.Reflection.FieldInfo tablePosition = typeof(RoomPrototypeLevelOneController).GetField(
			"tableRoomPosition",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);

		try
		{
			Assert.That(tablePosition, Is.Not.Null, "The table position must be editable in the Inspector.");
			tablePosition.SetValue(controller, new Vector2(1.3f, 0.5f));
			root.SetActive(true);

			RectTransform[] visibleTables = root.GetComponentsInChildren<RectTransform>(true)
				.Where(rect => rect.name == "TABLE" && rect.gameObject.activeInHierarchy)
				.ToArray();

			Assert.That(visibleTables.Select(GetOwningPanelName), Is.EquivalentTo(new[]
			{
				"TopLeft Panel",
				"BottomLeft Panel"
			}));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_UsesInspectorConfiguredApplePosition()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.SetActive(false);
		RoomPrototypeLevelOneController controller = root.AddComponent<RoomPrototypeLevelOneController>();
		System.Reflection.FieldInfo applePosition = typeof(RoomPrototypeLevelOneController).GetField(
			"appleRoomPosition",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);

		try
		{
			Assert.That(applePosition, Is.Not.Null, "The apple position must be editable in the Inspector.");
			applePosition.SetValue(controller, new Vector2(3.25f, 1.25f));
			root.SetActive(true);

			RectTransform apple = root.GetComponentsInChildren<RectTransform>(true)
				.Single(rect => rect.name == "APPLE" && rect.gameObject.activeInHierarchy);
			Vector2 panelSize = apple.parent.GetComponent<RectTransform>().rect.size;
			Vector2 expectedPosition = new Vector2(-panelSize.x * 0.25f, panelSize.y * 0.25f);

			Assert.That(apple.anchoredPosition.x, Is.EqualTo(expectedPosition.x).Within(0.01f));
			Assert.That(apple.anchoredPosition.y, Is.EqualTo(expectedPosition.y).Within(0.01f));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_UsesInspectorAssignedKeyRackAndAppleSprites()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.SetActive(false);
		RoomPrototypeLevelOneController controller = root.AddComponent<RoomPrototypeLevelOneController>();
		System.Reflection.FieldInfo keyRackSpriteField = typeof(RoomPrototypeLevelOneController).GetField(
			"keyRackSprite",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);
		System.Reflection.FieldInfo appleSpriteField = typeof(RoomPrototypeLevelOneController).GetField(
			"appleSprite",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);
		Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
		Sprite keyRackSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
		Sprite appleSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));

		try
		{
			Assert.That(keyRackSpriteField, Is.Not.Null, "The key rack sprite must be assignable in the Inspector.");
			Assert.That(appleSpriteField, Is.Not.Null, "The apple sprite must be assignable in the Inspector.");
			keyRackSpriteField.SetValue(controller, keyRackSprite);
			appleSpriteField.SetValue(controller, appleSprite);
			root.SetActive(true);

			Image keyRack = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "KEY RACK" && image.gameObject.activeInHierarchy);
			Image apple = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "APPLE" && image.gameObject.activeInHierarchy && GetOwningPanelName(image.rectTransform) == "BottomRight Panel");

			Assert.That(keyRack.sprite, Is.SameAs(keyRackSprite));
			Assert.That(apple.sprite, Is.SameAs(appleSprite));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
			UnityEngine.Object.DestroyImmediate(keyRackSprite);
			UnityEngine.Object.DestroyImmediate(appleSprite);
			UnityEngine.Object.DestroyImmediate(texture);
		}
	}

	[Test]
	public void LevelThreePrototype_UsesMaskedPlantBehindInspectorAssignedPot()
	{
		GameObject root = new GameObject("Level Three Plant Test Root");
		root.SetActive(false);
		RoomPrototypeLevelThreeController controller = root.AddComponent<RoomPrototypeLevelThreeController>();
		System.Reflection.FieldInfo potSpriteField = typeof(RoomPrototypeLevelThreeController).GetField(
			"flowerPotSprite",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);
		System.Reflection.FieldInfo plantSpriteField = typeof(RoomPrototypeLevelThreeController).GetField(
			"plantSprite",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);
		Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
		Sprite potSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
		Sprite plantSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));

		try
		{
			Assert.That(potSpriteField, Is.Not.Null, "The flower pot sprite must be assignable in the Inspector.");
			Assert.That(plantSpriteField, Is.Not.Null, "The plant sprite must be assignable in the Inspector.");
			potSpriteField.SetValue(controller, potSprite);
			plantSpriteField.SetValue(controller, plantSprite);
			root.SetActive(true);

			Image pot = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "Flower Pot" && image.gameObject.activeInHierarchy);
			Image plant = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "Plant" && image.gameObject.activeInHierarchy);

			Assert.That(pot.sprite, Is.SameAs(potSprite));
			Assert.That(plant.sprite, Is.SameAs(plantSprite));
			RectMask2D plantMask = plant.GetComponentInParent<RectMask2D>();
			Assert.That(plantMask, Is.Not.Null);
			Assert.That(plantMask.transform.parent, Is.SameAs(pot.transform.parent));
			Assert.That(plantMask.transform.GetSiblingIndex(), Is.LessThan(pot.transform.GetSiblingIndex()));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
			UnityEngine.Object.DestroyImmediate(potSprite);
			UnityEngine.Object.DestroyImmediate(plantSprite);
			UnityEngine.Object.DestroyImmediate(texture);
		}
	}

	[Test]
	public void LevelThreePrototype_UsesAssignedSkyBirdSpriteWithoutTextLabel()
	{
		GameObject root = new GameObject("Level Three Sky Bird Test Root");
		root.SetActive(false);
		RoomPrototypeLevelThreeController controller = root.AddComponent<RoomPrototypeLevelThreeController>();
		System.Reflection.FieldInfo skyBirdSpriteField = typeof(RoomPrototypeLevelThreeController).GetField(
			"skyBirdSprite",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);
		Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
		Sprite birdSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));

		try
		{
			Assert.That(skyBirdSpriteField, Is.Not.Null, "The sky bird sprite must be assignable in the Inspector.");
			skyBirdSpriteField.SetValue(controller, birdSprite);
			root.SetActive(true);

			Image bird = root.GetComponentsInChildren<Image>(true).Single(image => image.name == "Bird");
			Assert.That(bird.sprite, Is.SameAs(birdSprite));
			Assert.That(bird.GetComponentsInChildren<Text>(true), Is.Empty);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
			UnityEngine.Object.DestroyImmediate(birdSprite);
			UnityEngine.Object.DestroyImmediate(texture);
		}
	}

	[Test]
	public void LevelTwoPrototype_UsesInspectorAssignedClockAtItsExistingWorldPosition()
	{
		GameObject root = new GameObject("Level Two Clock Test Root");
		root.SetActive(false);
		RoomPrototypeLevelTwoController controller = root.AddComponent<RoomPrototypeLevelTwoController>();
		System.Reflection.FieldInfo clockSpriteField = typeof(RoomPrototypeLevelTwoController).GetField(
			"clockSprite",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);
		Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
		Sprite clockSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));

		try
		{
			Assert.That(clockSpriteField, Is.Not.Null, "The clock sprite must be assignable in the Inspector.");
			clockSpriteField.SetValue(controller, clockSprite);
			root.SetActive(true);

			Image clock = root.GetComponentsInChildren<Image>(true).Single(image => image.name == "Clock");
			Assert.That(clock.sprite, Is.SameAs(clockSprite));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
			UnityEngine.Object.DestroyImmediate(clockSprite);
			UnityEngine.Object.DestroyImmediate(texture);
		}
	}

	[Test]
	public void LevelThreePrototype_ExposesPlantMaskAndPlantOffsetsInInspector()
	{
		string[] requiredFields = { "plantMaskWorldOffset", "plantLocalOffset" };

		foreach (string fieldName in requiredFields)
		{
			System.Reflection.FieldInfo field = typeof(RoomPrototypeLevelThreeController).GetField(
				fieldName,
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
			);
			Assert.That(field, Is.Not.Null, $"The {fieldName} must be editable in the Inspector.");
		}
	}

	[Test]
	public void LevelThreeFlowerPull_KeepsTheMovingPlantBehindThePot()
	{
		GameObject root = new GameObject("Level Three Pull Layer Test Root");
		root.AddComponent<RoomPrototypeLevelThreeController>();

		try
		{
			Image movingPlant = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "Growing Plant");
			Image foregroundPot = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "Pulled Flower Pot");

			RectMask2D pullMask = movingPlant.GetComponentInParent<RectMask2D>();
			Assert.That(pullMask, Is.Not.Null);
			Assert.That(pullMask.transform.parent, Is.SameAs(foregroundPot.transform.parent));
			Assert.That(pullMask.transform.GetSiblingIndex(), Is.LessThan(foregroundPot.transform.GetSiblingIndex()));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_ShowsAppleInBothRightViewsButOnlyTheBottomRightOneCanBeClicked()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.AddComponent<RoomPrototypeLevelOneController>();

		try
		{
			RectTransform[] visibleApples = root.GetComponentsInChildren<RectTransform>(true)
				.Where(rect => rect.name == "APPLE" && rect.gameObject.activeInHierarchy)
				.ToArray();

			Assert.That(visibleApples.Select(GetOwningPanelName), Is.EquivalentTo(new[]
			{
				"TopRight Panel",
				"BottomRight Panel"
			}));

			RectTransform topRightApple = visibleApples.Single(rect => GetOwningPanelName(rect) == "TopRight Panel");
			Button topRightButton = topRightApple.GetComponent<Button>();
			Assert.That(topRightButton.interactable, Is.False);
			Assert.That(topRightApple.GetComponent<Image>().raycastTarget, Is.False);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_ShowsCarInBothLeftViews()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.AddComponent<RoomPrototypeLevelOneController>();

		try
		{
			RectTransform[] visibleTrucks = root.GetComponentsInChildren<RectTransform>(true)
				.Where(rect => rect.name == "TRUCK" && rect.gameObject.activeInHierarchy)
				.ToArray();

			Assert.That(visibleTrucks.Select(GetOwningPanelName), Is.EquivalentTo(new[]
			{
				"TopLeft Panel",
				"BottomLeft Panel"
			}));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	[TestCase(typeof(RoomPrototypeLevelTwoController))]
	[TestCase(typeof(RoomPrototypeLevelThreeController))]
	public void RoomPrototypeControllers_ExposeTableAndCageLayoutInInspector(System.Type controllerType)
	{
		string[] requiredFields =
		{
			"tableSprite",
			"birdSprite",
			"cageSprite",
			"tableWorldPosition",
			"tableWorldSize",
			"birdWorldPosition",
			"birdWorldSize",
			"cageWorldPosition",
			"cageWorldSize"
		};

		foreach (string fieldName in requiredFields)
		{
			System.Reflection.FieldInfo field = controllerType.GetField(
				fieldName,
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
			);
			Assert.That(field, Is.Not.Null, $"{controllerType.Name} must expose {fieldName} in the Inspector.");
		}
	}

	[Test]
	[TestCase(typeof(RoomPrototypeLevelTwoController))]
	[TestCase(typeof(RoomPrototypeLevelThreeController))]
	public void RemainingPrototypeControllers_ProvideInspectorPreviewForInteractiveLayout(System.Type controllerType)
	{
		Assert.That(
			controllerType.GetCustomAttributes(typeof(ExecuteAlways), true),
			Is.Not.Empty,
			$"{controllerType.Name} must provide the same edit-mode preview as level one."
		);

		Assert.That(
			controllerType.GetField("EditorPreviewRootName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic),
			Is.Not.Null,
			$"{controllerType.Name} must keep its generated preview separate from the scene."
		);
	}

	[Test]
	[TestCase(typeof(RoomPrototypeLevelOneController))]
	[TestCase(typeof(RoomPrototypeLevelTwoController))]
	[TestCase(typeof(RoomPrototypeLevelThreeController))]
	public void PrototypeControllers_ExposeInteractionAndZoomSoundsInInspector(System.Type controllerType)
	{
		string[] requiredFields = { "interactionClickSound", "zoomSound", "sfxVolume", "interactionClickGainDb", "zoomGainDb" };
		foreach (string fieldName in requiredFields)
		{
			Assert.That(
				controllerType.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
				Is.Not.Null,
				$"{controllerType.Name} must expose {fieldName} for prototype sound design."
			);
		}
	}

	[Test]
	public void BuiltPrototype_DrawsInspectorAssignedBirdBehindCage()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.SetActive(false);
		RoomPrototypeLevelOneController controller = root.AddComponent<RoomPrototypeLevelOneController>();
		System.Reflection.FieldInfo birdSpriteField = typeof(RoomPrototypeLevelOneController).GetField(
			"birdSprite",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);
		Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
		Sprite birdSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));

		try
		{
			Assert.That(birdSpriteField, Is.Not.Null, "The bird sprite must be assignable in the Inspector.");
			birdSpriteField.SetValue(controller, birdSprite);
			root.SetActive(true);

			Image bird = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "BIRD" && GetOwningPanelName(image.rectTransform) == "TopRight Panel");
			Image cage = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "CAGE" && GetOwningPanelName(image.rectTransform) == "TopRight Panel");

			Assert.That(bird.sprite, Is.SameAs(birdSprite));
			Assert.That(bird.rectTransform.GetSiblingIndex(), Is.LessThan(cage.rectTransform.GetSiblingIndex()));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(birdSprite);
			UnityEngine.Object.DestroyImmediate(texture);
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_DoesNotTintDisabledTruckSprite()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.AddComponent<RoomPrototypeLevelOneController>();

		try
		{
			Image truck = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "TRUCK" && GetOwningPanelName(image.rectTransform) == "BottomLeft Panel");
			Button truckButton = truck.GetComponent<Button>();

			Assert.That(truckButton, Is.Not.Null);
			Assert.That(truckButton.transition, Is.EqualTo(Selectable.Transition.None));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_UsesTruckInsteadOfRopeToMoveItToTheTable()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.AddComponent<RoomPrototypeLevelOneController>();

		try
		{
			Assert.That(root.GetComponentsInChildren<RectTransform>(true).Any(rect => rect.name == "ROPE"), Is.False);
			Image truck = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "TRUCK" && GetOwningPanelName(image.rectTransform) == "BottomLeft Panel");
			Assert.That(truck.GetComponent<Button>(), Is.Not.Null);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_KeepsTableTruckVisibleWhileItTilts()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.SetActive(false);
		RoomPrototypeLevelOneController controller = root.AddComponent<RoomPrototypeLevelOneController>();
		System.Reflection.FieldInfo truckReachedTable = typeof(RoomPrototypeLevelOneController).GetField(
			"truckReachedTable",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);
		System.Reflection.FieldInfo truckIsTipping = typeof(RoomPrototypeLevelOneController).GetField(
			"truckIsTipping",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);

		try
		{
			Assert.That(truckReachedTable, Is.Not.Null);
			Assert.That(truckIsTipping, Is.Not.Null);
			truckReachedTable.SetValue(controller, true);
			truckIsTipping.SetValue(controller, true);
			root.SetActive(true);

			RectTransform truck = root.GetComponentsInChildren<RectTransform>(true)
				.Single(rect => rect.name == "TRUCK" && GetOwningPanelName(rect) == "BottomRight Panel");
			Assert.That(truck.gameObject.activeInHierarchy, Is.True);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void TruckTiltAngle_IsClockwise()
	{
		System.Reflection.FieldInfo truckTiltAngle = typeof(RoomPrototypeLevelOneController).GetField(
			"TruckTiltAngle",
			System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
		);

		Assert.That(truckTiltAngle, Is.Not.Null, "The truck tilt direction must be an explicit gameplay setting.");
		Assert.That((float)truckTiltAngle.GetRawConstantValue(), Is.LessThan(0f));
	}

	[Test]
	public void TippedTruckPivot_KeepsItsVisiblePosition()
	{
		System.Reflection.MethodInfo getAnchoredPosition = typeof(RoomPrototypeLevelOneController).GetMethod(
			"GetAnchoredPositionKeepingVisualPosition",
			System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
		);

		Assert.That(getAnchoredPosition, Is.Not.Null, "Changing the truck pivot must preserve its visual position.");
		Vector2 result = (Vector2)getAnchoredPosition.Invoke(null, new object[]
		{
			new Vector2(120f, -35f),
			new Vector2(200f, 80f),
			new Vector2(0.5f, 0.5f),
			new Vector2(0.22f, 0.2f)
		});

		Assert.That(result.x, Is.EqualTo(64f));
		Assert.That(result.y, Is.EqualTo(-59f));
	}

	[Test]
	public void BuiltPrototype_DrawsTruckAboveTableWhenItHasReachedTheTable()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.SetActive(false);
		RoomPrototypeLevelOneController controller = root.AddComponent<RoomPrototypeLevelOneController>();
		System.Reflection.FieldInfo truckReachedTable = typeof(RoomPrototypeLevelOneController).GetField(
			"truckReachedTable",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);

		try
		{
			Assert.That(truckReachedTable, Is.Not.Null);
			truckReachedTable.SetValue(controller, true);
			root.SetActive(true);

			RectTransform table = root.GetComponentsInChildren<RectTransform>(true)
				.Single(rect => rect.name == "TABLE" && GetOwningPanelName(rect) == "BottomRight Panel");
			RectTransform truck = root.GetComponentsInChildren<RectTransform>(true)
				.Single(rect => rect.name == "TRUCK" && rect.gameObject.activeInHierarchy && GetOwningPanelName(rect) == "BottomRight Panel");

			Assert.That(truck.GetSiblingIndex(), Is.GreaterThan(table.GetSiblingIndex()));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BuiltPrototype_UsesInspectorAssignedCarSpriteForTruck()
	{
		GameObject root = new GameObject("Room Prototype Test Root");
		root.SetActive(false);
		RoomPrototypeLevelOneController controller = root.AddComponent<RoomPrototypeLevelOneController>();
		System.Reflection.FieldInfo carSpriteField = typeof(RoomPrototypeLevelOneController).GetField(
			"carSprite",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		);
		Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
		Sprite carSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));

		try
		{
			Assert.That(carSpriteField, Is.Not.Null, "The car sprite must be assignable in the Inspector.");
			carSpriteField.SetValue(controller, carSprite);
			root.SetActive(true);

			Image truck = root.GetComponentsInChildren<Image>(true)
				.Single(image => image.name == "TRUCK" && image.gameObject.activeInHierarchy);
			Assert.That(truck.sprite, Is.SameAs(carSprite));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
			UnityEngine.Object.DestroyImmediate(carSprite);
			UnityEngine.Object.DestroyImmediate(texture);
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
