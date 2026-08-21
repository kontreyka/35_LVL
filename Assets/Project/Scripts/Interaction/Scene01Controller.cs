using System.Collections;
using UnityEngine;

public class Scene01Controller : MonoBehaviour
{
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

	private bool isTransitioning;

	public void FocusWindow()
	{
		if (isTransitioning)
			return;

		StartCoroutine(FocusWindowRoutine());
	}

	private IEnumerator FocusWindowRoutine()
	{
		isTransitioning = true;

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

		Debug.Log("Камера приблизилась к окну.");
	}
}