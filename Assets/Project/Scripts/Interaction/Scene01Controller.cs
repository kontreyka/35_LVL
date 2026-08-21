using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Scene01Controller : MonoBehaviour
{
	private enum ClickStep
	{
		FocusWindow,
		ChangeBackground,
		ZoomOut,
		Done
	}

	private static readonly int RadiusProperty = Shader.PropertyToID("_Radius");
	private static readonly int SoftnessProperty = Shader.PropertyToID("_Softness");
	private static readonly int AspectProperty = Shader.PropertyToID("_Aspect");

	[Header("Camera")]
	[SerializeField] private Camera sceneCamera;

	[Header("Window")]
	[SerializeField] private Transform windowTarget;

	[Header("Zoom")]
	[SerializeField] private float zoomDuration = 1.2f;
	[SerializeField] private float targetCameraSize = 2.2f;

	[SerializeField]
	private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(
		0f, 0f,
		1f, 1f
	);

	[Header("Background Transition")]
	[SerializeField] private SpriteRenderer backgroundRenderer;
	[SerializeField] private Sprite secondBackgroundSprite;
	[SerializeField] private Material vignetteMaterial;
	[SerializeField] private float vignetteDuration = 0.9f;
	[SerializeField] private float openVignetteRadius = 1.15f;

	[FormerlySerializedAs("closedVignetteRadius")]
	[Tooltip("Минимальный радиус отверстия в момент максимального сжатия. Чем меньше значение, тем сильнее виньетка закрывает картину с клеткой.")]
	[Range(0.05f, 1.15f)]
	[SerializeField] private float vignetteCompressionRadius = 0.32f;

	[SerializeField] private float vignetteSoftness = 0.08f;
	[SerializeField] private int vignetteSortingOrder = 50;

	[SerializeField]
	private AnimationCurve vignetteCurve = AnimationCurve.EaseInOut(
		0f, 0f,
		1f, 1f
	);

	private bool isTransitioning;
	private bool hasInitialCameraState;
	private ClickStep clickStep;
	private Vector3 initialCameraPosition;
	private float initialCameraSize;
	private SpriteRenderer vignetteRenderer;
	private MaterialPropertyBlock vignetteProperties;
	private Texture2D solidVignetteTexture;
	private Sprite solidVignetteSprite;

	private void Awake()
	{
		CaptureInitialCameraState();
	}

	public void FocusWindow()
	{
		if (isTransitioning)
			return;

		switch (clickStep)
		{
			case ClickStep.FocusWindow:
				StartCoroutine(FocusWindowRoutine());
				break;
			case ClickStep.ChangeBackground:
				StartCoroutine(ChangeBackgroundRoutine());
				break;
			case ClickStep.ZoomOut:
				StartCoroutine(ZoomOutRoutine());
				break;
		}
	}

	private IEnumerator FocusWindowRoutine()
	{
		isTransitioning = true;

		if (sceneCamera == null || windowTarget == null)
		{
			Debug.LogWarning("Scene01Controller: не назначены камера или цель окна.");
			isTransitioning = false;
			yield break;
		}

		CaptureInitialCameraState();

		Vector3 startPosition = sceneCamera.transform.position;

		Vector3 targetPosition = new Vector3(
			windowTarget.position.x,
			windowTarget.position.y,
			startPosition.z
		);

		float startSize = sceneCamera.orthographicSize;

		float time = 0f;

		while (time < zoomDuration)
		{
			time += Time.deltaTime;

			float t = Mathf.Clamp01(time / zoomDuration);
			float smoothT = zoomCurve.Evaluate(t);

			sceneCamera.transform.position = Vector3.Lerp(
				startPosition,
				targetPosition,
				smoothT
			);

			sceneCamera.orthographicSize = Mathf.Lerp(
				startSize,
				targetCameraSize,
				smoothT
			);

			yield return null;
		}

		sceneCamera.transform.position = targetPosition;
		sceneCamera.orthographicSize = targetCameraSize;
		clickStep = ClickStep.ChangeBackground;
		isTransitioning = false;

		Debug.Log("Камера приблизилась к окну.");
	}

	private IEnumerator ChangeBackgroundRoutine()
	{
		isTransitioning = true;

		if (backgroundRenderer == null || secondBackgroundSprite == null)
		{
			Debug.LogWarning("Scene01Controller: не назначены фон или второй спрайт фона.");
			isTransitioning = false;
			yield break;
		}

		EnsureVignetteRenderer();
		UpdateVignetteOverlay();

		if (vignetteRenderer != null)
		{
			float compressedRadius = GetVignetteCompressionRadius();

			vignetteRenderer.enabled = true;
			SetVignetteRadius(openVignetteRadius);
			yield return AnimateVignette(openVignetteRadius, compressedRadius);
		}

		backgroundRenderer.sprite = secondBackgroundSprite;

		if (vignetteRenderer != null)
		{
			yield return AnimateVignette(GetVignetteCompressionRadius(), openVignetteRadius);
			SetVignetteRadius(openVignetteRadius);
			vignetteRenderer.enabled = false;
		}

		clickStep = ClickStep.ZoomOut;
		isTransitioning = false;
	}

	private IEnumerator ZoomOutRoutine()
	{
		isTransitioning = true;

		if (sceneCamera == null)
		{
			Debug.LogWarning("Scene01Controller: не назначена камера сцены.");
			isTransitioning = false;
			yield break;
		}

		if (!hasInitialCameraState)
		{
			CaptureInitialCameraState();
		}

		if (vignetteRenderer != null)
		{
			vignetteRenderer.enabled = false;
		}

		Vector3 startPosition = sceneCamera.transform.position;
		float startSize = sceneCamera.orthographicSize;
		float time = 0f;

		while (time < zoomDuration)
		{
			time += Time.deltaTime;

			float t = Mathf.Clamp01(time / zoomDuration);
			float smoothT = zoomCurve.Evaluate(t);

			sceneCamera.transform.position = Vector3.Lerp(
				startPosition,
				initialCameraPosition,
				smoothT
			);

			sceneCamera.orthographicSize = Mathf.Lerp(
				startSize,
				initialCameraSize,
				smoothT
			);

			yield return null;
		}

		sceneCamera.transform.position = initialCameraPosition;
		sceneCamera.orthographicSize = initialCameraSize;
		clickStep = ClickStep.Done;
		isTransitioning = false;

		Debug.Log("Камера отдалилась от окна.");
	}

	private IEnumerator AnimateVignette(float fromRadius, float toRadius)
	{
		float duration = Mathf.Max(0f, vignetteDuration) * 0.5f;

		if (duration <= 0f)
		{
			SetVignetteRadius(toRadius);
			yield break;
		}

		float time = 0f;

		while (time < duration)
		{
			time += Time.deltaTime;

			float t = Mathf.Clamp01(time / duration);
			float smoothT = vignetteCurve.Evaluate(t);
			SetVignetteRadius(Mathf.Lerp(fromRadius, toRadius, smoothT));

			yield return null;
		}

		SetVignetteRadius(toRadius);
	}

	private float GetVignetteCompressionRadius()
	{
		return Mathf.Clamp(vignetteCompressionRadius, 0.05f, openVignetteRadius);
	}

	private void EnsureVignetteRenderer()
	{
		if (vignetteRenderer != null)
			return;

		Shader vignetteShader = vignetteMaterial != null
			? vignetteMaterial.shader
			: Shader.Find("UI/IrisReveal");

		if (vignetteMaterial == null && vignetteShader == null)
		{
			Debug.LogWarning("Scene01Controller: не найден материал или шейдер виньетки.");
			return;
		}

		GameObject vignetteObject = new GameObject("Scene01_Vignette");
		vignetteObject.transform.SetParent(transform, false);

		vignetteRenderer = vignetteObject.AddComponent<SpriteRenderer>();
		vignetteRenderer.sprite = GetSolidVignetteSprite();
		vignetteRenderer.sharedMaterial = vignetteMaterial != null
			? vignetteMaterial
			: new Material(vignetteShader);
		vignetteRenderer.sortingLayerID = backgroundRenderer.sortingLayerID;
		vignetteRenderer.sortingOrder = vignetteSortingOrder;
		vignetteRenderer.enabled = false;

		vignetteProperties = new MaterialPropertyBlock();
	}

	private void UpdateVignetteOverlay()
	{
		if (vignetteRenderer == null || sceneCamera == null)
			return;

		Vector3 cameraPosition = sceneCamera.transform.position;
		vignetteRenderer.transform.position = new Vector3(
			cameraPosition.x,
			cameraPosition.y,
			0f
		);

		Sprite sprite = vignetteRenderer.sprite;

		if (sprite == null)
			return;

		float height = sceneCamera.orthographicSize * 2f;
		float width = height * sceneCamera.aspect;
		Vector2 spriteSize = sprite.bounds.size;

		if (spriteSize.x <= 0f || spriteSize.y <= 0f)
			return;

		vignetteRenderer.transform.localScale = new Vector3(
			width / spriteSize.x,
			height / spriteSize.y,
			1f
		);

		SetVignetteAspect(width / height);
	}

	private void SetVignetteRadius(float radius)
	{
		if (vignetteRenderer == null)
			return;

		EnsureVignetteProperties();
		vignetteRenderer.GetPropertyBlock(vignetteProperties);
		vignetteProperties.SetFloat(RadiusProperty, radius);
		vignetteProperties.SetFloat(SoftnessProperty, vignetteSoftness);
		vignetteRenderer.SetPropertyBlock(vignetteProperties);
	}

	private void SetVignetteAspect(float aspect)
	{
		if (vignetteRenderer == null)
			return;

		EnsureVignetteProperties();
		vignetteRenderer.GetPropertyBlock(vignetteProperties);
		vignetteProperties.SetFloat(AspectProperty, aspect);
		vignetteRenderer.SetPropertyBlock(vignetteProperties);
	}

	private void EnsureVignetteProperties()
	{
		vignetteProperties ??= new MaterialPropertyBlock();
	}

	private void CaptureInitialCameraState()
	{
		if (sceneCamera == null || hasInitialCameraState)
			return;

		initialCameraPosition = sceneCamera.transform.position;
		initialCameraSize = sceneCamera.orthographicSize;
		hasInitialCameraState = true;
	}

	private Sprite GetSolidVignetteSprite()
	{
		if (solidVignetteSprite != null)
			return solidVignetteSprite;

		solidVignetteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
		{
			name = "Scene01_SolidVignetteTexture",
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};
		solidVignetteTexture.SetPixel(0, 0, Color.white);
		solidVignetteTexture.Apply();

		solidVignetteSprite = Sprite.Create(
			solidVignetteTexture,
			new Rect(0f, 0f, 1f, 1f),
			new Vector2(0.5f, 0.5f),
			1f
		);
		solidVignetteSprite.name = "Scene01_SolidVignetteSprite";

		return solidVignetteSprite;
	}

	private void OnDestroy()
	{
		if (solidVignetteSprite != null)
		{
			Destroy(solidVignetteSprite);
		}

		if (solidVignetteTexture != null)
		{
			Destroy(solidVignetteTexture);
		}
	}
}
