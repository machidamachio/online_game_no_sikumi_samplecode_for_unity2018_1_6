using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// タイトル画面のシーケンス.
public class TitleControl : MonoBehaviour {

	public GUISkin	gui_skin = null;
	public GUISkin	gui_skin_text = null;

	//private bool			avator1 = true;				// キャラクター true:"Folk1" false:"Folk2".

	public Texture	title_image = null;					// タイトル画面.

	// ================================================================ //

	public enum STEP {

		NONE = -1,

		WAIT = 0,			// 入力まち.
		SERVER_START,		// 待ち受け開始.
		SERVER_CONNECT,		// ゲームサーバへ接続.
		CLIENT_CONNECT,		// クライアント間の接続.
		PREPARE,			// 各種準備.
		GAME_START,			// ゲームはじめ.

		ERROR,				// エラー発生.
		WAIT_RESTART,		// エラーからの復帰待ち.

		NUM,
	};

	public STEP			step      = STEP.NONE;
	public STEP			next_step = STEP.NONE;

	private float		step_timer = 0.0f;

	private const int 	usePost = 50765;

	// 通信モジュールのコンポーネント.
	private Network		network_ = null;

	// ホストとして遊ぶフラグ.
	private bool		isHost = false;

	// ホストのIPアドレス.
	private string		hostAddress = "";

	// ゲーム開始所の同期情報の取得状態.
	private bool		isReceiveSyncGameData = false;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.step      = STEP.NONE;
		this.next_step = STEP.WAIT;

		GlobalParam.getInstance().fadein_start = true;

		GameObject obj = new GameObject("Network");
		if (obj != null) {
				network_ = obj.AddComponent<Network>();
				network_.RegisterReceiveNotification(PacketId.GameSyncInfo, OnReceiveSyncGamePacket);
		}

		// ホスト名を取得する.
		hostAddress = network_.GetServerIPAddress();
	}
	
	void	Update()
	{
		// ---------------------------------------------------------------- //
		// ステップ内の経過時間を進める.

		this.step_timer += Time.deltaTime;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.


		if(this.next_step == STEP.NONE) {

			switch(this.step) {
	
				case STEP.WAIT:
				{

				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.next_step != STEP.NONE) {

			this.step      = this.next_step;
			this.next_step = STEP.NONE;

			switch(this.step) {
	
			case STEP.SERVER_START:
				{
					Debug.Log("Launch Listening socket.");
					// 同一の端末で実行できるようにポート番号をずらしています.
					// 別々の端末で実行する場合はポート番号が同じものを使います.
					int port = isHost? NetConfig.GAME_PORT : NetConfig.GAME_PORT + 1;
					bool ret = network_.StartServer(port, Network.ConnectionType.UDP);
					if (isHost) {
						// ゲームサーバ起動.
						ret &= network_.StartGameServer();
					}
					if (ret == false) {
						// エラーに強制移行.
						Debug.Log("Error occured.");
						this.step = STEP.ERROR;
					}
				}
				break;

			case STEP.SERVER_CONNECT:
				{
					Debug.Log("Connect to gameserver.");
					// ホスト名を取得する.
					string serverAddress = this.hostAddress;
					if (isHost) {
						serverAddress = network_.GetServerIPAddress();
					}
					bool ret = network_.Connect(serverAddress, NetConfig.SERVER_PORT, Network.ConnectionType.TCP);
					if (ret == false) {
						// エラーに強制移行.
						Debug.Log("Error occured.");
						this.step = STEP.ERROR;
					}
				}
				break;

			case STEP.CLIENT_CONNECT:
				{
					Debug.Log("Connect to host.");
					// 同一の端末で実行できるようにポート番号をずらしています.
					// 別々の端末で実行する場合はポート番号が同じものを使います.
					int port = isHost? NetConfig.GAME_PORT + 1 : NetConfig.GAME_PORT;
					bool ret = network_.Connect(this.hostAddress, port, Network.ConnectionType.UDP);
					if (ret == false) {
						// エラーに強制移行.
						Debug.Log("Error occured.");
						this.step = STEP.ERROR;
					}
				}
				break;

			case STEP.GAME_START:
				{
					if (isHost) {
						GlobalParam.getInstance().account_name = "Toufuya";
						GlobalParam.getInstance().global_acount_id = 0;
					}
					else {
						GlobalParam.getInstance().account_name = "Daizuya";
						GlobalParam.getInstance().global_acount_id = 1;
					}
					GlobalParam.get().is_host = isHost;
					SceneManager.LoadScene("GameScene 1");
				}
				break;

			case STEP.WAIT_RESTART:
				{
					network_.Disconnect();
					network_.StopGameServer();
					network_.StopServer();
				}
				break;
			}
			this.step_timer = 0.0f;
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step) {

		case STEP.SERVER_START:
			{
				if (this.step_timer > 3.0f){
					this.next_step = STEP.SERVER_CONNECT;
				}
			}
			break;

		case STEP.SERVER_CONNECT:
			{
					this.next_step = STEP.CLIENT_CONNECT;
			}
			break;
			
		case STEP.CLIENT_CONNECT:
			{
				this.next_step = STEP.PREPARE;
			}
			break;

		case STEP.PREPARE:
			{
				// ゲーム開始前の同期情報を取得するまで待ちます.
				if (isReceiveSyncGameData) {
					this.next_step = STEP.GAME_START;
				}
			}
			break;
			
		case STEP.WAIT:
			{
			}
			break;

		case STEP.ERROR:
			{
				this.next_step = STEP.WAIT_RESTART;
			}
			break;

		case STEP.WAIT_RESTART:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

	void	OnGUI()
	{

		GUI.skin = this.gui_skin;

		// 背景画像.

		GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), this.title_image);

		int		x = Screen.width/2 - 100;
		int		y = Screen.height/2 - 100;

		if (this.step == STEP.WAIT_RESTART) {
			NotifyError();
		}
		else {
			// スタートボタン.

			x = 80;
			y = 220;

			this.hostAddress = GUI.TextField(new Rect(x, y, 150, 20), this.hostAddress);
			
			GUIStyle style = new GUIStyle();
			style.fontSize = 18;
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.white;
			GUI.Label(new Rect(x, y-20, 200.0f, 50.0f), "おともだちのアドレス", style);
			y += 25;

			if(GUI.Button(new Rect(x, y, 150, 20), "おともだちを待つ")) {
				this.isHost = true;
				this.next_step = STEP.SERVER_START;
			}

			if(GUI.Button(new Rect(x+160, y, 100, 20), "遊びに行く")) {
				this.isHost = false;
				this.next_step = STEP.SERVER_START;
			}
		}


		//
		GUI.skin = null;
	}

	// ---------------------------------------------------------------- //
	// 通信処理関数.

	// 
	public void OnReceiveSyncGamePacket(PacketId id, byte[] data)
	{
		Debug.Log("Receive GameSyncPacket.[TitleControl]");

		SyncGamePacket packet = new SyncGamePacket(data);
		SyncGameData sync = packet.GetPacket();
		
		for (int i = 0; i < sync.itemNum; ++i) {
			string log = "[CLIENT] Sync item pickedup " +
				"itemId:" + sync.items[i].itemId +
					" state:" + sync.items[i].state + 
					" ownerId:" + sync.items[i].ownerId;
			Debug.Log(log);
			
			ItemManager.ItemState istate = new ItemManager.ItemState();
			
			// アイテムの状態をマネージャーに登録.
			istate.item_id = sync.items[i].itemId;
			istate.state = (ItemController.State) sync.items[i].state;
			istate.owner = sync.items[i].ownerId;
			
			if (GlobalParam.get().item_table.ContainsKey(istate.item_id)) {
				GlobalParam.get().item_table.Remove(istate.item_id);
			}
			GlobalParam.get().item_table.Add(istate.item_id, istate);
		}

		isReceiveSyncGameData = true;
	}

	// エラー通知.
	void NotifyError()
	{
		GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
		style.normal.textColor = Color.white;
		style.fontSize = 25;
		
		float sx = 450;
		float sy = 200;
		float px = Screen.width / 2 - sx * 0.5f;
		float py = Screen.height / 2 - sy * 0.5f;
		
		string message = "ゲームを開始できませんでした.\n\nぼたんをおしてね.";
		if (GUI.Button (new Rect (px, py, sx, sy), message, style)) {
			this.step      = STEP.WAIT;
			this.next_step = STEP.NONE;
		}
	}
}
