using UnityEngine;
using System.Collections;

/** 回転して吹っ飛ぶパターン */
public class Damage : MonoBehaviour {
    float m_speed;

    // Use this for initialization
    void Start() {
        //SE.
        GetComponent<AudioSource>().clip = GetComponent<Player>().m_hitSE;
        GetComponent<AudioSource>().Play();
        //Effect.
        GameObject effect = transform.Find("HitEffect").gameObject;
        effect.transform.parent = null;                 //キャラクターに付随しないように親設定を外す.
        effect.GetComponent<ParticleSystem>().Play();   //再生.


        //物理つける.
        gameObject.AddComponent<Rigidbody>();
        GetComponent<Rigidbody>().AddForce(Vector3.up * 6.0f, ForceMode.VelocityChange);    //上に飛ばす.

        //-2.0f～-1.0fの範囲で作る.
        float r = Random.Range(-2.0f, -1.0f);
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

        //Debug.Log("start" + Time.time);
        gameObject.GetComponent<Player>().ChangeAnimation(Player.Motion.Damage);
    }

    // Update is called once per frame
    void Update() {
        transform.Rotate(Vector3.up * 900 * Time.deltaTime * m_speed, Space.Self);       //横に回転.
        transform.Rotate(Vector3.forward * 200 * Time.deltaTime * m_speed, Space.World); //縦に回転.
    }

    //hit.
    void OnCollisionEnter(Collision col) {
        //落下中に何かに当たったら回転を止める.
        if (GetComponent<Rigidbody>().velocity.y < 0) {
            if (m_speed != 0) {
                GetComponent<AudioSource>().clip = GetComponent<Player>().m_collideGroundSE; //m_collideSE;
                GetComponent<AudioSource>().Play();
            }

            m_speed = 0;
        }

        //Debug.Log("col" + Time.time);
    }
}
