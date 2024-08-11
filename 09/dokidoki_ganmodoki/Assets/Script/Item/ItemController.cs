using UnityEngine;
using System.Collections;



// アイテムのコントローラー.
public class ItemController : MonoBehaviour {

	private GameObject			myCamera   = null;	// カメラ.

	public ItemBehaviorBase		behavior = null;	// ビヘイビアー.

	public string	type = "";						// 種類("apple" とか "orange" とか）.
	public string	owner_account;					// このアイテムを作ったアカウント.
	public string	id = "";						// ユニークなID.

	public string	picker = "";					// 今このアイテムを持ち歩いている人.

	// ステート.
	// "ing" が付いているステートは、ステートを変えるための調停中です.
	//
	// 1.ローカルプレイヤーがアイテムを拾おうとした.
	// 2.[PickingUp] ネットワークプレイヤーに拾ってもいいか聞いてみる.
	// 3.[Picked] OK が返ってきたら拾う.
	//
	public enum State
	{
		None = 0, 				// 未取得.
		PickingUp,				// 取得中.
		Picked,					// 取得済.
		Dropping,				// 破棄中.
		Dropped,				// 破棄.
	};

	public State	state = State.None;	// アイテムの状態.

	// ---------------------------------------------------------------- //

	public float	timer = 0.0f;				// アイテムが作られてからの時間.
	public float	timer_prev;

	private struct Billboard {

		public bool		is_enable;				// ビルボード？.
		public float	roll;					// Z軸周りの回転.
	};

	private bool		is_pickable = true;		// 拾える？（成長中は拾えない）.
	private Billboard	billboard;

	private Vector3		initial_position = Vector3.zero;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.myCamera  = GameObject.FindGameObjectWithTag("MainCamera");

		this.timer       = -1.0f;
		this.timer_prev  = -1.0f;

		this.billboard.is_enable = false;
		this.billboard.roll      = 0.0f;

		// ３Dモデルのアイテムが出来るまでは必ずビルボードで.
		this.setBillboard(true);

		this.initial_position = this.transform.position;

		this.behavior.start();
	}
	
	void	Update()
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

		if (this.behavior != null) {
			this.behavior.execute();
		}

		// ビルボードのときは、カメラの方に向ける.
		if(this.billboard.is_enable) {

			this.transform.rotation = this.myCamera.transform.rotation;
		}
	}

	// ================================================================ //
	// ビヘイビアー用のコマンド.

	public void		cmdSetPickable(bool is_pickable)
	{
		this.is_pickable = is_pickable;
	}

	// ================================================================ //

	// 拾われる.

	public void		startPicked()
	{
		this.behavior.onPicked();
	}

	// リスポーンする.
	public void		startRespawn()
	{
		this.transform.position = this.initial_position;

		this.timer       = -1.0f;
		this.timer_prev  = -1.0f;

		this.behavior.onRespawn();
	}

	// 削除する（拾われた後）.
	public void		vanish()
	{
		GameObject.Destroy(this.gameObject);
	}

	// ================================================================ //

	// 拾える？.
	public bool		isPickable()
	{
		return(is_pickable && state == State.None);
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
}
