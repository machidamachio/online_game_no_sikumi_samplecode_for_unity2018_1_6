using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


// レベルごとのシーケンスモジュール　基底クラス.
public class SequenceBase : MonoBehaviour {

	public virtual void		createDebugWindow(dbwin.Window window) {}	// デバッグウインドウ生成時に呼ばれる.

	public virtual void		beforeInitializeMap() {}	// マップ生成直前に呼ばれる.
	public virtual void		start() {}					// レベル開始時に呼ばれる.
	public virtual void		execute() {}				// 毎フレーム呼ばれる.

	public virtual bool	isFinished() { return(false); }

	public SequenceBase		parent = null;
	public SequenceBase		child = null;

};

// ゲームのシーケンス.
public class GameRoot : MonoBehaviour {

	// ================================================================ //

	public string	owner = "";					// この村（シーン）のオーナーのアカウント名.

	public bool		is_host = true;				// ローカルプレイヤーが、現在表示中のシーン（村）のオーナー？.

	// ================================================================ //
	// 吹き出し用（どこかに移動予定）.

	public Texture texture_main    = null;
	public Texture texture_belo    = null;
	public Texture texture_kado_lu = null;
	public Texture texture_kado_ru = null;
	public Texture texture_kado_ld = null;
	public Texture texture_kado_rd = null;

	public Texture	title_image = null;					// タイトル画面.

	// ================================================================ //

	public enum STEP {

		NONE = -1,

		IDLE = 0,
		GAME,				// プレイ中.
		RELOAD,				// リロード（デバッグ用）.

		ERROR,				// 通信エラー.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	private float		scene_timer = 0.0f;

	public	bool[]	controlable = null;

	public	struct DebugFlag {

		public	bool	play_bgm;
	};
	public	DebugFlag	debug_flag;

	protected string	next_scene = "";				// 次にロードするシーン.

	// ================================================================ //
	// マップ生成関係のパラメータ.
	
	public MapInitializer 		map_initializer;		//< Reference to map creator object.
	public SequenceBase			sequence;				// レベルごとのシーケンスモジュール.

	protected Network			network;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
		// シーケンスモジュールが設定されてなかったら、デフォルトのものをくっつけておく.
		if(this.sequence == null) {

			this.sequence = this.gameObject.AddComponent<SequenceBase>();
		}

		// Networkクラスのコンポーネントを取得.
		GameObject	obj = GameObject.Find("Network");

		if(obj != null) {

			this.network = obj.GetComponent<Network>();
		}
	}

	void	Start()
	{
		this.step.set_next(STEP.GAME);

		this.controlable = new bool[4];

		for(int i = 0;i < 4;i++) {

			this.controlable[i] = true;
		}

		//

		this.debug_flag.play_bgm = false;

		dbwin.root();

		if(dbwin.root().getWindow("game") == null) {

			this.create_debug_window();
		}
		if(dbwin.root().getWindow("player") == null) {

			this.create_debug_window_player();
		}
	#if false
		PseudoRandom.Plant	plant = PseudoRandom.get().createPlant("test0");
		plant = PseudoRandom.get().createPlant("test1");

		for(int i = 0;i < 16;i++) {

			Debug.Log(plant.getRandom().ToString());
		}
	#endif
	}
	
	void	Update()
	{
		this.scene_timer += Time.deltaTime;

		// エラーハンドリング
		NetEventHandling();

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.GAME:
			{
				if(this.next_scene != "") {

					if(this.network != null) {

						this.network.ClearReceiveNotification();
					}
					SceneManager.LoadScene(this.next_scene);
					this.next_scene = "";
					this.step.set_next(STEP.IDLE);
				}
			}
			break;
		}
				
		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.GAME:
				{
					SoundManager.get().playBGM(Sound.ID.TKJ_BGM01);

					this.sequence.beforeInitializeMap();

					this.map_initializer.initializeMap(this);

					LevelControl.get().onFloorCreated();

					this.sequence.start();
				}
				break;

				case STEP.RELOAD:
				{
					SceneManager.LoadScene("GameScene");
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.GAME:
			{
				// レベルごとのシーケンスモジュール.
				this.sequence.execute();
			}
			break;

		}

		// ---------------------------------------------------------------- //

	}

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

		switch (this.step.get_current()) {
			case STEP.ERROR:
			{
				NotifyError();
			}
			break;
		}
	}

	// ================================================================ //

	// シーンをロードする.
	public void	setNextScene(string next_scene)
	{
		this.next_scene = next_scene;
	}

	// ケーキバイキング（ボス戦のあとのおまけ）中？.
	public bool	isNowCakeBiking()
	{
		bool	ret = false;

		do {

			if(this.sequence == null) {
	
				break;
			}

			BossLevelSequence	boss_seq = this.sequence as BossLevelSequence;

			if(boss_seq == null) {

				break;
			}

			ret = boss_seq.isNowCakeBiking();

		} while(false);

		return(ret);
	}

	// お使いの端末がホスト？.
	public bool	isHost()
	{
		bool	ret = true;

		if(this.network != null) {

			ret = GlobalParam.get().is_host;

		} else {

			ret = true;
		}

		return(ret);
	}

	// ネットプレイヤーと通信がつながってる？.
	public bool		isConnected(int global_account_index)
	{
		bool	ret = false;

		if(this.network != null) {

			int node = this.network.GetClientNode(global_account_index);

			ret = this.network.IsConnected(node);

		} else {

			// デバッグ用.
			if(global_account_index == GlobalParam.get().global_account_id) {

				ret = true;

			} else {

				ret = GlobalParam.get().db_is_connected[global_account_index];
			}
		}

		return(ret);
	}

	// ローカルプレイヤーを作る.
	public void		createLocalPlayer()
	{
		PartyControl.get().createLocalPlayer(GlobalParam.get().global_account_id);
	}

	// リモートプレイヤーを作る.
	public void		createNetPlayers()
	{
		Debug.Log("Create Remote players");

		for(int i = 0;i < NetConfig.PLAYER_MAX;i++) {

			// ローカルプレイヤーはスキップ.
			if(i == GlobalParam.get().global_account_id) {

				continue;
			}

			Debug.Log("Create Remote players[" + i + "] " + this.isConnected(i));

			if(!this.isConnected(i)) {

				continue;
			}
			
			if(this.network != null) {

				PartyControl.get().createNetPlayer(i);

			} else {

				PartyControl.get().createFakeNetPlayer(i);
			}
		}

		if(this.network == null) {

			for(int i = 0;i < PartyControl.get().getFriendCount();i++) {
	
				chrBehaviorFakeNet	friend = PartyControl.get().getFriend(i) as chrBehaviorFakeNet;
	
				if(friend == null) {
	
					continue;
				}
	
				friend.in_formation = true;
			}
		}
	}

	public void		restartGameScane()
	{
		this.step.set_next(STEP.RELOAD);
	}

	// 敵種のゲームオブジェクトの思考（ビヘイビア）と制御（コントローラ）にポーズを設定・解除
	public void		setEnemyPause(bool newPause)
	{
		foreach (GameObject gobj in GameObject.FindGameObjectsWithTag("Enemy")) {
			gobj.GetComponent<chrControllerEnemyBase>().SetPause(newPause);
			gobj.GetComponent<chrBehaviorEnemy>().SetPause(newPause);
		}
		foreach (GameObject gobj in GameObject.FindGameObjectsWithTag("EnemyLair")) {
			gobj.GetComponent<chrControllerEnemyBase>().SetPause(newPause);
			gobj.GetComponent<chrBehaviorEnemy>().SetPause(newPause);
		}
		foreach (GameObject gobj in GameObject.FindGameObjectsWithTag("Boss")) {
			gobj.GetComponent<chrControllerEnemyBase>().SetPause(newPause);
			gobj.GetComponent<chrBehaviorEnemy>().SetPause(newPause);
		}
		foreach (GameObject gobj in GameObject.FindGameObjectsWithTag("Enemy Bullet")) {
			gobj.GetComponent<EnemyBulletControl>().SetPause(newPause);
		}
	}
	
	
	// ================================================================ //
	
	
	public void NetEventHandling()
	{
		if (network == null) {
			return;
		}

		NetEventState state = network.GetEventState();
		
		if (state == null) {
			return;
		}
		
		switch (state.type) {
		case NetEventType.Connect:
			Debug.Log("[CLIENT]connect event handling:" + state.node);
			break;
			
		case NetEventType.Disconnect:
			Debug.Log("[CLIENT]disconnect event handling:" + state.node);
			DisconnectEventProc(state.node);
			break;
		}
	}


	// 切断に関するイベント処理.
	private void DisconnectEventProc(int node)
	{
		int global_index = network.GetPlayerIdFromNode(node);
		
		if (node == network.GetServerNode () ||
			global_index == 0) {
			// サーバかホストと切断したらゲーム終了.
			this.step.set_next (STEP.ERROR);
		} else {
			Debug.Log("[CLIENT]disconnect character:" + global_index);

			PartyControl.get ().deleteNetPlayer (global_index);
		}
	}

	// エラー通知.
	private void NotifyError()
	{
		GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
		style.normal.textColor = Color.white;
		style.fontSize = 25;
		
		float sx = 450;
		float sy = 200;
		float px = Screen.width / 2 - sx * 0.5f;
		float py = Screen.height / 2 - sy * 0.5f;

		string message = "通信エラーが発生しました.\nゲームを終了します.\n\nぼたんをおしてね.";
		if (GUI.Button (new Rect (px, py, sx, sy), message, style)) {
			this.step.set_next(STEP.NONE);
			if (GlobalParam.get().is_host) {
				network.StopGameServer();
			}
			network.StopServer();
			network.Disconnect();

			GameObject.Destroy(network);

			SceneManager.LoadScene("TitleScene");
		}
	}

	// ================================================================ //

	// デバッグウインドウを作る（プレイヤー関連）.
	protected void		create_debug_window_player()
	{
		var		window = dbwin.root().createWindow("player");

		window.createButton("やられた")
			.setOnPress(() =>
			{
				var		player = PartyControl.getInstance().getLocalPlayer();

				player.control.causeDamage(100.0f, -1);
			});

		window.createButton("アイス食べ過ぎ")
			.setOnPress(() =>
			{
				var		player = PartyControl.getInstance().getLocalPlayer();

				if(!player.isNowJinJin()) {

					player.startJinJin();

				} else {

					player.stopJinJin();
				}	
			});

		window.createButton("クリームまみれテスト")
			.setOnPress(() =>
			{
				var		player = PartyControl.getInstance().getLocalPlayer();

				if(!player.isNowCreamy()) {

					player.startCreamy();

				} else {

					player.stopCreamy();
				}	
			});

		window.createButton("体力回復テスト")
			.setOnPress(() =>
			{
				var		player = PartyControl.getInstance().getLocalPlayer();

				if(!player.isNowHealing()) {

					player.startHealing();

				} else {

					player.stopHealing();
				}	
			});

		window.createButton("アイスあたりテスト")
			.setOnPress(() =>
			{
				window.close();
				EventRoot.get().startEvent<EventIceAtari>();
			});

		window.createButton("武器チェンジ")
			.setOnPress(() =>
			{
				var		player = PartyControl.getInstance().getLocalPlayer();
	
				SHOT_TYPE	shot_type = player.getShotType();

				shot_type = (SHOT_TYPE)(((int)shot_type + 1)%((int)SHOT_TYPE.NUM));

				player.changeBulletShooter(shot_type);
			});
	}

	// デバッグウインドウを作る（いろいろ）.
	protected void		create_debug_window()
	{
		var		window = dbwin.root().createWindow("game");

		window.createCheckBox("アカウント１", GlobalParam.get().db_is_connected[0])
			.setOnChanged((bool is_checked) =>
			{
				GlobalParam.get().db_is_connected[0] = is_checked;
			});			
		window.createCheckBox("アカウント２", GlobalParam.get().db_is_connected[1])
			.setOnChanged((bool is_checked) =>
			{
				GlobalParam.get().db_is_connected[1] = is_checked;
			});			
		window.createCheckBox("アカウント３", GlobalParam.get().db_is_connected[2])
			.setOnChanged((bool is_checked) =>
			{
				GlobalParam.get().db_is_connected[2] = is_checked;
			});			
		window.createCheckBox("アカウント４", GlobalParam.get().db_is_connected[3])
			.setOnChanged((bool is_checked) =>
			{
				GlobalParam.get().db_is_connected[3] = is_checked;
			});			

		window.createCheckBox("BGM", this.debug_flag.play_bgm)
			.setOnChanged((bool is_checked) =>
			{
				this.debug_flag.play_bgm = is_checked;

				if(this.debug_flag.play_bgm) {

					SoundManager.get().playBGM(Sound.ID.TKJ_BGM01);

				} else {

					SoundManager.get().stopBGM();
				}
			});

		/*window.createButton("あきた")
			.setOnPress(() =>
			{
				dbwin.console().print("hoge");
				//this.next_step = STEP.TO_TITLE;
			});*/
#if false
		window.createButton("敵作る")
			.setOnPress(() =>
			{
				dbwin.console().print("fuga");

				/*GameObject		go =  GameObject.FindGameObjectWithTag("EnemyLair");
	
				if(go != null) {
	
					chrBehaviorEnemyLair0	lair = go.GetComponent<chrBehaviorEnemyLair0>();
	
					lair.createEnemy();
				}*/
			});

		window.createButton("敵をい～っぱい作る")
			.setOnPress(() =>
			{
				EnemyRoot.getInstance().createManyEnemies();
			});

		window.createButton("メガクラッシュ")
			.setOnPress(() =>
			{
				EnemyRoot.getInstance().debugCauseDamageToAllEnemy();
			});
#endif
		window.createButton("メガメガクラッシュ")
			.setOnPress(() =>
			{
				EnemyRoot.getInstance().debugCauseVanishToAllEnemy();
			});
		window.createButton("ジェネレーターを探す")
			.setOnPress(() =>
			{
				var		enemies = EnemyRoot.get().getEnemies();

				var		lairs = enemies.FindAll(x => (x.behavior as chrBehaviorEnemy_Lair) != null);

				foreach(var lair in lairs) {

					Debug.Log(lair.name);
				}
			});
#if false
		window.createButton("みんなにダメージ")
			.setOnPress(() =>
			{
				var		players = PartyControl.getInstance().getPlayers();
	
				foreach(var player in players) {
	
					player.controll.vital.causeDamage(10.0f);
				}
			});


		window.createButton("なぐる")
			.setOnPress(() =>
			{
				var		player = PartyControl.getInstance().getLocalPlayer();
	
				player.controll.cmdSetMotion("m003_attack", 1);
	
				var		enemies = EnemyRoot.getInstance().getEnemies();
	
				foreach(var enemy in enemies) {
	
					enemy.cmdSetMotion("m003_attack", 1);
				}
			});

		window.createButton("魔法")
			.setOnPress(() =>
			{
				var		player = PartyControl.getInstance().getLocalPlayer();

				player.controll.cmdSetMotion("m004_use", 1);
			});

		window.createButton("いてっ")
			.setOnPress(() =>
			{
				var		player = PartyControl.getInstance().getLocalPlayer();
	
				player.controll.causeDamage(0.0f);

				var		enemies = EnemyRoot.getInstance().getEnemies();
	
				foreach(var enemy in enemies) {
	
					enemy.cmdSetMotion("m005_damage", 1);
				}
			});
#endif
		window.createButton("ポーズ設定")
			.setOnPress(() =>
            {
				this.setEnemyPause(true);
			});

		window.createButton("ポーズ解除")
			.setOnPress(() =>
            {
				this.setEnemyPause(false);
			});

		this.sequence.createDebugWindow(window);
	}

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
