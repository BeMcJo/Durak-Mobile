using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnimatedCard : MonoBehaviour {
    public GameObject sparklePrefab;
    public GameObject shine;
    public Image borderHighlight;
    float timeToSparkle = 1f, timeToShine = 0f;
    bool shining;
    int rgbPhase = 0, rgbAlt = 4, animationType, effectType, cardType;
    Sprite[] cards;
    float defaultY = 150f, resetY = -160f, selectedY = 50f, fallLimit = -700f, rotateSpd = 1f, fallSpd = 1f,rotationReset = 90f, prevYRotate = 0f;
    

    // Use this for initialization
    void Start()
    {
        cards = new Sprite[2];
        cards[0] = GameManager.gm.cardSprites[UnityEngine.Random.Range(0, 53)];
        cards[1] = GameManager.gm.cardSprites[54];
        cardType = UnityEngine.Random.Range(0, 1);
        animationType = 0;
        rotateSpd = UnityEngine.Random.Range(1f, 5f);
        fallSpd = UnityEngine.Random.Range(1.5f, 4f);
        effectType = UnityEngine.Random.Range(0, 2);
        transform.Find("CardSprite").GetComponent<Image>().sprite = cards[cardType];
        
    }

    public void Sparkle(Color c)
    {
        if (c == Color.black)
        {
            c = new Color(UnityEngine.Random.Range(0, 255), UnityEngine.Random.Range(0, 255), UnityEngine.Random.Range(0, 255), 255) / 255;
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
        if (shine.transform.localPosition.y <= resetY)
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
        Color c = 255 * borderHighlight.color;
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
        borderHighlight.color = c / 255;
        //borderHighlight.color = Color.Lerp(borderHighlight.color, Color.blue, 100f);
        //borderHighlight.color = Color.Lerp(borderHighlight.color, Color.green, 100f);
        //borderHighlight.color = Color.Lerp(borderHighlight.color, Color.red, 100f);
    }

    void Fall()
    {
        transform.localPosition -= new Vector3(0, fallSpd, 0);
        if (transform.localPosition.y <= fallLimit)
            Destroy(gameObject);
    }

    void Spin()
    {
        float prevYRotate = transform.localEulerAngles.y;
        transform.Rotate(new Vector3(0, rotateSpd, 0));
        
        float yRotate = transform.localEulerAngles.y;
        if (prevYRotate <= 90f && yRotate >= 90f)
        {
            cardType = (cardType + 1) % 2;
            transform.Find("CardSprite").GetComponent<Image>().sprite = cards[cardType];
        }
        if (prevYRotate <= 270f && yRotate >= 270f)
        {
            cardType = (cardType + 1) % 2;
            transform.Find("CardSprite").GetComponent<Image>().sprite = cards[cardType];
            //Debug.Log("rote");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (effectType == 0)
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
            if (effectType == 1)
            {
                //Debug.Log("same suit" + Card.CARDS[card]);
                Sparkle(new Color(240 / 255, 255 / 255, 0));
            }
            if (effectType == 2)
            {
                Color c = Color.black;
                Sparkle(c);
                Shines();
                //Debug.Log("same val" + Card.CARDS[card]);
            }
        }
        switch(animationType){
            case 0:
                Fall();
                Spin();
                break;
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
