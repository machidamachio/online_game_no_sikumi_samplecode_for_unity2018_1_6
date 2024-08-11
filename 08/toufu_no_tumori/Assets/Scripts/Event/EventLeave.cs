using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;
using MathExtension;

// 出発イベント.
public class LeaveEvent : EventBase {


	// ================================================================ //

	public enum STEP {

		NONE = -1,

		IDLE = 0,			// 実行中じゃない.

		START,				// イベント開始.
		OPEN_DOOR,			// 白菜がよこに動く＆タライが登場.
		RIDE_TARAI_0,		// タライにのる（岸まで歩く）.
		RIDE_TARAI_1,		// タライにのる（タライに向かってジャンプ）.
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
	protected bool				is_map_change   = true;			// イベント終了後にマップチェンジする？（デバッグのときに falseにする）.

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
	public class RippleEffect {

		public bool		is_created    = false;
		public Vector3	last_position = Vector3.zero;
	};
	protected RippleEffect	ripple_effect = new RippleEffect();


	// ================================================================ //

	public LeaveEvent()
	{
	}

	public override void	initialize()
	{
		this.hakusai_set = new HakusaiSet();
		this.hakusai_set.attach();

		this.tarai_fune = GameObject.Find("tarai_boat").gameObject;

		GameObject		map_go = GameObject.Find(MapCreator.getInstance().getCurrentMapName()).gameObject;

		this.data_holder = map_go.transform.Find("LeaveEventData").gameObject;

		this.tarai_enter_spline = this.data_holder.gameObject.findDescendant("tarai_enter_spline").GetComponent<SimpleSplineObject>();
		this.tarai_leave_spline = this.data_holder.gameObject.findDescendant("tarai_leave_spline").GetComponent<SimpleSplineObject>();
		this.tarai_tracer.attach(this.tarai_enter_spline.curve);

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

				// マップに紐づかないアイテムはお持ち帰り.
				bool isPickingup = false;
				string current_map = MapCreator.get().getCurrentMapName();
				ItemController itemYuzu = ItemManager.get().findItem("Yuzu");
				if (itemYuzu != null && itemYuzu.isActive()) {
					if (current_map != itemYuzu.getProduction()) {
						// お持ち帰りするアイテムがあった.
						this.player.controll.cmdItemQueryPick(itemYuzu.name, true, true);
						isPickingup = true;
					}
				}

				ItemController itemNegi = ItemManager.get().findItem("Negi");
				if (itemNegi != null && itemNegi.isActive()) {
					if (current_map != itemNegi.getProduction()) {
						// お持ち帰りするアイテムがあった.
						this.player.controll.cmdItemQueryPick(itemNegi.name, true, true);
						isPickingup = true;
					}
				}

				if (this.player.controll.item_carrier.isCarrying() && isPickingup == false) {
					ItemController item = this.player.controll.item_carrier.getItem();
					if (item.isExportable() == false) {

						// 持ち出せないものは置いていく.
						QueryItemDrop	query_drop = this.player.controll.cmdItemQueryDrop();

						query_drop.is_drop_done = true;

						this.player.controll.cmdItemDrop(this.player.controll.account_name);
					}
				}
			}
			break;

			// 白菜がよこに動く＆タライが登場.
			case STEP.OPEN_DOOR:
			{
				if(this.hakusai_fcurve.isDone() && this.tarai_fcurve.isDone()) {

					this.step.set_next(STEP.RIDE_TARAI_0);
				}
			}
			break;

			// タライにのるタライにのる（岸まで歩く）.
			case STEP.RIDE_TARAI_0:
			{
				if(!this.player_move.isMoving()) {

					this.step.set_next(STEP.RIDE_TARAI_1);
				}
			}
			break;

			// タライにのる（タライに向かってジャンプ）.
			case STEP.RIDE_TARAI_1:
			{
				if(!this.player_jump.isMoving()) {

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
					this.tarai_fune.transform.position = this.tarai_enter_spline.curve.cvs.front().position;
				}
				break;

				// 白菜がよこに動く＆タライが登場.
				case STEP.OPEN_DOOR:
				{
					this.hakusai_set.setControl(true);
					this.hakusai_fcurve.setSlopeAngle(10.0f, 10.0f);
					this.hakusai_fcurve.setDuration(4.0f);
					this.hakusai_fcurve.start();
					this.hakusai_tracer.restart();

					this.tarai_fcurve.setSlopeAngle(60.0f, 10.0f);
					this.tarai_fcurve.setDuration(2.0f);
					this.tarai_fcurve.setDelay(2.0f);
					this.tarai_fcurve.start();
				}
				break;

				// タライにのる（岸まで歩く）.
				case STEP.RIDE_TARAI_0:
				{
					this.player_move.position.start = this.player.controll.getPosition();
					this.player_move.position.goal  = this.get_locator_position("chr_loc_0");
					this.player_move.startConstantVelocity(chrBehaviorLocal.MOVE_SPEED);
				}
				break;

				// タライにのる（タライに向かってジャンプ）.
				case STEP.RIDE_TARAI_1:
				{
					Vector3		start = this.player.controll.getPosition();
					Vector3		goal  = this.tarai_enter_spline.curve.cvs.back().position;
				
					this.player_jump.start(start, goal, 1.0f);
				}
				break;

				// 白菜が戻る＆タライが外に向かって移動.
				case STEP.CLOSE_DOOR:
				{
					this.hakusai_fcurve.setSlopeAngle(10.0f, 10.0f);
					this.hakusai_fcurve.setDuration(4.0f);
					this.hakusai_fcurve.start();
					this.hakusai_tracer.restart();
					this.hakusai_tracer.setCurrentByDistance(this.hakusai_tracer.curve.calcTotalDistance());

					this.tarai_tracer.attach(this.tarai_leave_spline.curve);
					this.tarai_tracer.restart();
					this.tarai_fcurve.reset();
					this.tarai_fcurve.setSlopeAngle(20.0f, 60.0f);
					this.tarai_fcurve.setDuration(2.0f);
					this.tarai_fcurve.start();
				}
				break;

				case STEP.END:
				{
					// イベント終了.
					this.hakusai_set.reset();
					this.hakusai_set.setControl(false);

					if(this.is_local_player) {

						if(this.is_map_change) {

							GlobalParam.get().skip_enter_event = false;
							GlobalParam.get().fadein_start     = false;

							if(GlobalParam.get().is_in_my_home) {

								GameRoot.get().step.set_next(GameRoot.STEP.VISIT);

							} else {

								GameRoot.get().step.set_next(GameRoot.STEP.GO_HOME);
							}

							// 庭移動を通知.
							if (GlobalParam.get().request_move_home) {
								Debug.Log("NotifyFieldMoving Leave END.");
								GameRoot.get().NotifyFieldMoving();
							}
						} else {

							GlobalParam.get().is_in_my_home = !GlobalParam.get().is_in_my_home;
							this.player.controll.cmdSetPosition(this.initial_player_position);
							this.player.endOuterControll();
						}

					} else {

						// アイテムを非アクティブにしてからドロップしないとリスポーンします.

						ItemBehaviorFruit fruit = this.player.controll.getCarriedItem<ItemBehaviorFruit>();

						if(fruit != null) {

							fruit.activeItem(false);
						}

						// マップ移動がないときにアイテムを持ち帰った場合にアイテムのGameObjectを.
						// 破棄しないようにするためにアイテムを捨てます.
						this.player.controll.cmdItemDrop(this.player.name);

						CharacterRoot.get().deletaCharacter(this.player.controll);
					}

					this.player = null;
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 白菜がよこに動く＆タライが登場.
			case STEP.OPEN_DOOR:
			{
				this.hakusai_fcurve.execute(Time.deltaTime);
				this.hakusai_tracer.proceedToDistance(this.hakusai_tracer.curve.calcTotalDistance()*this.hakusai_fcurve.getValue());
				this.hakusai_set.setPosition(this.hakusai_tracer.getCurrent().position);

				this.tarai_fcurve.execute(Time.deltaTime);
				this.tarai_tracer.proceedToDistance(this.tarai_tracer.curve.calcTotalDistance()*this.tarai_fcurve.getValue());
				this.tarai_fune.setPosition(this.tarai_tracer.getCurrent().position);

				if(this.tarai_fcurve.isTriggerDone()) {
				
					this.ripple_effect.is_created = false;
				}
			}
			break;

			// タライにのるタライにのる（岸まで歩く）.
			case STEP.RIDE_TARAI_0:
			{
				this.player_move.execute(Time.deltaTime);
				this.player.controll.cmdSetPosition(this.player_move.position.current);
				this.player.controll.cmdSmoothHeadingTo(this.player_move.position.goal);
				this.player.playWalkMotion();
			}
			break;

			// タライにのる（タライに向かってジャンプ）.
			case STEP.RIDE_TARAI_1:
			{
				this.player_jump.execute(Time.deltaTime);
				this.player.controll.cmdSetPosition(this.player_jump.position);
				this.player.controll.cmdSmoothHeadingTo(this.player_jump.goal);
				this.player.stopWalkMotion();

				if(this.player_jump.is_trigger_bounce && this.player_jump.bounce_count == 1) {

					this.ripple_effect.is_created = false;
				}
			}
			break;

			// 白菜が戻る＆タライが外に向かって移動.
			case STEP.CLOSE_DOOR:
			{
				this.hakusai_fcurve.execute(Time.deltaTime);
				this.hakusai_tracer.proceedToDistance(this.hakusai_tracer.curve.calcTotalDistance()*(1.0f - this.hakusai_fcurve.getValue()));
				this.hakusai_set.setPosition(this.hakusai_tracer.cv.position);

				this.tarai_fcurve.execute(Time.deltaTime);
				this.tarai_tracer.proceedToDistance(this.tarai_tracer.curve.calcTotalDistance()*this.tarai_fcurve.getValue());
		
				SimpleSpline.ControlVertex	cv = this.tarai_tracer.getCurrent();
	
				this.tarai_fune.setPosition(cv.position);
				this.player.controll.cmdSetPosition(cv.position);
				this.player.controll.cmdSmoothHeadingTo(cv.position + cv.tangent.Y(0.0f));
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

	// ローカルプレイヤーが主役？.
	public void		setIsLocalPlayer(bool is_local_player)
	{
		this.is_local_player = is_local_player;
	}

	public void		setIsMapChange(bool is_map_change)
	{
		this.is_map_change = is_map_change;
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
