using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ビヘイビアーの基底クラス.
public class ItemBehaviorBase : MonoBehaviour {

	public enum STEP {

		NONE = -1,

		IDLE = 0,
		SPAWN,			// 倒した敵から出現.
		FALL,			// 上から落ちてくる（武器選択シーンのかぎ用）.
		BUFFET,			// ケーキバイキングのときのケーキ.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	public ItemController	controll = null;

	public Item.Favor		item_favor = null;					// 特典　アイテムを持っているキャラクターにつく、特殊効果.

	private		List<string>	preset_texts = null;			// プリセットなセリフ.
	private		bool			is_texts_editable = false;		// preset_texts を編集できる？.

	protected ipModule.Jump		ip_jump = new ipModule.Jump();

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //


	// 生成直後に呼ばれる.
	public void	initialize()
	{
		this.item_favor   = new Item.Favor();
		this.preset_texts = new List<string>();

		this.is_texts_editable = true;
		this.initialize_item();
		this.is_texts_editable = false;

		switch(this.controll.type) {

			case "candy00":
			{
				this.item_favor.category = Item.CATEGORY.CANDY;
			}
			break;

			case "key00":
			case "key01":
			case "key02":
			case "key03":
			{
				this.item_favor.category = Item.CATEGORY.KEY;
				break;
			}

			case "key04":
			{
				this.item_favor.category = Item.CATEGORY.FLOOR_KEY;
				break;
			}

			case "ice00":
			{
				this.item_favor.category = Item.CATEGORY.SODA_ICE;
				this.item_favor.option0 = (object)false;
			}
			break;

			case "cake00":
			{
				this.item_favor.category = Item.CATEGORY.FOOD;
			}
			break;

			case "shot_negi":
			case "shot_yuzu":
			{
				this.item_favor.category = Item.CATEGORY.WEAPON;
			}
			break;


			case "dagger00":
			{
				this.item_favor.category = Item.CATEGORY.WEAPON;
			}
			break;
			case "arrow00":
			{
				this.item_favor.category = Item.CATEGORY.WEAPON;
			}
			break;
			case "bomb00":
			{
				this.item_favor.category = Item.CATEGORY.ETC;
			}
			break;
		}
	}

	// アイテム効果のオプションパラメーター（『アイスの当たり』など）をセットする.
	public void	setFavorOption(object option0)
	{
		this.item_favor.option0 = option0;
	}

	// 定型文を返す
	// 定型文のふきだし（NPCのふきだし）を表示するときによばれる.
	public string	getPresetText(int text_id)
	{
		string	text = "";

		if(0 <= text_id && text_id < this.preset_texts.Count) {

			text = this.preset_texts[text_id];
		}

		return(text);
	}

	// ================================================================ //

	// 生成直後に呼ばれる 派生クラス用.
	public virtual void	initialize_item()
	{
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public virtual void	start()
	{
		if(this.controll.type == "shot_negi" || this.controll.type == "shot_yuzu") {

			this.controll.setBillboard(false);
		}

		if(this.step.get_next() == STEP.NONE) {

			this.step.set_next(STEP.IDLE);
		}
	}

	public Vector3	buffet_goal = Vector3.zero;
	public float	buffet_height = 1.0f;

	// 毎フレームよばれる.
	public virtual void	execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.


		switch(this.step.do_transition()) {

			case STEP.IDLE:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.SPAWN:
				{
					this.ip_jump.setBounciness(new Vector3(0.0f, -0.5f, 0.0f));

					Vector3		start = this.transform.position;
					Vector3		goal  = this.transform.position;

					this.ip_jump.start(start, goal, 1.0f);

					EffectRoot.get().createSmokeMiddle(this.transform.position);
				}
				break;

				case STEP.FALL:
				{
					this.ip_jump.setBounciness(new Vector3(0.0f, -0.5f, 0.0f));

					Vector3		start = this.transform.position + Vector3.up*10.0f;
					Vector3		goal  = this.transform.position;

					this.ip_jump.start(start, goal, 1.0f);
				}
				break;

				case STEP.BUFFET:
				{
					this.ip_jump.setBounciness(new Vector3(0.0f, -0.5f, 0.0f));

					Vector3		start = this.transform.position;
					Vector3		goal  = this.buffet_goal;

					this.ip_jump.start(start, goal, this.buffet_height);

					//EffectRoot.get().createSmokeMiddle(this.transform.position);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.FALL:
			case STEP.SPAWN:
			{
				this.ip_jump.execute(Time.deltaTime);

				this.transform.position = this.ip_jump.position;
			}
			break;

			case STEP.BUFFET:
			{
				this.ip_jump.execute(Time.deltaTime);

				if(!this.controll.isPickable()) {

					if(this.ip_jump.velocity.y <= 0.0f) {

						this.controll.cmdSetPickable(true);
					}
				}

				this.transform.position = this.ip_jump.position;
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

	public void		beginFall()
	{
		this.step.set_next(STEP.FALL);
	}

	public void		beginSpawn()
	{
		this.step.set_next(STEP.SPAWN);
	}

	public void		beginBuffet()
	{
		this.controll.cmdSetPickable(false);
		this.step.set_next(STEP.BUFFET);
	}

	// 拾われたときに呼ばれる.
	public virtual void		onPicked()
	{
	}

	// リスポーンしたときに呼ばれる.
	public virtual void		onRespawn()
	{
	}

	// ================================================================ //
	// 継承先のクラス用

	protected void	addPresetText(string text)
	{
		if(this.is_texts_editable) {

			this.preset_texts.Add(text);

		} else {

			// initialize() メソッド以外ではテキストの追加はできない.
			Debug.LogError("addPresetText() can use only in initialize_npc().");
		}
	}
}
