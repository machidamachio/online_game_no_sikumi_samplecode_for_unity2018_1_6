using UnityEngine;
using System.Collections;
using GameObjectExtension;

// プレイヤーモデルの色をエフェクト的に変える.
// （頭じんじん、クリームまみれなど）.
public class SkinColorControl {

	public enum STEP {

		NONE = -1,

		IDLE = 0,			// 実行中じゃない.
		JINJIN,				// アイス食べ過ぎで頭痛い状態.
		CREAMY,				// クリームまみれ.
		HEALING,			// 体力回復中.

		END,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	protected chrBehaviorPlayer	player = null;

	// ================================================================ //

	public void		create(chrBehaviorPlayer player)
	{
		this.player = player;
	}

	public void		execute()
	{
		float	healing_time = 2.0f;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.HEALING:
			{
				if(this.step.get_time() >= healing_time) {

					this.step.set_next(STEP.IDLE);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.IDLE:
				{
					this.player.gameObject.setMaterialProperty("_BlendRate", 0.0f);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.HEALING:
			{
				float	h0, h1;
				float	blend;
				float	cycle = 2.0f;

				// レインボーカラー.
				{
					h0 = Mathf.Repeat(this.step.get_time(), cycle);
					h0 = h0/cycle*360.0f;
					h1 = h0 - 10.0f;
				}

				// ノーマル　→　レインボー　→　ノーマル.
				{
					float	t0 = 0.1f*healing_time;
					float	t1 = 0.8f*healing_time;

					blend = Mathf.Repeat(this.step.get_time(), cycle);

					if(blend < t0) {
	
						blend = Mathf.InverseLerp(0.0f, t0, blend);
						blend = Mathf.Sin(Mathf.PI/2.0f*blend);
	
					} else if(blend < t1) {

						blend = 1.0f;

					} else {
	
						blend = Mathf.InverseLerp(t1, healing_time*1.0f, blend);
						blend = Mathf.Lerp(0.0f, Mathf.PI, blend);
						blend = Mathf.Cos(blend);
						blend = Mathf.InverseLerp(-1.0f, 1.0f, blend);
					}
				}

				Color	color0 = DoorMojiControl.HSVToRGB(h0, 1.0f, 1.0f);
				Color	color1 = DoorMojiControl.HSVToRGB(h1, 1.0f, 1.0f);

				this.player.gameObject.setMaterialProperty("_SecondColor",   color0);
				this.player.gameObject.setMaterialProperty("_ThirdColor",    color1);
				this.player.gameObject.setMaterialProperty("_BlendRate",     blend);
				this.player.gameObject.setMaterialProperty("_MaskAffection", 0.0f);
			}
			break;

			case STEP.CREAMY:
			{
				this.player.gameObject.setMaterialProperty("_SecondColor",   Color.white);
				this.player.gameObject.setMaterialProperty("_ThirdColor",   Color.white);
				this.player.gameObject.setMaterialProperty("_BlendRate",     0.9f);
				this.player.gameObject.setMaterialProperty("_MaskAffection", 1.0f);
			}
			break;

			case STEP.JINJIN:
			{
				float	cycle = 1.0f;
				float	rate  = ipCell.get().setInput(this.step.get_time())
									.repeat(cycle).normalize().uradian().sin().lerp(0.2f, 0.5f).getCurrent();

				this.player.gameObject.setMaterialProperty("_SecondColor",   Color.blue);
				this.player.gameObject.setMaterialProperty("_ThirdColor",    Color.blue);
				this.player.gameObject.setMaterialProperty("_BlendRate",     rate);
				this.player.gameObject.setMaterialProperty("_MaskAffection", 0.0f);
			}
			break;
		}
	}

	// ---------------------------------------------------------------- //

	// 頭じんじん状態（アイス食べ過ぎ）を始める.
	public void		startJinJin()
	{
		this.step.set_next(STEP.JINJIN);
	}

	// 頭じんじん状態（アイス食べ過ぎ）を終わる.
	public void		stopJinJin()
	{
		this.step.set_next(STEP.IDLE);
	}

	// ---------------------------------------------------------------- //

	// クリームまみれを始める.
	public void		startCreamy()
	{
		this.step.set_next(STEP.CREAMY);
	}

	// クリームまみれを終わる.
	public void		stopCreamy()
	{
		this.step.set_next(STEP.IDLE);
	}

	// クリームまみれ中？.
	public bool		isNowCreamy()
	{
		return(this.step.get_current() == STEP.CREAMY);
	}

	// ---------------------------------------------------------------- //

	// 体力回復中を始める.
	public void		startHealing()
	{
		this.step.set_next(STEP.HEALING);
	}

	// 体力回復中を終わる.
	public void		stopHealing()
	{
		this.step.set_next(STEP.IDLE);
	}

	// 体力回復中？.
	public bool		isNowHealing()
	{
		return(this.step.get_current() == STEP.HEALING);
	}
}
