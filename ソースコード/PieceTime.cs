using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceTime : MonoBehaviour {

    private bool finAnimFrag;
    public bool FinAnim
    {
        set { finAnimFrag = value; }
        get { return finAnimFrag; }
    }

    private GameObject game_manager;

    // Use this for initialization
    void Start () {

        game_manager = GameObject.Find("GameManager");
        this.GetComponent<Animator>().SetTrigger("CountDownTrigger");
        this.GetComponent<Animator>().Play("CountDown",0);                    // 即座に再生
        this.GetComponent<Animator>().CrossFade("CountDown", 0, 0, 0.5f);     // 飛ばして再生
    }
	
	// Update is called once per frame
	void Update () {

        if (game_manager.GetComponent<GameManager>().IsGameClear || 
              game_manager.GetComponent<GameManager>().IsGameOver ||
               game_manager.GetComponent<GameManager>().IsPause)
        {
            this.GetComponent<Animator>().speed = 0;
            return;
        }

        this.GetComponent<Animator>().speed = 1;

        if (!Mouse.CaptureFlag)
        {
            this.GetComponent<Transform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }


        if (this.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
        {
            finAnimFrag = true;
        }

    }

    //-------------------------------------------
    // キャプチャ中に大きくする
    //-------------------------------------------
    public void Big()
    {
        this.GetComponent<Transform>().localScale = new Vector3(0.6f, 0.6f, 0.6f);
    }
    
}
