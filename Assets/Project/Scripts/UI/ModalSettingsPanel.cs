using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ModalSettingsPanel : MonoBehaviour
{
	[SerializeField] private int sortingOrder = 1500;

	private static int activePanelCount;

	private float previousTimeScale = 1f;
	private bool ownsPause;
	private bool registeredAsOpen;

	public static bool IsOpen => activePanelCount > 0;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticState()
	{
		activePanelCount = 0;
	}

	private void OnEnable()
	{
		transform.SetAsLastSibling();
		ConfigureModalCanvas();

		CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
		if (canvasGroup == null)
			canvasGroup = gameObject.AddComponent<CanvasGroup>();

		canvasGroup.interactable = true;
		canvasGroup.blocksRaycasts = true;

		previousTimeScale = Time.timeScale;
		Time.timeScale = 0f;
		ownsPause = true;

		if (!registeredAsOpen)
		{
			activePanelCount++;
			registeredAsOpen = true;
		}
	}

	private void OnDisable()
	{
		RestoreTimeScale();
	}

	private void OnDestroy()
	{
		RestoreTimeScale();
	}

	private void RestoreTimeScale()
	{
		if (registeredAsOpen)
		{
			activePanelCount = Mathf.Max(0, activePanelCount - 1);
			registeredAsOpen = false;
		}

		if (!ownsPause)
			return;

		Time.timeScale = previousTimeScale;
		ownsPause = false;
	}

	private void ConfigureModalCanvas()
	{
		Canvas canvas = GetComponent<Canvas>();
		if (canvas == null)
			canvas = gameObject.AddComponent<Canvas>();

		canvas.overrideSorting = true;
		canvas.sortingOrder = sortingOrder;

		if (GetComponent<GraphicRaycaster>() == null)
			gameObject.AddComponent<GraphicRaycaster>();
	}
}
