using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider2D))]
public class ClickableHotspot : MonoBehaviour
{
	[SerializeField] private UnityEvent onClick;

	private Collider2D hitbox;
	private Camera mainCamera;

	private void Awake()
	{
		hitbox = GetComponent<Collider2D>();
		mainCamera = Camera.main;
	}

	private void Update()
	{
#if ENABLE_INPUT_SYSTEM
		if (Mouse.current != null &&
			Mouse.current.leftButton.wasPressedThisFrame)
		{
			TryClick(Mouse.current.position.ReadValue());
		}
#else
        if (Input.GetMouseButtonDown(0))
        {
            TryClick(Input.mousePosition);
        }
#endif
	}

	private void TryClick(Vector2 screenPosition)
	{
		if (IsPointerOverUI())
			return;

		if (mainCamera == null)
			return;

		Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
			new Vector3(
				screenPosition.x,
				screenPosition.y,
				0f
			)
		);

		if (!hitbox.OverlapPoint(worldPosition))
			return;

		Debug.Log("Нажали на: " + gameObject.name);

		onClick?.Invoke();
	}

	private static bool IsPointerOverUI()
	{
		return EventSystem.current != null &&
			EventSystem.current.IsPointerOverGameObject();
	}
}
