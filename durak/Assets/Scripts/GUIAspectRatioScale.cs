using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIAspectRatioScale : MonoBehaviour {

    public Vector2 scaleOnRatio = new Vector2(.1f, .1f);
    private float widthHeightRatio;
    float ratioW, ratioH;
    float originalW, originalH;
    // Use this for initialization
    void Start () {
        RectTransform rt = transform.GetComponent<RectTransform>();
        originalW = rt.rect.width;
        originalH = rt.rect.height;
        ratioW = originalW / GameManager.WIDTH;
        ratioH = originalH / GameManager.HEIGHT;
        Debug.Log(Screen.width + " " + Screen.height);
        SetScale();
	}
	
	// Update is called once per frame
	void Update ()
    {
        Debug.Log(gameObject.name + "> " + transform.GetComponent<RectTransform>().rect.width + " " + transform.GetComponent<RectTransform>().rect.height);
        GameManager.gm.gameCanvas.transform.Find("Text").GetComponent<Text>().text = Screen.width + "," + Screen.height;
        SetScale();
    }

    void SetScale()
    {

        //widthHeightRatio = (float)Screen.width / Screen.height;
        
        transform.localScale = new Vector3((float) Screen.width / GameManager.WIDTH,(float) Screen.height / GameManager.HEIGHT, 0);
    }
}
