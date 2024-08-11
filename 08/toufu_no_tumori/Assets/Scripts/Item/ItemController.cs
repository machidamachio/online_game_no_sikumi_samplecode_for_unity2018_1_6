using UnityEngine;
using System.Collections;

// アイテムを持っているときに、キャラクターに付与される特典.
public class ItemFavor {

	public ItemFavor()
	{
		this.term_word = "";
		this.is_enable_house_move = false;
	}

	public string	term_word = "";			// 語尾.
	public bool		is_enable_house_move;	// ひっこしできるようになる.
};


// アイテムのコントローラー.
public class ItemController : MonoBehaviour {

	public static float		BALLOON_HEIGHT   = 1.0f;		// ふきだしの高さ.
	public static float		COLLISION_RADIUS = 0.5f;		// コリジョン球の半径.

	// ---------------------------------------------------------------- //

	private GameObject			main_camera = null;	// カメラ.

	public ItemBehaviorBase		behavior = null;	// ビヘイビアー.
	public GameObject			model    = null;	// モデル.

	public string		owner_account;					// このアイテムを作ったアカウント.
	public string		id = "";						// ユニークなID.
	protected string	production = "";				// 生産地　外に持ち出せるアイテムの、スポーンするマップ名.

	public string	picker = "";					// 今このアイテムを持ち歩いている人.

	public ChattyBalloon			balloon = null;		// ふきだし.

	// ステート.
	// "ing" が付いているステートは、ステートを変えるための調停中です.
	//
	// 1.ローカルプレイヤーがアイテムを拾おうとした.
	// 2.[PickingUp] ネットワークプレイヤーに拾ってもいいか聞いてみる.
	// 3.[Picked] OK が返ってきたら拾う.
	//

	public enum State
	{
		Growing = 0, 			// 発生中.
		None, 					// 未取得.
		PickingUp,				// 取得中.
		Picked,					// 取得済.
		Dropping,				// 破棄中.
		Dropped,				// 破棄.
	}

	public State	state = State.None;	// アイテムの状態.

	// ---------------------------------------------------------------- //

	public float	timer = 0.0f;				// アイテムが作られてからの時間.
	public float	timer_prev;

	protected struct Billboard {

		public bool		is_enable;				// ビルボード？
		public float	roll;					// Z軸周りの回転.
	};
	protected Billboard		billboard;

	protected bool		is_visible = true;		// アイテムの可視性.
	protected bool		is_pickable = true;		// 拾える？（成長中は拾えない）.
	protected bool		is_exportable = false;	// 他のマップに持ち出せる？.

	protected Vector3		initial_position = Vector3.zero;
	protected Quaternion	initial_rotation = Quaternion.identity;

	protected float	collision_radius = COLLISION_RADIUS;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.main_camera  = GameObject.FindGameObjectWithTag("MainCamera");

		this.balloon = BalloonRoot.get().createBalloon();

		this.timer       = -1.0f;
		this.timer_prev  = -1.0f;

		this.billboard.is_enable = false;
		this.billboard.roll      = 0.0f;

		this.initial_position = this.transform.position;
		this.initial_rotation = this.transform.rotation;

		this.is_visible = true;
	
		this.behavior.start();
	}

	void	Update()
	{
		if(this.isActive()) {

			this.update_entity();
		}
	}

	void	update_entity()
	{
		if(this.timer < 0.0f) {

			this.timer_prev = -1.0f;
			this.timer      =  0.0f;

		} else {

			this.timer_prev = this.timer;
			this.timer += Time.deltaTime;
		}

		// ---------------------------------------------------------------- //
		// ビヘイビアーの実行.
		//
		// （マウスの移動（ローカル）、ネットから受信したデーターで移動（ネット））.
		//

		this.behavior.execute();

		// ビルボードのときは、カメラの方に向ける.
		if(this.billboard.is_enable) {

			this.model.transform.localPosition = Vector3.zero;
			this.model.transform.localRotation = Quaternion.identity;

			Vector3		camera_position = this.transform.InverseTransformPoint(this.main_camera.transform.position);

			this.model.transform.Translate(Vector3.up*this.collision_radius);
			this.model.transform.localRotation *= Quaternion.LookRotation(-camera_position, Vector3.up);
			this.model.transform.Translate(-Vector3.up*this.collision_radius);
		}

		// ---------------------------------------------------------------- //
		// ふきだしの位置/いろ.

		if(this.balloon != null && this.balloon.getText() != "") {

			Vector3		on_screen_position = Camera.main.WorldToScreenPoint(this.transform.position + Vector3.up*BALLOON_HEIGHT);

			this.balloon.setPosition(new Vector2(on_screen_position.x, Screen.height - on_screen_position.y));

			this.balloon.setColor(Color.yellow);
		}
	}

	// ================================================================ //
	// ビヘイビアーの使うコマンド.
	
	// 位置をセットする.
	public void		cmdSetPosition(Vector3 position)
	{
		this.transform.position = position;
	}

	// 向きをセットする.
	public void		cmdSetDirection(float angle)
	{
		this.transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
	}

	// ================================================================ //
	// ビヘイビアー用のコマンド.

	public void		cmdSetPickable(bool is_pickable)
	{
		this.is_pickable = is_pickable;
	}

	// 定型文を使って吹き出しを表示する.
	public void		cmdDispBalloon(int text_id)
	{
		this.balloon.setText(this.behavior.getPresetText(text_id));
	}

	// 吹き出しを消す.
	public void		cmdHideBalloon()
	{
		this.balloon.clearText();
	}

	// 表示/非表示する.
	public void		cmdSetVisible(bool is_visible)
	{
		this.setVisible(is_visible);
	}
	
	public bool		isActive()
	{
		return this.behavior.is_active;
	}

	// コリジョンを ON/OFF する.
	public void		cmdSetCollidable(bool is_enable)
	{
		this.GetComponent<Collider>().enabled = is_enable;
	}

	// ================================================================ //

	// 表示/非表示する.
	public void		setVisible(bool is_visible)
	{
		this.is_visible = is_visible;

		Renderer[]		renderers = this.gameObject.GetComponentsInChildren<Renderer>();

		foreach(var renderer in renderers) {

			renderer.enabled = this.is_visible;
		}

		// 影.
		Projector[]		projectors = this.gameObject.GetComponentsInChildren<Projector>();
	
		foreach(var projector in projectors) {

			projector.enabled = this.is_visible;
		}

		// ふき出し.
		this.balloon.setVisible(this.is_visible);

		// コリジョンも連動させる.
		this.cmdSetCollidable(this.is_visible);
	}

	// 拾われる.
	public void		startPicked()
	{
		if (this.balloon != null) {
			this.balloon.clearText();
		}

		this.behavior.onPicked();
	}

	// リスポーンする.
	public void		startRespawn()
	{
		this.transform.position = this.initial_position;
		this.transform.rotation = this.initial_rotation;

		this.timer       = -1.0f;
		this.timer_prev  = -1.0f;

		if (isActive()) {
			this.behavior.onRespawn();
		}
		else {
			this.setVisible(false);
		}
	}

	// アイテムを成長状態にする（拾えるようにする）.
	public void		finishGrowing()
	{
		this.behavior.finishGrowing();
	}

	// 他のマップに持ち出せる？.
	public bool		isExportable()
	{
		return(this.is_exportable);
	}

	// 他のマップに持ち出せる/持ち出せないをセットする.
	public void		setExportable(bool is_exportable)
	{
		this.is_exportable = is_exportable;
	}

	// 生産地（スポーンしたマップの名前）をゲットする.
	public string		getProduction()
	{
		return(this.production);
	}

	// 生産地（スポーンしたマップの名前）をセットする.
	public 	void	setProduction(string production)
	{
		this.production = production;
	}

	// ================================================================ //

	// 拾える？.
	public bool		isPickable()
	{
		return(is_pickable);
	}

	// ビルボード？をセットする.
	public void		setBillboard(bool is_billboard)
	{
		this.billboard.is_enable = is_billboard;
		this.billboard.roll      = 0.0f;
	}


	// timer が timer をまたいだ瞬間なら true.
	public bool		isPassingTime(float time)
	{
		bool	ret = false;

		if(this.timer_prev < time && time <= this.timer) {

			ret = true;
		}

		return(ret);
	}

	// コリジョン球の半径を取得する.
	public float	getCollisionRadius()
	{
		return(this.collision_radius);
	}
}
