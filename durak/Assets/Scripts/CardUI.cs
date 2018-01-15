using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler,IPointerUpHandler,IPointerDownHandler, IDragHandler{
    public bool clicked = false;
    public bool dragged = false;
    public bool mouseDown = false;
    public bool scrolling = false;
    public bool selected = false;
    public bool played = false;
    public bool inHand = false;
    public FieldPair fp;
    public Vector3 pos, nextPos;
    public Transform cardHolder, originalCardHolder, committedCardHolder;
    public int holdTimer, holdTimeToScroll = 7;
    public ScrollRect hand;
    public Vector3 originalPosition;
    public Card card;
    public GameObject sparklePrefab;
    public GameObject shine;
    public Image borderHighlight;
    float timeToSparkle = 1f, timeToShine = 0f;
    bool shining;
    int rgbPhase = 0, rgbAlt = 4;
    float defaultY = 150f, resetY = -160f, selectedY = 50f;

    public void OnPointerClick(PointerEventData eventData)
    {
        //Debug.Log(scrolling + " " + dragged);
        if (!clicked || played)
            return;
        
       // Debug.Log("clcikable");

        clicked = false;
        if (inHand)
        {
            selected = !selected;
            if (selected)
            {
                //Debug.Log("selected");
                //originalPosition = transform.position;
                transform.position += new Vector3(0, selectedY, 0);
                //Card c = Card.NameToCard(transform.GetChild(0).GetComponent<Text>().text);
                //Debug.Log("CARD> " + c.ToString());
                GameManager.gm.selected.Add(card);
                GameManager.gm.UpdateActionButtons();
                //GameManager.gm.selected.Add(Card.NameToCard(transform.GetChild(0).GetComponent<Text>().text));
            }
            else
            {
                //Debug.Log("deselcted");
                transform.position -= new Vector3(0, selectedY, 0);
                //GameManager.gm.selected.Remove(Card.NameToCard(transform.GetChild(0).GetComponent<Text>().text));
                GameManager.gm.selected.Remove(card);

                GameManager.gm.UpdateActionButtons();
                //transform.position = originalPosition;
            }
        }
        else
        {
            ReturnToHand();
            /*
            Debug.Log("deselecting defend card");
            transform.SetParent(originalCardHolder);
            transform.position = originalCardHolder.position;
            GameManager.gm.defendCards.Remove(fp);
            fp.defend = null;
            fp = null;
           */ //GameManager.gm.defendCards.Remove(card);
        }
       // Debug.Log("CLICKED");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (played || !inHand)
            return;
        nextPos = Input.mousePosition;
        Vector3 difference = nextPos - pos;
        //pos.localEulerAngles = Vector3.zero;
        float angle = Mathf.Atan2(nextPos.y - pos.y, nextPos.x - pos.x) * Mathf.Rad2Deg;
        //Debug.Log("ANGLE:" + angle);
        //float angle = Vector2.Angle(pos, nextPos);
        //Debug.Log("float" +angle + " " + angle*Mathf.Rad2Deg);
        if (!scrolling && !dragged)
        {
            if (angle >= 45 && angle <= 135)
                dragged = true;
            if ((angle > 135 && angle <= 180) || (angle < 45 && angle > - 45))
                scrolling = true;
            if (angle >= -135 && angle <= -45)
                dragged = true;
            if ((angle < -135 && angle >= -180))
                scrolling = true;
            if (dragged)
            {
                //selected = false;
                cardHolder = transform.parent;
                transform.SetParent(GameManager.gm.gameCanvas.transform);
            }
        }
        if (dragged)
        {
            //Debug.Log("DRAGGIN");
            transform.position += difference;
            List<RaycastResult> res = new List<RaycastResult>();
            
            EventSystem.current.RaycastAll(eventData, res);
            cardHolder = null;
            foreach (RaycastResult r in res)
            {
                if (LayerMask.LayerToName(r.gameObject.layer) == "CardHolder" && r.gameObject.transform.tag != originalCardHolder.tag)
                {
                    cardHolder = r.gameObject.transform;
                }
            }
        }
        else if(scrolling)
        {
            //Debug.Log("scrolling");
            //hand.
            hand.content.position += new Vector3(difference.x,0,0);
        }
        pos = nextPos;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (played)
            return;
        inHand = transform.parent.tag == "Hand";
        //Debug.Log("drag");
        pos = Input.mousePosition;
        List<RaycastResult> res = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, res);
        if (committedCardHolder == null)
            committedCardHolder = transform.parent;
        foreach (RaycastResult r in res)
        {
            if (r.gameObject.tag == "ScrollHand")
            {
                if (!hand)
                {
                    hand = r.gameObject.transform.GetComponent<ScrollRect>();

                    hand.movementType = ScrollRect.MovementType.Clamped;
                }
            }
        }
        pos = Input.mousePosition;
        if (inHand)
        {
            originalCardHolder = transform.parent;
            originalPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        }
        dragged = false;
        scrolling = false;
        mouseDown = true;
        //holdTimer = 0;
        //dragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (played)
            return;
        clicked = !scrolling && !dragged;
        //Debug.Log("UP" + dragged + " " + scrolling + " " + clicked);
        if (inHand)
        {
            hand.movementType = ScrollRect.MovementType.Elastic;
            hand = null;
        }
        scrolling = false;
        mouseDown = false;
        if (dragged)
        {
            if(originalCardHolder.tag == "Hand")
            {
                //transform.position = or;
                if (cardHolder)
                {
                    if (cardHolder.tag == "DefendField")
                    {
                        int fpID = cardHolder.transform.GetSiblingIndex();
                        fp = GameManager.gm.field[fpID];
                        if (GameManager.gm.defender == GameManager.gm.myTurn && GameManager.gm.CanDefend(fp, card))
                        {
                            if(selected)
                            {
                                selected = false;
                                GameManager.gm.selected.Remove(card);
                            }
                            Debug.Log("CAN DEFEND");
                            transform.SetParent(cardHolder);
                            transform.position = cardHolder.position;
                            fp.defend = card;
                            GameManager.gm.defendCards.Add(fp);
                            GameManager.gm.UpdateActionButtons();
                            //GameManager.gm.defendCards.Add(card);
                        }
                        else
                        {
                            fp = null;
                            Debug.Log("CANT");
                            transform.SetParent(originalCardHolder);
                            transform.position = originalPosition;
                        }
                    }
                    else
                    {
                        Debug.Log("WHAT IS THIS");
                    }
                }
                else
                {
                    transform.SetParent(originalCardHolder);
                    transform.position = originalPosition;
                }
            }
            cardHolder = null;
            //transform.i
        }
        dragged = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("ME");
    }
    
    public void ReturnToHand()
    {
        Debug.Log("deselecting defend card" + card);
        transform.SetParent(originalCardHolder);
        //Debug.Log(1);
        transform.position = originalCardHolder.position;
        //Debug.Log(2);
        GameManager.gm.defendCards.Remove(fp);
        //Debug.Log(3);
        fp.defend = null;
        fp = null;
        GameManager.gm.UpdateActionButtons();
        //Debug.Log(40);
    }

    // Use this for initialization
    void Start () {
		
	}
	
    public void Sparkle(Color c)
    {
        if(c == Color.black)
        {
            c = new Color(UnityEngine.Random.Range(0, 255), UnityEngine.Random.Range(0,255), UnityEngine.Random.Range(0, 255),255)/255;
            //Debug.Log(c.ToString());
        }
        timeToSparkle -= .05f;
        if (timeToSparkle <= 0)
        {
            timeToSparkle = 1f;

            RectTransform rt = GetComponent<RectTransform>();
            float x = UnityEngine.Random.Range(0, rt.rect.width) - (rt.rect.width / 2);
            float y = UnityEngine.Random.Range(0, rt.rect.height) - (rt.rect.height / 2);
            GameObject sparkle = Instantiate(sparklePrefab);
            sparkle.transform.position = new Vector3(x, y, 0) + transform.position;
            sparkle.transform.SetParent(transform);
            sparkle.transform.GetComponent<Image>().color = c;
        }
        //sparkle.transform.SetParent(GameManager.gm.gameCanvas.transform);
    }

    public void Shines()
    {
        if (!shining)
        {
            if (timeToShine <= 0)
            {
                //Debug.Log("Shine");
                shining = true;
                timeToShine = 1f;
                StartCoroutine("Shine");
            }
            else
            {

                timeToShine -= .005f;
            }
        }
    }

    public bool ShineHasFinished()
    {
        shine.transform.position -= new Vector3(0, 8f, 0);
        if(shine.transform.localPosition.y <= resetY)
        {
            //Debug.Log("done shine");
            shining = false;
            shine.transform.localPosition = new Vector3(0, defaultY, 0);
            return true;
        }
        return false;
    }


    public IEnumerator Shine()
    {
        yield return new WaitUntil(ShineHasFinished);
    }

    public void CycleThroughRainbow()
    {
        Color c = 255*borderHighlight.color;
        //Debug.Log(c.r + "r " + c.g + "g " + c.b  + "b.... rgb>>" + rgbPhase + " alt" + rgbAlt);
        switch (rgbPhase)
        {
            case 0:
                c.g = (c.g + rgbAlt);
                if (c.g >= 255)
                {
                    c.g = 255;
                    rgbPhase++;
                }
                break;
            case 1:
                c.r = (c.r - rgbAlt);

                if (c.r <= 0)
                {
                    c.r = 0;
                    rgbPhase++;
                }
                break;
            case 2:
                c.b = (c.b + rgbAlt);
                if (c.b >= 255)
                {
                    c.g = 255;
                    rgbPhase++;
                }
                break;
            case 3:
                c.g = (c.g - rgbAlt);
                if (c.g <= 0)
                {
                    c.g = 0;
                    rgbPhase++;
                }
                break;
            case 4:
                c.r = (c.r + rgbAlt);

                if (c.r >= 255)
                {
                    c.r = 255;
                    rgbPhase++;
                }
                break;
            case 5:
                c.b = (c.b - rgbAlt);
                if (c.b <= 0)
                {
                    c.b = 0;
                    rgbPhase = 0;
                }
                break;
        }
        borderHighlight.color = c/255;
        //borderHighlight.color = Color.Lerp(borderHighlight.color, Color.blue, 100f);
        //borderHighlight.color = Color.Lerp(borderHighlight.color, Color.green, 100f);
        //borderHighlight.color = Color.Lerp(borderHighlight.color, Color.red, 100f);
    }

	// Update is called once per frame
	void Update () {
        if (card.Equals(GameManager.gm.trump))
        {
            borderHighlight.gameObject.SetActive(true);
            CycleThroughRainbow();
            Color c = Color.black;
            Sparkle(c);
            Shines();
        }
        else
        {
            borderHighlight.gameObject.SetActive(false);
            if (card.EqualSuit(GameManager.gm.trump))
            {
                //Debug.Log("same suit" + Card.CARDS[card]);
                Sparkle(new Color(240 / 255, 255 / 255, 0));
            }
            if (card.EqualValue(GameManager.gm.trump))
            {
                Color c = Color.black;
                Sparkle(c);
                Shines();
                //Debug.Log("same val" + Card.CARDS[card]);
            }
        }
        /*
        if (mouseDown)
        {
            List<RaycastResult> res = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, res);
            cardHolder = null;
            foreach (RaycastResult r in res)
            {
                Debug.Log(r.gameObject.name);
                if (r.gameObject.tag == "CardHolder")
                {
                    cardHolder = r.gameObject.transform;
                }
                else if (r.gameObject.tag == "Hand")
                {
                    r.gameObject.transform.GetComponent<ScrollRect>().content.transform.position = new Vector3(eventData.delta.x, 0, 0);//verticalNormalizedPosition -= eventData.delta.y / ((float)Screen.height);
                                                                                                                                        //                    scrollRect.verticalNormalizedPosition -=;
                }
            }
        }*/
    }

}
