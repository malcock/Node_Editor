using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NodeEditorFramework;
using UnityEngine.EventSystems;
using UnityEngine;

namespace NodeEditorFramework.RealTime
{
    public class RTNode : RTDraggable
    {
        public Node node;
        public RectTransform content; 
        public List<RTKnob> nodeKnobs = new List<RTKnob>();

        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            node.position = eventData.position;
        }
    }
}
