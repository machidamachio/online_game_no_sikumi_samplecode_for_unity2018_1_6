using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ルーム移動イベント.
public class TransportEvent : EventBase {
	// ================================================================ //

	public enum STEP {

		NONE = -1,

		IDLE = 0,			// 実行中じゃない.

		START,				// イベント開始.
		HOLE_IN,			// プレイヤーを吸い込む.
		CAMERA_MOVE,		// カメラが出口に移動.
		HOLE_OUT,			// プレイヤーが飛び出す.
		READY,				// カメラがローカルプレイヤーに戻る.
		END,				// イベント終了.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	protected	DoorControl	door = null;

	// プレイヤーをコントロールするための情報いろいろ.
	protected class PlayerStep {

		public 	PlayerStep()
		{
			this.step_hole_in  = new StepHoleIn();
			this.step_hole_out = new StepHoleOut();

			this.ip_jump = new ipModule.Jump();
		}

		public	chrBehaviorPlayer	player;
		public	ipModule.Jump		ip_jump;

		// 穴に吸い込まれるまで.
		public 	class StepHoleIn {
	
			public bool		is_done = false;
		};
		public 	StepHoleIn	step_hole_in;

		// 穴から飛び出るところ.
		public 	class StepHoleOut {
	
			public	float		delay;	
	
			public	Vector3		position;
			public	Vector3		pivot;
			public	float		omega;
	
		};
		public 	StepHoleOut	step_hole_out;
	};
	protected	List<PlayerStep>	player_steps = new List<PlayerStep>();

	// カメラの移動.
	protected struct CameraMove {

		public bool		is_done;

		public Vector3	start_interest;
		public Vector3	interest;
	};
	protected CameraMove	step_camera_move;
	protected CameraMove	step_ready;

	protected bool		is_end_at_hole_in = false;

	// ================================================================ //

	public TransportEvent()
	{
		this.step.set_next(STEP.IDLE);
	}

	public override void	initialize()
	{
	}

	public override void	execute()
	{
		CameraControl		camera = CameraControl.getInstance();

		float	camera_move_time = 1.5f;
		float	ready_time = 1.5f;


		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.START:
			{
				this.step.set_next(STEP.HOLE_IN);
			}
			break;

			case STEP.HOLE_IN:
			{
				// プレイヤーがみんな穴に入ったら.
				// （『穴に入っていないプレイヤー』がいなかったら.
				if(!this.player_steps.Exists(x => !x.step_hole_in.is_done)) {

					if(this.is_end_at_hole_in) {

						// 『穴に入るまで』のとき（フロアー移動ドアのとき）はおしまい.
						this.step.set_next(STEP.END);

					} else {

						this.step.set_next(STEP.CAMERA_MOVE);
					}
				}
			}
			break;

			case STEP.CAMERA_MOVE:
			{
				if(this.step.get_time() >= camera_move_time) {

					this.step.set_next(STEP.HOLE_OUT);
				}
			}
			break;

			case STEP.HOLE_OUT:
			{
				if(!this.player_steps.Exists(x => !x.ip_jump.isDone())) {

					this.step.set_next(STEP.READY);
				}
			}
			break;

			case STEP.READY:
			{
				if(this.step.get_time() >= ready_time) {

					this.step.set_next(STEP.END);
				}
			}
			break;

			case STEP.END:
			{
				foreach(PlayerStep player_step in this.player_steps) {

					player_step.player.endOuterControll();
				}
				camera.endOuterControll();

				this.step.set_next(STEP.IDLE);
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.START:
				{
					// イベント開始.

					this.player_steps.Clear();

					List<chrBehaviorPlayer>		players = PartyControl.get().getPlayers();

					foreach(chrBehaviorPlayer player in players) {

						PlayerStep		player_step = new PlayerStep();

						player_step.player = player;

						this.player_steps.Add(player_step);
					}

					foreach(PlayerStep player_step in this.player_steps) {

						player_step.player.beginOuterControll();
						player_step.player.control.cmdEnableCollision(false);
						player_step.player.GetComponent<Rigidbody>().useGravity = false;
					}

					// 演奏開始.
					SoundManager.getInstance().playSE(Sound.ID.DDG_SE_SYS05);

					camera.beginOuterControll();

				}
				break;

				case STEP.HOLE_IN:
				{
				}
				break;

				case STEP.CAMERA_MOVE:
				{
					// 壁のフェードイン/フェードアウト.

					if(this.door.door_dir == Map.EWSN.NORTH) {

						List<RoomWallControl> 	walls = this.door.connect_to.GetRoom().GetRoomWalls(Map.EWSN.SOUTH);
	
						foreach(RoomWallControl wall in walls) {
	
							wall.FadeOut();
						}

					} else if(this.door.door_dir == Map.EWSN.SOUTH) {

						List<RoomWallControl> 	walls = this.door.GetRoom().GetRoomWalls(Map.EWSN.SOUTH);
	
						foreach(RoomWallControl wall in walls) {
	
							wall.FadeIn();
						}
					}

					this.step_camera_move.start_interest = camera.calcGroundLevelInterest();
				}
				break;

				case STEP.HOLE_OUT:
				{
					float	peak  = 5.0f;
					float	delay = 0.0f;

					foreach(PlayerStep player_step in this.player_steps) {

						chrBehaviorPlayer	player = player_step.player;

						float	y_angle = door_dir_to_y_angle(this.door.door_dir);

						// 着地する場所　キャラごとに違う.
						Vector3		landing_position = this.calc_landing_position(player);

						Vector3		start = this.door.connect_to.transform.position;
						Vector3		goal  = start + landing_position;

						player_step.step_hole_out.position = start;
						player_step.step_hole_out.delay    = delay;

						player_step.ip_jump.setBounciness(new Vector3(0.0f, -0.5f, 0.0f));
						player_step.ip_jump.start(start, goal, peak);

						player_step.step_hole_out.pivot = landing_position;
						player_step.step_hole_out.pivot.Normalize();
						player_step.step_hole_out.pivot = Quaternion.AngleAxis(90.0f, Vector3.up)*player_step.step_hole_out.pivot;
						player_step.step_hole_out.omega = 360.0f/(player_step.ip_jump.t0 + player_step.ip_jump.t1);
	
						player.control.cmdSetPosition(player_step.step_hole_out.position);
						player.control.cmdSetDirection(y_angle);

						player.getModel().SetActive(false);
						player.getModel().transform.localPosition = player.getInitialLocalPositionModel();
						player.getModel().transform.localScale = Vector3.one;
					
						delay += 0.2f;

						this.door.leavePlayer(player_step.player.control.local_index);
						this.door.connect_to.enterPlayer(player_step.player.control.local_index);
					}
				}
				break;

				case STEP.READY:
				{
					this.step_ready.start_interest = camera.calcGroundLevelInterest();
				}
				break;

				case STEP.END:
				{
					// イベント終了.

					foreach(PlayerStep player_step in this.player_steps) {

						chrBehaviorPlayer	player = player_step.player;

						Vector3		landing_position = this.calc_landing_position(player);

						if(this.door.connect_to != null) {

							player_step.player.control.cmdSetPositionAnon(this.door.connect_to.transform.position + landing_position);
						}
						player_step.player.GetComponent<Rigidbody>().useGravity = true;
						player_step.player.control.cmdEnableCollision(true);
					}

					this.door.beginSleep();

					if(this.door.connect_to != null) {

						this.door.connect_to.beginWaitLeave();
						PartyControl.get().setCurrentRoom(this.door.connect_to.GetRoom());
					}

					this.door = null;
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.IDLE:
			{
			}
			break;


			case STEP.HOLE_IN:
			{
				foreach(PlayerStep player_step in this.player_steps) {

					chrBehaviorPlayer	player = player_step.player;
				
					if(player_step.step_hole_in.is_done) {

						continue;
					}

					// 位置.

					Vector3		player_position = player.control.getPosition();
					Vector3		door_position   = this.door.gameObject.transform.position;
	
					Vector3		distance = player_position - door_position;
	
					distance.y = 0.0f;
	
					float	speed;
					float	tangent_speed;
					float	rotate_speed;
	
					float	radius = distance.magnitude;
	
					speed = Mathf.InverseLerp(5.0f, 0.0f, radius);
					speed = Mathf.Clamp01(speed);
					speed = Mathf.Lerp(10.0f, 0.2f, speed)*Time.deltaTime;
	
					tangent_speed = Mathf.InverseLerp(5.0f, 0.1f, radius);
					tangent_speed = Mathf.Clamp01(tangent_speed);
					tangent_speed = Mathf.Lerp(0.01f, 15.0f, tangent_speed)*Time.deltaTime;
	
					rotate_speed = Mathf.InverseLerp(5.0f, 0.1f, radius);
					rotate_speed = Mathf.Clamp01(rotate_speed);
					rotate_speed = Mathf.Pow(rotate_speed, 2.0f);
					rotate_speed = Mathf.Lerp(0.2f, 1.0f, rotate_speed)*360.0f*Time.deltaTime;
	
					if(distance.magnitude > speed) {
	
						distance -= distance.normalized*speed;
	
						float	angle = Mathf.Atan2(tangent_speed, distance.magnitude)*Mathf.Rad2Deg;
	
						angle = Mathf.Min(angle, 20.0f);
	
						distance = Quaternion.AngleAxis(angle, Vector3.up)*distance;
	
						player_position = door_position + distance;
	
					} else {
	
						player_step.step_hole_in.is_done = true;
	
						player_position = door_position;
					}
	
					player.control.cmdSetPositionAnon(player_position);

					// ローテーション.
	
					player.transform.Rotate(Vector3.up, rotate_speed);

					// スケール.

					player.GetComponent<Rigidbody>().velocity = Vector3.zero;
	
					float	scale;
	
					scale = Mathf.InverseLerp(0.5f, 0.0f, radius);
					scale = Mathf.Clamp01(scale);
					scale = Mathf.Lerp(1.0f, 0.2f, scale);

					player.getModel().transform.localPosition = player.getInitialLocalPositionModel()*scale;
					player.getModel().transform.localScale = scale*Vector3.one;

					if(player_step.step_hole_in.is_done) {

						player.getModel().SetActive(false);
					}
				}
	
				// カメラ.

				Vector3		p0 = camera.calcGroundLevelInterest();
				Vector3		p1 = Vector3.Lerp(p0, this.door.transform.position, 0.01f);

				camera.getModule().parallelInterestTo(p1);
			}
			break;

			case STEP.CAMERA_MOVE:
			{
				float		ratio = this.step.get_time()/camera_move_time;

				ratio = Mathf.Clamp01(ratio);
				ratio = Mathf.Sin(ratio*Mathf.PI/2.0f);

				Vector3		p0 = this.step_camera_move.start_interest;
				Vector3		p1 = this.door.connect_to.transform.position;

				p1 = Vector3.Lerp(p0, p1, ratio);

				camera.getModule().parallelInterestTo(p1);
			}
			break;

			case STEP.HOLE_OUT:
			{
				foreach(PlayerStep player_step in this.player_steps) {

					if(this.step.get_time() < player_step.step_hole_out.delay) {

						continue;
					}

					chrBehaviorPlayer	player = player_step.player;

					player.getModel().SetActive(true);

					if(player_step.ip_jump.isDone()) {

						continue;
					}
					player_step.ip_jump.execute(Time.deltaTime);

					player_step.step_hole_out.position = player_step.ip_jump.position;
		
					float		ratio = this.step.get_time()/(player_step.ip_jump.t0 + player_step.ip_jump.t1);
	
					ratio = Mathf.Clamp01(ratio);
					ratio = Mathf.Pow(ratio, 0.35f);
					ratio = Mathf.Lerp(-360.0f*1.5f, 0.0f, ratio);
	
					player.getModel().transform.localRotation = Quaternion.AngleAxis(ratio, player_step.step_hole_out.pivot);
	
					player.control.cmdSetPosition(player_step.step_hole_out.position);
				}
			}
			break;

			case STEP.READY:
			{
				float		ratio = this.step.get_time()/ready_time;

				ratio = Mathf.Clamp01(ratio);
				ratio = Mathf.Lerp(-Mathf.PI/2.0f, Mathf.PI/2.0f, ratio);
				ratio = Mathf.Sin(ratio);
				ratio = Mathf.InverseLerp(-1.0f, 1.0f, ratio);

				Vector3		p0 = this.step_ready.start_interest;
				Vector3		p1 = PartyControl.get().getLocalPlayer().control.getPosition();

				p1 = Vector3.Lerp(p0, p1, ratio);

				camera.getModule().parallelInterestTo(p1);
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

	// イベントが実行中？.
	public override  bool	isInAction()
	{
		bool	ret = !(this.step.get_current() == STEP.IDLE && this.step.get_next() == STEP.NONE);

		return(ret);
	}

	// イベント開始.
	public override void		start()
	{
		this.step.set_next(STEP.START);
	}

	// ================================================================ //

	// ドアをセットする.
	public void		setDoor(DoorControl door)
	{
		this.door = door;
	}

	// ドアをゲットする.
	public DoorControl		getDoor()
	{
		return(this.door);
	}

	// ================================================================ //

	// 穴に吸い込まれた時点でイベント終了する？（フロアー移動のとき）.
	public void		setEndAtHoleIn(bool is_end)
	{
		this.is_end_at_hole_in = is_end;
	}

	// ================================================================ //

	// ドアの向きを角度（ディグリー）にする.
	protected float		door_dir_to_y_angle(Map.EWSN door_dir)
	{
		float	y_angle = 0.0f;

		switch(door_dir) {

			case Map.EWSN.NORTH:	y_angle =   0.0f;	break;
			case Map.EWSN.SOUTH:	y_angle = 180.0f;	break;
			case Map.EWSN.EAST:		y_angle =  90.0f;	break;
			case Map.EWSN.WEST:		y_angle = -90.0f;	break;
		}

		return(y_angle);
	}

	// 着地する場所　キャラごとに違う.
	protected Vector3	calc_landing_position(chrBehaviorPlayer player)
	{
		Vector3		position = Vector3.zero;

		position = PartyControl.get().getPositionOffset(player.control.global_index);

		position = Quaternion.AngleAxis(this.door_dir_to_y_angle(this.door.door_dir), Vector3.up)*position;

		return(position);
	}

	// ================================================================ //
}
