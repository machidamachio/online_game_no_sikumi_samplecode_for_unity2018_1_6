using UnityEngine;
using System.Collections;

/** ジャン・ケン・ポン! の掛け声演出 */
public class ShootCall : MonoBehaviour {
    public GameObject m_jankenPrefab;
    public GameObject m_ponPrefab;
    
    GameObject m_janken = null;
    GameObject m_pon = null;
    GameObject[] m_players;

    enum State {    //演出.
        Janken,
        PonIn,
        PonOut,
        End,
    }
    State m_state;

	// Use this for initialization
	void Start () {
        m_state = State.Janken;

        m_players = new GameObject[2];
        m_players[0] = GameObject.Find("Daizuya");
        m_players[1] = GameObject.Find("Toufuya");
	}
	
	// Update is called once per frame
	void Update () {
        switch (m_state) {
        case State.Janken:
            UpdateJanken();
            break;
        case State.PonIn:
            UpdatePonIn();
            break;
        case State.PonOut:
            UpdatePonOut();
            break;
        case State.End:
            break;
        }
	}

    //【ジャンケン】表示中.
    void UpdateJanken() {
        if (m_janken == null) {
            //初期化.
            Vector3 pos = new Vector3(0, 0, 0);
            m_janken = Instantiate(m_jankenPrefab, pos, Quaternion.identity) as GameObject;
            GetComponent<AudioSource>().PlayDelayed(0.7f); //「ケン」のSEを遅れて再生させます.
        }
        
        Animation animation = m_janken.GetComponent<Animation>();
        if(animation.isPlaying == false){
            m_state = State.PonIn;
        }
    }

    //【ポン！】入場.
    void UpdatePonIn() {
        if (m_pon == null) {
            //初期化.
            Destroy(m_janken); //ジャンケンの表示は消す.
            Vector3 pos = new Vector3(0.42f, 1.8f, 0);
            m_pon = Instantiate(m_ponPrefab, pos, Quaternion.identity) as GameObject;

            //プレイヤーのアニメーションをジャンケンにする.
            foreach (GameObject player in m_players) {
                player.GetComponent<Player>().ChangeAnimationJanken();
            }
        }

        Animation animation = m_pon.GetComponent<Animation>();
        if (animation.isPlaying == false) {
            m_state = State.PonOut;
            animation.Play("FadeOut");
        }
    }

    //【ポン！】退場.
    void UpdatePonOut() {
        Animation animation = m_pon.GetComponent<Animation>();

        //プレイヤーのアニメーション待ち.
        bool isEndAnimation = true;
        foreach (GameObject player in m_players) {
            if (player.GetComponent<Player>().IsCurrentAnimationEnd() == false) {
                isEndAnimation = false;
            }
        }
        if (isEndAnimation == true && animation.isPlaying == false){
            //Debug.Log(isEndAnimation);
            m_state = State.End;
        }
    }
    
    //演出終わりならtrue.
    public bool IsEnd() {
        return (m_state == State.End);
    }
}
