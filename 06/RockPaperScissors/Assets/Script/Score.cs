using UnityEngine;
using System.Collections;

/** リザルトで使用するスコア */
public class Score : MonoBehaviour {
    int m_prevScore;    //以前の点数.
    int m_newScore;     //変化後の点数.
    Animation m_animation;

    enum State {
        Wait,               //待機中.
        PreChangeAnimation, //縮むアニメーション.
        ChangeAnimation,    //数字変化後、大きくなるアニメーション.
        End,                //演出終了.
    };
    State m_state = State.Wait;


    void Awake() {
        m_animation = GetComponent<Animation>();
    }
	
	// Update is called once per frame
	void Update () {
	    switch(m_state){
        case State.Wait:
            break;
        case State.PreChangeAnimation:
            //縮小させる.
            if (m_animation.isPlaying == false) {
                GetComponentInChildren<AsciiCharacter>().SetNumber(m_newScore);
                m_animation.Play("Change");
                m_state = State.ChangeAnimation;
            }
            break;
        case State.ChangeAnimation:
            //拡大させる.
            if (m_animation.isPlaying == false) {
                m_state = State.End;
            }
            break;
        case State.End:
            break;
        }
	}

    
    //事前に呼び出してください.
    public void Setup(int prevScore, int newScore) {
        m_prevScore = prevScore;
        m_newScore = newScore;
    }

    //アニメーション開始.
    public void StartAnimation() {
        //ステート切り替え.
        if (m_prevScore == m_newScore) {
            m_state = State.End;    //この場合、アニメーションはないのでEndにする.
        }
        else {
            //アニメーションスタート.
            m_animation.Play("PreChange");
            m_state = State.PreChangeAnimation;
        }

        //スコア表示.
        GetComponentInChildren<AsciiCharacter>().SetNumber(m_prevScore);
    }

    //演出終了ならtrue.
    public bool IsEnd() {
        return (m_state == State.End);
    }
}
