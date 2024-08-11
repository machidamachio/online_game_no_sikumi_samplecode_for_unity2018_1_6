using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ビヘイビアー　なんちゃってネットプレイヤー（ゲスト）用.
public class chrBehaviorFakeNet : chrBehaviorPlayer {

	private Vector3		move_target;			// 移動先の位置.
	private Vector3		heading_target;			// 向く先.

	protected string	move_target_item = "";	// アイテムを目指して移動しているとき.

	protected string	collision = "";

	public chrBehaviorLocal	local_player          = null;

	public	bool		in_formation = false;	// ローカルプレイヤーと一緒に移動する（デバッグ用）.

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 通常時.
		OUTER_CONTROL,		// 外部制御.

		NUM,
	};

	public STEP			step      = STEP.NONE;
	public STEP			next_step = STEP.NONE;
	public float		step_timer = 0.0f;


	// ================================================================ //

	// ローカルプレイヤー？.
	public override bool	isLocal()
	{
		return(false);
	}

	// 外部からのコントロールを開始する.
	public override void 	beginOuterControll()
	{
		this.next_step = STEP.OUTER_CONTROL;
	}

	// 外部からのコントロールを終了する.
	public override void		endOuterControll()
	{
		this.next_step = STEP.MOVE;
	}


	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// コリジョンにヒットしている間中よばれるメソッド.
	void 	OnCollisionStay(Collision other)
	{
		switch(other.gameObject.tag) {

			case "Item":
			case "Enemy":
			case "Boss":
			{
				CollisionResult	result = new CollisionResult();
		
				result.object0    = this.gameObject;
				result.object1    = other.gameObject;
				result.is_trigger = false;

				CollisionManager.getInstance().results.Add(result);
			}
			break;
		}
	}

	// トリガーにヒットした瞬間だけよばれるメソッド.
	void	OnTriggerEnter(Collider other)
	{
		this.on_trigger_common(other);
	}
	// トリガーにヒットしている間中よばれるメソッド.
	void	OnTriggerStay(Collider other)
	{
		this.on_trigger_common(other);
	}

	protected	void	on_trigger_common(Collider other)
	{
		switch(other.gameObject.tag) {

			case "Door":
			{
				CollisionResult	result = new CollisionResult();
		
				result.object0    = this.gameObject;
				result.object1    = other.gameObject;
				result.is_trigger = false;

				CollisionManager.getInstance().results.Add(result);
			}
			break;
		}
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

		this.next_step = STEP.MOVE;
	}
	public override	void	execute()
	{
		base.execute();
		
		// ---------------------------------------------------------------- //
		// ステップ内の経過時間を進める.
		
		this.step_timer += Time.deltaTime;
		
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.
		
		if(this.next_step == STEP.NONE) {
			
			switch(this.step) {
				
				case STEP.MOVE:
				{
				}
				break;
			}
		}
		
		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.
		
		while(this.next_step != STEP.NONE) {
			
			this.step      = this.next_step;
			this.next_step = STEP.NONE;
			
			switch(this.step) {
				
				case STEP.OUTER_CONTROL:
				{
				this.GetComponent<Rigidbody>().Sleep();
				}
				break;
				
				case STEP.MOVE:
				{
					this.move_target = this.transform.position;
					this.heading_target = this.transform.TransformPoint(Vector3.forward);
				}
				break;
				
			}
			
			this.step_timer = 0.0f;
		}
		
		// ---------------------------------------------------------------- //
		// 各状態での実行処理.
		
		GameInput	gi = GameInput.getInstance();

		switch(this.step) {
			
			case STEP.MOVE:
			{
				if(this.in_formation) {
					
					chrBehaviorLocal	player = PartyControl.get().getLocalPlayer();
					
					Vector3		leader_position = player.transform.TransformPoint(this.position_in_formation);
					
					if(Vector3.Distance(this.control.getPosition(), leader_position) > 1.0f) {
						
						this.move_target    = leader_position;
						this.heading_target = this.move_target;
					}
				}
				
				this.exec_step_move();
			}
			break;
		}
		
		this.bullet_shooter.execute(gi.shot.current);
		
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

		GameInput	gi = GameInput.getInstance();
		if(!gi.pointing.current && gi.shot.trigger_on) {

		} else {

			dir_diff *= 0.1f;
		}

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
