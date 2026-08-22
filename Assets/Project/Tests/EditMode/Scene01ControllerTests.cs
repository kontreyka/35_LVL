using System.Linq;
using NUnit.Framework;
using UnityEngine;

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
	public void ShouldFadeCageAuraForClickStepIndex_FadesOnlyOnFirstClick()
	{
		Assert.That(Scene01Controller.ShouldFadeCageAuraForClickStepIndex(0), Is.True);
		Assert.That(Scene01Controller.ShouldFadeCageAuraForClickStepIndex(1), Is.False);
		Assert.That(Scene01Controller.ShouldFadeCageAuraForClickStepIndex(2), Is.False);
		Assert.That(Scene01Controller.ShouldFadeCageAuraForClickStepIndex(3), Is.False);
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
	public void BuiltInFontResourceName_UsesUnity6000RuntimeFont()
	{
		Assert.That(RoomPrototypeLevelOneController.BuiltInFontResourceName, Is.EqualTo("LegacyRuntime.ttf"));
	}

	[Test]
	public void GetInitialViewport_ReturnsExpectedLevelOnePanels()
	{
		Assert.That(RoomPrototypeLevelOnePanelModel.GetInitialViewport(RoomPrototypePanelSlot.TopLeft), Is.EqualTo(new RoomPrototypeViewport(1, 0, 2, 2)));
		Assert.That(RoomPrototypeLevelOnePanelModel.GetInitialViewport(RoomPrototypePanelSlot.BottomLeft), Is.EqualTo(new RoomPrototypeViewport(0, 0, 2, 2)));
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

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(bottomLeftZoom, RoomPrototypePanelDirection.Right, out RoomPrototypePanelState truckState), Is.True);
		Assert.That(truckState.Viewport, Is.EqualTo(new RoomPrototypeViewport(1, 1, 1, 1)));

		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(topLeftZoom, RoomPrototypePanelDirection.Down, out _), Is.False);
		Assert.That(RoomPrototypeLevelOnePanelModel.TryNavigate(RoomPrototypeLevelOnePanelModel.GetInitialState(RoomPrototypePanelSlot.BottomRight), RoomPrototypePanelDirection.Left, out _), Is.False);
	}
}
