using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Net;
using System.Text;

// ゲームのシーケンス.
public class GameRoot : MonoBehaviour {

	// ================================================================ //

	public string	owner = "";					// この村（シーン）のオーナーのアカウント名.

	public bool		is_host = true;				// ローカルプレイヤーが、現在表示中のシーン（村）のオーナー？.

	// ================================================================ //
	// 

	public Texture	title_image = null;					// タイトル画面.

	// ================================================================ //

	public string	account_name_local = "";
	public string	account_name_net   = "";

	public chrController		local_player = null;
	public chrController		net_player   = null;

	// ================================================================ //

	public enum STEP {

		NONE = -1,

		GAME = 0,			// プレイ中.
		TO_TITLE,			// タイトル画面に戻る.
		VISIT,				// 他のプレイヤーの村に遊びに行く.
		WELCOME,			// 他のプレイヤーが遊びにきた.
		BYEBYE,				// 他のプレイヤーが帰る.
		GO_HOME,			// 自分の村に帰る.

		CHARACTER_CHANGE,	// キャラクターを変える（デバッグ用）.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	private float		scene_timer = 0.0f;

	private float		disp_timer = 0.0f;

	// 接続時のイベント処理フラグ.
	private bool		request_connet_event = false;
	// 切断時のイベント処理フラグ.
	private bool		request_disconnet_event = false;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.step.set_next(STEP.GAME);

		//

		dbwin.root();

		if(dbwin.root().getWindow("game") == null) {

			this.create_debug_window();
		}

		// イベント監視用.
		// Networkクラスのコンポーネントを取得.
		GameObject obj = GameObject.Find("Network");
		
		if(obj != null) {
			Network network = obj.GetComponent<Network>();
			network.RegisterReceiveNotification(PacketId.GoingOut, OnReceiveGoingOutPacket);
			network.RegisterEventHandler(OnEventHandling);
		}
	}
	
	void	Update()
	{
		this.scene_timer += Time.deltaTime;
		this.disp_timer -= Time.deltaTime;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.GAME:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.CHARACTER_CHANGE:
				{
					GlobalParam.get().account_name = this.account_name_net;
					GlobalParam.get().skip_enter_event = true;

					SceneManager.LoadScene("GameScene 1");
				}
				break;
	
				case STEP.GAME:
				{
					this.account_name_local = GlobalParam.get().account_name;
					this.account_name_net   = GameRoot.getPartnerAcountName(this.account_name_local);

					if(this.owner == this.account_name_local) {

						this.is_host = true;

					} else {

						// 他のプレイヤーの村に遊びにいったとき.
						this.is_host = false;
					}

					// プレイヤーを作る.
					this.local_player = CharacterRoot.get().createPlayerAsLocal(this.account_name_local);

					this.local_player.cmdSetPosition(Vector3.zero);

					if(GlobalParam.get().is_in_my_home == GlobalParam.get().is_remote_in_my_home) {

						this.net_player = CharacterRoot.get().createPlayerAsNet(this.account_name_net);

						this.net_player.cmdSetPosition(Vector3.left*1.0f);
					}

					// レベルデーター(level_data.txt) をよんで、 NPC/アイテムを配置する.
					MapCreator.get().loadLevel(this.account_name_local, this.account_name_net, !this.is_host);

					SoundManager.get().playBGM(Sound.ID.TFT_BGM01);

					//

					if(!GlobalParam.get().skip_enter_event) {

						EnterEvent	enter_event = EventRoot.get().startEvent<EnterEvent>();
			
						if(enter_event != null) {
					
							enter_event.setPrincipal(this.local_player.behavior as chrBehaviorPlayer);
							enter_event.setIsLocalPlayer(true);
						}
					}

					foreach (ItemManager.ItemState istate in GlobalParam.get().item_table.Values) {
						if (istate.state == ItemController.State.Picked) {
							// すでにアイテムを取得していたら持っていけるようにする.
							ItemManager.get().finishGrowingItem(istate.item_id);
							chrController controll = CharacterRoot.getInstance().findPlayer(istate.owner);
							if (controll != null) {
								QueryItemPick query = controll.cmdItemQueryPick(istate.item_id, false, true);
								if (query != null) {
									query.is_anon = true;
									query.set_done(true);
									query.set_success(true);
								}
							}
						}
					}

				}
				break;

				// 他のプレイヤーが遊びにきた.
				case STEP.WELCOME:
				{
					if(this.net_player == null) {

						EnterEvent	enter_event = EventRoot.get().startEvent<EnterEvent>();
		
						if(enter_event != null) {

							this.net_player = CharacterRoot.getInstance().createPlayerAsNet(this.account_name_net);
			
							this.net_player.cmdSetPosition(Vector3.left);
		
							enter_event.setPrincipal(this.net_player.behavior as chrBehaviorPlayer);
							enter_event.setIsLocalPlayer(false);
						}
					}
				}
				break;

				// 他のプレイヤーが帰る.
				case STEP.BYEBYE:
				{
					if(this.net_player != null) {

						LeaveEvent	leave_event = EventRoot.get().startEvent<LeaveEvent>();
		
						if(leave_event != null) {
			
							leave_event.setPrincipal(this.net_player.behavior as chrBehaviorPlayer);
							leave_event.setIsLocalPlayer(false);
						}
					}
				}
				break;

				case STEP.VISIT:
				{
					GlobalParam.get().is_in_my_home = false;
					SceneManager.LoadScene("GameScene 1");
				}
				break;

				case STEP.GO_HOME:
				{
					// 自分の村に帰る.
					GlobalParam.get().is_in_my_home = true;
					SceneManager.LoadScene("GameScene 1");
				}
				break;

				case STEP.TO_TITLE:
				{
					SceneManager.LoadScene("TitleScene");
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.GAME:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 通信によるイベント処理.

		// Networkクラスのコンポーネントを取得.
		GameObject go = GameObject.Find("Network");
		Network network = go.GetComponent<Network>();
		if (network != null) {
			if (network.IsCommunicating() == true) {
				if (request_connet_event) {
					// 接続イベントを発動する.
					GlobalParam.get().is_connected = true;
					request_connet_event = false;
				}
				else if (GlobalParam.get().is_connected == false) {
					// 接続した
					Debug.Log("Guest connected.");
					request_connet_event = true;
					disp_timer = 5.0f;
				}
			}
		}

		// 切断イベントを発動する.
		if (request_disconnet_event) {
			GlobalParam.get().is_disconnected = true;
			request_disconnet_event = false;
			// 切断時のイベント.
			disconnect_event();
		}
	
		// ---------------------------------------------------------------- //

		if(Input.GetKeyDown(KeyCode.Z)) {

			dbwin.console().print("Log テスト " + this.log_test_count);
			this.log_test_count++;
		}
	}
	private int 	log_test_count = 0;

	void	OnGUI()
	{
		// 背景画像.

		if(GlobalParam.getInstance().fadein_start) {

			float	title_alpha = Mathf.InverseLerp(1.0f, 0.0f, this.scene_timer);

			if(title_alpha > 0.0f) {

				GUI.color = new Color(1.0f, 1.0f, 1.0f, title_alpha);
				GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), this.title_image, ScaleMode.ScaleToFit, true);
			}
		}

		if (GlobalParam.get().is_disconnected) {
			GUI.Button(new Rect(Screen.width - 220.0f, Screen.height - 50.0f, 200.0f, 30.0f),
			           "おともだちと切断しました");
		}
		else if (GlobalParam.get().is_connected) {
			if (this.disp_timer > 0.0f) {
				string message = (GlobalParam.get().account_name == "Toufuya")? "おともだちがあそびに来ました" :
																				"おともだちとあそべます";
				// 待ち受けいしているプレイヤーにゲストが来たことを通知.
				GUI.Button(new Rect(Screen.width - 280.0f, Screen.height - 50.0f, 250.0f, 30.0f), message);
			}
		}
	}

	// ================================================================ //

	// 自分以外のプレイヤーのアカウント名をゲットする.
	public static string	getPartnerAcountName(string myname)
	{
		string	partner = "";

		if(myname == "Toufuya") {

			partner = "Daizuya";

		} else {

			partner = "Toufuya";
		}

		return(partner);
	}

	// 通信相手との接続状態.
	public bool isConnected()
	{
		return (GlobalParam.get().is_connected && !GlobalParam.get().is_disconnected);
	}

	// ================================================================ //

	public void OnReceiveGoingOutPacket(PacketId id, byte[] data)
	{
		GoingOutPacket packet = new GoingOutPacket(data);
		GoingOutData go = packet.GetPacket();

		Debug.Log("OnReceiveGoingOutPacket");
		if (GlobalParam.get().account_name == go.characterId) {
			// 自分自身はすでに行動済みなので処理しない.
			return;
		}

		if (GlobalParam.get().is_in_my_home) {
			// 自分の庭にいる.
			if (go.goingOut) {
				// おともだちが来た.
				this.step.set_next(STEP.WELCOME);	
				GlobalParam.get().is_remote_in_my_home = true;		
			}
			else {
				// おともだちが帰る
				this.step.set_next(STEP.BYEBYE);
				GlobalParam.get().is_remote_in_my_home = false;		
			}
		}
		else {
			// おともだちの庭にいる.
			if (go.goingOut) {
				// おともだちがいく.
				this.step.set_next(STEP.BYEBYE);
				GlobalParam.get().is_remote_in_my_home = true;		
			}
			else {
				// おともだちがもどる
				this.step.set_next(STEP.WELCOME);
				GlobalParam.get().is_remote_in_my_home = false;		
			}
		}
	}

	
	public void NotifyFieldMoving()
	{
		GameObject go = GameObject.Find("Network");
		if (go != null) {
			Network network = go.GetComponent<Network>();
			if (network != null) {
				GoingOutData data = new GoingOutData();
				
				data.characterId = GlobalParam.get().account_name;
				data.goingOut = GlobalParam.get().is_in_my_home;
				
				GoingOutPacket packet = new GoingOutPacket(data);
				network.SendReliable<GoingOutData>(packet);
			}
		}
	}

	// ================================================================ //
	

	public void OnEventHandling(NetEventState state)
	{
		switch (state.type) {
		case NetEventType.Connect:
			// 接続イベントはこの関数が登録される前に発生することがあるので.
			// 取り損ねることがあります.
			// このため, 接続状態を監視して接続のイベントを発生させます.
			break;

		case NetEventType.Disconnect:
			Debug.Log("Guest disconnected.");
			request_disconnet_event = true;
			break;
		}
	}

	// ================================================================ //

	protected void disconnect_event()
	{
		if (GlobalParam.get().is_in_my_home == false && 
		    GlobalParam.get().is_remote_in_my_home == false) {

			chrBehaviorPlayer	player = this.net_player.behavior as chrBehaviorPlayer;

			if(player != null) {

				if(!player.isNowHouseMoving()) {

					HouseMoveStartEvent	start_event = EventRoot.get().startEvent<HouseMoveStartEvent>();
			
					start_event.setPrincipal(player);
					start_event.setHouse(CharacterRoot.get().findCharacter<chrBehaviorNPC_House>("House1"));
				}
			}
		}
		else if(GlobalParam.get().is_in_my_home &&
		        GlobalParam.get().is_remote_in_my_home) {
			this.step.set_next(STEP.BYEBYE);	
		}
	}


	// ================================================================ //

	protected void		create_debug_window()
	{
		var		window = dbwin.root().createWindow("game");

		window.createButton("キャラ変える")
			.setOnPress(() =>
			{
				this.step.set_next(STEP.CHARACTER_CHANGE);
			});

		if(GlobalParam.get().is_in_my_home) {

			window.createButton("遊びに行く！")
				.setOnPress(() =>
				{
					LeaveEvent	leave_event = EventRoot.get().startEvent<LeaveEvent>();

					leave_event.setPrincipal(this.local_player.behavior as chrBehaviorPlayer);
					leave_event.setIsLocalPlayer(true);
				});

		} else {

			window.createButton("お家にかえる～")
				.setOnPress(() =>
				{
					LeaveEvent	leave_event = EventRoot.get().startEvent<LeaveEvent>();

					leave_event.setPrincipal(this.local_player.behavior as chrBehaviorPlayer);
					leave_event.setIsLocalPlayer(true);
				});
		}

		window.createButton("だれか来た！")
			.setOnPress(() =>
			{
				this.step.set_next(STEP.WELCOME);
			});

		window.createButton("ばいば～い")
			.setOnPress(() =>
			{
				this.step.set_next(STEP.BYEBYE);
			});

		window.createButton("出発イベントテスト")
			.setOnPress(() =>
			{
				LeaveEvent	leave_event = EventRoot.get().startEvent<LeaveEvent>();

				leave_event.setPrincipal(this.local_player.behavior as chrBehaviorPlayer);
				leave_event.setIsLocalPlayer(true);
				leave_event.setIsMapChange(false);

				window.close();
			});

		window.createButton("到着イベントテスト")
			.setOnPress(() =>
			{
				EnterEvent	enter_event = EventRoot.get().startEvent<EnterEvent>();

				enter_event.setPrincipal(this.local_player.behavior as chrBehaviorPlayer);

				window.close();
			});

		window.createButton("ひっこし開始イベントテスト")
			.setOnPress(() =>
			{
				HouseMoveStartEvent	start_event = EventRoot.get().startEvent<HouseMoveStartEvent>();

				start_event.setPrincipal(this.local_player.behavior as chrBehaviorPlayer);
				start_event.setHouse(CharacterRoot.get().findCharacter<chrBehaviorNPC_House>("House1"));

				window.close();
			});
		window.createButton("ひっこし終了イベントテスト")
			.setOnPress(() =>
			{
				HouseMoveEndEvent	end_event = EventRoot.get().startEvent<HouseMoveEndEvent>();

				end_event.setPrincipal(this.local_player.behavior as chrBehaviorPlayer);
				end_event.setHouse(CharacterRoot.get().findCharacter<chrBehaviorNPC_House>("House1"));

				window.close();
			});
	}

#if false
	void	WindowFunction(int id)
	{
		int		x = 10;
		int		y = 30;

		if(GUI.Button(new Rect(x, y, 100, 20), "あきた")) {

			this.next_step = STEP.TO_TITLE;
		}

		y += 30;

	}
#endif

	// ================================================================ //
	// インスタンス.

	private	static GameRoot	instance = null;

	public static GameRoot	getInstance()
	{
		if(GameRoot.instance == null) {

			GameRoot.instance = GameObject.Find("GameRoot").GetComponent<GameRoot>();
		}

		return(GameRoot.instance);
	}

	public static GameRoot	get()
	{
		return(GameRoot.getInstance());
	}
}
