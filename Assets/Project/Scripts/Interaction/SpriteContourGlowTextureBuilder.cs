using System.Collections.Generic;
using UnityEngine;

public static class SpriteContourGlowTextureBuilder
{
	public static Texture2D BuildGlowTexture(
		Texture2D sourceTexture,
		Rect textureRect,
		Color glowColor,
		int sampleStepPixels,
		float alphaThreshold,
		int glowRadiusPixels,
		float intensity
	)
	{
		int sourceWidth = Mathf.Max(1, Mathf.RoundToInt(textureRect.width));
		int sourceHeight = Mathf.Max(1, Mathf.RoundToInt(textureRect.height));
		int radius = Mathf.Max(1, glowRadiusPixels);
		int width = sourceWidth + radius * 2;
		int height = sourceHeight + radius * 2;
		Texture2D glowTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
		{
			name = "Generated_CageContourGlow",
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp
		};

		float[] alphaMap = new float[width * height];
		List<Vector2Int> contourPixels = SpriteContourSampler.FindContourPixels(
			sourceTexture,
			textureRect,
			sampleStepPixels,
			alphaThreshold
		);

		int xOffset = Mathf.RoundToInt(textureRect.xMin);
		int yOffset = Mathf.RoundToInt(textureRect.yMin);
		float clampedIntensity = Mathf.Clamp01(intensity);

		for (int i = 0; i < contourPixels.Count; i++)
		{
			Vector2Int sourcePixel = contourPixels[i];
			int centerX = sourcePixel.x - xOffset + radius;
			int centerY = sourcePixel.y - yOffset + radius;
			PaintGlowBlob(alphaMap, width, height, centerX, centerY, radius, clampedIntensity);
		}

		Color32[] pixels = new Color32[width * height];
		byte r = FloatToByte(glowColor.r);
		byte g = FloatToByte(glowColor.g);
		byte b = FloatToByte(glowColor.b);

		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = new Color32(r, g, b, FloatToByte(alphaMap[i] * glowColor.a));
		}

		glowTexture.SetPixels32(pixels);
		glowTexture.Apply(false, false);

		return glowTexture;
	}

	private static void PaintGlowBlob(float[] alphaMap, int width, int height, int centerX, int centerY, int radius, float intensity)
	{
		int xMin = Mathf.Max(0, centerX - radius);
		int xMax = Mathf.Min(width - 1, centerX + radius);
		int yMin = Mathf.Max(0, centerY - radius);
		int yMax = Mathf.Min(height - 1, centerY + radius);
		float softRadius = radius + 1f;

		for (int y = yMin; y <= yMax; y++)
		{
			for (int x = xMin; x <= xMax; x++)
			{
				float distance = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(x, y));

				if (distance > radius)
					continue;

				float falloff = 1f - distance / softRadius;
				float alpha = falloff * falloff * intensity;
				int index = y * width + x;
				alphaMap[index] = Mathf.Max(alphaMap[index], alpha);
			}
		}
	}

	private static byte FloatToByte(float value)
	{
		return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
	}
}
