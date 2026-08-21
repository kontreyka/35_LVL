using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CageEdgeGlowEffect : MonoBehaviour
{
	[Header("Aura Main Controls")]
	[FormerlySerializedAs("maxGlowIntensity")]
	[Tooltip("Основная интенсивность желтоватой ауры. Чем больше значение, тем ярче свечение.")]
	[Range(0f, 1f)]
	[SerializeField] private float auraIntensity = 0.24f;

	[FormerlySerializedAs("glowWidthPixels")]
	[Tooltip("Размер ареола в пикселях вокруг края PNG/спрайта.")]
	[Range(1f, 160f)]
	[SerializeField] private float auraSizePixels = 52f;

	[FormerlySerializedAs("particleRate")]
	[Tooltip("Количество частиц, появляющихся по краям за секунду.")]
	[Range(0f, 60f)]
	[SerializeField] private float particleCount = 9f;

	[Header("Aura Details")]
	[SerializeField] private Color glowColor = new Color(1f, 0.86f, 0.48f, 0.5f);
	[SerializeField] private float glowPulseDuration = 4.8f;
	[SerializeField] private float alphaThreshold = 0.2f;
	[SerializeField] private int glowSortingOffset = 1;

	[Header("Edge Particles")]
	[SerializeField] private Color particleColor = new Color(1f, 0.9f, 0.55f, 0.42f);
	[SerializeField] private int particleSampleStepPixels = 24;
	[SerializeField] private Vector2 particleSizeRange = new Vector2(0.01f, 0.026f);
	[SerializeField] private Vector2 particleLifetimeRange = new Vector2(1.2f, 2.4f);
	[SerializeField] private float particleDriftSpeed = 0.025f;
	[SerializeField] private float particleJitter = 0.01f;
	[SerializeField] private int particleSortingOffset = 2;

	private readonly List<Vector3> contourLocalPositions = new List<Vector3>();

	private SpriteRenderer sourceRenderer;
	private SpriteRenderer glowRenderer;
	private ParticleSystem edgeParticles;
	private Material particleMaterial;
	private Sprite cachedSprite;
	private Color cachedGlowColor;
	private float cachedAuraSizePixels;
	private float cachedAlphaThreshold;
	private int cachedParticleSampleStepPixels;
	private Texture2D generatedGlowTexture;
	private Sprite generatedGlowSprite;
	private float particleEmitAccumulator;

	private void Awake()
	{
		Setup();
	}

	private void OnEnable()
	{
		Setup();

		if (edgeParticles != null)
		{
			edgeParticles.Play();
		}
	}

	private void Update()
	{
		if (sourceRenderer == null)
			return;

		Setup();
		SyncGlowRenderer();
		UpdateGlowVisual();
		EmitEdgeParticles();
	}

	private void Setup()
	{
		if (sourceRenderer == null)
		{
			sourceRenderer = GetComponent<SpriteRenderer>();
		}

		EnsureGlowRenderer();
		EnsureParticleSystem();
		UpdateParticleSystemCapacity();
		RefreshContourIfNeeded();
	}

	private void EnsureGlowRenderer()
	{
		if (glowRenderer != null || sourceRenderer == null)
			return;

		GameObject glowObject = new GameObject("Cage_SoftEdgeGlow");
		glowObject.transform.SetParent(transform, false);
		glowObject.transform.localPosition = Vector3.zero;

		glowRenderer = glowObject.AddComponent<SpriteRenderer>();
		SyncGlowRenderer();
	}

	private void EnsureParticleSystem()
	{
		if (edgeParticles != null)
			return;

		GameObject particleObject = new GameObject("Cage_EdgeParticles");
		particleObject.transform.SetParent(transform, false);
		particleObject.transform.localPosition = Vector3.zero;
		edgeParticles = particleObject.AddComponent<ParticleSystem>();

		ParticleSystem.MainModule main = edgeParticles.main;
		main.loop = false;
		main.playOnAwake = true;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.maxParticles = GetParticleCapacity();
		main.startSpeed = 0f;
		main.startSize = new ParticleSystem.MinMaxCurve(particleSizeRange.x, particleSizeRange.y);
		main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetimeRange.x, particleLifetimeRange.y);
		main.startColor = particleColor;

		ParticleSystem.EmissionModule emission = edgeParticles.emission;
		emission.enabled = false;

		ParticleSystem.ShapeModule shape = edgeParticles.shape;
		shape.enabled = false;

		ParticleSystemRenderer particleRenderer = edgeParticles.GetComponent<ParticleSystemRenderer>();
		particleRenderer.sortingLayerID = sourceRenderer != null ? sourceRenderer.sortingLayerID : 0;
		particleRenderer.sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder + particleSortingOffset : particleSortingOffset;

		Shader particleShader = Shader.Find("Sprites/Default");

		if (particleShader != null)
		{
			particleMaterial = new Material(particleShader)
			{
				name = "Cage_EdgeParticles_Runtime"
			};
			particleRenderer.sharedMaterial = particleMaterial;
		}
	}

	private void UpdateParticleSystemCapacity()
	{
		if (edgeParticles == null)
			return;

		ParticleSystem.MainModule main = edgeParticles.main;
		main.maxParticles = GetParticleCapacity();
	}

	private void SyncGlowRenderer()
	{
		if (glowRenderer == null || sourceRenderer == null)
			return;

		glowRenderer.sprite = generatedGlowSprite;
		glowRenderer.enabled = generatedGlowSprite != null;
		glowRenderer.flipX = sourceRenderer.flipX;
		glowRenderer.flipY = sourceRenderer.flipY;
		glowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
		glowRenderer.sortingOrder = sourceRenderer.sortingOrder + glowSortingOffset;
	}

	private void UpdateGlowVisual()
	{
		if (glowRenderer == null)
			return;

		float pulse = GetSlowPulse();
		Color color = Color.white;
		color.a = Mathf.Lerp(auraIntensity * 0.35f, auraIntensity, pulse);
		glowRenderer.color = color;
	}

	private void RefreshContourIfNeeded()
	{
		Sprite sprite = sourceRenderer != null ? sourceRenderer.sprite : null;

		if (sprite == cachedSprite && IsGlowCacheValid())
			return;

		cachedSprite = sprite;
		cachedGlowColor = glowColor;
		cachedAuraSizePixels = auraSizePixels;
		cachedAlphaThreshold = alphaThreshold;
		cachedParticleSampleStepPixels = particleSampleStepPixels;
		contourLocalPositions.Clear();

		if (sprite == null)
			return;

		try
		{
			RebuildGlowSprite(sprite);

			List<Vector2Int> contourPixels = SpriteContourSampler.FindContourPixels(
				sprite.texture,
				sprite.textureRect,
				particleSampleStepPixels,
				alphaThreshold
			);

			for (int i = 0; i < contourPixels.Count; i++)
			{
				contourLocalPositions.Add(TexturePixelToLocalPosition(sprite, contourPixels[i]));
			}
		}
		catch (UnityException exception)
		{
			Debug.LogWarning($"CageEdgeGlowEffect: не удалось прочитать пиксели спрайта, использую края bounds. {exception.Message}");
			ClearGlowSprite();
		}

		if (contourLocalPositions.Count == 0)
		{
			AddBoundsContourPositions(sprite);
		}

		SyncGlowRenderer();
	}

	private bool IsGlowCacheValid()
	{
		return cachedGlowColor == glowColor
			&& Mathf.Approximately(cachedAuraSizePixels, auraSizePixels)
			&& Mathf.Approximately(cachedAlphaThreshold, alphaThreshold)
			&& cachedParticleSampleStepPixels == particleSampleStepPixels;
	}

	private void RebuildGlowSprite(Sprite sprite)
	{
		ClearGlowSprite();
		int glowRadius = Mathf.Max(1, Mathf.RoundToInt(auraSizePixels));

		generatedGlowTexture = SpriteContourGlowTextureBuilder.BuildGlowTexture(
			sprite.texture,
			sprite.textureRect,
			glowColor,
			particleSampleStepPixels,
			alphaThreshold,
			glowRadius,
			1f
		);
		generatedGlowSprite = Sprite.Create(
			generatedGlowTexture,
			new Rect(0f, 0f, generatedGlowTexture.width, generatedGlowTexture.height),
			new Vector2(
				(sprite.pivot.x + glowRadius) / generatedGlowTexture.width,
				(sprite.pivot.y + glowRadius) / generatedGlowTexture.height
			),
			sprite.pixelsPerUnit
		);
		generatedGlowSprite.name = "Generated_CageContourGlowSprite";
	}

	private void ClearGlowSprite()
	{
		if (generatedGlowSprite != null)
		{
			Destroy(generatedGlowSprite);
			generatedGlowSprite = null;
		}

		if (generatedGlowTexture != null)
		{
			Destroy(generatedGlowTexture);
			generatedGlowTexture = null;
		}
	}

	private void EmitEdgeParticles()
	{
		if (edgeParticles == null || contourLocalPositions.Count == 0 || particleCount <= 0f)
			return;

		float pulse = GetSlowPulse();
		particleEmitAccumulator += Time.deltaTime * particleCount * Mathf.Lerp(0.45f, 1f, pulse);

		while (particleEmitAccumulator >= 1f)
		{
			particleEmitAccumulator -= 1f;
			EmitParticle(pulse);
		}
	}

	private void EmitParticle(float pulse)
	{
		int index = Random.Range(0, contourLocalPositions.Count);
		Vector3 localPosition = contourLocalPositions[index];
		Vector2 jitter = Random.insideUnitCircle * particleJitter;
		Vector2 drift = Random.insideUnitCircle.normalized * particleDriftSpeed;

		ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
		{
			position = transform.TransformPoint(localPosition + new Vector3(jitter.x, jitter.y, 0f)),
			velocity = new Vector3(drift.x, drift.y, 0f),
			startSize = Random.Range(particleSizeRange.x, particleSizeRange.y),
			startLifetime = Random.Range(particleLifetimeRange.x, particleLifetimeRange.y),
			startColor = new Color(
				particleColor.r,
				particleColor.g,
				particleColor.b,
				particleColor.a * Mathf.Lerp(0.55f, 1f, pulse)
			)
		};

		edgeParticles.Emit(emitParams, 1);
	}

	private float GetSlowPulse()
	{
		float duration = Mathf.Max(0.01f, glowPulseDuration);
		return (Mathf.Sin(Time.time * Mathf.PI * 2f / duration) + 1f) * 0.5f;
	}

	private int GetParticleCapacity()
	{
		float longestLifetime = Mathf.Max(particleLifetimeRange.x, particleLifetimeRange.y, 1f);
		return Mathf.Max(16, Mathf.CeilToInt(particleCount * longestLifetime * 2f));
	}

	private static Vector3 TexturePixelToLocalPosition(Sprite sprite, Vector2Int pixel)
	{
		Rect textureRect = sprite.textureRect;
		Vector2 pivot = sprite.pivot;
		float pixelsPerUnit = sprite.pixelsPerUnit;
		float localX = (pixel.x + 0.5f - textureRect.xMin - pivot.x) / pixelsPerUnit;
		float localY = (pixel.y + 0.5f - textureRect.yMin - pivot.y) / pixelsPerUnit;

		return new Vector3(localX, localY, 0f);
	}

	private void AddBoundsContourPositions(Sprite sprite)
	{
		Bounds bounds = sprite.bounds;
		int samplesPerSide = Mathf.Max(8, Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.y) * 10f));

		for (int i = 0; i < samplesPerSide; i++)
		{
			float t = samplesPerSide <= 1 ? 0f : i / (samplesPerSide - 1f);
			contourLocalPositions.Add(new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, t), bounds.min.y, 0f));
			contourLocalPositions.Add(new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, t), bounds.max.y, 0f));
			contourLocalPositions.Add(new Vector3(bounds.min.x, Mathf.Lerp(bounds.min.y, bounds.max.y, t), 0f));
			contourLocalPositions.Add(new Vector3(bounds.max.x, Mathf.Lerp(bounds.min.y, bounds.max.y, t), 0f));
		}
	}

	private void OnDestroy()
	{
		ClearGlowSprite();

		if (particleMaterial != null)
		{
			Destroy(particleMaterial);
		}
	}
}
