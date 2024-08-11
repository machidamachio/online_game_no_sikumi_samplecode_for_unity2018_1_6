using UnityEngine;
using System.Collections;

// 遣い魔（召喚獣　犬）.
public class chrBehaviorBeast_Dog : chrBehaviorBase {

	public Vector3		position_in_formation = Vector3.zero;

	private Vector3		move_target;			// 移動先の位置.
	private Vector3		heading_target;			// 向く先.

	//protected string	move_target_item = "";	// アイテムを目指して移動しているとき.

	protected string	collision = "";

	//public chrBehaviorLocal	local_player          = null;

	public	bool		in_formation = true;	// ローカルプレイヤーと一緒に移動する（デバッグ用）.

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,		// 移動.
		STOP,			// 停止.

		NUM,
	};
	public	Step<STEP>	step = new Step<STEP>(STEP.NONE);

	//public STEP			step      = STEP.NONE;
	//public STEP			next_step = STEP.NONE;
	//public float		step_timer = 0.0f;

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

		this.move_target = this.transform.position;
	}
	public override void	start()
	{
		base.start();

		this.step.set_next(STEP.STOP);
	}
	public override	void	execute()
	{
		base.execute();

		float	stop_to_move = 5.0f;
		float	move_to_stop = 3.0f;

		chrBehaviorLocal	player = PartyControl.get().getLocalPlayer();

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.STOP:
			{
				Vector3		ditance_vector = player.control.getPosition() - this.control.getPosition();

				ditance_vector.y = 0.0f;

				if(ditance_vector.magnitude >= stop_to_move) {

					this.step.set_next(STEP.MOVE);
				}
			}
			break;

			case STEP.MOVE:
			{
				Vector3		ditance_vector = player.control.getPosition() - this.control.getPosition();

				ditance_vector.y = 0.0f;

				if(ditance_vector.magnitude <= move_to_stop) {

					this.step.set_next(STEP.STOP);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.MOVE:
				{
					this.move_target    = player.control.getPosition();
					this.heading_target = this.move_target;
				}
				break;

			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.MOVE:
			{
				this.move_target    = player.control.getPosition();
				this.heading_target = this.move_target;

				this.exec_step_move();
			}
			break;
		}

		this.collision = "";

		// ---------------------------------------------------------------- //

	}

	// ================================================================ //

	// STEP.MOVE の実行.
	// 移動.
	protected void	exec_step_move()
	{
		// ---------------------------------------------------------------- //
		// 移動（位置座標の補間）.

		Vector3		position  = this.control.getPosition();
		float		cur_dir   = this.control.getDirection();

		Vector3		dist = this.move_target - position;

		dist.y = 0.0f;

		float		speed = 5.0f;
		float		speed_per_frame = speed*Time.deltaTime;

		if(dist.magnitude < speed_per_frame) {

			// 立ち止まる.
			this.control.cmdSetMotion("m002_idle", 0);

			dist = Vector3.zero;

		} else {

			// 歩く.
			this.control.cmdSetMotion("m001_walk", 0);

			dist *= (speed_per_frame)/dist.magnitude;
		}

		position += dist;

		// 向きの補間.

		float	tgt_dir;

		if(Vector3.Distance(this.heading_target, position) > 0.01f) {

			tgt_dir = Quaternion.LookRotation(this.heading_target - position).eulerAngles.y;

		} else {

			tgt_dir = cur_dir;
		}

		float	dir_diff = tgt_dir - cur_dir;

		if(dir_diff > 180.0f) {

			dir_diff = dir_diff - 360.0f;

		} else if(dir_diff < -180.0f) {

			dir_diff = dir_diff + 360.0f;
		}

		//if(!gi.pointing.current && gi.shot.trigger_on) {
		
		//} else {

			dir_diff *= 0.1f;
		//}

		if(Mathf.Abs(dir_diff) < 1.0f) {

			cur_dir = tgt_dir;

		} else {

			cur_dir += dir_diff;
		}

		position.y = this.control.getPosition().y;

		this.control.cmdSetPosition(position);
		this.control.cmdSetDirection(cur_dir);

	}

	// ================================================================ //
}
