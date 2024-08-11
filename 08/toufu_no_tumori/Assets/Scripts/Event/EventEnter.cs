using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;
using MathExtension;


// 到着イベント.
public class EnterEvent : EventBase {


	// ================================================================ //

	public enum STEP {

		NONE = -1,

		IDLE = 0,			// 実行中じゃない.

		START,				// イベント開始.
		OPEN_DOOR,			// 白菜がよこに動く＆タライにのってプレイヤーが登場.
		GET_OFF_TARAI_0,	// タライから降りる（岸に向かってジャンプ）.
		GET_OFF_TARAI_1,	// タライから降りる（少し歩く）.
		CLOSE_DOOR,			// 白菜が戻る＆タライが外に向かって移動.
		END,				// イベント終了.

		NUM,
	};
	protected Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	protected HakusaiSet	hakusai_set;

	protected GameObject	tarai_fune;

	protected chrBehaviorPlayer	player = null;
	protected bool				is_local_player = true;

	protected ipModule.Simple2Points	player_move  = new ipModule.Simple2Points();

	protected ipModule.Jump		tarai_jump  = new ipModule.Jump();
	protected ipModule.Jump		player_jump = new ipModule.Jump();

	protected Vector3	initial_player_position;

	protected SimpleSplineObject	tarai_enter_spline = null;					// タライが登場するときの移動経路.
	protected SimpleSplineObject	tarai_leave_spline = null;					// タライが画面外にはけるときの移動経路.
	protected SimpleSpline.Tracer	tarai_tracer = new SimpleSpline.Tracer();
	protected ipModule.FCurve		tarai_fcurve = new ipModule.FCurve();
	
	protected SimpleSplineObject	hakusai_spline = null;
	protected SimpleSpline.Tracer	hakusai_tracer = new SimpleSpline.Tracer();
	protected ipModule.FCurve		hakusai_fcurve = new ipModule.FCurve();

	// タライのうしろに出る波紋.
	protected LeaveEvent.RippleEffect	ripple_effect = new LeaveEvent.RippleEffect();

	// ================================================================ //

	public EnterEvent()
	{
	}

	public override void	initialize()
	{
		this.hakusai_set = new HakusaiSet();
		this.hakusai_set.attach();

		this.tarai_fune = GameObject.Find("tarai_boat").gameObject;

		GameObject		map_go = GameObject.Find(MapCreator.get().getCurrentMapName()).gameObject;

		this.data_holder = map_go.transform.Find("LeaveEventData").gameObject;

		this.tarai_enter_spline = this.data_holder.gameObject.findDescendant("tarai_enter_spline").GetComponent<SimpleSplineObject>();
		this.tarai_leave_spline = this.data_holder.gameObject.findDescendant("tarai_leave_spline").GetComponent<SimpleSplineObject>();
		this.tarai_tracer.attach(this.tarai_leave_spline.curve);
		
		this.hakusai_spline = this.data_holder.gameObject.findDescendant("hakusai_spline").GetComponent<SimpleSplineObject>();
		this.hakusai_tracer.attach(this.hakusai_spline.curve);

		this.step.set_next(STEP.IDLE);
		this.execute();
	}

	public override void	execute()
	{

		CameraControl		camera = CameraControl.get();

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// イベント開始.
			case STEP.START:
			{
				this.step.set_next(STEP.OPEN_DOOR);
				//camera.module.parallelMoveTo(this.get_locator_position("cam_loc_0"));

				Debug.Log("Name:" + this.player.controll.account_name);

				foreach (ItemManager.ItemState istate in GlobalParam.get().item_table.Values) {

					Debug.Log("Item:" + istate.item_id + " Own:" + istate.owner + " State:" + istate.state);

					if (istate.owner == this.player.controll.account_name &&
				    	istate.state == ItemController.State.Picked) {
						// すでにアイテムを取得していたら持っていけるようにする.
						ItemManager.get().activeItme(istate.item_id, true);
						ItemManager.get().finishGrowingItem(istate.item_id);
						QueryItemPick query = this.player.controll.cmdItemQueryPick(istate.item_id, false, true);
						if (query != null) {
							query.is_anon = true;
							query.set_done(true);
							query.set_success(true);
						}
						ItemManager.get().setVisible(istate.item_id, true);
					}
				}

				// リモートで引越し中はローカルも引越しする.
				do {

					MovingData moving = GlobalParam.get().remote_moving;
					if(!moving.moving) {

						break;
					}

					chrController remote = CharacterRoot.get().findCharacter(moving.characterId);
					if(remote == null) {

						break;
					}

					chrBehaviorNet	remote_player = remote.behavior as chrBehaviorNet;
					if(remote_player == null) {

						break;
					}

					chrBehaviorNPC_House	house = CharacterRoot.getInstance().findCharacter<chrBehaviorNPC_House>(moving.houseId);
					if(house == null) {

						break;
					}

					Debug.Log("House move event call:" + moving.characterId + ":" + moving.houseId);

					remote_player.beginHouseMove(house);

					// 「引越し中～」のふきだし表示.
					house.startHouseMove();

				} while(false);	
			}
			break;

			// 白菜がよこに動く＆タライにのってプレイヤーが登場.
			case STEP.OPEN_DOOR:
			{
				if(this.hakusai_fcurve.isDone() && this.tarai_fcurve.isDone()) {
					
					this.step.set_next(STEP.GET_OFF_TARAI_0);
				}
			}
			break;

			// タライから降りる（岸に向かってジャンプ）.
			case STEP.GET_OFF_TARAI_0:
			{
				if(!this.player_jump.isMoving()) {

					this.step.set_next(STEP.GET_OFF_TARAI_1);
				}
			}
			break;

			// タライから降りる（少し歩く）.
			case STEP.GET_OFF_TARAI_1:
			{
				if(!this.player_move.isMoving()) {

					this.step.set_next(STEP.CLOSE_DOOR);
				}
			}
			break;


			// 白菜が戻る＆タライが外に向かって移動.
			case STEP.CLOSE_DOOR:
			{
				if(this.hakusai_fcurve.isDone() && this.tarai_fcurve.isDone()) {
					
					this.step.set_next(STEP.END);
				}
			}
			break;

			case STEP.END:
			{
				camera.module.popPosture();

				this.step.set_next(STEP.IDLE);
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			dbwin.console().print(this.step.ToString());

			switch(this.step.do_initialize()) {
	
				// イベント開始.
				case STEP.START:
				{
					camera.module.pushPosture();

					this.player.beginOuterControll();
					this.player.controll.cmdSetPosition(this.tarai_leave_spline.curve.cvs.back().position);
				
					this.tarai_fune.transform.position = this.tarai_leave_spline.curve.cvs.back().position;
				
					if(!this.is_local_player) {

						SoundManager.get().playSE(Sound.ID.SMN_JINGLE01);
					}
				}
				break;

				// 白菜がよこに動く＆タライにのってプレイヤーが登場.
				case STEP.OPEN_DOOR:
				{
					this.hakusai_set.setControl(true);
					this.hakusai_fcurve.setSlopeAngle(10.0f, 10.0f);
					this.hakusai_fcurve.setDuration(4.0f);
					this.hakusai_fcurve.start();
					this.hakusai_tracer.restart();

					this.tarai_fcurve.setSlopeAngle(60.0f, 5.0f);
					this.tarai_fcurve.setDuration(3.5f);
					this.tarai_fcurve.setDelay(0.5f);
					this.tarai_fcurve.start();
				}
				break;

				// タライから降りる（岸に向かってジャンプ）.
				case STEP.GET_OFF_TARAI_0:
				{
					Vector3		start = this.player.controll.getPosition();
					Vector3		goal  = this.get_locator_position("chr_loc_0");

					this.player_jump.start(start, goal, 1.0f);

				}
				break;

				// タライから降りる（少し歩く）.
				case STEP.GET_OFF_TARAI_1:
				{
					this.player_move.position.start = this.player.controll.getPosition();
					this.player_move.position.goal  = this.get_locator_position("chr_loc_1");
					this.player_move.startConstantVelocity(chrBehaviorLocal.MOVE_SPEED);
				}
				break;

				// 白菜が戻る＆タライが外に向かって移動.
				case STEP.CLOSE_DOOR:
				{
					this.hakusai_fcurve.setSlopeAngle(10.0f, 10.0f);
					this.hakusai_fcurve.setDuration(4.0f);
					this.hakusai_fcurve.setDelay(1.0f);
					this.hakusai_fcurve.start();
					this.hakusai_tracer.restart();
					this.hakusai_tracer.setCurrentByDistance(this.hakusai_tracer.curve.calcTotalDistance());
				
					this.tarai_tracer.attach(this.tarai_enter_spline.curve);
					this.tarai_tracer.restart();
					this.tarai_fcurve.reset();
					this.tarai_fcurve.setSlopeAngle(10.0f, 60.0f);
					this.tarai_fcurve.setDuration(2.5f);
					this.tarai_fcurve.start();
				
					this.ripple_effect.is_created = false;
				}
				break;

				case STEP.END:
				{
					// イベント終了.
					this.hakusai_set.reset();
					this.hakusai_set.setControl(false);

					this.player.endOuterControll();

					this.player = null;
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 白菜がよこに動く＆タライにのってプレイヤーが登場.
			case STEP.OPEN_DOOR:
			{
				this.hakusai_fcurve.execute(Time.deltaTime);
				this.hakusai_tracer.proceedToDistance(this.hakusai_tracer.curve.calcTotalDistance()*this.hakusai_fcurve.getValue());
				this.hakusai_set.setPosition(this.hakusai_tracer.cv.position);
			
				this.tarai_fcurve.execute(Time.deltaTime);
				this.tarai_tracer.proceedToDistance(this.tarai_tracer.curve.calcTotalDistance()*(1.0f - this.tarai_fcurve.getValue()));

				SimpleSpline.ControlVertex	cv = this.tarai_tracer.getCurrent();
				
				this.tarai_fune.setPosition(cv.position);
				this.player.controll.cmdSetPosition(cv.position);
				this.player.controll.cmdSmoothHeadingTo(cv.position - cv.tangent.Y(0.0f));

				if(this.tarai_fcurve.isTriggerDone()) {
					
					this.ripple_effect.is_created = false;
				}
			}
			break;

			// タライから降りる（岸に向かってジャンプ）.
			case STEP.GET_OFF_TARAI_0:
			{
				this.player_jump.execute(Time.deltaTime);
				this.player.controll.cmdSetPosition(this.player_jump.position);
				this.player.controll.cmdSmoothHeadingTo(this.player_jump.goal);
			}
			break;

			// タライから降りる（少し歩く）.
			case STEP.GET_OFF_TARAI_1:
			{
				this.player_move.execute(Time.deltaTime);
				this.player.controll.cmdSetPosition(this.player_move.position.current);
				this.player.controll.cmdSmoothHeadingTo(this.player_move.position.goal);
				this.player.playWalkMotion();
			}
			break;

			// 白菜が戻る＆タライが外に向かって移動.
			case STEP.CLOSE_DOOR:
			{
				this.hakusai_fcurve.execute(Time.deltaTime);
				this.hakusai_tracer.proceedToDistance(this.hakusai_tracer.curve.calcTotalDistance()*(1.0f - this.hakusai_fcurve.getValue()));
				this.hakusai_set.setPosition(this.hakusai_tracer.getCurrent().position);
			
				this.tarai_fcurve.execute(Time.deltaTime);
				this.tarai_tracer.proceedToDistance(this.tarai_tracer.curve.calcTotalDistance()*(1.0f - this.tarai_fcurve.getValue()));
				this.tarai_fune.transform.position = this.tarai_tracer.getCurrent().position;
				this.player.stopWalkMotion();
			}
			break;
		}

		// タライのうしろに出る波紋.
		if(!this.ripple_effect.is_created || Vector3.Distance(this.ripple_effect.last_position, this.tarai_fune.getPosition()) > 2.0f) {
			
			this.ripple_effect.is_created = true;
			this.ripple_effect.last_position = this.tarai_fune.transform.position;
			
			EffectRoot.get().createRipple(this.ripple_effect.last_position);
		}

		// ---------------------------------------------------------------- //
	}

	public override void		onGUI()
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

	// ローカルプレイヤーが主役？.
	public void		setIsLocalPlayer(bool is_local_player)
	{
		this.is_local_player = is_local_player;
	}

	// ================================================================ //

	// 白菜と波エフェクト.
	protected class HakusaiSet {

		public TransformModifier	hakusai;
		public TransformModifier	nami0;
		public TransformModifier	nami1;

		public void attach()
		{
			this.hakusai = GameObject.Find("hakusai2").gameObject.AddComponent<TransformModifier>();
			this.nami0   = GameObject.Find("nami_00").gameObject.AddComponent<TransformModifier>();
			this.nami1   = GameObject.Find("nami_01").gameObject.AddComponent<TransformModifier>();

			this.hakusai.setWriteMask("xz");
		}

		public void detach()
		{
			GameObject.Destroy(this.hakusai);
			GameObject.Destroy(this.nami0);
			GameObject.Destroy(this.nami1);
		}

		public void setControl(bool is_control)
		{
			this.hakusai.setControl(is_control);
			this.nami0.setControl(is_control);
			this.nami1.setControl(is_control);
		}

		public void	setPosition(Vector3 position)
		{
			this.hakusai.setPosition(position);
			this.nami0.setPosition(this.nami0.getInitialPosition() - this.hakusai.getInitialPosition() + position);
			this.nami1.setPosition(this.nami1.getInitialPosition() - this.hakusai.getInitialPosition() + position);
		}

		public void reset()
		{
			this.hakusai.reset();
			this.nami0.reset();
			this.nami1.reset();
		}
	};
}
