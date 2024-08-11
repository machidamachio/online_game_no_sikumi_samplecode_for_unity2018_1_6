using UnityEngine;
using System.Collections;

public class BoardYou : MonoBehaviour {
    enum State {
        Run,
        Sleep,
    };
    State m_state;
    Animation m_anim;

	// Use this for initialization
	void Start () {
        m_anim = GetComponent<Animation>();
        Sleep();
    }
	
    
	// Update is called once per frame
	void Update () {
	}

    //表示を有効にする.
    public void Run() {
        if (m_state == State.Run) {
            return; //既に動いていればなにもしない.
        }
        m_state = State.Run;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        Color col = renderer.color;
        col.a = 1;
        renderer.color = col;

        m_anim.Play("appeal");
    }

    //表示を無効にする.
    public void Sleep() {
        m_state = State.Sleep;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        Color col = renderer.color;
        col.a = 0;
        renderer.color = col;

        m_anim.Stop("appeal");
    }

    //演出が終わっていればtrue.
    public bool IsEnd() {
        AnimationState animState = m_anim["appeal"];
        if (animState.time >= animState.length) {
            return true;
        }
        return false;
    }
}
