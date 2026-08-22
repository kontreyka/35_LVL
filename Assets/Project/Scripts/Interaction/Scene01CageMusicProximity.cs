using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class Scene01CageMusicProximity : MonoBehaviour
{
	[SerializeField] private AudioSource musicSource = null;
	[SerializeField] private SpriteRenderer targetRenderer = null;
	[SerializeField] private Camera targetCamera = null;

	[Header("Volume")]
	[Range(0f, 1f)]
	[SerializeField] private float minimumVolumeMultiplier = 0.2f;
	[SerializeField] private float fullVolumePadding = 0f;
	[SerializeField] private float quietDistance = 4f;
	[SerializeField] private float volumeSmoothTime = 0.18f;

	private float initialMusicVolume = 1f;
	private float volumeVelocity;

	private void Awake()
	{
		if (targetCamera == null)
		{
			targetCamera = Camera.main;
		}

		if (musicSource != null)
		{
			initialMusicVolume = musicSource.volume;
		}
	}

	private void Update()
	{
		if (musicSource == null || targetRenderer == null || targetCamera == null)
			return;

		if (!TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
			return;

		Bounds targetBounds = targetRenderer.bounds;
		float distance = CalculateDistanceToBounds(
			pointerWorldPosition,
			targetBounds,
			Mathf.Max(0f, fullVolumePadding)
		);

		float volumeMultiplier = CalculateVolumeMultiplier(
			distance,
			Mathf.Max(0.01f, quietDistance),
			minimumVolumeMultiplier
		);

		float targetVolume = initialMusicVolume * volumeMultiplier;
		musicSource.volume = Mathf.SmoothDamp(
			musicSource.volume,
			targetVolume,
			ref volumeVelocity,
			Mathf.Max(0.01f, volumeSmoothTime)
		);
	}

	private bool TryGetPointerWorldPosition(out Vector2 worldPosition)
	{
#if ENABLE_INPUT_SYSTEM
		if (Mouse.current == null)
		{
			worldPosition = default;
			return false;
		}

		Vector2 screenPosition = Mouse.current.position.ReadValue();
#else
		Vector2 screenPosition = Input.mousePosition;
#endif

		float targetZ = targetRenderer != null
			? targetRenderer.bounds.center.z
			: 0f;

		float cameraDistance = Mathf.Abs(targetCamera.transform.position.z - targetZ);
		Vector3 worldPoint = targetCamera.ScreenToWorldPoint(
			new Vector3(screenPosition.x, screenPosition.y, cameraDistance)
		);

		worldPosition = worldPoint;
		return true;
	}

	public static float CalculateDistanceToBounds(
		Vector2 point,
		Bounds bounds,
		float padding
	)
	{
		Vector2 min = new Vector2(bounds.min.x - padding, bounds.min.y - padding);
		Vector2 max = new Vector2(bounds.max.x + padding, bounds.max.y + padding);

		float dx = Mathf.Max(min.x - point.x, 0f, point.x - max.x);
		float dy = Mathf.Max(min.y - point.y, 0f, point.y - max.y);

		return new Vector2(dx, dy).magnitude;
	}

	public static float CalculateVolumeMultiplier(
		float distance,
		float quietDistance,
		float minimumVolumeMultiplier
	)
	{
		float clampedMinimum = Mathf.Clamp01(minimumVolumeMultiplier);

		if (quietDistance <= Mathf.Epsilon)
			return clampedMinimum;

		float distanceT = Mathf.Clamp01(distance / quietDistance);
		float heat = 1f - Mathf.SmoothStep(0f, 1f, distanceT);

		return Mathf.Lerp(clampedMinimum, 1f, heat);
	}

	public void DisableProximityAndRestoreVolume()
	{
		RestoreInitialMusicVolume();
		enabled = false;
	}

	private void OnDisable()
	{
		RestoreInitialMusicVolume();
	}

	private void RestoreInitialMusicVolume()
	{
		if (musicSource != null)
		{
			musicSource.volume = initialMusicVolume;
		}
	}
}
