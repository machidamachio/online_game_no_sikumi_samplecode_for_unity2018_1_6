using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossLevelSequenceCake : SequenceBase {

	public enum STEP {

		NONE = -1,

		IDLE = 0,
		IN_ACTION,
		FINISH,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	protected float		time_limit = 30.0f;		// [sec] 制限時間（仮）.

	// ================================================================ //

	// デバッグウインドウ生成時に呼ばれる.
	public override void		createDebugWindow(dbwin.Window window)
	{
		window.createButton("ケーキタイム終わり")
			.setOnPress(() =>
			{
				this.step.do_execution(this.time_limit);
			});
	}

	// レベル開始時に呼ばれる.
	public override void		start()
	{
		this.step.set_next(STEP.IN_ACTION);
	}

	// 毎フレーム呼ばれる.
	public override void		execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.IN_ACTION:
			{
				// タイムオーバー.
				if(this.step.get_time() > this.time_limit) {

					Navi.get().dispatchYell(YELL_WORD.TIMEUP);

					CakeTrolley.get().stopServe();

					// 残っているケーキを全部消す.
					CakeTrolley.get().deleteAllCakes();

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
					// ショットは撃てなくする.

					var	players = PartyControl.get().getPlayers();

					foreach(var player in players) {

						player.setShotEnable(false);
					}

					Navi.get().dispatchYell(YELL_WORD.OYATU);
					Navi.get().createCakeTimer();

					CakeTrolley.get().startServe();
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		float	current_time = 1.0f;

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.IN_ACTION:
			{
				current_time = this.step.get_time()/this.time_limit;
			}
			break;
		}

		Navi.get().getCakeTimer().setTime(current_time);

		//dbPrint.setLocate(20, 15);
		//dbPrint.print("のこり時間 ." + Mathf.FloorToInt(this.time_limit - this.step.get_time()).ToString());

		//dbPrint.setLocate(20, 20);
		//dbPrint.print("とった数（デバッグ用）. " + PartyControl.get().getLocalPlayer().getCakeCount().ToString());

		// ---------------------------------------------------------------- //
	}

	public override bool	isFinished()
	{
		return(this.step.get_current() == STEP.FINISH);
	}
}
