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
			0.5f,
			0.35f
		).ToArray();

		Assert.That(contour, Has.Member(new Vector2Int(1, 1)));
		Assert.That(contour, Has.Member(new Vector2Int(3, 2)));
		Assert.That(contour, Has.No.Member(new Vector2Int(2, 2)));
		Assert.That(contour, Has.Length.EqualTo(8));
	}

	[Test]
	public void FindContourPixels_FallsBackToLuminanceEdgesWhenAlphaIsOpaque()
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
			0.5f,
			0.35f
		).ToArray();

		Assert.That(contour, Has.Member(new Vector2Int(1, 1)));
		Assert.That(contour, Has.Member(new Vector2Int(0, 2)));
		Assert.That(contour, Has.No.Member(new Vector2Int(2, 2)));
		Assert.That(contour.Length, Is.GreaterThan(0));
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
			0.35f,
			1,
			1f
		);

		Assert.That(glowTexture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(0.001f));
		Assert.That(glowTexture.GetPixel(1, 3).a, Is.GreaterThan(0f));
		Assert.That(glowTexture.GetPixel(3, 3).a, Is.GreaterThan(0f));
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
