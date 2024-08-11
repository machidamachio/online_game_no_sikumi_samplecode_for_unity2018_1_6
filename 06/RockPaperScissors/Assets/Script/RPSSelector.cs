using UnityEngine;
using System.Collections;

/** ジャンケンの選択パネルの管理 */
public class RPSSelector : MonoBehaviour {
    RPSKind m_selected; //ジャンケンの選択.

    // Use this for initialization
    void Start() {
        m_selected = RPSKind.None;

        string[] names = { "Daizuya", "Toufuya" };
        foreach (string n in names) {
            GameObject player = GameObject.Find(n);
            player.GetComponent<Player>().ChangeAnimation(Player.Motion.RPSInputWait);
        }
    }

    // Update is called once per frame
    void Update(){
        if(m_selected != RPSKind.None){
            return;     //選択済みなので何もしない.
        }

        RPSPanel[] panels = transform.GetComponentsInChildren<RPSPanel>();
        foreach (RPSPanel p in panels) {
            if (p.IsSelected()) {   //選択済みのパネルがあった.
                m_selected = p.m_rpsKind;
            }
        }

        if (m_selected != RPSKind.None) {
            //各パネルを選択後の演出に変える.
            foreach (RPSPanel p in panels) {
                p.ChangeSelectedState();
            }
        }
    }


    //まだ選択されていないときはRPSKind.Noneが返ります.
    public RPSKind GetRPSKind() {
        RPSPanel[] panels = transform.GetComponentsInChildren<RPSPanel>();
        foreach (RPSPanel p in panels) {
            if (p.IsEnd() == false) {   //演出待ちのときは未決定扱いにする.
                return RPSKind.None;
            }
        }

        return m_selected;
    }
    
}
