using UnityEngine;
using System.Collections;

namespace Character {

// 敵キャラクターアクションの基底クラス.
public class ActionBase {

	public chrController		control  = null;
	public chrBehaviorEnemy		behavior = null;

	public ActionBase			parent   = null;
	public ActionBase			child    = null;

	public bool					is_finished = false;

	public class DescBase {

	}

	// ================================================================ //

	// 生成する.
	public virtual void		create(chrBehaviorEnemy behavior, ActionBase.DescBase desc_base = null)
	{
		this.behavior = behavior;
		this.control  = behavior.control;
	}

	public virtual void		start() {}				// スタート.
	public virtual void		resume() {}				// 子階層から復帰する.
	public virtual void		execute() {}			// 毎フレームの実行.
	public virtual void		stealth() {}			// 親が実行中も実行.

	// 子階層を開始する.
	public void		push(ActionBase child)
	{
		this.child = child;
		this.child.start();
	}

	// 子階層の実行.
	public void		execute_child()
	{
		if(this.child != null) {

			this.child.execute();
		}
	}

	// 子階層の終了チェック.
	public bool		finish_child()
	{
		bool	ret = false;

		do {

			if(this.child == null) {

				break;
			}
			if(!this.child.is_finished) {

				break;
			}

			//

			this.child = null;
			ret = true;

		} while(false);

		return(ret);
	}
}

// 敵共通の基本アクション.
public class BasicAction : ActionBase {

	public const float	MOVE_SPEED_DEFAULT = 2.0f;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		IDLE  = 0,	
		SPAWN,				// 箱（ジェネレーター）から飛び出し中（着地まで）.
		VANISH,				// さよーならー.
		STILL,				// その場でとまる.

		UNIQUE,				// 敵の思考タイプごとのユニークなアクション.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	public ActionBase	unique_action = new ActionBase();
	public Animator		animator;

	// 移動モーションのスピード.
	protected struct MotionSpeed {

		public float	current;
		public float	goal;
	}
	protected MotionSpeed		motion_speed;

	// ---------------------------------------------------------------- //

	public float		move_dir   = 0.0f;					// 移動方向.
	public float		move_speed = 1.0f;
	public float		turn_rate  = 0.0f;
	public Vector3		position_xz;

	public struct WallColi {

		public bool		is_valid;
		public Vector3	normal;
	}
	public WallColi		wall_coli;

	public ipModule.Jump	jump;

	public bool			is_spawn_from_lair = false;
	public Vector3		outlet_position    = Vector3.zero;		// ジェネレーターから飛び出るときの開始位置.
	public Vector3		outlet_vector      = Vector3.forward;	// ジェネレーターから飛び出るときの速度.


	// ================================================================ //

	// 生成する.
	public override void		create(chrBehaviorEnemy behavior, ActionBase.DescBase desc_base = null)
	{
		base.create(behavior, desc_base);

		this.motion_speed.current = 0.0f;
		this.motion_speed.goal    = 0.0f;

		this.animator = this.behavior.gameObject.GetComponent<Animator>();

		this.jump = new ipModule.Jump();
		this.jump.gravity *= 3.0f;
		this.jump.bounciness = new Vector3(1.0f, -0.4f, 1.0f);
	}

	// スタート.
	public override void	start()
	{
		if(this.step.get_next() == STEP.NONE) {

			if(this.is_spawn_from_lair) {
	
				// ジェネレーターから飛び出すとき.
	
				this.position_xz = this.outlet_position;
	
				Vector3		start = this.outlet_position;
				Vector3		goal  = start + this.outlet_vector*3.0f;
	
				goal.y = MapCreator.get().getFloorHeight();

				this.jump.start(start, goal, start.y + 2.0f);
	
				this.move_dir = Mathf.Atan2(this.outlet_vector.x, this.outlet_vector.z)*Mathf.Rad2Deg;
	
				this.step.set_next(STEP.SPAWN);
	
			} else {
	
				this.move_dir    = this.control.getDirection();
				this.position_xz = this.control.getPosition();
				this.position_xz.y = 0.0f;
	
				this.step.set_next(STEP.UNIQUE);
			}

		} else {

			// 生成直後にステップが指定されていたときに、上書きしてしまわないように.

			this.move_dir    = this.control.getDirection();
			this.position_xz = this.control.getPosition();
			this.position_xz.y = 0.0f;
		}
	
		this.control.cmdSetPositionAnon(this.position_xz);
		this.control.cmdSetDirectionAnon(this.move_dir);
	}

	// 子階層から復帰する.
	public override void		resume()
	{
		if(this.control.vital.hit_point <= 0.0f) {

			this.step.set_next(STEP.VANISH);

		} else {

			this.step.set_next(STEP.IDLE);
		}
	}

	// 毎フレームの実行.
	public override void		execute()
	{
		this.position_xz = this.control.getPosition();

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// 箱（ジェネレーター）から飛び出し中（着地まで）.
			case STEP.SPAWN:
			{
				if(this.jump.isDone()) {

					this.step.set_next(STEP.UNIQUE);
				}
			}
			break;

			// さよーならー.
			case STEP.VANISH:
			{
				if(this.behavior.control.damage_effect.isVacant()) {

					// 自分自身のインスタンスを削除する.
					this.behavior.deleteSelf();
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.UNIQUE:
				{
					this.unique_action.parent = this;
					this.unique_action.start();
				}
				break;

				// 箱（ジェネレーター）から飛び出し中（着地まで）.
				case STEP.SPAWN:
				{
					this.control.cmdSetDirection(this.move_dir);
				}
				break;

				// さよーならー.
				case STEP.VANISH:
				{
					this.animator.speed = 0.0f;
					this.control.cmdBeginVanish();
				}
				break;

				// その場でとまる.
				case STEP.STILL:
				{
					this.setMoveMotionSpeed(0.0f);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.UNIQUE:
			{
				this.unique_action.execute();
			}
			break;

			// 箱（ジェネレーター）から飛び出し中（着地まで）.
			case STEP.SPAWN:
			{
				this.position_xz += this.jump.xz_velocity()*Time.deltaTime;
			}
			break;
		}

		this.unique_action.stealth();

		// ---------------------------------------------------------------- //

		this.update_motion_speed();

		this.jump.execute(Time.deltaTime);
		this.position_xz.y = this.jump.position.y;

		// ルーム外壁から飛び出ないように.
		do {

			chrBehaviorEnemy	behavior = this.control.behavior as chrBehaviorEnemy;

			if(behavior == null) {

				break;
			}

			Rect	room_rect = MapCreator.get().getRoomRect(behavior.room.getIndex());
	
			this.wall_coli.is_valid = false;
	
			if(this.position_xz.x < room_rect.min.x) {
	
				this.position_xz.x      = room_rect.min.x;
				this.wall_coli.is_valid = true;
				this.wall_coli.normal   = Vector3.right;
			}
			if(this.position_xz.x > room_rect.max.x) {
	
				this.position_xz.x      = room_rect.max.x;
				this.wall_coli.is_valid = true;
				this.wall_coli.normal   = Vector3.left;
			}
			if(this.position_xz.z < room_rect.min.y) {
	
				this.position_xz.z      = room_rect.min.y;
				this.wall_coli.is_valid = true;
				this.wall_coli.normal   = Vector3.forward;
			}
			if(this.position_xz.z > room_rect.max.y) {
	
				this.position_xz.z      = room_rect.max.y;
				this.wall_coli.is_valid = true;
				this.wall_coli.normal   = Vector3.back;
			}

		} while(false);

		this.control.cmdSetPosition(this.position_xz);

		// 壁に当たったら壁沿いに移動方向を変える.
		do {

			if(!this.wall_coli.is_valid) {

				break;
			}

			Vector3		v = Quaternion.AngleAxis(this.move_dir, Vector3.up)*Vector3.forward;

			float	dp = Vector3.Dot(v, this.wall_coli.normal);

			if(dp > 0.0f) {

				break;
			}

			v = v - 2.0f*this.wall_coli.normal*dp;

			this.move_dir = Mathf.Atan2(v.x, v.z)*Mathf.Rad2Deg;

		} while(false);

		// ターン（向きの補間）.
		if(this.turn_rate > 0.0f) {

			this.control.cmdSmoothDirection(this.move_dir, this.turn_rate);
			this.turn_rate = 0.0f;

		} else {

			this.control.cmdSmoothDirection(this.move_dir);
		}
	}

	// 移動.
	public void		executeMove()
	{
		// ---------------------------------------------------------------- //
		// 移動（位置座標の補間）.

		Vector3		position  = this.control.getPosition();

		float		speed_per_frame = this.move_speed*MOVE_SPEED_DEFAULT*Time.deltaTime;

		Vector3		move_vector = Quaternion.AngleAxis(this.move_dir, Vector3.up)*Vector3.forward;

		position += move_vector*speed_per_frame;

		this.position_xz = position;
	}

	// スポーンアクション（箱からすぽ～んと飛び出る）.
	public void		beginSpawn(Vector3 start, Vector3 dir_vector)
	{
		this.is_spawn_from_lair = true;
		this.outlet_position    = start;
		this.outlet_vector      = dir_vector;
	}

	// 移動のスピードをセットする.
	public void		setMoveSpeed(float speed)
	{
		this.move_speed = speed;
	}

	// 移動モーションのスピードをセットする.
	// 立ちモーションと歩きモーションのブレンド率.
	public void		setMoveMotionSpeed(float speed)
	{
		this.motion_speed.goal = speed;
	}

	// モーションの再生スピードをセットする.
	public void		setMotionPlaySpeed(float speed)
	{
		this.animator.speed = speed;
	}

	// 移動方向のベクトルをゲットする.
	public Vector3	getMoveVector()
	{
		return(Quaternion.AngleAxis(this.move_dir, Vector3.up)*Vector3.forward);
	}

	// 移動方向のベクトル→移動方向（Yアングル）.
	public void		setMoveDirByVector(Vector3 v)
	{
		this.move_dir = Mathf.Atan2(v.x, v.z)*Mathf.Rad2Deg;
	}

	// ================================================================ //

	// モーションスピードの更新.
	public void		update_motion_speed()
	{
		if(this.motion_speed.current != this.motion_speed.goal) {

			float	delta = (1.0f/0.2f)*Time.deltaTime;

			if(this.motion_speed.current < this.motion_speed.goal) {

				this.motion_speed.current = Mathf.Min(this.motion_speed.current + delta, this.motion_speed.goal);

			} else {

				this.motion_speed.current = Mathf.Max(this.motion_speed.current - delta, this.motion_speed.goal);
			}
			this.animator.SetFloat("Motion_Speed", this.motion_speed.current*0.1f);
		}
	}

}

}
