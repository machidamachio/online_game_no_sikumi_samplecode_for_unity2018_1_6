using UnityEngine;
using System.Collections;

/** 戦闘アクションの制御 */
public class ActionController : MonoBehaviour {
    float m_time;


	// Use this for initialization
	void Start () {
        m_time = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
        
	}


    //演出終了ならtrue.
    public bool IsEnd() {
        float dt = Time.time - m_time;
        return (dt > 5.0f);
    }



    //再生させるモーションと、勝敗結果、今の得点を渡す.
    public void Setup(Winner winner, int serverScore, int clientScore) {
        GameObject serverPlayer = GameObject.Find("Daizuya");
        GameObject clientPlayer = GameObject.Find("Toufuya");

        //スロー再生を元に戻して、吹っ飛びアクションを発動させる.
        float delay = 0.0f;
        switch (winner) {
        case Winner.ServerPlayer:
            serverPlayer.GetComponent<Player>().SetDefaultAnimationSpeed();
            serverPlayer.GetComponent<Collider>().enabled = false; //吹っ飛び処理に干渉しないようにヒットを切る.
            delay = serverPlayer.GetComponent<Player>().GetRemainAnimationTime();
            clientPlayer.GetComponent<Player>().StartDamage(serverScore, delay * 0.3f); //モーションが半分くらいで終わるので*0.5fする.
            break;
        case Winner.ClientPlayer:
            clientPlayer.GetComponent<Player>().SetDefaultAnimationSpeed();
            clientPlayer.GetComponent<Collider>().enabled = false; //吹っ飛び処理に干渉しないようにヒットを切る.
            delay = clientPlayer.GetComponent<Player>().GetRemainAnimationTime();
            serverPlayer.GetComponent<Player>().StartDamage(clientScore, delay * 0.3f); //モーションが半分くらいで終わるので*0.5fする.
            break;
        case Winner.Draw:
        default:
            serverPlayer.GetComponent<Player>().SetDefaultAnimationSpeed();
            clientPlayer.GetComponent<Player>().SetDefaultAnimationSpeed();
            serverPlayer.GetComponent<Player>().ActionEffectOn(); //攻撃ミス音や失敗音を有効にする.
            clientPlayer.GetComponent<Player>().ActionEffectOn();
            break;
        }
    }

}

