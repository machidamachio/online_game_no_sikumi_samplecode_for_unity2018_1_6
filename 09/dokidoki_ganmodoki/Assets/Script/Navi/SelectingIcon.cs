using UnityEngine;
using System.Collections;

// 武器選択中アイコン.
public class SelectingIcon : MonoBehaviour {

	// テクスチャー.
	public Texture	uun_texture;			// キャラ　考え中.
	public Texture	hai_texture;			// キャラ　決まり！.

	public Texture	moya_negi_texture;		// もやもや　ネギ.
	public Texture	moya_yuzu_texture;		// もやもや　ゆず.
	public Texture	moya_oke_texture;		// もやもや　おけ.
	public Texture	moya_kara_texture;		// もやもや　からっぽ.

	public int		player_index;			// プレイヤーの、account_global_index.

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		UUN = 0,		// 考え中.
		HAI,			// 『決めた！』アクション.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	protected Sprite2DControl	root_sprite;	
	protected Sprite2DControl	chr_sprite;		// キャラのスプライト.
	
	protected WeaponSelectNavi.Moya		moya;	// もやもや（吹き出し）.

	protected float		timer = 0.0f;

	protected ipModule.Spring	ip_spring = new ipModule.Spring();

	public Vector3	position = Vector3.zero;
	public bool		is_flip = false;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
	}
	void	Start()
	{
	}

	void	Update()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.UUN:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 『決めた！』アクション
				case STEP.HAI:
				{
					this.chr_sprite.setTexture(this.hai_texture);

					this.ip_spring.k      = 750.0f - 400.0f;
					this.ip_spring.reduce = 0.77f;
					this.ip_spring.start(-50.0f);

					this.moya.beginHai();
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 『決めた！』アクション
			case STEP.HAI:
			{
				this.ip_spring.execute(Time.deltaTime);

				Vector3		chr_position = this.position + Vector3.up*(this.ip_spring.position + 50.0f);

				this.root_sprite.setPosition(chr_position);

				this.moya.setPosition(new Vector3(20.0f, 50.0f, 0.0f));
			}
			break;
		}

		// ---------------------------------------------------------------- //

		if(this.moya != null) {

			this.moya.execute();
		}

		this.timer += Time.deltaTime;
	}

	// ================================================================ //

	// 生成する.
	public void		create()
	{
		this.root_sprite = Sprite2DRoot.get().createNull();

		// キャラ.
		this.chr_sprite = Sprite2DRoot.get().createSprite(this.uun_texture, true);
		this.chr_sprite.setSize(new Vector2(this.uun_texture.width, this.uun_texture.height)/4.0f);
		this.chr_sprite.transform.parent = this.root_sprite.transform;

		// もやもや.
		this.moya = new WeaponSelectNavi.Moya();
		this.moya.root_sprite    = this.root_sprite;
		this.moya.negi_texture   = this.moya_negi_texture;
		this.moya.yuzu_texture   = this.moya_yuzu_texture;
		this.moya.oke_texture    = this.moya_oke_texture;
		this.moya.kara_texture   = this.moya_kara_texture;
		this.moya.selecting_icon = this;
		this.moya.create();
		this.moya.setPosition(new Vector3(20.0f, 50.0f, 0.0f));

		//

		this.setPosition(Vector3.zero);
	}

	// 位置をセットする.
	public void		setPosition(Vector3 position)
	{
		this.position = position;
		this.root_sprite.setPosition(this.position);
	}

	// 表示/非表示をセットする.
	public void		setVisible(bool is_visible)
	{
		this.root_sprite.gameObject.SetActive(is_visible);
	}

	// 左右反転する/しないをセットする.
	public void		setFlip(bool is_flip)
	{
		this.is_flip = is_flip;

		if(this.is_flip) {

			this.root_sprite.setScale(new Vector2(-1.0f, 1.0f));

		} else {

			this.root_sprite.setScale(new Vector2( 1.0f, 1.0f));
		}
	}

	//『決めた！』アクションを開始する.
	public void		beginHai()
	{
		this.step.set_next(STEP.HAI);
	}

}

namespace WeaponSelectNavi {

// 頭の上の、もやもや.
public class Moya {

	public enum STEP {

		NONE = -1,

		UUN = 0,		// 考え中.
		HAI,			// 『決めた！』アクション.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	public SelectingIcon	selecting_icon;

	// テクスチャー.
	public Texture		negi_texture;		// もやもや　ネギ.
	public Texture		yuzu_texture;		// もやもや　ゆず.
	public Texture		oke_texture;		// もやもや　おけ.
	public Texture		kara_texture;		// もやもや　からっぽ.

	public    Sprite2DControl	root_sprite;
	protected Sprite2DControl	negi_sprite;
	protected Sprite2DControl	yuzu_sprite;
	protected Sprite2DControl[]	mini_moya_sprite;	// しっぽ（小さなもやもや×２）.

	protected Vector3	moya_position;
	protected Vector3	moya_position_center;		// 大きなもやもやの動きの中心.
	protected Vector3	moya_root_position;			// しっぽがキャラにくっつくところの位置.

	protected float			timer = 0.0f;

	protected const float	MOYA_CYCLE = 2.3f;			// [sec] もやもやの周期.

	// 『決めた！』アクション中の情報.
	protected struct StepHai {

		public Vector3	moya_offset;
	}
	protected StepHai	step_hai;

	// ================================================================ //

	public void		create()
	{
		// もやもや（しっぽ）.

		this.mini_moya_sprite = new Sprite2DControl[2];

		this.mini_moya_sprite[0] =  Sprite2DRoot.get().createSprite(this.kara_texture, true);
		this.mini_moya_sprite[0].setSize(new Vector2(this.kara_texture.width, this.kara_texture.height)*0.05f);
		this.mini_moya_sprite[0].transform.parent = this.root_sprite.transform;

		this.mini_moya_sprite[1] =  Sprite2DRoot.get().createSprite(this.kara_texture, true);
		this.mini_moya_sprite[1].setSize(new Vector2(this.kara_texture.width, this.kara_texture.height)*0.1f);
		this.mini_moya_sprite[1].transform.parent = this.root_sprite.transform;

		// もやもや　ネギ/ゆず.
		this.negi_sprite = Sprite2DRoot.get().createSprite(this.negi_texture, true);
		this.negi_sprite.setSize(new Vector2(this.negi_texture.width, this.negi_texture.height)/4.0f);
		this.negi_sprite.transform.parent = this.root_sprite.transform;

		this.yuzu_sprite = Sprite2DRoot.get().createSprite(this.yuzu_texture, true);
		this.yuzu_sprite.setSize(new Vector2(this.yuzu_texture.width, this.yuzu_texture.height)/4.0f);
		this.yuzu_sprite.transform.parent = this.root_sprite.transform;

		//

		this.step.set_next(STEP.UUN);
	}

	public void		destroy()
	{
		foreach(var sprite in mini_moya_sprite) {

			sprite.destroy();
		}

		this.negi_sprite.destroy();
		this.yuzu_sprite.destroy();
	}

	public void		execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.UUN:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				// 『決めた！』アクション
				case STEP.HAI:
				{
					foreach(var sprite in mini_moya_sprite) {

						sprite.setVisible(false);
					}

					this.step_hai.moya_offset = this.moya_position - this.moya_root_position;
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.UUN:
			{
				this.exec_step_uun();
			}
			break;

			// 『決めた！』アクション
			case STEP.HAI:
			{
				this.exec_step_hai();
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

	// 位置をセットする.
	public void		setPosition(Vector3 position)
	{
		this.moya_root_position   = position;
		this.moya_position_center = this.moya_root_position + new Vector3(32.0f, 60.0f, 0.0f);
	}

	// 決まった後のアクションを開始する.
	public void		beginHai()
	{
		this.step.set_next(STEP.HAI);
	}

	// ---------------------------------------------------------------- //

	protected void		exec_step_uun()
	{
		// 一番上の大きなもやもや.

		this.moya_position = this.calc_moya_position(this.timer);
		this.negi_sprite.setPosition(this.moya_position);
		this.yuzu_sprite.setPosition(this.moya_position);

		// ねぎ/ゆずを交互に表示.

		float	cycle = 4.0f;
		float	time0 = 1.5f;
		float	time1 = 2.0f;
		float	time2 = 3.5f;
		float	time3 = 4.0f;

		float	rate = Mathf.Repeat(this.timer, cycle);

		float	alpha = 1.0f;

		if(rate < time0) {

			alpha = 1.0f;

		} else if(rate < time1) {

			alpha = Mathf.InverseLerp(time0, time1, rate);
			alpha = Mathf.Cos(alpha*Mathf.PI/2.0f);

		} else if(rate < time2) {

			alpha = 0.0f;

		} else {

			alpha = Mathf.InverseLerp(time2, time3, rate);
			alpha = Mathf.Sin(alpha*Mathf.PI/2.0f);
		}

		this.yuzu_sprite.setVertexAlpha(1.0f - alpha);

		// 小さなもやもや×２こ.
		// 少し遅れて動いているように見せる.

		Vector3		p;

		p = Vector3.Lerp(this.moya_root_position, this.calc_moya_position(this.timer - MOYA_CYCLE*0.2f), 0.5f);
		this.mini_moya_sprite[1].setPosition(p);

		p = Vector3.Lerp(this.moya_root_position, this.calc_moya_position(this.timer - MOYA_CYCLE*0.4f), 0.3f);
		this.mini_moya_sprite[0].setPosition(p);

		//

		this.timer += Time.deltaTime;
	}

	// 一番上のもやもやの位置を求める.
	protected Vector3	calc_moya_position(float time)
	{
		Vector3		position = this.moya_position_center;
		float		rate;

		rate = Mathf.Repeat(time, MOYA_CYCLE)/MOYA_CYCLE;
		rate = Mathf.Lerp((Mathf.Sin((rate - 0.5f)*Mathf.PI) + 1.0f)/2.0f, rate, 0.7f);
		position.x += Mathf.Sin(rate*Mathf.PI*2.0f)*16.0f;

		rate = Mathf.Repeat(time, MOYA_CYCLE*2.0f)/(MOYA_CYCLE*2.0f);

		if(rate < 0.5f) {

			rate *= 2.0f;
			rate = Mathf.Lerp((Mathf.Sin((rate - 0.5f)*Mathf.PI) + 1.0f)/2.0f, rate, 0.7f);
			rate = rate*0.5f;

		} else {

			rate = (rate - 0.5f)*2.0f;
			rate = Mathf.Lerp((Mathf.Sin((rate - 0.5f)*Mathf.PI) + 1.0f)/2.0f, rate, 0.7f);
			rate = 0.5f + rate*0.5f;
		}

		position.y += Mathf.Sin(rate*Mathf.PI*2.0f)*8.0f;

		return(position);
	}

	// ---------------------------------------------------------------- //

	// 『決めた！』アクションの実行.
	protected void		exec_step_hai()
	{
		float	FLIP_START    = 0.2f;
		float	FLIP_DURATION = 0.2f;

		Vector3		p0 = this.moya_root_position + this.step_hai.moya_offset;
		Vector3		p1 = this.moya_root_position + Vector3.up*40.0f;

		float	rate, scale;

		rate = Mathf.Min(this.step.get_time(), FLIP_START)/FLIP_START;
		rate = Mathf.Sin(rate*Mathf.PI/2.0f);

		this.moya_position = Vector3.Lerp(p0, p1, rate);

		this.negi_sprite.setPosition(this.moya_position);
		this.yuzu_sprite.setPosition(this.moya_position);

		if(this.step.get_time() > FLIP_START) {

			rate = Mathf.Min(this.step.get_time() - FLIP_START, FLIP_DURATION)/FLIP_DURATION;

			scale = Mathf.Sin(rate*Mathf.PI/2.0f);
			scale = Mathf.Cos(scale*Mathf.PI);
			scale = Mathf.Abs(scale);

			this.negi_sprite.setScale(new Vector2(scale, 1.0f));
			this.yuzu_sprite.setScale(new Vector2(scale, 1.0f));

			// 半周回ったところでテクスチャーを入れかえる.
			if(this.step.is_acrossing_time(FLIP_START + FLIP_DURATION*0.5f)) {

				this.negi_sprite.setTexture(this.oke_texture);
				this.yuzu_sprite.setVisible(false);

			}
			if(this.step.get_time() >= FLIP_START + FLIP_DURATION*0.5f) {

				if(this.selecting_icon.is_flip) {

					this.negi_sprite.setScale(new Vector2(-scale, 1.0f));
				}
			}
		}
	}

}

}

