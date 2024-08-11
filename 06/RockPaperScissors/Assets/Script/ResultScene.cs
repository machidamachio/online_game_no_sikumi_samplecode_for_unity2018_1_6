using UnityEngine;
using System.Collections;
using System;

/** リザルトの制御 */
public class ResultScene : MonoBehaviour {
    public GameObject m_winPrefab;
    public GameObject m_losePrefab;
    GameObject m_winLose; //勝ち・負けの演出用のオブジェクト.

    public enum Result {    //自分の勝敗結果を表す.
        Win,
        Lose,
        Draw,
    }
    Result m_result;

    enum State {
        Wait,           //待機中.
        ScoreAnimation, //得点演出中.
        End,            //演出終了.
    }
    State m_state;
    float m_endTimer; //演出終了待ちの時間計測用.

	// Use this for initialization
	void Start () {
        m_state = State.Wait;
    }
	
	// Update is called once per frame
	void Update () {
        switch (m_state) {
        case State.Wait:
            UpdateWait();
            break;

        case State.ScoreAnimation:
            UpdateScoreAnimation();
            break;

        case State.End:
            break;
        }
	}

    //待機中.
    void UpdateWait() {
        bool isPlaying = false;
        if (m_winLose) { //引き分けのときはm_winLose==null.
            isPlaying = m_winLose.GetComponent<Animation>().isPlaying;
        }

        if (isPlaying == false) {
            //勝ち負け表示が済んだら、得点演出を開始する.
            Score[] scores = GetComponentsInChildren<Score>();
            foreach (Score s in scores) {
                s.StartAnimation();
            }
            GameObject.Find("hyphen").GetComponent<AsciiCharacter>().SetChar('-');

            m_state = State.ScoreAnimation;
        }
    }

    //得点演出中.
    void UpdateScoreAnimation() {
        Score[] scores = GetComponentsInChildren<Score>();
        foreach (Score s in scores) {
            if (s.IsEnd() == false) {
                return;     //演出中なら何もしない.
            }
        }
        //演出終了なら状態切り替え.
        m_endTimer = Time.time;
        m_state = State.End;
    }



    //スコア、勝敗を渡してください.
    public void Setup(int[] prevScores, int[] scores, Result result) {
        Debug.Log("GameWinner:" + result.ToString() + "[" + scores[0] + " - " + scores[1] + "]");
        
        m_result = result;

        //得点表示の設定をする.
        string[] names = { "Score0", "Score1" };
        for(int i=0; i < names.Length; ++i){
            Transform scoreTransform = transform.Find( names[i] );
            Score s = scoreTransform.GetComponent<Score>();
            s.Setup( prevScores[i], scores[i]);
        }

        //勝ち負け演出を出現させる.
        if(m_result == Result.Win){
            m_winLose = Instantiate(m_winPrefab) as GameObject;
            m_winLose.transform.parent = transform;
        }
        else if(m_result == Result.Lose){
            m_winLose = Instantiate(m_losePrefab) as GameObject;
            m_winLose.transform.parent = transform;
        }

    }

    //演出終了ならtrue.
    public bool IsEnd() {
        if (m_state == State.End) {
            float dt = Time.time - m_endTimer;
            return (dt > 2.0f);
        }
        return false;
    }
}
