using UnityEngine;
using System.Collections;

/** 攻守選択用のパネル */
public class BattlePanel : MonoBehaviour {
    public AudioClip m_onCursorSE;  //カーソルが乗ったときのSE.
    public AudioClip m_decideSE;    //決定音.

    public ActionKind m_actionKind;
    bool m_isSelected;  //選択されたときはtrue.

    enum State {    //演出.
        FadeIn,
        SelectWait,
        FadeOut,
        End,
    }
    State m_state;

    //アニメーション.
    State m_currentAnimation;
    Animation m_animation;      
    void ChangeAnimation(State animation) {
        m_currentAnimation = animation;
        m_animation.Play(m_currentAnimation.ToString());
    }

	// Use this for initialization
	void Start () {
        m_isSelected = false;
        m_state = State.FadeIn;

        m_animation = GetComponent<Animation>();
        m_currentAnimation = State.FadeIn;
        ChangeAnimation(State.FadeIn);
	}
	
	// Update is called once per frame
	void Update () {

        switch (m_state) {
        case State.FadeIn:
            UpdateFadeIn();
            break;
        case State.SelectWait:
            UpdateSelectWait();
            break;
        case State.FadeOut:
            UpdateFadeOut();
            break;
        }
	}


    //入場.
    void UpdateFadeIn() {
        //アニメーションが終わったら次の状態へ.
        if (m_animation.isPlaying == false) {
            m_state = State.SelectWait;
        }
    }

    //選択待ち.
    void UpdateSelectWait() {
        if (IsHit()) {
            //カーソルが乗ったときSEを鳴らす.
            if (transform.localScale == Vector3.one) {
                GetComponent<AudioSource>().clip = m_onCursorSE;
                GetComponent<AudioSource>().Play();
            }

            transform.localScale = Vector3.one * 1.2f;
            if (Input.GetMouseButtonDown(0)) {
                m_isSelected = true; //状態の通知は親に任せる.
                //SE.
                GetComponent<AudioSource>().clip = m_decideSE;
                GetComponent<AudioSource>().Play();
            }
        }
        else {
            transform.localScale = Vector3.one;
        }
    }

    //退場.
    void UpdateFadeOut() {
        if (m_currentAnimation != State.FadeOut) {
            ChangeAnimation(State.FadeOut);
        }

        //アニメーションが終わったら次の状態へ.
        if (m_animation.isPlaying == false) {
            m_state = State.End;
        }
    }



    //マウスが乗っていればtrueを返す.
    bool IsHit() {
        GameObject obj = GameObject.Find("GUICamera");
        Ray ray = obj.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit raycastHit;

        return GetComponent<Collider>().Raycast(ray, out raycastHit, 100);
    }



    //クリックされていたらtrue.
    public bool IsSelected() {
        return m_isSelected;
    }

    //攻防選択の決定後の演出に移行する.
    public void ChangeSelectedState() {
        m_state = State.FadeOut;

        if (m_isSelected == false) {
            Destroy(gameObject); //選択されなかったパネルは消す.
        }
    }
}
