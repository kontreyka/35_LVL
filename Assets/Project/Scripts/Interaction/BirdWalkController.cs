using UnityEngine;
using UnityEngine.Serialization;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class BirdWalkController : MonoBehaviour
{
	[SerializeField] private SpriteRenderer birdRenderer = null;
	[SerializeField] private Sprite[] walkFrames = new Sprite[2];
	[SerializeField] private SpriteRenderer platformRenderer = null;

	[Header("Movement")]
	[FormerlySerializedAs("moveSpeed")]
	[SerializeField] private float stepDistance = 1f;
	[SerializeField] private float birdFootHalfWidth = 0.35f;
	[SerializeField] private float platformEdgePadding = 0.05f;

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
		MoveOneStep(horizontalInput);
		SetFrame(currentFrameIndex + 1);
	}

	private void MoveOneStep(float horizontalInput)
	{
		Vector3 position = transform.position;
		position.x += Mathf.Sign(horizontalInput) * Mathf.Max(0f, stepDistance);
		position.x = ClampXToPlatform(position.x);
		transform.position = position;
	}

	private float ClampXToPlatform(float x)
	{
		if (platformRenderer == null)
			return x;

		Bounds platformBounds = platformRenderer.bounds;
		float halfWidth = Mathf.Max(0f, birdFootHalfWidth);
		float padding = Mathf.Max(0f, platformEdgePadding);
		float minX = platformBounds.min.x + halfWidth + padding;
		float maxX = platformBounds.max.x - halfWidth - padding;

		if (minX > maxX)
			return platformBounds.center.x;

		return Mathf.Clamp(x, minX, maxX);
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
