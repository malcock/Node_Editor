using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using NodeEditorFramework;

namespace NodeEditorFramework.RealTime
{
    public class RTKnob : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform m_transform = null;


        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("hello knob " + this.GetType());
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            
        }

        // Use this for initialization
        void Start()
        {
            m_transform = (RectTransform) transform;
        }

        // Update is called once per frame
        void Update()
        {

        }

        
    }

}