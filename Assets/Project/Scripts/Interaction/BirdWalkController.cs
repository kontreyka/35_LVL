using System;
using UnityEngine;
using UnityEngine.Serialization;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class BirdWalkController : MonoBehaviour
{
	public event Action StepMade;

	[SerializeField] private SpriteRenderer birdRenderer = null;
	[SerializeField] private Sprite[] walkFrames = new Sprite[2];
	[SerializeField] private SpriteRenderer platformRenderer = null;

	[Header("Movement")]
	[FormerlySerializedAs("moveSpeed")]
	[SerializeField] private float stepDistance = 1f;
	[SerializeField] private float birdFootHalfWidth = 0.35f;
	[SerializeField] private float platformEdgePadding = 0.05f;

	[Header("Platform Bounds")]
	[Tooltip("Рабочая длина платформы в world units. 0 использует полный SpriteRenderer bounds.")]
	[SerializeField] private float platformWalkableWidth = 0f;

	[Tooltip("Смещение центра рабочей длины платформы относительно Transform brown cage 1.")]
	[SerializeField] private float platformWalkableCenterOffsetX = 0f;

	[Header("Animation")]
	[SerializeField] private float stepFrameDelay = 0.5f;

	private int currentFrameIndex;
	private float nextFrameTime;

	private void Awake()
	{
		if (birdRenderer == null)
		{
			birdRenderer = GetComponent<SpriteRenderer>();
		}

		SetFrame(0);
	}

	private void Update()
	{
		float horizontalInput = GetHorizontalInput();

		if (Mathf.Abs(horizontalInput) <= Mathf.Epsilon)
			return;

		UpdateDirection(horizontalInput);
		TryStep(horizontalInput);
	}

	private float GetHorizontalInput()
	{
		float input = 0f;

#if ENABLE_INPUT_SYSTEM
		if (Keyboard.current == null)
			return input;

		if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
		{
			input -= 1f;
		}

		if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
		{
			input += 1f;
		}
#else
		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
		{
			input -= 1f;
		}

		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
		{
			input += 1f;
		}
#endif

		return Mathf.Clamp(input, -1f, 1f);
	}

	private void TryStep(float horizontalInput)
	{
		if (Time.time < nextFrameTime)
			return;

		nextFrameTime = Time.time + Mathf.Max(0.01f, stepFrameDelay);

		if (!MoveOneStep(horizontalInput))
			return;

		SetFrame(currentFrameIndex + 1);
		StepMade?.Invoke();
	}

	private bool MoveOneStep(float horizontalInput)
	{
		Vector3 currentPosition = transform.position;
		Vector3 position = currentPosition;
		position.x += Mathf.Sign(horizontalInput) * Mathf.Max(0f, stepDistance);
		position.x = ClampXToPlatform(position.x);

		if (Mathf.Approximately(position.x, currentPosition.x))
			return false;

		transform.position = position;
		return true;
	}

	private float ClampXToPlatform(float x)
	{
		if (platformRenderer == null)
			return x;

		GetPlatformWalkableRange(out float platformMinX, out float platformMaxX);

		float halfWidth = Mathf.Max(0f, birdFootHalfWidth);
		float padding = Mathf.Max(0f, platformEdgePadding);
		float minX = platformMinX + halfWidth + padding;
		float maxX = platformMaxX - halfWidth - padding;

		if (minX > maxX)
			return (platformMinX + platformMaxX) * 0.5f;

		return Mathf.Clamp(x, minX, maxX);
	}

	private void GetPlatformWalkableRange(out float minX, out float maxX)
	{
		if (platformWalkableWidth > Mathf.Epsilon)
		{
			Vector3 center = platformRenderer.transform.TransformPoint(
				new Vector3(platformWalkableCenterOffsetX, 0f, 0f)
			);
			float halfWidth = platformWalkableWidth * Mathf.Abs(platformRenderer.transform.lossyScale.x) * 0.5f;
			minX = center.x - halfWidth;
			maxX = center.x + halfWidth;
			return;
		}

		Bounds platformBounds = platformRenderer.bounds;
		minX = platformBounds.min.x;
		maxX = platformBounds.max.x;
	}

	private void UpdateDirection(float horizontalInput)
	{
		if (birdRenderer == null)
			return;

		birdRenderer.flipX = horizontalInput < 0f;
	}

	private void SetFrame(int frameIndex)
	{
		if (birdRenderer == null || walkFrames == null || walkFrames.Length == 0)
			return;

		currentFrameIndex = Mathf.Abs(frameIndex) % walkFrames.Length;
		Sprite frame = walkFrames[currentFrameIndex];

		if (frame != null)
		{
			birdRenderer.sprite = frame;
		}
	}
}
