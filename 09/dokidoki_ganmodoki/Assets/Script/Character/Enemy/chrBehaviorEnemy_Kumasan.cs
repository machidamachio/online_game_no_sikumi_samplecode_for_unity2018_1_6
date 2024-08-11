using UnityEngine;
using System.Collections;

// 敵ビヘイビアー　くまさん.
public class chrBehaviorEnemy_Kumasan : chrBehaviorEnemy {

	public const float	HIT_POINT       = 5.0f;
	public const float	ATTACK_POWER    = 3.0f;
	public const float	ATTACK_DISTANCE = 3.0f;

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

		base.setBehaveKind(Enemy.BEHAVE_KIND.OUFUKU);
	}

	public override void	start()
	{
		base.start();

		this.control.vital.setHitPoint(HIT_POINT);
		this.control.vital.setAttackPower(ATTACK_POWER);
		this.control.vital.setAttackDistance(ATTACK_DISTANCE);
	}

	public override	void	execute()
	{
		base.execute();
		this.basic_action.execute();

		this.is_attack_motion_finished = false;
		this.is_attack_motion_impact = false;
	}

	// ================================================================ //
	// アニメーションイベント.

	// Attack モーションが終わったとき.
	public void NotifyFinishedAttack()
	{
		this.is_attack_motion_finished = true;
	}

	public void NotifyAttackImpact()
	{
		this.is_attack_motion_impact = true;
	}

}

