using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ビヘイビアー　NPC用.
public class chrBehaviorKabu : chrBehaviorBase {


	public enum STEP {

		NONE = -1,

		IDLE = 0,			// 登場.
		MOVE,				// プレイヤーの後をついて移動中.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	protected ipModule.Hover	hover = new ipModule.Hover();

	protected chrBehaviorLocal	player;
	protected CameraControl		camera_control;
	protected float				radius;

	protected Vector3			door_position;			// ドアの位置.
	protected bool				is_in_event = false;	// フロアー移動イベント中？.

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// デモでセリフをしゃべっているときの位置.
	public static Vector3	getStayPosition()
	{
		return(new Vector3(0.0f, 3.0f, -5.0f));
	}

	// デモでセリフをしゃべっているときの向き.
	public static float		getStayDirection()
	{
		return(180.0f);
	}

	// デモで登場するときの高さ.
	public static float	getStartHeight()
	{
		return(6.0f);
	}

	// フロアー移動イベントが始まったときに呼ばれる.
	public void		onBeginTransportEvent()
	{
		TransportEvent	ev = EventRoot.get().getCurrentEvent<TransportEvent>();

		this.door_position = ev.getDoor().getPosition();
		this.is_in_event   = true;
	}

	// ================================================================ //

	// 生成直後に呼ばれる.
	public override sealed void	initialize()
	{
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public override void	start()
	{
		this.player = PartyControl.get().getLocalPlayer();

		this.camera_control = CameraControl.get();

		// ---------------------------------------------------------------- //

		this.control.GetComponent<Rigidbody>().isKinematic = true;
		this.control.GetComponent<Rigidbody>().Sleep();

		this.control.setVisible(false);

		this.hover.gravity     = Physics.gravity.y*0.1f;
		this.hover.accel_scale = 2.0f;

		this.hover.zero_level = 3.0f;
		this.hover.hard_limit.max = 100.0f;

		this.radius = 5.0f;

		//

		this.step.set_next(STEP.IDLE);
	}

	// 毎フレームよばれる.
	public override	void	execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.


		switch(this.step.do_transition()) {

			case STEP.IDLE:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.IDLE:
				{
					Vector3		stay_position = chrBehaviorKabu.getStayPosition();
					float		start_height  = chrBehaviorKabu.getStartHeight();

					this.hover.height      = stay_position.y + start_height;
					this.hover.gravity     = Physics.gravity.y*2.0f;
					this.hover.accel_scale = 1.5f;

					this.control.setVisible(true);

					this.control.cmdSetPositionAnon(stay_position);
					this.control.cmdSetDirectionAnon(chrBehaviorKabu.getStayDirection());
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.IDLE:
			{
				this.hover.execute(Time.deltaTime);

				// 落下が止まったところでパラメーターを変える.
				// （ゆっくりした動きにしたいから）.		
				if(this.hover.trigger_down_peak) {

					this.hover.gravity     = Physics.gravity.y*0.1f;
					this.hover.accel_scale = 2.0f;
				}

				Vector3		position = this.control.getPosition();
		
				position.y = this.hover.height;
	
				this.control.cmdSetPosition(position);
			}
			break;

			case STEP.MOVE:
			{
				this.execute_step_move();
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

	protected void	execute_step_move()
	{
		this.hover.execute(Time.deltaTime);

		Vector3		position = this.control.getPosition();
		Vector3		next_position = position;

		// ---------------------------------------------------------------- //

		Vector3		camera_position = this.camera_control.getModule().getPosture().position;
		Vector3		player_position = this.player.control.getPosition();

		if(this.is_in_event) {

			player_position = this.door_position;
		}

		float	rate = (this.control.getPosition().y - player_position.y)/(camera_position.y - player_position.y);

		Vector3	interest = Vector3.Lerp(player_position, camera_position, rate);

		// ---------------------------------------------------------------- //

		Vector3		v = position - interest;

		float		current_distance = v.magnitude;

		current_distance = Mathf.Lerp(current_distance, this.radius, 0.05f);

		v.Normalize();
		v *= current_distance;

		next_position = interest + v;

		next_position.y = this.hover.height;

		this.control.cmdSetPosition(next_position);
		this.control.cmdSmoothHeadingTo(interest);
	}

	// ================================================================ //

	public void		beginMove()
	{
		this.step.set_next(STEP.MOVE);
	}
}