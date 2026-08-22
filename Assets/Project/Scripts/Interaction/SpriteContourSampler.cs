using System.Collections.Generic;
using UnityEngine;

public static class SpriteContourSampler
{
	public static List<Vector2Int> FindContourPixels(
		Texture2D texture,
		Rect textureRect,
		int sampleStepPixels,
		float alphaThreshold
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
		bool hasTransparentPixels = HasTransparentPixels(pixels, texture.width, xMin, yMin, xMax, yMax, alphaLimit);

		if (!hasTransparentPixels)
		{
			AddPerimeterPixels(contourPixels, xMin, yMin, xMax, yMax, step);
			return contourPixels;
		}

		AddOuterSilhouettePixels(contourPixels, pixels, texture.width, xMin, yMin, xMax, yMax, step, alphaLimit);

		return contourPixels;
	}

	private static bool HasTransparentPixels(
		Color32[] pixels,
		int width,
		int xMin,
		int yMin,
		int xMax,
		int yMax,
		byte alphaLimit
	)
	{
		for (int y = yMin; y <= yMax; y++)
		{
			for (int x = xMin; x <= xMax; x++)
			{
				if (pixels[ToIndex(x, y, width)].a < alphaLimit)
					return true;
			}
		}

		return false;
	}

	private static void AddOuterSilhouettePixels(
		List<Vector2Int> contourPixels,
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
		HashSet<Vector2Int> uniquePixels = new HashSet<Vector2Int>();

		for (int y = yMin; y <= yMax; y += step)
		{
			AddRowExtremes(contourPixels, uniquePixels, pixels, width, xMin, xMax, y, alphaLimit);
		}

		if ((yMax - yMin) % step != 0)
		{
			AddRowExtremes(contourPixels, uniquePixels, pixels, width, xMin, xMax, yMax, alphaLimit);
		}

		for (int x = xMin; x <= xMax; x += step)
		{
			AddColumnExtremes(contourPixels, uniquePixels, pixels, width, x, yMin, yMax, alphaLimit);
		}

		if ((xMax - xMin) % step != 0)
		{
			AddColumnExtremes(contourPixels, uniquePixels, pixels, width, xMax, yMin, yMax, alphaLimit);
		}
	}

	private static void AddRowExtremes(
		List<Vector2Int> contourPixels,
		HashSet<Vector2Int> uniquePixels,
		Color32[] pixels,
		int width,
		int xMin,
		int xMax,
		int y,
		byte alphaLimit
	)
	{
		int left = -1;
		int right = -1;

		for (int x = xMin; x <= xMax; x++)
		{
			if (IsOpaque(pixels, width, x, y, alphaLimit))
			{
				left = x;
				break;
			}
		}

		for (int x = xMax; x >= xMin; x--)
		{
			if (IsOpaque(pixels, width, x, y, alphaLimit))
			{
				right = x;
				break;
			}
		}

		AddUniquePixel(contourPixels, uniquePixels, left, y);
		AddUniquePixel(contourPixels, uniquePixels, right, y);
	}

	private static void AddColumnExtremes(
		List<Vector2Int> contourPixels,
		HashSet<Vector2Int> uniquePixels,
		Color32[] pixels,
		int width,
		int x,
		int yMin,
		int yMax,
		byte alphaLimit
	)
	{
		int bottom = -1;
		int top = -1;

		for (int y = yMin; y <= yMax; y++)
		{
			if (IsOpaque(pixels, width, x, y, alphaLimit))
			{
				bottom = y;
				break;
			}
		}

		for (int y = yMax; y >= yMin; y--)
		{
			if (IsOpaque(pixels, width, x, y, alphaLimit))
			{
				top = y;
				break;
			}
		}

		AddUniquePixel(contourPixels, uniquePixels, x, bottom);
		AddUniquePixel(contourPixels, uniquePixels, x, top);
	}

	private static void AddUniquePixel(
		List<Vector2Int> contourPixels,
		HashSet<Vector2Int> uniquePixels,
		int x,
		int y
	)
	{
		if (x < 0 || y < 0)
			return;

		Vector2Int pixel = new Vector2Int(x, y);

		if (uniquePixels.Add(pixel))
		{
			contourPixels.Add(pixel);
		}
	}

	private static bool IsOpaque(Color32[] pixels, int width, int x, int y, byte alphaLimit)
	{
		return pixels[ToIndex(x, y, width)].a >= alphaLimit;
	}

	private static void AddPerimeterPixels(List<Vector2Int> contourPixels, int xMin, int yMin, int xMax, int yMax, int step)
	{
		for (int x = xMin; x <= xMax; x += step)
		{
			contourPixels.Add(new Vector2Int(x, yMin));

			if (yMax != yMin)
			{
				contourPixels.Add(new Vector2Int(x, yMax));
			}
		}

		if ((xMax - xMin) % step != 0)
		{
			contourPixels.Add(new Vector2Int(xMax, yMin));

			if (yMax != yMin)
			{
				contourPixels.Add(new Vector2Int(xMax, yMax));
			}
		}

		for (int y = yMin + step; y < yMax; y += step)
		{
			contourPixels.Add(new Vector2Int(xMin, y));

			if (xMax != xMin)
			{
				contourPixels.Add(new Vector2Int(xMax, y));
			}
		}
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
