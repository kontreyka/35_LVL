using System.Collections.Generic;
using UnityEngine;

public static class SpriteContourSampler
{
	public static List<Vector2Int> FindContourPixels(
		Texture2D texture,
		Rect textureRect,
		int sampleStepPixels,
		float alphaThreshold,
		float luminanceEdgeThreshold
	)
	{
		List<Vector2Int> contourPixels = new List<Vector2Int>();

		if (texture == null)
			return contourPixels;

		int step = Mathf.Max(1, sampleStepPixels);
		int xMin = Mathf.Clamp(Mathf.FloorToInt(textureRect.xMin), 0, texture.width - 1);
		int yMin = Mathf.Clamp(Mathf.FloorToInt(textureRect.yMin), 0, texture.height - 1);
		int xMax = Mathf.Clamp(Mathf.CeilToInt(textureRect.xMax) - 1, 0, texture.width - 1);
		int yMax = Mathf.Clamp(Mathf.CeilToInt(textureRect.yMax) - 1, 0, texture.height - 1);
		Color32[] pixels = texture.GetPixels32();
		byte alphaLimit = FloatToByte(alphaThreshold);
		bool hasTransparentPixels = HasTransparentPixels(pixels, texture.width, xMin, yMin, xMax, yMax, step, alphaLimit);

		for (int y = yMin; y <= yMax; y += step)
		{
			for (int x = xMin; x <= xMax; x += step)
			{
				if (hasTransparentPixels)
				{
					if (IsAlphaContourPixel(pixels, texture.width, texture.height, x, y, alphaLimit))
					{
						contourPixels.Add(new Vector2Int(x, y));
					}
				}
				else if (IsLuminanceContourPixel(pixels, texture.width, texture.height, x, y, luminanceEdgeThreshold))
				{
					contourPixels.Add(new Vector2Int(x, y));
				}
			}
		}

		return contourPixels;
	}

	private static bool HasTransparentPixels(
		Color32[] pixels,
		int width,
		int xMin,
		int yMin,
		int xMax,
		int yMax,
		int step,
		byte alphaLimit
	)
	{
		for (int y = yMin; y <= yMax; y += step)
		{
			for (int x = xMin; x <= xMax; x += step)
			{
				if (pixels[ToIndex(x, y, width)].a < alphaLimit)
					return true;
			}
		}

		return false;
	}

	private static bool IsAlphaContourPixel(Color32[] pixels, int width, int height, int x, int y, byte alphaLimit)
	{
		if (pixels[ToIndex(x, y, width)].a < alphaLimit)
			return false;

		for (int oy = -1; oy <= 1; oy++)
		{
			for (int ox = -1; ox <= 1; ox++)
			{
				if (ox == 0 && oy == 0)
					continue;

				int nx = x + ox;
				int ny = y + oy;

				if (nx < 0 || nx >= width || ny < 0 || ny >= height)
					return true;

				if (pixels[ToIndex(nx, ny, width)].a < alphaLimit)
					return true;
			}
		}

		return false;
	}

	private static bool IsLuminanceContourPixel(
		Color32[] pixels,
		int width,
		int height,
		int x,
		int y,
		float luminanceEdgeThreshold
	)
	{
		float center = GetLuminance(pixels[ToIndex(x, y, width)]);
		float strongestDifference = 0f;

		for (int oy = -1; oy <= 1; oy++)
		{
			for (int ox = -1; ox <= 1; ox++)
			{
				if (ox == 0 && oy == 0)
					continue;

				int nx = Mathf.Clamp(x + ox, 0, width - 1);
				int ny = Mathf.Clamp(y + oy, 0, height - 1);
				float neighbor = GetLuminance(pixels[ToIndex(nx, ny, width)]);
				strongestDifference = Mathf.Max(strongestDifference, Mathf.Abs(center - neighbor));
			}
		}

		return strongestDifference >= luminanceEdgeThreshold;
	}

	private static float GetLuminance(Color32 color)
	{
		return (0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b) / 255f;
	}

	private static byte FloatToByte(float value)
	{
		return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
	}

	private static int ToIndex(int x, int y, int width)
	{
		return y * width + x;
	}
}
