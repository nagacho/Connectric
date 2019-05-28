using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveButton : MonoBehaviour {

    private GameObject LeftArrow;
    private GameObject RightArrow;
    private bool left = false;

	// Use this for initialization
	void Start () {
        LeftArrow = GameObject.Find("LeftButton");
        RightArrow = GameObject.Find("RightButton");
        left = false;
    }
	
	// Update is called once per frame
	void Update () {

        Vector3 leftpos = LeftArrow.GetComponent<RectTransform>().localPosition;
        Vector3 rightpos = RightArrow.GetComponent<RectTransform>().localPosition;

        if(leftpos.x < -370.0f)
        {
            leftpos.x = -370.0f;
            rightpos.x = 370.0f;
            left = true;
        }
        if (leftpos.x > -350.0f)
        {
            leftpos.x = -350.0f;
            rightpos.x = 350.0f;
            left = false;
        }
        if (!left)
        {
            leftpos.x -= 0.8f;
            rightpos.x += 0.8f;
        }
        if(left)
        {
            leftpos.x += 0.8f;
            rightpos.x -= 0.8f;
        }
        
        LeftArrow.GetComponent<RectTransform>().localPosition = leftpos;
        RightArrow.GetComponent<RectTransform>().localPosition = rightpos;

    }
}
