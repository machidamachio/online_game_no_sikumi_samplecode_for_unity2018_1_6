using UnityEngine;
using System.Collections;
using MathExtension;
using GameObjectExtension;

namespace Character {

// ============================================================================ //
//																				//
//		// JUMBO,			// ジャンボ.										//
//																				//
// ============================================================================ //
public class JumboAction : ActionBase {

	protected MeleeAttackAction		melee_attack;

	protected ipModule.Spring	spring;				// スケール制御用のばね.
	protected float				scale = 1.0f;		// スケール.

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 歩き中.
		REST,				// 止まってる.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	public override void		create(chrBehaviorEnemy behavior, ActionBase.DescBase desc_base = null)
	{
		base.create(behavior, desc_base);

		this.melee_attack = new MeleeAttackAction();
		this.melee_attack.create(this.behavior);

		this.spring = new ipModule.Spring();
		this.spring.k      = 300.0f;
		this.spring.reduce = 0.90f;

		// スポーン→着地時にバウンドしないようにする.
		this.behavior.basic_action.jump.bounciness.y = 0.0f;
	}

	public override void	start()
	{
		this.spring.start(-1.0f);

		this.control.vital.setHitPoint(chrBehaviorPlayer.MELEE_ATTACK_POWER*2.5f);
		this.control.vital.setAttackPower(chrBehaviorEnemy_Kumasan.ATTACK_POWER*3.0f);

		this.step.set_next(STEP.REST);
	}

	public override void	execute()
	{
		chrBehaviorEnemy	mine = this.behavior;
		BasicAction			basic_action = mine.basic_action;

		if(this.finish_child()) {

			this.step.set_next(STEP.MOVE);
		}

		chrBehaviorPlayer	target_player = this.behavior.selectTargetPlayer(float.MaxValue, float.MaxValue);

		this.melee_attack.target_player = target_player;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// 歩き中.
			case STEP.MOVE:
			{
				do {

					if(target_player == null) {
	
						break;
					}
					if(!this.behavior.isInAttackRange(target_player.control)) {

						break;
					}

					//

					this.push(this.melee_attack);
					this.step.sleep();

				} while(false);
			}
			break;

			// 止まってる.
			case STEP.REST:
			{
				if(this.step.get_time() > 1.0f) {

					this.step.set_next(STEP.MOVE);
				}

				do {

					if(target_player == null) {
	
						break;
					}
					if(!this.behavior.isInAttackRange(target_player.control)) {

						break;
					}

					//

					this.push(this.melee_attack);
					this.step.sleep();

				} while(false);
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 歩き中.
				case STEP.MOVE:
				{
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 歩き中.
			case STEP.MOVE:
			{
				do {

					if(target_player == null) {

						break;
					}

					basic_action.move_dir = MathUtility.calcDirection(mine.control.getPosition(), target_player.control.getPosition());
	
					basic_action.setMoveMotionSpeed(1.0f);
					basic_action.setMoveSpeed(0.6f);

					basic_action.setMotionPlaySpeed(0.6f);

					basic_action.executeMove();

				} while(false);
			}
			break;

			// 止まってる.
			case STEP.REST:
			{
				basic_action.setMoveMotionSpeed(0.0f);
			}
			break;
		}

		this.spring.execute(Time.deltaTime);

		if(this.spring.isMoving()) {

			this.scale = Mathf.InverseLerp(-1.0f, 1.0f, this.spring.position);
			this.scale = Mathf.Lerp(1.0f, 2.0f, this.scale);
		}

		this.control.transform.localScale = Vector3.one*this.scale;

		// ---------------------------------------------------------------- //

		this.execute_child();

		if(this.child == this.melee_attack) {

			this.attack_motion_speed_control();
		}

	}

	// 攻撃モーションのスピード.
	protected void	attack_motion_speed_control()
	{
		chrBehaviorEnemy	mine = this.behavior;
		BasicAction			basic_action = mine.basic_action;

		if(this.melee_attack.step.get_current() == MeleeAttackAction.STEP.ATTACK) {

			float	current_time = this.melee_attack.step.get_time();
			float	furikaburi_time = 0.38f + 0.3f;

			float	play_speed = 0.5f;

			if(current_time < furikaburi_time) {

				// 振りかぶるまで　徐々に遅く.

				float	rate = Mathf.Clamp01(Mathf.InverseLerp(0.0f, furikaburi_time, current_time));

				play_speed = Mathf.Lerp(0.3f, 0.1f, rate);

			} else {

				// 振りかぶってからたたきおろすまで　一気に早くなる.

				float	rate = Mathf.Clamp01(Mathf.InverseLerp(furikaburi_time, furikaburi_time + 0.3f, current_time));

				play_speed = Mathf.Lerp(0.1f, 0.7f, rate);
			}
			basic_action.setMotionPlaySpeed(play_speed);
		}
	}
}
// ============================================================================ //
//																				//
//		TOTUGEKI,			// プレイヤーに近寄って近接攻撃.					//
//																				//
// ============================================================================ //
public class TotugekiAction : ActionBase {

	protected MeleeAttackAction		melee_attack;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 歩き中.
		REST,				// 止まってる.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	public override void		create(chrBehaviorEnemy behavior, ActionBase.DescBase desc_base = null)
	{
		base.create(behavior, desc_base);

		this.melee_attack = new MeleeAttackAction();
		this.melee_attack.create(this.behavior);
	}

	public override void	start()
	{
		this.step.set_next(STEP.REST);
	}

	public override void	execute()
	{
		chrBehaviorEnemy	mine = this.behavior;
		BasicAction			basic_action = mine.basic_action;

		if(this.finish_child()) {

			this.step.set_next(STEP.MOVE);
		}

		chrBehaviorPlayer	target_player = this.behavior.selectTargetPlayer(float.MaxValue, float.MaxValue);

		this.melee_attack.target_player = target_player;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// 歩き中.
			case STEP.MOVE:
			{
				do {

					if(target_player == null) {
	
						break;
					}
					if(!this.behavior.isInAttackRange(target_player.control)) {

						break;
					}

					//

					this.push(this.melee_attack);
					this.step.sleep();

				} while(false);
			}
			break;

			// 止まってる.
			case STEP.REST:
			{
				if(this.step.get_time() > 1.0f) {

					this.step.set_next(STEP.MOVE);
				}

				do {

					if(target_player == null) {
	
						break;
					}
					if(!this.behavior.isInAttackRange(target_player.control)) {

						break;
					}

					//

					this.push(this.melee_attack);
					this.step.sleep();

				} while(false);
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 歩き中.
				case STEP.MOVE:
				{
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 歩き中.
			case STEP.MOVE:
			{
				do {

					if(target_player == null) {

						break;
					}

					basic_action.move_dir = MathUtility.calcDirection(mine.control.getPosition(), target_player.control.getPosition());
	
					basic_action.executeMove();
	
					basic_action.setMoveMotionSpeed(1.0f);

				} while(false);
			}
			break;

			// 止まってる.
			case STEP.REST:
			{
				basic_action.setMoveMotionSpeed(0.0f);
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.execute_child();
	}
}
// ============================================================================ //
//																				//
//		WARP_DE_FIRE,		// ワープを繰り返す.								//
//																				//
// ============================================================================ //
public class WarpDeFireAction : ActionBase {

	protected ShootAction		shoot;
	protected WarpAction		warp;

	protected chrBehaviorPlayer	target_player;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		READY = 0,			// ぼっ立ち.
		WARP,				// ワープ.
		TURN,				// ターゲットの方に旋回.
		SHOT,				// 弾をうつ.
		FINISH,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	protected STEP	resume_step = STEP.READY;

	// ================================================================ //

	public override void	start()
	{
		this.is_finished = false;

		this.step.set_next(STEP.READY);

		this.shoot = new ShootAction();
		this.shoot.create(this.behavior);

		this.warp = new WarpAction();
		this.warp.create(this.behavior);
	}

	// 毎フレームの実行.
	public override void	execute()
	{
		chrBehaviorEnemy	mine = this.behavior;
		BasicAction			basic_action = mine.basic_action;

		float	distance_limit = 10.0f;
		float	angle_limit    = 180.0f;

		if(this.finish_child()) {

			this.step.set_next(this.resume_step);
		}

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// ぼっ立ち.
			case STEP.READY:
			{
				// 攻撃可能範囲にいて、かつ一番近く、正面にいるプレイヤーを.
				// 探す.
				this.target_player = this.behavior.selectTargetPlayer(distance_limit, angle_limit);

				this.step.set_next(STEP.WARP);
			}
			break;

			// ターゲットの方に旋回.
			case STEP.TURN:
			{
				if(this.target_player == null) {

					this.step.set_next(STEP.READY);

				} else {

					basic_action.move_dir = MathUtility.calcDirection(mine.control.getPosition(), this.target_player.control.getPosition());
	
					float	dir_diff = MathUtility.snormDegree(basic_action.move_dir - mine.control.getDirection());
	
					if(Mathf.Abs(dir_diff) < 5.0f) {
	
						this.step.set_next(STEP.SHOT);
					}
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// ワープ.
				case STEP.WARP:
				{
					this.warp.target_player = this.target_player;
					this.resume_step = STEP.TURN;
					this.push(this.warp);
					this.step.sleep();
				}
				break;

				// 弾をうつ.
				case STEP.SHOT:
				{
					this.resume_step = STEP.READY;
					this.push(this.shoot);
					this.step.sleep();
				}
				break;

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

			// ターゲットの方に旋回.
			case STEP.TURN:
			{
				if(this.target_player != null) {

					basic_action.move_dir = MathUtility.calcDirection(mine.control.getPosition(), this.target_player.control.getPosition());
					basic_action.turn_rate = 0.5f;
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.execute_child();
	}

	// 親が実行中も実行.
	public override void	stealth()
	{
		chrBehaviorEnemy	mine = this.behavior;
		BasicAction			basic_action = mine.basic_action;

		switch(basic_action.step.get_current()) {

			case BasicAction.STEP.SPAWN:
			{
				if(basic_action.jump.velocity.y < 0.0f) {

					basic_action.step.set_next(BasicAction.STEP.UNIQUE);
				}
			}
			break;
		}
	}

}
// ============================================================================ //
//																				//
//		SONOBA_DE_FIRE,		//	その場でショット.								//
//																				//
// ============================================================================ //
public class SonobaDeFireAction : ActionBase {

	protected ShootAction		shoot;

	protected chrBehaviorPlayer	target_player;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		READY = 0,			// ぼっ立ち.
		TURN,				// ターゲットの方に旋回.
		FINISH,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	public override void	start()
	{
		this.is_finished = false;

		this.step.set_next(STEP.READY);

		this.shoot = new ShootAction();
		this.shoot.create(this.behavior);
	}


	public override void	execute()
	{
		chrBehaviorEnemy	mine = this.behavior;
		BasicAction			basic_action = mine.basic_action;

		float	distance_limit = 10.0f;
		float	angle_limit    = 45.0f;

		if(this.finish_child()) {

			this.step.set_next(STEP.READY);
		}

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.READY:
			{
				// 攻撃可能範囲にいて、かつ一番近く、正面にいるプレイヤーを.
				// 探す.
				this.target_player = this.behavior.selectTargetPlayer(distance_limit, angle_limit);

				if(target_player != null) {

					this.step.set_next(STEP.TURN);
				}
			}
			break;

			// ターゲットの方に旋回.
			case STEP.TURN:
			{
				if(this.target_player == null) {

					this.step.set_next(STEP.READY);

				} else {

					basic_action.move_dir = MathUtility.calcDirection(mine.control.getPosition(), this.target_player.control.getPosition());
	
					float	dir_diff = MathUtility.snormDegree(basic_action.move_dir - mine.control.getDirection());
	
					if(Mathf.Abs(dir_diff) < 5.0f) {
	
						this.push(this.shoot);
						this.step.sleep();
					}
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// ターゲットの方に旋回.
				case STEP.TURN:
				{
				}
				break;

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

			// ターゲットの方に旋回.
			case STEP.TURN:
			{
				basic_action.move_dir = MathUtility.calcDirection(mine.control.getPosition(), this.target_player.control.getPosition());
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.execute_child();
	}
}
// ============================================================================ //
//																				//
//		OUFUKU = 0,		// ２か所を往復する.									//
//																				//
// ============================================================================ //
public class OufukuAction : ActionBase {

	protected MeleeAttackAction		melee_attack;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 歩き中.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	// BEHAVE_KIND の Action をつくるときのオプション.
	public class Desc : ActionBase.DescBase {
			
		public Desc() {}
		public Desc(Vector3 center)
		{
			this.position0 = center + Vector3.right;
			this.position1 = center - Vector3.right;
		}

		public Vector3	position0;
		public Vector3	position1;
	}

	protected Vector3[]	positions = new Vector3[2];
	protected int		next_position;

	// ================================================================ //

	public override void		create(chrBehaviorEnemy behavior, ActionBase.DescBase desc_base = null)
	{
		base.create(behavior, desc_base);

		Desc	desc = desc_base as Desc;

		if(desc == null) {

			desc = new Desc(this.control.getPosition());
		}

		this.positions[0]  = desc.position0;
		this.positions[1]  = desc.position1;

		this.melee_attack = new MeleeAttackAction();
		this.melee_attack.create(this.behavior);
	}

	public override void	start()
	{
		this.step.set_next(STEP.MOVE);
	}

	public override void	execute()
	{
		BasicAction	basic_action = this.behavior.basic_action;

		if(this.finish_child()) {

			this.step.set_next(STEP.MOVE);
		}

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// 歩き中.
			case STEP.MOVE:
			{
				chrBehaviorLocal	player = PartyControl.get().getLocalPlayer();

				if(this.behavior.isInAttackRange(player.control)) {

					this.push(this.melee_attack);
					this.step.sleep();
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 歩き中.
				case STEP.MOVE:
				{
					this.next_position = 0;

					Vector3		move_vector = this.positions[this.next_position] - this.control.getPosition();

					basic_action.move_dir = Mathf.Atan2(move_vector.x, move_vector.z)*Mathf.Rad2Deg;
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 歩き中.
			case STEP.MOVE:
			{
				Vector3		tgt_vector0 = this.positions[0] - this.control.getPosition();
				Vector3		tgt_vector1 = this.positions[1] - this.control.getPosition();
				Vector3		move_vector = this.control.getMoveVector();

				float	dp0 = Vector3.Dot(tgt_vector0, move_vector);
				float	dp1 = Vector3.Dot(tgt_vector1, move_vector);

				if(dp0 > 0.0f && dp1 > 0.0f) {

					if(tgt_vector0.sqrMagnitude < tgt_vector1.sqrMagnitude) {

						this.next_position = 0;

					} else {

						this.next_position = 1;
					}

				} else if(dp0 < 0.0f && dp1 < 0.0f) {

					if(tgt_vector0.sqrMagnitude > tgt_vector1.sqrMagnitude) {

						this.next_position = 0;

					} else {

						this.next_position = 1;
					}
				}

				Vector3		tgt_vector = this.positions[this.next_position] - this.control.getPosition();

				basic_action.move_dir = Mathf.Atan2(tgt_vector.x, tgt_vector.z)*Mathf.Rad2Deg;

				basic_action.executeMove();

				basic_action.setMoveMotionSpeed(1.0f);
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.execute_child();
	}
}
// ============================================================================ //
//																				//
//		GOROGORO,				// ごろごろ転がって、壁で反射.					//
//																				//
// ============================================================================ //
public class GoroGoroAction : ActionBase {

	protected MeleeAttackAction		melee_attack;

	protected GameObject	model;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 歩き中.
		REST,				// 止まってる.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	public override void		create(chrBehaviorEnemy behavior, ActionBase.DescBase desc_base = null)
	{
		base.create(behavior, desc_base);

		this.melee_attack = new MeleeAttackAction();
		this.melee_attack.create(this.behavior);

		this.model = this.behavior.gameObject.findChildGameObject("model");
	}

	public override void	start()
	{
		this.step.set_next(STEP.REST);
	}

	public override void	execute()
	{
		BasicAction	basic_action = this.behavior.basic_action;

		if(this.finish_child()) {

			this.step.set_next(STEP.MOVE);
		}

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// 歩き中.
			case STEP.MOVE:
			{
			}
			break;

			// 止まってる.
			case STEP.REST:
			{
				if(this.step.get_time() > 1.0f) {

					this.step.set_next(STEP.MOVE);
				}

				chrBehaviorLocal	player = PartyControl.get().getLocalPlayer();

				if(this.behavior.isInAttackRange(player.control)) {

					this.push(this.melee_attack);
					this.step.sleep();
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 歩き中.
				case STEP.MOVE:
				{
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 歩き中.
			case STEP.MOVE:
			{
				var coli = basic_action.control.collision_results.Find(x => x.object1.tag == "Wall" || x.object1.tag == "Player");

				// 壁（またはプレイヤー）に当たったら、反射して向きを変える.
				do {

					if(coli == null) {

						break;
					}
					if(coli.option0 == null) {

						break;
					}
					ContactPoint	cp = (ContactPoint)coli.option0;
					
					// 壁の法線の向きを９０°単位にする.

					Vector3		normal = cp.normal;

					float	normal_angle = Mathf.Atan2(normal.x, normal.z)*Mathf.Rad2Deg;

					normal_angle = MathUtility.unormDegree(normal_angle);
					normal_angle = Mathf.Round(normal_angle/90.0f)*90.0f;

					normal = Quaternion.AngleAxis(normal_angle, Vector3.up)*Vector3.forward;

					Vector3		v = basic_action.getMoveVector();

					if(Vector3.Dot(v, normal) >= 0.0f) {

						break;
					}

					v -= 2.0f*Vector3.Dot(v, normal)*normal;
					basic_action.setMoveDirByVector(v);

					// プレイヤーに当たったらダメージを与える.
					do {

						if(coli.object1.tag != "Player") {
	
							break;
						}
	
						chrController	chr = coli.object1.GetComponent<chrController>();
	
						if(chr == null) {
	
							break;
						}
						if(!(chr.behavior is chrBehaviorLocal)) {
	
							break;
						}
	
						chr.causeDamage(this.control.vital.getAttackPower(), -1);
	
					} while(false);

				} while(false);

				basic_action.setMoveSpeed(4.0f);
				basic_action.setMoveMotionSpeed(0.0f);

				basic_action.executeMove();

				//

				this.model.transform.Rotate(new Vector3(7.0f, 0.0f, 0.0f));

			}
			break;

			// 止まってる.
			case STEP.REST:
			{
				basic_action.setMoveMotionSpeed(0.0f);
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.execute_child();
	}
}
// ============================================================================ //
//																				//
//		UROURO,				// 止まる→歩く.									//
//																				//
// ============================================================================ //
public class UroUroAction : ActionBase {

	protected MeleeAttackAction		melee_attack;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 歩き中.
		REST,				// 止まってる.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	public override void		create(chrBehaviorEnemy behavior, ActionBase.DescBase desc_base = null)
	{
		base.create(behavior, desc_base);

		this.melee_attack = new MeleeAttackAction();
		this.melee_attack.create(this.behavior);
	}

	public override void	start()
	{
		this.step.set_next(STEP.REST);
	}

	public override void	execute()
	{
		BasicAction	basic_action = this.behavior.basic_action;

		if(this.finish_child()) {

			this.step.set_next(STEP.MOVE);
		}

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// 歩き中.
			case STEP.MOVE:
			{
				if(this.step.get_time() > 1.0f) {

					this.step.set_next(STEP.REST);
				}

				chrBehaviorLocal	player = PartyControl.get().getLocalPlayer();

				if(this.behavior.isInAttackRange(player.control)) {

					this.push(this.melee_attack);
					this.step.sleep();
				}
			}
			break;

			// 止まってる.
			case STEP.REST:
			{
				if(this.step.get_time() > 1.0f) {

					this.step.set_next(STEP.MOVE);
				}

				chrBehaviorLocal	player = PartyControl.get().getLocalPlayer();

				if(this.behavior.isInAttackRange(player.control)) {

					this.push(this.melee_attack);
					this.step.sleep();
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 歩き中.
				case STEP.MOVE:
				{
					basic_action.move_dir += Random.Range(-90.0f, 90.0f);

					if(basic_action.move_dir > 180.0f) {

						basic_action.move_dir -= 360.0f;

					} else if(basic_action.move_dir < -180.0f) {

						basic_action.move_dir += 360.0f;
					}
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 歩き中.
			case STEP.MOVE:
			{
				basic_action.executeMove();

				basic_action.setMoveMotionSpeed(1.0f);
			}
			break;

			// 止まってる.
			case STEP.REST:
			{
				basic_action.setMoveMotionSpeed(0.0f);
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.execute_child();
	}
}
// ============================================================================ //
//																				//
//		BOTTACHI = 0,		// 立ってるだけ。デバッグ用.						//
//																				//
// ============================================================================ //
public class BottachiAction : ActionBase {

	protected MeleeAttackAction		melee_attack;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		BOTTACHI = 0,			// ぼっ立ち.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	public override void		create(chrBehaviorEnemy behavior, ActionBase.DescBase desc_base = null)
	{
		base.create(behavior, desc_base);

		this.melee_attack = new MeleeAttackAction();
		this.melee_attack.create(this.behavior);
	}

	public override void	start()
	{
		this.step.set_next(STEP.BOTTACHI);
	}

	public override void	execute()
	{
		if(this.finish_child()) {

			this.step.set_next(STEP.BOTTACHI);
		}

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// ぼっ立ち.
			case STEP.BOTTACHI:
			{
				chrBehaviorLocal	player = PartyControl.get().getLocalPlayer();

				if(this.behavior.isInAttackRange(player.control)) {

					this.push(this.melee_attack);
					this.step.sleep();
				}
			}
			break;

		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.BOTTACHI:
				{
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.BOTTACHI:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.execute_child();
	}
}

}
