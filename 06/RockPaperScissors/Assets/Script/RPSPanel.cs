using UnityEngine;
using System.Collections;

/** ジャンケンの手を表示する */
public class RPSPanel : MonoBehaviour {
    public AudioClip m_onCursorSE;  //カーソルが乗ったときのSE.
    public AudioClip m_decideSE;    //決定音.

    public RPSKind m_rpsKind;
    bool m_isSelected;  //選択されたときはtrue.

    enum State {
        FadeIn,     //入場.
        SelectWait, //選択待ち.
        OnSelected, //選択された.
        UnSelected, //選択されなかった.
        FadeOut,    //退場.
        End,
    }
    State m_state;

    //アニメーション.
    State m_currentAnimation;
    Animation m_animation;
    void ChangeAnimation(State animation) {
        m_currentAnimation = animation;

        //フェードアウト時だけ別アニメーションになっています.
        if (m_currentAnimation == State.FadeOut) {
            //FadeOut_Rock, FadeOut_Paper, FadeOut_Scissor,
            string name = m_currentAnimation.ToString() + "_" + m_rpsKind.ToString();
            m_animation.Play(name);
        }
        else {
            m_animation.Play(m_currentAnimation.ToString());
        }
    }

	// Use this for initialization
	void Start () {
        m_state = State.FadeIn;
        m_isSelected = false;

        transform.localScale = Vector3.zero;

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
        case State.OnSelected:
            UpdateOnSelected();
            break;
        case State.UnSelected:
            UpdateUnSelected();
            break;
        case State.FadeOut:
            UpdateFadeOut();
            break;
        case State.End:
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

            //選択範囲に入っていたら拡大表示.
            transform.localScale = Vector3.one * 1.2f;
            if (Input.GetMouseButtonDown(0)) {
                m_isSelected = true;    //クリックされた.
                //SE.
                GetComponent<AudioSource>().clip = m_decideSE;
                GetComponent<AudioSource>().Play();

                /*
                 * 親の方で状態を監視するので、ここではまだstateは変えません.
                 */
            }
        }
        else {
            transform.localScale = Vector3.one;
        }
    }

    //選択された状態.
    void UpdateOnSelected() {
        if (m_currentAnimation != State.OnSelected) {
            ChangeAnimation(State.OnSelected);
        }

        //アニメーションが終わったら次の状態へ.
        if (m_animation.isPlaying == false) {
            m_state = State.FadeOut;
        }
    }

    //選択されなかった.
    void UpdateUnSelected() {
        if (m_currentAnimation != State.UnSelected) {
            ChangeAnimation(State.UnSelected);
        }

        //アニメーションが終わったら次の状態へ.
        if (m_animation.isPlaying == false) {
            m_state = State.End;
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
    public bool IsSelected(){
        return m_isSelected;
    }

    //終了してるならtrue.
    public bool IsEnd() {
        return (m_state == State.End);
    }

    //ジャンケン決定後の演出に移行する.
    public void ChangeSelectedState() {
        m_state = State.UnSelected;
        if (m_isSelected) {
            m_state = State.OnSelected;
        }
    }

}
