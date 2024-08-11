using UnityEngine;
using System.Collections;

public class BossLevelSequence : SequenceBase {

	public enum STEP {

		NONE = -1,

		IDLE = 0,
		VS_BOSS,			// ボスと戦闘中.
		CAKE_BIKING,		// ケーキバイキング中.
		RESULT,				// リザルト.
		FINISH,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	//===================================================================

	dbwin.Window	window = null;

	// デバッグウインドウ生成時に呼ばれる.
	public override void		createDebugWindow(dbwin.Window window)
	{
		this.window = window;

		/*window.createButton("つぎ")
			.setOnPress(() =>
			{
				switch(this.step.get_current()) {

					case STEP.VS_BOSS:
					{
						this.boss.causeVanish();
					}
					break;

					case STEP.CAKE_BIKING:
					{
						this.is_cake_time_over = true;
					}
					break;

					case STEP.RESULT:
					{
						this.is_result_done = true;
					}
					break;
				}
			});*/
	}

	protected bool	is_result_done = false;			// テスト用　リザルトおしまい？.

	// レベル開始時に呼ばれる.
	public override void		start()
	{
		this.step.set_next(STEP.VS_BOSS);
	}

	// ケーキバイキング（ボス戦のあとのおまけ）中？.
	public bool	isNowCakeBiking()
	{
		bool	ret = false;

		if(this.child != null) {

			ret = this.child is BossLevelSequenceCake;
		}

		return(ret);
	}

	// 毎フレーム呼ばれる.
	public override void		execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.VS_BOSS:
			{
				if(this.child.isFinished()) {

					GameObject.Destroy(this.child);
					this.child = null;

					this.step.set_next(STEP.CAKE_BIKING);
				}
			}
			break;

			case STEP.CAKE_BIKING:
			{
				if(this.child.isFinished()) {

					GameObject.Destroy(this.child);
					this.child = null;

					this.step.set_next(STEP.RESULT);
				}
			}
			break;

			case STEP.RESULT:
			{
				if(this.child.isFinished()) {

					GameObject.Destroy(this.child);
					this.child = null;

					this.step.set_next(STEP.FINISH);
				}
			}
			break;
		}
				
		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.VS_BOSS:
				{
					this.child = this.gameObject.AddComponent<BossLevelSequenceBoss>();
					this.child.parent = this;

					this.child.createDebugWindow(this.window);

					this.child.start();

					Navi.get().dispatchPlayerMarker();
				}
				break;

				case STEP.CAKE_BIKING:
				{
					this.child = this.gameObject.AddComponent<BossLevelSequenceCake>();
					this.child.parent = this;

					this.child.createDebugWindow(this.window);

					this.child.start();
				}
				break;

				case STEP.RESULT:
				{
					this.child = this.gameObject.AddComponent<BossLevelSequenceResult>();
					this.child.parent = this;

					this.child.createDebugWindow(this.window);

					this.child.start();
				}
				break;

				case STEP.FINISH:
				{
					// Networkクラスのコンポーネントを取得.
					GameObject	obj = GameObject.Find("Network");
					Network		network = null;	

					if(obj != null) {
					
						network = obj.GetComponent<Network>();
					}
	
					if (network != null) {

					if (GameRoot.get().isHost()) {
							network.StopGameServer();
						}
						
						network.StopServer();
						network.Disconnect();
						GameObject.Destroy(network);
					}

					GameRoot.get().setNextScene("TitleScene");
				}
				break;

			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.VS_BOSS:
			{
				this.child.execute();
			}
			break;

			case STEP.CAKE_BIKING:
			{
				this.child.execute();
			}
			break;

			case STEP.RESULT:
			{
				this.child.execute();
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

}
