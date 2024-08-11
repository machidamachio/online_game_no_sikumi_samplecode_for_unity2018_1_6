using UnityEngine;
using System.Collections;

/** 盛大に吹っ飛ぶパターン */
public class HeavyDamage : MonoBehaviour {
    float m_boundPower; //他のものに当たったときの跳ね返り具合.
    float m_speed;

    //吹っ飛びパターンその2.
    void Start() {
        //SE.
        GetComponent<AudioSource>().clip = GetComponent<Player>().m_hitSE; ;
        GetComponent<AudioSource>().Play();
        //Effect.
        GameObject effect = transform.Find("HitEffect").gameObject;
        effect.transform.parent = null;                 //キャラクターに付随しないように親設定を外す.
        effect.GetComponent<ParticleSystem>().Play();   //再生.


        //物理つける.
        gameObject.AddComponent<Rigidbody>();
        GetComponent<Rigidbody>().AddForce(0.0f, 5.0f, 2.0f, ForceMode.VelocityChange);    //奥へ飛ばす.

        //-2.5f～1.0fの範囲で作る.
        float r = Random.Range(-2.5f, 1.0f);
        if (gameObject.name == "Daizuya") {
            r = -r;     //1P,2Pでの切り替え.
        }

        if (r < 0) {
            m_speed = 1.0f;
        }
        else {
            m_speed = -1.0f;
        }

        GetComponent<Rigidbody>().AddForce(Vector3.right * r, ForceMode.VelocityChange);
        //Debug.Log(r);

        m_boundPower = 1.0f;
        gameObject.GetComponent<Player>().ChangeAnimation(Player.Motion.Damage);
    }


    void Update() {
        transform.Rotate(Vector3.up * 900 * Time.deltaTime * m_speed, Space.Self);       //横に回転.
        transform.Rotate(Vector3.forward * 200 * Time.deltaTime * m_speed, Space.World); //縦に回転.
    }


    void OnCollisionEnter(Collision col) {
        if (GetComponent<Rigidbody>().velocity.y >= 0 && col.gameObject.name == "ground") {
            return;     //地面から浮くまえにoncollisionenterされてしまうので対策.
        }
        //SE.
        if (col.gameObject.name == "ground") {  //地面衝突時には地面用のSEを再生.
            GetComponent<AudioSource>().clip = GetComponent<Player>().m_collideGroundSE;
            GetComponent<AudioSource>().Play();
        }
        else if (m_boundPower > 0.1f) {         //ある程度弱まったらSEが鳴らないようにする.
            GetComponent<AudioSource>().clip = GetComponent<Player>().m_collideSE;
            GetComponent<AudioSource>().Play();
        }

        
        //ヒットした場所に対して力を加える。面白い飛び方をするようにパラメータの加工をしてます.
        Vector3 v = col.relativeVelocity;
        if (v.y < 0) {
            v.y = -v.y;
        }
        v.z = -v.z;

        //自分を吹っ飛ばす.
        GetComponent<Rigidbody>().AddForce(Vector3.up * 4 * m_boundPower, ForceMode.VelocityChange);

        //当たったものを吹っ飛ばす.
        Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();
        if (rb) {
            rb.AddForceAtPosition(v * 2.0f * m_boundPower, col.contacts[0].point, ForceMode.VelocityChange);
        }


        //何かに当たったら回転を止める.
        m_speed = 0;
        //何かに当たったら跳ね返りを弱めていく.
        m_boundPower *= 0.8f;
    }
}
