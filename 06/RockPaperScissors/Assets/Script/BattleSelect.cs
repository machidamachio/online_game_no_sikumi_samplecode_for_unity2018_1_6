using UnityEngine;
using System.Collections;

/** 叩いてかぶっての選択 */
public class BattleSelect : MonoBehaviour {
    ActionKind m_selected; //叩いてかぶっての選択.

    enum State {
        SelectWait, //選択待ち.
        Selected,   //選択終了.
    }
    State m_state;

	// Use this for initialization
	void Start () {
        m_selected = ActionKind.None;
        m_state = State.SelectWait;
	}
	
    
	// Update is called once per frame
	void Update () {

        switch (m_state) {
        case State.SelectWait:
            UpdateSelectWait();
            break;
        case State.Selected:
            UpdateSelected();
            break;
        }        
	}

    //選択中.
    void UpdateSelectWait() {

        //選択されたかをチェック.
        BattlePanel[] panels = transform.GetComponentsInChildren<BattlePanel>();
        foreach (BattlePanel p in panels) {
            if (p.IsSelected()) {   //選択済みのパネルがあった.
                m_selected = p.m_actionKind;
            }
        }

        //選択・時間切れで次の状態に変える.
        Timer timer = transform.GetComponentInChildren<Timer>();
        if (m_selected != ActionKind.None || timer.IsTimeZero()) {
            //各パネルを選択後の演出に変える.
            foreach (BattlePanel p in panels) {
                p.ChangeSelectedState();
            }

            timer.Stop();
            m_state = State.Selected;
        }
    }

    //選択後.
    void UpdateSelected() {
        //Debug.Log("BattleSelect end.");
    }



    //選択終了ならtrue.
    public bool IsEnd() {
        if (m_state == State.Selected) {
            return true;
        }
        return false;
    }

    //選択時間を返す.
    public float GetTime() {
        Timer timer = transform.GetComponentInChildren<Timer>();
        return timer.GetNumber();
    }

    //選択された行動を返す.
    public ActionKind GetActionKind() {
        return m_selected;
    }

    //
    public void Setup(RPSKind kind0, RPSKind kind1) {
        //選択済みのジャンケンを表示したいときはここでInstantiateする.
        ////キャラクターがジャンケンの看板を持つようになったため現在未使用です.

        //Debug.Log(kind0.ToString());
        //Debug.Log(kind1.ToString());
        //Debug.Log("BattleSelect Setup");
    }
}

