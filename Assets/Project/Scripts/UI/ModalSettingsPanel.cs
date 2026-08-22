using UnityEngine;

[DisallowMultipleComponent]
public sealed class ModalSettingsPanel : MonoBehaviour
{
	private float previousTimeScale = 1f;
	private bool ownsPause;

	private void OnEnable()
	{
		transform.SetAsLastSibling();

		CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
		if (canvasGroup == null)
			canvasGroup = gameObject.AddComponent<CanvasGroup>();

		canvasGroup.interactable = true;
		canvasGroup.blocksRaycasts = true;

		previousTimeScale = Time.timeScale;
		Time.timeScale = 0f;
		ownsPause = true;
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
		if (!ownsPause)
			return;

		Time.timeScale = previousTimeScale;
		ownsPause = false;
	}
}
