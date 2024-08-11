using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {
    public AudioClip m_landingSE;   //着地音.
    public AudioClip m_missSE;      //空振り音.
    public AudioClip m_slipSE;      //転ぶ音.

    //吹っ飛び処理はAddComponentで追加される-->SEを持つ場所がないため、ここで保持します.
    public AudioClip m_hitSE;       //最初の吹っ飛び音.
    public AudioClip m_collideSE;   //書き割りに当たったときの音.
    public AudioClip m_collideGroundSE; //地面にぶつかったときの音.

    //アニメーション定義.
    public enum Motion {
        In,             //入場.
        Idle,           //待機モーション.
        RPSInputWait,   //ジャンケン選択待ち.
        Rock,           //ジャンケングー.
        Paper,          //ジャンケンパー.
        Scissor,        //ジャンケンチョキ.
        AttackRock,     //攻撃グー.
        AttackPaper,    //攻撃パー.
        AttackScissor,  //攻撃チョキ.
        MissRock,       //失敗グー.
        MissPaper,      //失敗パー.
        MissScissor,    //失敗チョキ.
        Defence,        //防御.
        Damage,         //吹っ飛び.
    };
    Motion m_currentMotion;
    Animation m_anim;

    RPSKind m_rps;
    RPSKind m_opponentRps;  //対戦相手のジャンケン.アニメーションさせる都合でここで保持します.
    int m_damage;           //吹っ飛びアクション用のダメージ値.
    bool m_actionEffectEnable; //アクションのエフェクトが有効ならtrue. (ダメージの時は互いにOFFになってるはず).
    bool m_actionSoundEnable;  //アクションの効果音が有効ならtrue. (ダメージの時は互いにOFFになってるはず).

    void Awake() {
        GetComponent<AudioSource>().clip = m_landingSE;
        GetComponent<AudioSource>().PlayDelayed(0.2f); //着地音を遅れて再生させる.

        m_currentMotion = Motion.In;
        m_anim = GetComponentInChildren<Animation>();

        m_rps = RPSKind.None;
        m_opponentRps = RPSKind.None;
        m_damage = 0;
        m_actionEffectEnable = false;
        m_actionSoundEnable = false;
    }
	
	// Update is called once per frame
	void Update () {
        switch (m_currentMotion) {
        case Motion.In:             //入場.
            if (m_anim.isPlaying == false) {
                ChangeAnimation(Motion.Idle);
                //待機モーションに移る時に、プレイヤー表記を出す.
                GameObject board = GameObject.Find("BoardYou");
                board.GetComponent<BoardYou>().Run();
            }
            break;
        case Motion.Idle:           //待機モーション.
        case Motion.RPSInputWait:   //ジャンケン選択待ち.
        case Motion.Rock:           //ジャンケングー.
        case Motion.Paper:          //ジャンケンパー.
        case Motion.Scissor:        //ジャンケンチョキ.
            break;

        case Motion.AttackRock:     //攻撃グー.
        case Motion.AttackPaper:    //攻撃パー.
        case Motion.AttackScissor:  //攻撃チョキ.
            //SE.
            if (m_actionSoundEnable) {
                if (GetRemainAnimationTime() < 1.7f) {
                    m_actionSoundEnable = false;
                    GetComponent<AudioSource>().clip = m_missSE;
                    GetComponent<AudioSource>().Play();
                }
            }
            break;

        case Motion.MissRock:       //失敗グー.
        case Motion.MissPaper:      //失敗パー.
        case Motion.MissScissor:    //失敗チョキ.
            //SE.
            if (m_actionSoundEnable) {
                if (GetRemainAnimationTime() < 1.1f) {
                    m_actionSoundEnable = false;
                    GetComponent<AudioSource>().clip = m_slipSE;
                    GetComponent<AudioSource>().Play();
                }
            }
            //Effect.
            if (m_actionEffectEnable) {
                if (GetRemainAnimationTime() < 0.5f) {
                    m_actionEffectEnable = false;
                    transform.Find("kurukuru").gameObject.SetActive(true);
                }
            }
            break;

        case Motion.Defence:        //防御.
            //Effect.
            if (m_actionEffectEnable) {
                if (GetRemainAnimationTime() < 1.7f) {
                    m_actionEffectEnable = false;
                    transform.Find("SweatEffect").gameObject.SetActive(true);
                }
            }
            if (IsCurrentAnimationEnd()) {
                transform.Find("SweatEffect").gameObject.SetActive(false);
            }
            break;

        case Motion.Damage:         //吹っ飛び.
            break;
        }
	}


    public void ChangeAnimation(Motion motion) {
        m_currentMotion = motion;
        m_anim.Play(m_currentMotion.ToString());
    }
    public void ChangeAnimationJanken() {
        switch (m_rps) {
        case RPSKind.Rock:
            ChangeAnimation(Motion.Rock);
            break;
        case RPSKind.Paper:
            ChangeAnimation(Motion.Paper);
            break;
        case RPSKind.Scissor:
            ChangeAnimation(Motion.Scissor);
            break;
        }
        Invoke("StarEffectOn", 0.5f); //エフェクト再生させる.
    }

    //ポンッのときの星のエフェクトを有効にする.
    void StarEffectOn() {
        GameObject star = transform.Find("StarEffect").gameObject;
        star.GetComponent<ParticleSystem>().Play();
    }


    public void ChangeAnimationAction(ActionKind action) {
        //サーバー、クライアントでの判定しかできないので、Winner.serverPlayerなら自分の勝ちとして扱います.
        Winner rpsWinner = ResultChecker.GetRPSWinner(m_rps, m_opponentRps);
        switch (rpsWinner) {
        case Winner.ServerPlayer:   //ジャンケンは自分の勝ち.
            if (action == ActionKind.Attack) {
                ChangeAnimationAttack();
            }
            else if (action == ActionKind.Block) {
                ChangeAnimation(Motion.Defence);
            }
            break;
        case Winner.ClientPlayer:   //ジャンケンは自分の負け.
            if (action == ActionKind.Attack) {
                ChangeAnimationMiss();
            }
            else if (action == ActionKind.Block) {
                ChangeAnimation(Motion.Defence);
            }
            break;
        case Winner.Draw:           //ジャンケンは引き分け.
            if (action == ActionKind.Attack) {
                ChangeAnimationMiss();
            }
            else if (action == ActionKind.Block) {
                ChangeAnimation(Motion.Defence);
            }
            break;
        }
        //Debug.Log(m_currentMotion.ToString() + m_anim[m_currentMotion.ToString()].length);
        //Debug.Log(m_anim[m_currentMotion.ToString()].speed);
        //Debug.Log(m_anim[m_currentMotion.ToString()].normalizedTime);

        m_anim[m_currentMotion.ToString()].speed = 0.1f; //スロー再生させます.
    }

    //通常の再生速度にする.
    public void SetDefaultAnimationSpeed() {
        m_anim[m_currentMotion.ToString()].speed = 1.0f;
    }

    //アニメーションの残り時間.
    public float GetRemainAnimationTime() {
        AnimationState anim = m_anim[m_currentMotion.ToString()];
        float time = anim.time;
        while (time > anim.length) {
            time -= anim.length;
        }
        //Debug.Log(anim.length - time);
        return anim.length - time;
    }

    
    void ChangeAnimationAttack() {
        switch (m_rps) {
        case RPSKind.Rock:
            ChangeAnimation(Motion.AttackRock);
            break;
        case RPSKind.Paper:
            ChangeAnimation(Motion.AttackPaper);
            break;
        case RPSKind.Scissor:
            ChangeAnimation(Motion.AttackScissor);
            break;
        }
    }
    void ChangeAnimationMiss() {
        switch (m_rps) {
        case RPSKind.Rock:
            ChangeAnimation(Motion.MissRock);
            break;
        case RPSKind.Paper:
            ChangeAnimation(Motion.MissPaper);
            break;
        case RPSKind.Scissor:
            ChangeAnimation(Motion.MissScissor);
            break;
        }
    }
    
    //自分と対戦相手のジャンケンの手をセットする.
    public void SetRPS(RPSKind rps, RPSKind opponentRps) {
        m_rps = rps;
        m_opponentRps = opponentRps;
    }


    //startTime秒後に吹っ飛び処理を開始する.
    public void StartDamage(int damage /*[0:2]*/, float startTime) {
        m_damage = damage;
        Invoke("SetDamage", startTime);
    }

    void SetDamage() {
        SetDefaultAnimationSpeed(); //アニメーションスピードを元に戻す.
        if (m_damage == 0) {
            //gameObject.AddComponent<LightDamage>();
            gameObject.AddComponent<Damage>();
        }
        else if (m_damage == 1) {
            gameObject.AddComponent<Damage>();
        }
        else {
            gameObject.AddComponent<HeavyDamage>();
        }
    }
    

    //アニメーションが終わっていればtrue.
    public bool IsCurrentAnimationEnd() {
        return (m_anim.isPlaying == false);
        //AnimationState current = m_anim[m_currentMotion.ToString()];
        //if (current.time >= current.length) {
        //    return true;
        //}
        //return false;
    }

    //待機アニメーションならtrue.
    public bool IsIdleAnimation() {
        return (m_currentMotion == Motion.Idle);
    }

    //アクション中のエフェクトを有効にする.
    public void ActionEffectOn() {
        m_actionEffectEnable = true;
        m_actionSoundEnable = true;
    }
}
