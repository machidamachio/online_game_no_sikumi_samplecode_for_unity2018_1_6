using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 『レディ！』『おやつタイム！』等の２Dフォントのかけ声表示.
public class YellDisp : MonoBehaviour {

	public enum STEP {

		NONE = -1,

		APPEAR = 0,		// キャラの移動に合わせてクッキーを表示.
		FLIP,			// キャラがターンすると、クッキーが順に文字に変わる.
		STAY,			// ループ表示.
		FADE_OUT,		// フェードアウト.
		FINISH,			// おしまい.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	public YELL_WORD		word = YELL_WORD.NONE;
	public Texture			icon_texture;
	public Texture			moji_mae_texture;
	public YELL_FONT[]		yell_words;
	public Sprite2DControl	root_sprite;

	public  const float			POSITION_Y = 128.0f;

	protected Yell.Icon			icon;

	protected List<Yell.Moji>	mojis;

	protected int				flip_count = 0;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
	}

	void	Start()
	{
		this.step.set_next(STEP.APPEAR);
	}

	void	Update()
	{
		float	fade_out_time = 0.5f;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// キャラの移動に合わせてクッキーを表示.
			case STEP.APPEAR:
			{
				if(this.icon.isFinished()) {

					this.step.set_next(STEP.FLIP);
				}
			}
			break;

			// キャラがターンすると、クッキーが順に文字に変わる.
			case STEP.FLIP:
			{
				if(!this.mojis.Exists(x => !x.isFinished())) {

					if(this.word == YELL_WORD.CAKE_COUNT || this.word == YELL_WORD.OSIMAI) {
	
						this.step.set_next(STEP.STAY);

					} else {

						this.step.set_next(STEP.FADE_OUT);
					}
				}
			}
			break;

			// フェードアウト.
			case STEP.FADE_OUT:
			{
				if(this.step.get_time() > fade_out_time) {

					this.destroy();
					GameObject.Destroy(this.gameObject);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// キャラの移動に合わせてクッキーを表示.
				case STEP.APPEAR:
				{
					this.icon.reset();
		
					foreach(var moji in this.mojis) {
			
						moji.reset();
					}
				}
				break;

				// キャラがターンすると、クッキーが順に文字に変わる.
				case STEP.FLIP:
				{
					this.flip_count = 0;
				}
				break;
	
				// フェードアウト.
				case STEP.STAY:
				case STEP.FADE_OUT:
				{
					this.icon.beginFadeOut();
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// キャラの移動に合わせてクッキーを表示.
			case STEP.APPEAR:
			{
				foreach(var moji in this.mojis) {
		
					if(moji.check_start(this.icon.sprite.getPosition().x)) {

						break;
					}
				}
			}
			break;

			// キャラがターンすると、クッキーが順に文字に変わる.
			case STEP.FLIP:
			{
				float	delay = 0.1f;

				int		n = Mathf.Min(Mathf.FloorToInt(this.step.get_time()/delay), this.mojis.Count);

				if(this.flip_count < n) {

					this.mojis[this.flip_count].startFlip();
					this.flip_count++;
				}
			}
			break;

			// フェードアウト.
			case STEP.FADE_OUT:
			{
				float	a = ipCell.get().setInput(this.step.get_time()).ilerp(0.0f, fade_out_time).lerp(1.0f, 0.0f).getCurrent();
	
				foreach(var moji in this.mojis) {

					if(moji.sprite.isVisible()) {

						moji.sprite.setVertexAlpha(a);
					}
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.icon.execute();

		foreach(var moji in this.mojis) {

			moji.execute();
		}
	}

	// ================================================================ //

	public void		create()
	{
		Navi	navi = Navi.get();

		//

		this.root_sprite = Sprite2DRoot.get().createNull();
		this.root_sprite.setPosition(new Vector3(0.0f, POSITION_Y, 0.0f));

		// アイコン、文字のオブジェクトを作る.

		this.icon = new Yell.Icon();
		this.icon.create(this.icon_texture);
		this.icon.sprite.transform.parent = this.root_sprite.transform;

		//

		this.mojis = new List<Yell.Moji>();

		for(int i = 0;i < this.yell_words.Length;i++) {

			YellFontData	font_data = navi.getYellFontData(this.yell_words[i]);

			Yell.Moji	moji = new Yell.Moji();

			moji.create(font_data.texture, this.moji_mae_texture);

			if(font_data.is_small) {

				moji.moji_mae_scale *= 0.5f;
			}
			moji.yell = this;
			moji.index = i;
			moji.sprite.transform.parent = this.root_sprite.transform;
			moji.reset();

			if(i%3 == 0) {

				moji.sprite.setVertexColor(new Color(1.0f, 1.0f, 0.5f));

			} else if(i%3 == 1) {

				moji.sprite.setVertexColor(new Color(1.0f, 0.7f, 0.7f));

			} else if(i%3 == 2) {

				moji.sprite.setVertexColor(new Color(0.3f, 1.0f, 1.0f));
			}

			this.mojis.Add(moji);
		}

		this.icon.sprite.setDepth(this.mojis[this.mojis.Count - 1].sprite.getDepth() - 0.1f);

		// 文字の位置.

		float		pitch = 54.0f;
		Vector2		p0 = Vector2.zero;
		Vector2		p1 = Vector2.zero;

		p0.x = (float)this.mojis.Count*pitch/2.0f;
		p0.y = 0.0f;

		p1.x = 0.0f - ((float)this.mojis.Count)*pitch/2.0f - pitch/2.0f;
		p1.y = p0.y;

		this.icon.p0 = p0;
		this.icon.p1 = p1;
		p1.x += pitch;

		p0.x = p1.x;

		for(int i = 0;i < this.mojis.Count;i++) {

			YellFontData	font_data = navi.getYellFontData(this.yell_words[i]);

			this.mojis[i].p0 = p0;
			this.mojis[i].reset();

			if(font_data.is_small) {

				this.mojis[i].p0.x -= pitch*0.25f;
				this.mojis[i].p0.y -= pitch*0.25f;

				p0.x += pitch*0.5f;

			} else {

				p0.x += pitch;
			}
		}

	}

	public void		destroy()
	{
		this.icon.sprite.destroy();

		foreach(var moji in this.mojis) {

			moji.sprite.destroy();
		}

		GameObject.Destroy(this.gameObject);
	}

	// 位置をセットする.
	public void		setPosition(Vector3 position)
	{
		this.root_sprite.setPosition(position);
	}

	// index文字目の文字のオブジェクトをゲットする.
	public Yell.Moji	getMoji(int index)
	{
		return(this.mojis[index]);
	}

	// フェードアウトを開始する.
	public void		beginFadeOut()
	{
		this.step.set_next(STEP.FADE_OUT);
	}

}

namespace Yell {

// 先頭のキャラクター.
public class Icon {

	public enum STEP {

		NONE = -1,

		MOVE = 0,		// 移動.
		TURN,			// くるっと回る.
		FINISH,			// おしまい.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	public Sprite2DControl	sprite;

	public Vector2		p0;					// スタート地点.
	public Vector2		p1;					// ゴール地点.

	public ipModule.FCurve	fcurve;
	
	protected bool		is_fading  = false;	// フェードアウト中？.
	protected float		fade_timer = 0.0f;
	
	// ================================================================ //

	public void		beginFadeOut()
	{
		if(!this.is_fading) {

			this.fade_timer = 0.0f;
			this.is_fading = true;
		}
	}

	public void		create(Texture texture)
	{
		this.sprite = Sprite2DRoot.get().createSprite(texture, true);
		this.sprite.setSize(Vector2.one*64.0f);
		this.sprite.setVisible(false);

		this.fcurve = new ipModule.FCurve();
		this.fcurve.setSlopeAngle(70.0f, 5.0f);
		this.fcurve.setDuration(0.7f);
		this.fcurve.start();
	}

	// 表示開始.
	public void		start()
	{
		this.step.set_next(STEP.MOVE);
	}

	// リセット（再スタート）　デバッグ用.
	public void		reset()
	{
		this.start();
	}

	protected	float	turn_time = 0.2f;

	// 終わった？.
	public bool		isFinished()
	{
		bool	ret = false;

		do {

			if(this.step.get_current() != STEP.TURN) {

				break;
			}
			if(this.step.get_time() < turn_time*0.5f) {

				break;
			}

			ret = true;

		} while(false);

		return(ret);
	}

	// 毎フレームの実行.
	public void		execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.MOVE:
			{
				if(this.fcurve.isDone()) {

					this.step.set_next(STEP.TURN);
				}
			}
			break;

			case STEP.TURN:
			{
				if(this.step.get_time() > turn_time) {

					this.step.set_next(STEP.FINISH);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.MOVE:
				{
					this.sprite.setPosition(this.p0);
					this.sprite.setVisible(true);		
					this.fcurve.start();
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.MOVE:
			{
				this.fcurve.execute(Time.deltaTime);
		
				Vector2	icon_pos = Vector2.Lerp(this.p0, this.p1, this.fcurve.getValue());
		
				this.sprite.setPosition(icon_pos);
				this.sprite.setScale(new Vector2( 1.0f, 1.0f));
			}
			break;

			case STEP.TURN:
			{
				float	rate = Mathf.Clamp01(this.step.get_time()/turn_time);

				rate = Mathf.Lerp(0.0f, Mathf.PI, rate);

				float	sx = Mathf.Cos(rate);

				this.sprite.setScale(new Vector2(sx, 1.0f));
			}
			break;
		}

		// ---------------------------------------------------------------- //

		if(this.is_fading) {

			float	fade_out_time = 0.5f;
			float	a = ipCell.get().setInput(this.fade_timer).ilerp(0.0f, fade_out_time).lerp(1.0f, 0.0f).getCurrent();

			this.sprite.setVertexAlpha(a);	

			this.fade_timer += Time.deltaTime;	
		}
	}
}

// もじ.
public class Moji {

	public enum STEP {

		NONE = -1,

		WAIT = 0,		// 表示開始待ち中（非表示）.
		APPEAR,			// 登場.
		FLIP,			// クッキーが文字に変わる.
		FINISH,			// おしまい.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	public YellDisp	yell;

	public int		index = -1;

	public Texture	moji_texture;
	public Texture	moji_mae_texture;
	public Vector2	moji_texture_size;

	public Vector2	p0;					// スタート地点.

	public Sprite2DControl		sprite;

	public ipModule.FCurve		fcurve;
	public ipModule.Spring		spring;

	public float	moji_mae_scale = 0.9f;

	protected bool	is_finished = false;

	// ================================================================ //

	public void		create(Texture moji_texture, Texture moji_mae_texture)
	{
		this.moji_texture      = moji_texture;
		this.moji_mae_texture  = moji_mae_texture;
		this.moji_texture_size = new Vector2(this.moji_texture.width, this.moji_texture.height);

		this.sprite = Sprite2DRoot.get().createSprite(this.moji_mae_texture, true);
		this.sprite.setSize(Vector2.one*64.0f);

		this.fcurve = new ipModule.FCurve();
		this.fcurve.setSlopeAngle(70.0f, 5.0f);
		this.fcurve.setDuration(0.3f);

		this.spring = new ipModule.Spring();
		this.spring.k      = 100.0f;
		this.spring.reduce = 0.90f;
	}

	// リセット（再スタート）.
	public void		reset()
	{
		this.sprite.setPosition(this.p0);
		this.sprite.setVisible(false);

		this.sprite.setTexture(this.moji_mae_texture);
		this.step.set_next(STEP.WAIT);
	}

	// 表示を開始するか、チェックする.
	public bool		check_start(float leader_x)
	{
		bool	trigger_start = false;

		do {

			if(this.step.get_current() != STEP.WAIT) {

				break;
			}
			if(leader_x > this.p0.x) {

				break;
			}

			this.start();
			trigger_start = true;

		} while(false);

		return(trigger_start);
	}

	// 表示開始.
	public void		start()
	{
		this.step.set_next(STEP.APPEAR);
	}

	// フリップ開始.
	public void		startFlip()
	{
		this.step.set_next(STEP.FLIP);
	}

	// おしまい？.
	public bool		isFinished()
	{
		return(this.is_finished);
	}

	// おしまいフラグをクリアーする.
	public void		clearFinishedFlag()
	{
		this.is_finished = false;
	}

	// 毎フレームの実行.
	public void		execute()
	{
		float	flip_time = 1.0f;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.FLIP:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 表示開始待ち中（非表示）.
				case STEP.WAIT:
				{
					this.is_finished = false;
				}
				break;

				// 登場.
				case STEP.APPEAR:
				{
					if(this.moji_mae_texture != null) {

						this.sprite.setVisible(true);
						this.sprite.setPosition(this.p0);
	
						this.fcurve.start();
	
						this.spring.start(0.75f);

					} else {

						this.sprite.setVisible(false);
					}
				}
				break;

				// クッキーが文字に変わる.
				case STEP.FLIP:
				{
					if(this.moji_texture != null) {

						this.sprite.setScale(Vector2.one);
						this.sprite.setTexture(this.moji_texture);
						this.sprite.setSize(this.moji_texture_size);
						this.sprite.setVertexColor(Color.white);

					} else {

						this.sprite.setVisible(false);
					}
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 登場.
			case STEP.APPEAR:
			{
				// スケールで一瞬ぽんっとふくらむ.

				float	scale;

				this.spring.execute(Time.deltaTime);
				
				scale = Mathf.InverseLerp(-1.0f, 1.0f, this.spring.position);
				scale = Mathf.Lerp(0.5f, 1.5f, scale);

				this.sprite.setPosition(this.p0);
				this.sprite.setScale(Vector2.one*scale*this.moji_mae_scale);
			}
			break;

			// クッキーが文字に変わる.
			case STEP.FLIP:
			{
				// くるっと回って文字に変わる → 上下にゆらゆら.

				float	cycle;
				float	s, y;

				switch(this.yell.word) {

					default:
					{
						cycle = 2.0f;
						s     = ipCell.get().setInput(this.step.get_time()).clamp(0.0f, cycle/2.0f).pow(0.2f).remap(36.0f, 0.0f).getCurrent();
	
	
						cycle = 0.6f;	
						y     = ipCell.get().setInput(this.step.get_time()).repeat(cycle).remap(0.0f, 2.0f*Mathf.PI).sin().scale(s).getCurrent();
					}
					break;

					case YELL_WORD.OSIMAI:
					{
						// ゆらゆらをくり返す.
	
						float	cycle0 = 1.8f;
						float	cycle1 = 0.6f;
	
						float	power, amplitude;
	
						if(this.step.get_time() < cycle0) {
	
							power     = 0.2f;
							amplitude = 36.0f;
	
						} else {
	
							power     = 0.4f;
							amplitude = 24.0f;
						}
	
						s = ipCell.get().setInput(this.step.get_time()).repeat(cycle0).getCurrent();
						s = ipCell.get().setInput(s).lerp(0.0f, cycle0/2.0f).pow(power).lerp(amplitude, 0.0f).getCurrent();
		
						y = ipCell.get().setInput(this.step.get_time()).repeat(cycle1).remap(0.0f, 2.0f*Mathf.PI).sin().scale(s).getCurrent();
					}
					break;

					case YELL_WORD.CAKE_COUNT:
					{
						cycle = 2.0f;

						if(this.step.get_time() < 0.6f) {

							s = ipCell.get().setInput(this.step.get_time()).clamp(0.0f, cycle/2.0f).pow(0.2f).remap(36.0f, 0.0f).getCurrent();

						} else {

							s = ipCell.get().setInput(this.step.get_time()).repeat(0.6f*2.0f).remap(0.0f, 2.0f*Mathf.PI).sin().remap(1.0f, 4.0f).getCurrent();
						}

						cycle = 0.6f;	
						y     = ipCell.get().setInput(this.step.get_time()).repeat(cycle).remap(0.0f, 2.0f*Mathf.PI).sin().scale(s).getCurrent();
					}
					break;
				}

				this.sprite.setPosition(new Vector2(this.p0.x, this.p0.y + y));

				if(this.step.is_acrossing_time(flip_time)) {

					this.is_finished = true;
				}
			}
			break;

		}

		// ---------------------------------------------------------------- //

	}

}

}
