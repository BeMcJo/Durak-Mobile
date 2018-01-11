using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SparkleEffect : MonoBehaviour {
    Image i;
    RectTransform rt;
    int alt;
    float altSize;
    float ttl, timer;
	// Use this for initialization
	void Start () {
        alt = 5;
        rt = GetComponent<RectTransform>();
        i = GetComponent<Image>();
        ttl = 100f;
        altSize = .01f;
        timer = Time.time;
        
    }
	
	// Update is called once per frame
	void Update () {
        //Debug.Log(timer + " " + Time.time + " " + i.color.a);
        if (i.color.a >= 1 || i.color.a <= 0)
            alt *= -1;
        //else if (i.color.a <= 0)
        //    alt = 1;
        Color c = i.color;
        c.a = ((c.a * 255) + alt)/255;
        //i.color = c;
        //Debug.Log(timer + " " + Time.time + " " + i.color.a);
        transform.Rotate(new Vector3(0, 0, 2.5f));
        if(transform.localScale.x <= 0 || transform.localScale.x >= 1)
        {
            altSize *= -1;
        }
        transform.localScale += new Vector3(altSize, altSize, 0);
        if (transform.localScale.x <= 0)
            Destroy(gameObject);
    }
}
