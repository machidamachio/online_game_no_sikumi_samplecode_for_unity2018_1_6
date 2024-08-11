using UnityEngine;
using System.Collections;

// アイテムのビヘイビアー　フルーツ用.
public class ItemBehaviorFruit : ItemBehaviorBase {

	private GameObject		germ  = null;		// 芽.
	private GameObject		glass = null;		// 草.
	private GameObject		fruit = null;		// 実.

	// ================================================================ //

	public enum STEP {

		NONE = -1,

		GERM = 0,			// 芽.
		GLASS,				// 草.
		APPEAR,				// 実が登場中.	ぼわん→草が実になる→バウンドして着地.
		FRUIT,				// 実.

		NUM,
	};
	protected Step<STEP>		step = new Step<STEP>(STEP.NONE);

	protected ipModule.Jump		ip_jump = new ipModule.Jump();

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	// 生成直後に呼ばれる.
	public override void	initialize_item()
	{
		this.germ  = this.gameObject.transform.Find("Germ").gameObject;
		this.glass = this.gameObject.transform.Find("Glass").gameObject;
		this.fruit = this.gameObject.transform.Find("Fruit").gameObject;

		this.germ.SetActive(is_active);
		this.glass.SetActive(false);
		this.fruit.SetActive(false);

		this.controll.cmdSetPickable(false);

		this.ip_jump.setBounciness(new Vector3(0.0f, -0.5f, 0.0f));

		switch(this.transform.parent.name) {

			case "Negi":
			{
				this.addPresetText("生えてきたよー");
				this.addPresetText("ネギでしたー");
			}
			break;
		}

		if (is_active) {
			this.step.set_next(STEP.GERM);
		}
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public override void	start()
	{
		this.controll.setVisible(is_active);

		this.controll.balloon.setColor(Color.green);

		this.controll.setBillboard(false);

		// 成長中は拾えない.
		this.controll.cmdSetPickable(false);
	}

	// 毎フレームよばれる.
	public override void	execute()
	{
		float	germ_time = 5.0f;
		float	glass_time = 5.0f;
		
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.GERM:
			{
				if(this.step.get_time() > germ_time) {

					this.step.set_next(STEP.GLASS);
				}
			}
			break;

			case STEP.GLASS:
			{
				if(this.step.get_time() > glass_time) {

					this.step.set_next(STEP.APPEAR);
				}
			}
			break;

			case STEP.APPEAR:
			{
				if(this.ip_jump.isDone()) {

					this.step.set_next(STEP.FRUIT);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.GERM:
				{
					// 成長中は拾えない.
					this.controll.cmdSetPickable(false);

					this.germ.SetActive(true);
					this.glass.SetActive(false);
					this.fruit.SetActive(false);

					if(this.transform.parent.name == "Negi") {
						ItemManager.ItemState state = ItemManager.get().FindItemState("Negi");
						
						if (state.state != ItemController.State.Picked) {
							this.controll.cmdDispBalloon(0);
						}
					}
				}
				break;

				case STEP.GLASS:
				{
					this.germ.SetActive(false);
					this.glass.SetActive(true);
					this.fruit.SetActive(false);
				}
				break;
	
				case STEP.APPEAR:
				{
					this.germ.SetActive(false);
					this.glass.SetActive(false);
					this.fruit.SetActive(true);

					Vector3		start = this.transform.position;
					Vector3		goal  = this.transform.position;

					this.ip_jump.start(start, goal, 1.0f);

					// ぼわんけむりのエフェクト.

					// エフェクトが実のモデルに埋まらないように、カメラの方へ押す.

					Vector3		smoke_position = this.transform.position + Vector3.up*0.3f;

					GameObject	main_camera = GameObject.FindGameObjectWithTag("MainCamera");

					Vector3		v = main_camera.transform.position - smoke_position;

					v.Normalize();
					v *= 1.0f;

					smoke_position += v;

					EffectRoot.getInstance().createSmoke01(smoke_position);
				}
				break;

				case STEP.FRUIT:
				{				
					if(this.transform.parent.name == "Negi") {
						ItemManager.ItemState state = ItemManager.get().FindItemState("Negi");
						
						if (state.state != ItemController.State.Picked) {
							this.controll.cmdDispBalloon(1);
						}
					}

					// 成長しきったら拾える.
					this.controll.cmdSetPickable(true);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.APPEAR:
			{
				this.ip_jump.execute(Time.deltaTime);

				this.transform.position = this.ip_jump.position;
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

	// リスポーンしたときに呼ばれる.
	public override void		onRespawn()
	{
		this.step.set_next(STEP.GERM);
	}

	// アイテムを成長状態にする（拾えるようにする）.
	public override void		finishGrowing()
	{
		this.step.set_next(STEP.FRUIT);

		this.germ.SetActive(false);
		this.glass.SetActive(false);
		this.fruit.SetActive(true);

		this.controll.cmdSetPickable(true);
	}

	// アイテムのアクティブ/非アクティブ設定.
	public override void		activeItem(bool active)
	{
		this.is_active = active;

		this.germ.SetActive(active);
		this.glass.SetActive(false);
		this.fruit.SetActive(false);
		
		this.controll.cmdSetPickable(false);

		this.controll.setVisible(active);

		this.step.set_next(STEP.GERM);
	}
}
