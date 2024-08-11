using UnityEngine;
using System.Collections;
using MathExtension;

// 各アクションから共通で使う、サブのアクション.

namespace Character {

// ============================================================================ //
//																				//
//		ワープ.																	//
//																				//
// ============================================================================ //
public class WarpAction : ActionBase {

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		WARP_IN = 0,			// 消える.
		WARP_OUT,				// 現れる.
		FINISH,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	public chrBehaviorPlayer	target_player;

	// ================================================================ //

	public override void	start()
	{
		this.is_finished = false;

		// 今のところおばけ専用.

		chrBehaviorEnemy_Obake	obake = this.behavior as chrBehaviorEnemy_Obake;

		if(obake != null) {

			this.step.set_next(STEP.WARP_IN);

		} else {

			this.step.set_next(STEP.FINISH);
		}
	}

	public override void	execute()
	{
		BasicAction	basic_action = this.behavior.basic_action;

		float	warp_in_time = 0.2f;
		float	warp_out_time = 0.2f;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// 消える.
			case STEP.WARP_IN:
			{
				if(this.step.get_time() > warp_in_time) {

					this.step.set_next(STEP.WARP_OUT);
				}
			}
			break;

			// 現れる.
			case STEP.WARP_OUT:
			{
				if(this.step.get_time() > warp_out_time) {

					this.step.set_next_delay(STEP.FINISH, 1.0f);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 現れる.
				case STEP.WARP_OUT:
				{
					if(this.target_player != null) {

						Vector3		v = this.control.getPosition() - this.target_player.control.getPosition();
	
						if(v.magnitude > 5.0f) {
	
							v *= 5.0f/v.magnitude;
						}
	
						v = Quaternion.AngleAxis(45.0f, Vector3.up)*v;
	
						basic_action.position_xz = this.target_player.control.getPosition() + v;
						basic_action.move_dir    = Mathf.Atan2(-v.x, -v.z)*Mathf.Rad2Deg;

					} else {

						Vector3		v = Quaternion.AngleAxis(this.control.getDirection() + Random.Range(-30.0f, 30.0f), Vector3.up)*Vector3.forward;

						v *= 5.0f;

						basic_action.position_xz = this.control.getPosition() + v;
						basic_action.move_dir    = Mathf.Atan2(v.x, v.z)*Mathf.Rad2Deg;
					}

					this.control.cmdSetDirection(basic_action.move_dir);

					// 着地する.
					// 箱から飛び出た直後は空中でワープするので.
					basic_action.jump.forceFinish();
				}
				break;

				// おしまい.
				case STEP.FINISH:
				{
					this.is_finished = true;
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 消える.
			case STEP.WARP_IN:
			{
				float	rate = Mathf.Clamp01(this.step.get_time()/warp_in_time);

				rate = Mathf.Pow(rate, 0.25f);

				float	xz_scale = Mathf.Lerp(1.0f, 0.0f, rate);
				float	y_scale  = Mathf.Lerp(1.0f, 2.0f, rate);

				this.control.transform.localScale = new Vector3(xz_scale, y_scale, xz_scale);

				this.control.GetComponent<Rigidbody>().Sleep();
			}
			break;

			// 現れる.
			case STEP.WARP_OUT:
			{
				float	rate = Mathf.Clamp01(this.step.get_time()/warp_out_time);

				rate = Mathf.Pow(rate, 0.25f);

				float	xz_scale = Mathf.Lerp(0.0f, 1.0f, rate);
				float	y_scale  = Mathf.Lerp(2.0f, 1.0f, rate);

				this.control.transform.localScale = new Vector3(xz_scale, y_scale, xz_scale);

				this.control.GetComponent<Rigidbody>().Sleep();
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}
}
// ============================================================================ //
//																				//
//		ショット.																//
//																				//
// ============================================================================ //
public class ShootAction : ActionBase {

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		SHOOT = 0,			// たま発射.
		FINISH,				// おしまい.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	public override void	start()
	{
		this.is_finished = false;

		// 今のところおばけ専用.

		chrBehaviorEnemy_Obake	obake = this.behavior as chrBehaviorEnemy_Obake;

		if(obake != null) {

			this.step.set_next(STEP.SHOOT);

		} else {

			this.step.set_next(STEP.FINISH);
		}
	}

	public override void	execute()
	{
		BasicAction	basic_action = this.behavior.basic_action;

		chrBehaviorEnemy_Obake	obake = this.behavior as chrBehaviorEnemy_Obake;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// たま発射.
			case STEP.SHOOT:
			{
				if(this.behavior.is_attack_motion_finished) {

					this.step.set_next_delay(STEP.FINISH, 1.0f);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// たま発射.
				case STEP.SHOOT:
				{
					basic_action.setMoveMotionSpeed(0.0f);
					basic_action.animator.SetTrigger("Attack");
				}
				break;

				// おしまい.
				case STEP.FINISH:
				{
					this.is_finished = true;
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// たま発射.
			case STEP.SHOOT:
			{
				if(this.behavior.is_attack_motion_impact) {

					obake.shootBullet();
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}
}
// ============================================================================ //
//																				//
//		近接攻撃.																//
//																				//
// ============================================================================ //
public class MeleeAttackAction : ActionBase {

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		ATTACK = 0,			// なぐり中.
		FINISH,				// おしまい.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	public chrBehaviorPlayer	target_player = null;

	// ================================================================ //

	public override void	start()
	{
		this.is_finished = false;

		this.step.set_next(STEP.ATTACK);
	}

	public override void	execute()
	{
		BasicAction	basic_action = this.behavior.basic_action;

		if(this.target_player == null) {

			this.target_player = PartyControl.get().getLocalPlayer();
		}

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// なぐり中.
			case STEP.ATTACK:
			{
				if(this.behavior.is_attack_motion_finished) {

					this.step.set_next_delay(STEP.FINISH, 1.0f);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// なぐり中.
				case STEP.ATTACK:
				{
					basic_action.setMoveMotionSpeed(0.0f);
					basic_action.animator.SetTrigger("Attack");
				}
				break;

				// おしまい.
				case STEP.FINISH:
				{
					this.is_finished = true;
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// なぐり中.
			case STEP.ATTACK:
			{
				if(this.behavior.is_attack_motion_impact) {

					if(this.behavior.isInAttackRange(this.target_player.control)) {

						if(this.target_player.isLocal()) {

							this.target_player.control.causeDamage(this.behavior.control.vital.getAttackPower(), -1);

						} else {

							// リモートプレイヤーにはダメージを与えない.
						}
					}
				}
				basic_action.move_dir = MathUtility.calcDirection(this.behavior.control.getPosition(), this.target_player.control.getPosition());
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}
}



}
