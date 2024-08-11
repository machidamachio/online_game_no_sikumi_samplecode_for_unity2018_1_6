using UnityEngine;
using System.Collections;
using MathExtension;
using GameObjectExtension;

// 武器選択シーンイベント.
public class EventWeaponSelect : EventBase {

	// ================================================================ //

	public enum STEP {

		NONE = -1,

		IDLE = 0,			// 実行中じゃない.
		START,
		KABUSAN_TOJO,		// かぶさん登場.
		KABUSAN_TALK,		// かぶさんしゃべる.

		END,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	protected chrBehaviorPlayer		player   = null;
	protected chrBehaviorKabu		kabu_san = null;

	// ================================================================ //

	public chrBehaviorKabu	getKabusan()
	{
		return(this.kabu_san);
	}

	public EventWeaponSelect()
	{
	}

	public override void	initialize()
	{
	}

	// イベント開始.
	public override void		start()
	{
		this.player = PartyControl.get().getLocalPlayer();
		this.player.beginOuterControll();

		this.kabu_san = CharacterRoot.get().findCharacter<chrBehaviorKabu>("NPC_Kabu_San");

		CameraControl.get().beginOuterControll();

		this.step.set_next(STEP.START);
	}

	public override void		end()
	{
		this.player.endOuterControll();

		Navi.get().finishKabusanSpeech();

		CameraControl.get().is_smooth_revert = true;
		CameraControl.get().endOuterControll();
	}

	public override void	execute()
	{
		CameraModule	camera_module = CameraControl.get().getModule();

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.IDLE:
			{
				this.step.set_next(STEP.START);
			}
			break;

			case STEP.START:
			{
				this.step.set_next(STEP.KABUSAN_TOJO);
			}
			break;

			case STEP.KABUSAN_TOJO:
			{
				if(this.step.get_time() > 2.0f) {

					this.step.set_next(STEP.KABUSAN_TALK);
				}
			}
			break;

			case STEP.KABUSAN_TALK:
			{
				if(Input.GetMouseButtonUp(0)) {

					this.step.set_next(STEP.END);
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
	
				case STEP.IDLE:
				{
				}
				break;

				case STEP.START:
				{
					camera_module.parallelMoveTo(new Vector3(0.0f, 12.4f, -13.0f));
					this.step.set_next(STEP.KABUSAN_TOJO);
				}
				break;

				case STEP.KABUSAN_TOJO:
				{
				}
				break;

				case STEP.KABUSAN_TALK:
				{
					Navi.get().dispatchKabusanSpeech();
				}
				break;

				case STEP.END:
				{
					this.kabu_san.beginMove();
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
		}

		// ---------------------------------------------------------------- //
	}

	public override void	onGUI()
	{
	}

	// イベントが実行中？.
	public override  bool	isInAction()
	{
		bool	ret = !(this.step.get_current() == STEP.IDLE && this.step.get_next() == STEP.NONE);

		return(ret);
	}
}
