using UnityEngine;
using System.Collections;

// ボスフロアーシーケンス　ボスと戦闘中.
public class BossLevelSequenceBoss : SequenceBase {

	public enum STEP {

		NONE = -1,

		IDLE = 0,
		IN_ACTION,			// ボスと戦闘中.

		FINISH,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	protected chrBehaviorEnemyBoss	boss;

	// ================================================================ //

	// デバッグウインドウ生成時に呼ばれる.
	public override void		createDebugWindow(dbwin.Window window)
	{
		window.createButton("ボス倒す")
			.setOnPress(() =>
			{
				this.boss.causeVanish();
			});
		window.createButton("ボスの敵リスト更新")
			.setOnPress(() =>
			            {
				this.boss.updateTargetPlayers();
			});
	}

	// レベル開始時に呼ばれる.
	public override void		start()
	{
		this.boss = CharacterRoot.get().findCharacter<chrBehaviorEnemyBoss>("Boss1");

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
				if(this.boss == null) {

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
					Navi.get().dispatchYell(YELL_WORD.READY);
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

	public override bool	isFinished()
	{
		return(this.step.get_current() == STEP.FINISH);
	}

}
