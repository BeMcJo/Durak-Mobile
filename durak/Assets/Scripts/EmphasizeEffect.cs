using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmphasizeEffect : MonoBehaviour {

    bool resizingX = false;
    bool resizingY = false;
    float stepX = .01f, stepY = .01f, x, y, stepVal = .005f;
    float minSize = .75f, maxSize = 1.2f;
    // Use this for initialization
    void Start () {
		
	}

    bool ReachedSizeX()
    {
        //Debug.Log(x + " " + transform.localScale.x + " " + stepX);
        transform.localScale += new Vector3(stepX, 0, 0);
        if (stepX > 0)
            resizingX = transform.localScale.x < x;
        else
            resizingX = transform.localScale.x > x;
        return !resizingX;
    }

    bool ReachedSizeY()
    {
        //Debug.Log(y + " " + transform.localScale.y + " " + stepY);
        transform.localScale += new Vector3(0, stepY, 0);
        if (stepY > 0)
            resizingY = transform.localScale.y < y;
        else
            resizingY = transform.localScale.y > y;
        return !resizingY;
    }

    IEnumerator RandomResizeX()
    {
        if (!resizingX)
        {
            //resizingX = true;
            x = Random.Range(minSize, maxSize);
            if (x >= transform.localScale.x)
                stepX = stepVal;
            else
                stepX = -stepVal;
            yield return new WaitUntil(ReachedSizeX);
        }
    }

    IEnumerator RandomResizeY()
    {
        if (!resizingY)
        {
            //resizingY = true;
            y = Random.Range(minSize, maxSize);
            if (y >= transform.localScale.y)
                stepY = stepVal;
            else
                stepY = -stepVal;
            yield return new WaitUntil(ReachedSizeY);
        }
    }


    // Update is called once per frame
    void Update () {
        transform.Rotate(new Vector3(0, 0, 2.5f));
        StartCoroutine("RandomResizeX");
        StartCoroutine("RandomResizeY");
    }
}
