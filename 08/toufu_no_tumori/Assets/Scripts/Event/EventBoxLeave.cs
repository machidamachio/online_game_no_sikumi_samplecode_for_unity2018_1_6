using UnityEngine;
using System.Collections;

public class EventBoxLeave : MonoBehaviour {

	public enum STEP {

		NONE = -1,

		IDLE = 0,
		WAIT_ENTER,				// 実行中じゃない.
		ENTERED,
		WAIT_LEAVE,
		LEAVE,

		NUM,
	};
	protected Step<STEP>	step = new Step<STEP>(STEP.NONE);

	protected chrBehaviorLocal	player = null;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.step.set_next(STEP.WAIT_ENTER);
	}
	
	void	Update()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.WAIT_ENTER:
			{
				if(this.player != null) {

					this.step.set_next(STEP.ENTERED);
				}
			}
			break;

			case STEP.ENTERED:
			{
				if(YesNoAskDialog.get().isSelected()) {

					if(YesNoAskDialog.get().getSelection() == YesNoAskDialog.SELECTION.YES) {

						LeaveEvent	leave_event = EventRoot.get().startEvent<LeaveEvent>();

						if(leave_event != null) {
			
							leave_event.setPrincipal(this.player);
							leave_event.setIsLocalPlayer(true);

							// 庭の移動のリクエスト発行.
							if (GameRoot.get().net_player)
							{
								GameRoot.get().NotifyFieldMoving();
								GlobalParam.get().request_move_home = false;
							}
							else {
								GlobalParam.get().request_move_home = true;
							}

						}

						this.step.set_next(STEP.IDLE);

					} else {

						this.step.set_next(STEP.WAIT_LEAVE);
					}

				} else if(this.player == null) {

					this.step.set_next(STEP.LEAVE);
				}
			}
			break;

			case STEP.WAIT_LEAVE:
			{
				if(this.player == null) {

					this.step.set_next(STEP.LEAVE);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.WAIT_ENTER:
				case STEP.IDLE:
				{
					this.player = null;

					YesNoAskDialog.get().close();
				}
				break;

				case STEP.ENTERED:
				{
					if(GlobalParam.get().is_in_my_home) {

						YesNoAskDialog.get().setText("友だちのところへ遊びにいく？");
						YesNoAskDialog.get().setButtonText("いく", "いかない");

					} else {

						YesNoAskDialog.get().setText("お家にかえる？");
						YesNoAskDialog.get().setButtonText("かえる", "まだあそぶ");
					}
					YesNoAskDialog.get().dispatch();
				}
				break;

				case STEP.WAIT_LEAVE:
				{
					YesNoAskDialog.get().close();
				}
				break;

				case STEP.LEAVE:
				{
					YesNoAskDialog.get().close();

					this.step.set_next(STEP.WAIT_ENTER);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.WAIT_ENTER:
			{
			}
			break;
		}
	}

	void	OnTriggerEnter(Collider other)
	{
		do {

			var		player = other.gameObject.GetComponent<chrBehaviorLocal>();

			if(player == null) {

				break;
			}

			// おひっこし中はマップ移動できない.
			if(player.isNowHouseMoving()) {

				break;
			}

			// 通信相手と接続するまで移動できない.
			if(GameRoot.get().isConnected() == false) {

				break;
			}

			this.player = player;

		} while(false);
	}

	void	OnTriggerExit(Collider other)
	{
		do {

			var		player = other.gameObject.GetComponent<chrBehaviorLocal>();

			if(player == null) {

				break;
			}
			if(player != this.player) {

				break;
			}

			this.player = null;

		} while(false);
	}

	// ================================================================ //

	public void		activate()
	{
		this.step.set_next(STEP.WAIT_ENTER);
	}

	public void		deactivate()
	{
		this.step.set_next(STEP.IDLE);
	}
}
