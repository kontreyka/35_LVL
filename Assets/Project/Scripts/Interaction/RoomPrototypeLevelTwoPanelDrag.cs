using UnityEngine;
using UnityEngine.EventSystems;

public sealed class RoomPrototypeLevelTwoPanelDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IEndDragHandler
{
	private RoomPrototypeLevelTwoController controller;
	private int panelId;

	public void Initialize(RoomPrototypeLevelTwoController owner, int id)
	{
		controller = owner;
		panelId = id;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		controller.OnPanelPointerDown(panelId, eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		controller.OnPanelDrag(panelId, eventData);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		controller.OnPanelPointerUp(panelId, eventData);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		controller.OnPanelEndDrag(panelId, eventData);
	}
}
