
using System.Collections.Generic;

using UnityEngine.EventSystems;
using UnityEngine.UI;


public class PageCanvasRaycaster : GraphicRaycaster
{
    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        if (MenuBook.CanvasMousePosition.HasValue)
        {
            eventData.position = MenuBook.CanvasMousePosition.Value;
            base.Raycast(eventData, resultAppendList);
        }
    }
}
