using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;
using MathExtension;

// ひっこし終了イベント.
public class HouseMoveEndEvent : EventBase {


	// ================================================================ //

	public enum STEP {

		NONE = -1,

		IDLE = 0,			// 実行中じゃない.

		START,				// イベント開始.
		HOUSE_TURN,			// 家がくるっとまわる.
		OPEN_DOOR,			// 戸が開く.
		EXIT,				// 家の外に出る.
		EXIT_CAT,			// ネコが家の外に出る.
		CLOSE_DOOR,			// 戸がしまる.

		END,				// イベント終了.

		NUM,
	};
	protected Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	protected chrBehaviorPlayer	player          = null;
	protected bool				is_local_player = true;

	protected chrBehaviorNPC_House	house = null;
	protected ItemBehaviorCat		cat   = null;

	protected ipModule.Simple2Points	player_move  = new ipModule.Simple2Points();
	protected ipModule.Simple2Points	cat_move     = new ipModule.Simple2Points();
	protected ipModule.Jump				cat_jump     = new ipModule.Jump();

	protected ipModule.FCurve			house_fcurve = new ipModule.FCurve();

	protected Vector3	initial_player_position;
	
	
	// ================================================================ //

	public HouseMoveEndEvent()
	{
	}

	public override void	initialize()
	{
		var		cat_go = this.house.gameObject.findChildGameObject("Cat");

		if(cat_go != null) {

			this.cat = cat_go.GetComponentInChildren<ItemBehaviorCat>();
		}

		// ---------------------------------------------------------------- //
		// デバッグのときのため（本来はいらない）.
		{
			if(this.cat == null) {
		
				this.cat = ItemManager.getInstance().findItem("Cat").behavior as ItemBehaviorCat;
			}

			// ねこ.
			if(this.cat != null) {
	
				// 縁側に座らせる.
				this.cat.controll.setVisible(true);
				this.cat.controll.cmdSetCollidable(false);
				this.cat.controll.setParent(this.house.controll.gameObject);		
				this.cat.controll.transform.localPosition = new Vector3(0.06996871f, 0.2095842f, -0.4440203f);
				this.cat.controll.transform.localRotation = Quaternion.AngleAxis(0.0f, Vector3.up);
				this.cat.controll.transform.localScale    = Vector3.one*0.6f;
			}

			this.house.transform.rotation = Quaternion.AngleAxis(315.0f, Vector3.up);
		}
		// ここまで　デバッグ用.
		// ---------------------------------------------------------------- //

		this.step.set_next(STEP.IDLE);
		this.execute();
	}

	public override void	execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// イベント開始.
			case STEP.START:
			{
				this.step.set_next(STEP.HOUSE_TURN);
			}
			break;

			// 家がくるっとまわる.
			case STEP.HOUSE_TURN:
			{
				if(!this.house_fcurve.isMoving()) {

					this.step.set_next_delay(STEP.OPEN_DOOR, 0.2f);
				}
			}
			break;

			// 玄関のとびらが開く.
			case STEP.OPEN_DOOR:
			{
				if(!this.house.controll.isMotionPlaying()) {

					this.step.set_next_delay(STEP.EXIT, 0.2f);
				}
			}
			break;

			// 家の外に出る.
			case STEP.EXIT:
			{
				if(this.player_move.isDone()) {

					this.step.set_next(STEP.EXIT_CAT);
				}
			}
			break;

			// ネコが家の外に出る.
			case STEP.EXIT_CAT:
			{
				if(Vector3.Distance(this.cat.controll.getPosition(), this.player.controll.getPosition()) <= 1.0f) {

					this.player.controll.item_carrier.beginCarry(this.cat.controll);
					this.step.set_next(STEP.CLOSE_DOOR);
				}
			}
			break;

			// 戸がしまる.
			case STEP.CLOSE_DOOR:
			{
				if(!this.house.controll.isMotionPlaying()) {

					this.step.set_next_delay(STEP.END, 0.2f);
				}
			}
			break;


			case STEP.END:
			{
				this.step.set_next(STEP.IDLE);
			}
			break;

		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				// イベント開始.
				case STEP.START:
				{
					this.player.beginOuterControll();
					this.player.GetComponent<Rigidbody>().isKinematic = true;

					this.house.controll.setParent(null);
				}
				break;

				// 家がくるっとまわる.
				case STEP.HOUSE_TURN:
				{
					this.house_fcurve.setSlopeAngle(70.0f, 0.0f);
					this.house_fcurve.setDuration(1.5f);
					this.house_fcurve.start();
				}
				break;

				// 玄関のとびらが開く.
				case STEP.OPEN_DOOR:
				{
					this.house.openDoor();
				}
				break;

				// 家の外に出る.
				case STEP.EXIT:
				{
					Vector3		start = this.house.controll.getPosition();
					Vector3		goal  = this.house.controll.transform.TransformPoint(Vector3.forward*2.0f);

					start = start + (goal - start).normalized*HouseMoveStartEvent.HIDE_DISTANCE;

					this.player.controll.cmdSetDirection(this.house.controll.getDirection() + 180.0f);
					this.player.controll.setVisible(true);
					this.player_move.position.start = start;
					this.player_move.position.goal  = goal;
					this.player_move.startConstantVelocity(chrBehaviorLocal.MOVE_SPEED);
				}
				break;

				// ネコが家の外に出る.
				case STEP.EXIT_CAT:
				{
					// ねこ.
					if(this.cat != null) {

						Vector3		start = this.house.controll.getPosition();
						Vector3		goal  = this.house.controll.transform.TransformPoint(Vector3.forward*2.0f);

						this.cat.controll.transform.localScale = Vector3.one;
						this.cat.controll.cmdSetDirection(this.house.controll.getDirection() + 180.0f);
						this.cat.controll.setParent(null);

						this.cat_move.position.start = start;
						this.cat_move.position.goal  = goal;
						this.cat_move.startConstantVelocity(chrBehaviorLocal.MOVE_SPEED);
					}
				}
				break;

				// 戸がしまる.
				case STEP.CLOSE_DOOR:
				{
					this.house.closeDoor();
				}
				break;

				case STEP.END:
				{
					this.player.endOuterControll();
					this.player.GetComponent<Rigidbody>().isKinematic = false;

					this.player.endHouseMove();

					this.house.endHouseMove();
				}
				break;

			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 家がくるっとまわる.
			case STEP.HOUSE_TURN:
			{
				this.house_fcurve.execute(Time.deltaTime);

				float	y_angle = Mathf.LerpAngle(315.0f, 135.0f, this.house_fcurve.getValue());

				this.house.transform.rotation = Quaternion.AngleAxis(y_angle, Vector3.up);
			}
			break;

			//玄関のとびらが開く.
			case STEP.OPEN_DOOR:
			{
			}
			break;

			// 家の外に出る.
			case STEP.EXIT:
			{
				this.player_move.execute(Time.deltaTime);
				this.player.controll.cmdSetPosition(this.player_move.position.current);
				this.player.controll.cmdSmoothHeadingTo(this.player_move.position.goal);

				if(!this.player_move.isDone()) {

					this.player.playWalkMotion();

				} else {

					this.player.stopWalkMotion();
				}
			}
			break;

			// ネコが家の外に出る.
			case STEP.EXIT_CAT:
			{
				// ねこ.
				if(this.cat != null) {

					this.cat_move.execute(Time.deltaTime);
					this.cat.controll.gameObject.setLocalPosition(this.cat_move.position.current);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

	public override  void		onGUI()
	{
	}

	// イベントが実行中？.
	public override bool	isInAction()
	{
		bool	ret = !(this.step.get_current() == STEP.IDLE && this.step.get_next() == STEP.NONE);

		return(ret);
	}

	// イベント開始.
	public override void	start()
	{
		if(this.player != null) {

			this.initial_player_position = this.player.transform.position;

			this.step.set_next(STEP.START);
		}
	}

	// ================================================================ //

	// 主役となるプレイヤー（ローカル/リモート）をセットする.
	public void		setPrincipal(chrBehaviorPlayer player)
	{
		this.player = player;
	}

	// 家をセットする.
	public void		setHouse(chrBehaviorNPC_House house)
	{
		this.house = house;
	}

	// ローカルプレイヤーが主役？.
	public void		setIsLocalPlayer(bool is_local_player)
	{
		this.is_local_player = is_local_player;
	}

	// ================================================================ //
}
