using UnityEngine;
using UnityEngine.EventSystems;
using InventorySolver;

public sealed class InventoryItemView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public InventoryRuntime.Entry Entry { get; private set; }
    public ERotation Rotation { get; private set; }
    public System.Action<InventoryItemView, PointerEventData> BeginDrag;
    public System.Action<InventoryItemView, PointerEventData> Dragged;
    public System.Action<InventoryItemView, PointerEventData> EndDrag;
    public void Bind(InventoryRuntime.Entry entry) { Entry = entry; Rotation = entry.Placement.Rotation; }
    public void OnPointerDown(PointerEventData eventData) => BeginDrag?.Invoke(this, eventData);
    public void OnDrag(PointerEventData eventData) => Dragged?.Invoke(this, eventData);
    public void OnPointerUp(PointerEventData eventData) => EndDrag?.Invoke(this, eventData);
    public void Rotate() { Rotation = (ERotation)(((int)Rotation + 1) % 4); }
}
