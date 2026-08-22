using UnityEngine;
using UnityEngine.EventSystems;

public sealed class RoomPrototypeLevelThreePanelDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IEndDragHandler
{
	private RoomPrototypeLevelThreeController controller;
	private int panelId;

	public void Initialize(RoomPrototypeLevelThreeController owner, int id)
	{
		controller = owner;
		panelId = id;
	}

	public void OnPointerDown(PointerEventData eventData) => controller.OnPanelPointerDown(panelId, eventData);
	public void OnDrag(PointerEventData eventData) => controller.OnPanelDrag(panelId, eventData);
	public void OnPointerUp(PointerEventData eventData) => controller.OnPanelPointerUp(panelId, eventData);
	public void OnEndDrag(PointerEventData eventData) => controller.OnPanelEndDrag(panelId, eventData);
}
