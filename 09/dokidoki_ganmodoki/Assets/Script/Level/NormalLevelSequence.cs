using UnityEngine;
using System.Collections;

public class NormalLevelSequence : SequenceBase {

	public enum STEP {

		NONE = -1,

		IDLE = 0,
		IN_ACTION,
		TRANSPORT,		// ルーム移動イベント中.
		READY,			// 『レディ！』の表示.
		RESTART,		// ばたんきゅーになったあとのリスタート.

		WAIT_FINISH,
		FINISH,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	//===================================================================

	// デバッグウインドウ生成時に呼ばれる.
	public override void		createDebugWindow(dbwin.Window window)
	{
		window.createButton("次のフロアーへ")
			.setOnPress(() =>
			{
				switch(this.step.get_current()) {

					case STEP.IN_ACTION:
					{
						this.step.set_next(STEP.FINISH);
					}
					break;
				}
			});
	}

	
	protected bool		is_floor_door_event = false;		// ルーム移動イベントのドアが、フロアー移動ドア？.
	protected bool		is_first_ready = true;				// レベルを開始して最初の STEP.READY

	// ばたんきゅー後のリスタート位置.
	protected class RestartPoint {

		public Vector3		position  = Vector3.zero;
		public float		direction = 0.0f;
		public DoorControl	door      = null;
	}

	protected RestartPoint	restart_point = new RestartPoint();

	protected float			wait_counter = 0.0f;

	//===================================================================

	// レベル開始時に呼ばれる.
	public override void		start()
	{
		this.step.set_next(STEP.READY);
	}

	// 毎フレーム呼ばれる.
	public override void		execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.IN_ACTION:
			{
				var		player = PartyControl.get().getLocalPlayer();

				if(player.step.get_current() == chrBehaviorLocal.STEP.WAIT_RESTART) {

					// 体力が０になったらリスタート.

					player.start();
					player.control.cmdSetPositionAnon(this.restart_point.position);
					player.control.cmdSetDirectionAnon(this.restart_point.direction);

					if(this.restart_point.door != null) {

						this.restart_point.door.beginWaitLeave();
					}

					this.step.set_next(STEP.RESTART);

				} else {

					// ルーム移動イベントが始まった？.
	
					var	ev = EventRoot.get().getCurrentEvent<TransportEvent>();
	
					if(ev != null) {
	
						DoorControl	door = ev.getDoor();
	
						if(door.type == DoorControl.TYPE.FLOOR) {
	
							// フロアー移動ドアのとき.

							this.is_floor_door_event = true;
	
							ev.setEndAtHoleIn(true);
	
						} else {

							// ルーム移動ドアのとき.
							this.restart_point.door = door.connect_to;

							this.is_floor_door_event = false;
						}
	
						this.step.set_next(STEP.TRANSPORT);
					}
				}
			}
			break;

			// ルーム移動イベント中.
			case STEP.TRANSPORT:
			{
				// ルーム移動イベントが終わったら、通常モードへ.

				var		ev = EventRoot.get().getCurrentEvent<TransportEvent>();

				wait_counter += Time.deltaTime;

				if(ev == null) {

					if(this.is_floor_door_event) {

						this.step.set_next(STEP.WAIT_FINISH);

					} else {

						this.step.set_next(STEP.READY);

						wait_counter = 0.0f;
					}

				} else {

					if(ev.step.get_current() == TransportEvent.STEP.READY) {

						this.step.set_next(STEP.READY);

						wait_counter = 0.0f;
					}
				}

			}
			break;

			// 『レディ！』の表示.
			// ばたんきゅーになったあとのリスタート.
			case STEP.READY:
			case STEP.RESTART:
			{
				if(this.is_first_ready) {

					if(this.step.get_time() > 1.0f) {

						this.step.set_next(STEP.IN_ACTION);
						this.is_first_ready = false;
					}

				} else {

					var		ev = EventRoot.get().getCurrentEvent<TransportEvent>();
	
					if(ev == null) {
	
						this.step.set_next(STEP.IN_ACTION);
					}
				}
			}
			break;

			case STEP.WAIT_FINISH:
			{
				// 若干待ちをいれます.
				wait_counter += Time.deltaTime;
				if (wait_counter > 3.0f) {
					this.step.set_next(STEP.FINISH);
				}
			}
			break;
		}
				
		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.


		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.IN_ACTION:
				{
					Navi.get().dispatchPlayerMarker();

					LevelControl.get().endStillEnemies(null, 0.0f);
				}
				break;

				// ルーム移動イベント中.
				case STEP.TRANSPORT:
				{
					var current_room = PartyControl.get().getCurrentRoom();
					var next_room    = PartyControl.get().getNextRoom();

					// 次のルームに敵を作る.
					// ルーム移動イベント終了時にいきなり敵があらわれないよう、
					// 早めに作っておく.
					LevelControl.get().createRoomEnemies(next_room);

					LevelControl.get().beginStillEnemies(next_room);
					LevelControl.get().beginStillEnemies(current_room);
				}
				break;

				// 『レディ！』の表示.
				case STEP.READY:
				{
					var current_room = PartyControl.get().getCurrentRoom();
					var next_room    = PartyControl.get().getNextRoom();

					if(this.is_first_ready) {

						LevelControl.get().createRoomEnemies(current_room);
						LevelControl.get().beginStillEnemies(current_room);
						LevelControl.get().onEnterRoom(current_room);

					} else {

						LevelControl.get().onLeaveRoom(current_room);
						LevelControl.get().onEnterRoom(next_room);
					}

					// 『レディ!』の表示.
					Navi.get().dispatchYell(YELL_WORD.READY);

					// 前にいたルームの敵を削除.
					if(next_room != current_room) {

						LevelControl.get().deleteRoomEnemies(current_room);
					}

					ItemWindow.get().onRoomChanged(next_room);

					this.restart_point.position  = PartyControl.get().getLocalPlayer().control.getPosition();
					this.restart_point.direction = PartyControl.get().getLocalPlayer().control.getDirection();
				}
				break;

				// ばたんきゅーになったあとのリスタート.
				case STEP.RESTART:
				{
					// 『レディ!』の表示.
					Navi.get().dispatchYell(YELL_WORD.READY);

					this.restart_point.position  = PartyControl.get().getLocalPlayer().control.getPosition();
					this.restart_point.direction = PartyControl.get().getLocalPlayer().control.getDirection();
				}
				break;

				case STEP.FINISH:
				{
					GameRoot.get().setNextScene("BossScene");
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.


		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.IN_ACTION:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

}
