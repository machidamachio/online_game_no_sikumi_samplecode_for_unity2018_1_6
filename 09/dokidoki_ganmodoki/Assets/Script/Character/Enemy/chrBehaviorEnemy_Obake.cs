using UnityEngine;
using System.Collections;

// 敵ビヘイビアー　おばけ.
public class chrBehaviorEnemy_Obake : chrBehaviorEnemy {

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
	}

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	public override void	initialize()
	{
		base.initialize();

		base.setBehaveKind(Enemy.BEHAVE_KIND.UROURO);
	}

	public override void	start()
	{
		base.start();

		this.control.vital.setHitPoint(5.0f);
		this.control.vital.setAttackPower(2.0f);
		this.control.vital.setAttackDistance(2.0f);
	}


	public override	void	execute()
	{
		base.execute();
		this.basic_action.execute();

		this.is_attack_motion_finished = false;
		this.is_attack_motion_impact = false;

		/*if(Input.GetKeyDown(KeyCode.A)) {

			this.shootBullet();
		}*/
	}

	// 進行方向へ向けて EnemyBullet を発射する.
	public void 	shootBullet()
	{
		GameObject	go = GameObject.Instantiate(CharacterRoot.get().enemy_bullet_prefab) as GameObject;
		
		go.transform.position = this.transform.TransformPoint(new Vector3(0.0f, 0.25f, 1.0f));
		go.transform.rotation = Quaternion.AngleAxis(this.control.getDirection(), Vector3.up);
		
		EnemyBulletControl		bullet = go.GetComponent<EnemyBulletControl>();

		bullet.owner = this.control;
	}

	// ================================================================ //
	// アニメーションイベント.

	// Attack モーションで打撃が相手にヒットする瞬間.
	public void 	NotifyAttackImpact()
	{
		this.is_attack_motion_impact = true;
	}

	// Attack モーションが終わったとき.
	public void		NotifyFinishedAttack()
	{
		this.is_attack_motion_finished = true;
	}

	// Attack モーションで飛び道具を打つ＆撃が相手にヒットする瞬間.
	public void 	PerformFire()
	{
		this.is_attack_motion_impact = true;
	}

	// Attack モーションが終わったとき.
	public void 	NotifyFinishedFire()
	{
		this.is_attack_motion_finished = true;
	}

}

