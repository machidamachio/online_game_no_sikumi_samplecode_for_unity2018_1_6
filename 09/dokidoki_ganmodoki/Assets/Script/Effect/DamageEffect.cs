using UnityEngine;
using System.Collections;

// ダメージをくらったときのエフェクト（白くフラッシュ）.
public class DamageEffect {

	public static float	DAMAGE_FLUSH_TIME = 0.1f;		// [sec] ダメージを受けたときに、白くフラッシュする時間.

	public static float	VANISH_TIME = 2.0f;

	protected Renderer[]	renders = null;				// レンダー.
	protected Material[]	org_materials = null;		// もともとのモデルにアサインされていたマテリアル.

	protected float			fade_duration = 1.0f;		// [sec] フェードイン/アウトの長さ.

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		IDLE = 0,			// 通常時.
		DAMAGE,				// ダメージ　白くフラッシュ.
		VANISH,				// やられた　アルファーでフェードアウト.

		FADE_OUT,			// フェードアウト（一時的に非表示にしたいとき用）.
		FADE_IN,			// フェードイン（一時的に非表示にしたいとき用）.

		VACANT,				// フェードが終わって削除まち中.
		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //

	public DamageEffect(GameObject go)
	{
		this.renders = go.GetComponentsInChildren<Renderer>();
	
		this.org_materials = new Material[renders.Length];

		for(int i = 0;i < this.renders.Length;i++) {

			this.org_materials[i] = this.renders[i].material;
		}

		//

		this.step.set_next(STEP.IDLE);
	}

	public void		execute()
	{

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.


		switch(this.step.do_transition()) {

			// ダメージ　白くフラッシュ.
			case STEP.DAMAGE:
			{
				if(this.step.get_time() >= DAMAGE_FLUSH_TIME) {

					this.step.set_next(STEP.IDLE);
				}
			}
			break;

			// フェードが終わって削除まち中.
			case STEP.VANISH:
			{
				if(this.step.get_time() >= VANISH_TIME) {
					
					this.step.set_next(STEP.VACANT);
				}
			}
			break;

			// フェードイン.
			case STEP.FADE_IN:
			{
				if(this.step.get_time() >= this.fade_duration) {
					
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
					// マテリアルをもとに戻す.
					for(int i = 0;i < this.renders.Length;i++) {
			
						this.renders[i].material = this.org_materials[i];
					}
				}
				break;

				// ダメージ　白くフラッシュ.
				case STEP.DAMAGE:
				{
					for(int i = 0;i < renders.Length;i++) {
			
						this.renders[i].material = CharacterRoot.getInstance().damage_material;
					}
				}
				break;

				// やられた.
				case STEP.VANISH:
				{
					// フェードアウト用のマテリアルに入れ替える.
					for(int i = 0;i < renders.Length;i++) {
			
						this.renders[i].material = CharacterRoot.getInstance().vanish_material;
					}
				}
				break;

				case STEP.FADE_IN:
				case STEP.FADE_OUT:
				{
					// フェード用のマテリアルに入れ替える.
					for(int i = 0;i < renders.Length;i++) {

						// テクスチャーをもとのマテリアルからコピーする.
						Texture		texture = this.renders[i].material.GetTexture(0);

						this.renders[i].material = CharacterRoot.getInstance().vanish_material;

						this.renders[i].material.SetTexture(0, texture);
					}
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// やられた.
			case STEP.VANISH:
			{
				float	rate = this.step.get_time()/VANISH_TIME;
	
				rate = Mathf.Pow(rate, 1.0f/2.0f);
		
				float	alpha = Mathf.Max(0.0f, 1.0f - rate);
		
				for(int i = 0;i < this.renders.Length;i++) {
		
					this.renders[i].material.color = new Color(1.0f, 1.0f, 1.0f, alpha);
				}
			}
			break;

			// フェードアウト.
			case STEP.FADE_IN:
			case STEP.FADE_OUT:
			{
				float	rate = this.step.get_time()/this.fade_duration;
	
				rate = Mathf.Pow(rate, 1.0f/2.0f);
		
				float	alpha;

				if(this.step.get_current() == STEP.FADE_IN) {

					alpha = Mathf.Min(rate, 1.0f);

				} else {

					alpha = Mathf.Max(0.0f, 1.0f - rate);
				}

				for(int i = 0;i < this.renders.Length;i++) {

					Color	color = this.renders[i].material.color;

					color.a = alpha;

					this.renders[i].material.color = color;
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

	// ================================================================ //

	// ダメージエフェクトをスタートする.
	public void		startDamage()
	{
		if(this.step.get_current() != STEP.VANISH) {

			this.step.set_next(STEP.DAMAGE);
		}
	}

	// やられエフェクトをスタートする.
	public void		startVanish()
	{
		this.step.set_next(STEP.VANISH);
	}

	// フェードインをスタートする.
	public void		startFadeIn(float duration)
	{
		this.fade_duration = duration;

		this.step.set_next(STEP.FADE_IN);
	}

	// フェードアウトをスタートする.
	public void		startFadeOut(float duration)
	{
		this.fade_duration = duration;

		this.step.set_next(STEP.FADE_OUT);
	}

	// 演出中？.
	public bool		isDone()
	{
		bool	is_done = (this.step.get_current() == STEP.IDLE);

		return(is_done);
	}

	// 消失演出が終わった？.
	public bool		isVacant()
	{
		bool	is_done = (this.step.get_current() == STEP.VACANT);

		return(is_done);
	}

}
