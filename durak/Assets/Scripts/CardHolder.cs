using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardHolder : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("cal");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("HERE");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
