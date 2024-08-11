// 1台の端末で動作させる場合に定義します.
//#define UNUSE_MATCHING_SERVER

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Net;
using System.Threading;

// タイトル画面のシーケンス.
public class TitleControl : MonoBehaviour {

	public Texture	title_image = null;					// タイトル画面.

	public const bool	is_single = false;				// シングルプレイ？（デバッグ用）.

	// ================================================================ //

	public enum STEP {

		NONE = -1,

		WAIT = 0,			// 入力まち.
		MATCHING,			// マッチング.
		WAIT_MATCHING,		// マッチング待ち.
		SERVER_START,		// 待ち受け開始.
		SERVER_CONNECT,		// ゲームサーバへ接続.
		CLIENT_CONNECT,		// クライアント間の接続.
		PREPARE,			// 各種準備.
		CONNECTION,			// 各種接続.

#if UNUSE_MATCHING_SERVER
		WAIT_SYNC,			// 同期待ち.
#endif

		GAME_START,			// ゲームはじめ.

		ERROR,				// エラー発生.
		WAIT_RESTART,		// エラーからの復帰待ち.

		NUM,
	};

	public STEP				step      = STEP.NONE;
	public STEP				next_step = STEP.NONE;

	private float			step_timer = 0.0f;

	private MatchingClient	m_client = null;

	private Network			m_network = null;
	
	private string			m_serverAddress = "";

#if UNUSE_MATCHING_SERVER
	private bool			m_syncFlag = false;
#endif

	// ホストフラグ.
	private bool			m_isHost = false;

	// エラーメッセージ.
	private string			m_errorMessage = ""; 

#if UNUSE_MATCHING_SERVER
	private int count_ = 0;
#endif

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.step      = STEP.NONE;
		this.next_step = STEP.WAIT;

		GlobalParam.getInstance().fadein_start = true;

		if(!TitleControl.is_single) {

	#if true
			this.m_serverAddress = "";
	
			// ホスト名を取得する.
			m_serverAddress = GetServerIPAddress();
	#endif	
			GameObject obj = GameObject.Find("Network");
			if (obj == null) {
				obj = new GameObject ("Network");
			}
	
			if (m_network == null) {
				m_network = obj.AddComponent<Network>();
				if (m_network != null) {
					DontDestroyOnLoad(m_network);
				}
			}
		}
	}
	
	void	Update()
	{
		// エラーハンドリング
		NetEventHandling();

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
	
			case STEP.MATCHING:
				{
#if !UNUSE_MATCHING_SERVER
					int serverNode = -1;
					if (m_network != null) {
						serverNode = m_network.Connect(m_serverAddress, NetConfig.MATCHING_SERVER_PORT, Network.ConnectionType.Reliable);
						if (serverNode >= 0) {

							GameObject obj = new GameObject("MatchingClient");
							m_client = obj.AddComponent<MatchingClient>();
							if (m_client == null) {
								// エラーに強制移行.
								m_errorMessage = "マッチングを開始できませんでした.\n\nぼたんをおしてね.";
								this.step = STEP.ERROR;
							}
							m_client.SetServerNode(serverNode);

						}
						else {
							// エラー.
							m_errorMessage = "マッチングサーバに\n接続できませんでした.\n\nぼたんをおしてね.";
							this.step = STEP.ERROR;
						}
					}
					else {
						// エラー.
						m_errorMessage = "通信を開始できませんでした.\n\nぼたんをおしてね.";
						this.step = STEP.ERROR;
					}
#else
					GameObject obj = new GameObject("MatchingClient");
					m_client = obj.AddComponent<MatchingClient>();
					if (m_client == null) {
						// エラーに強制移行.
						m_errorMessage = "マッチングを開始できませんでした.\n\nぼたんをおしてね.";
						this.step = STEP.ERROR;
					}
#endif
				}
				break;

			case STEP.SERVER_START:
				{
					m_network.ClearReceiveNotification();

					Debug.Log("Launch Listening socket.");
					// 同一の端末で実行できるようにポート番号をずらしています.
					// 別々の端末で実行する場合はポート番号が同じものを使います.
					int port = NetConfig.GAME_PORT + GlobalParam.getInstance().global_account_id;
					bool ret = m_network.StartServer(port, NetConfig.PLAYER_MAX, Network.ConnectionType.Unreliable);
					if (m_isHost) {
						// ゲームサーバ起動.
						int playerNum = m_client.GetMemberNum();
						ret &= m_network.StartGameServer(playerNum);
					}
					if (ret == false) {
						// エラーに強制移行.
						m_errorMessage = "ゲームの通信を開始できませんでした.\n\nぼたんをおしてね.";
						this.step = STEP.ERROR;
					}
				}
				break;

			case STEP.SERVER_CONNECT:
				{
					// ホスト名を取得する.
					if (m_isHost) {
						m_serverAddress = GetServerIPAddress();
					}
					// ゲームサーバへ接続.
					Debug.Log("Connect to GameServer.");
					int serverNode = m_network.Connect(m_serverAddress, NetConfig.GAME_SERVER_PORT, Network.ConnectionType.Reliable);
					if (serverNode >= 0) {
						m_network.SetServerNode(serverNode);
					}
					else {
						// エラーに強制移行.
						m_errorMessage = "ゲームサーバと通信できませんでした.\n\nぼたんをおしてね.";
						this.step = STEP.ERROR;
					}
				}
				break;
				
			case STEP.CLIENT_CONNECT:
				{
					Debug.Log("Connect to host.");
					MatchingClient.MemberList[] list = m_client.GetMemberList();
					int playerNum = m_client.GetMemberNum();
					for (int i = 0; i < playerNum; ++i) {
						// 同一端末で実行する時は専用に付与したプレイヤーIDで判別します.
						// 別の端末で実行するときはIPアドレスで判別できます.
						// サンプルコードでは両方が対応できるプレイヤーIDで判別しています.
						if (m_client.GetPlayerId() == i) {
							// 自分自身は接続しない
							continue;
						}
						if (list[i] == null) {
							continue;
						}
						// 同一の端末で実行できるようにポート番号をずらしています.
						// 別々の端末で実行する場合はポート番号が同じものを使います.
						int port = NetConfig.GAME_PORT + i;
						string memberAddress = list[i].endPoint.Address.ToString();
						int clientNode = m_network.Connect(memberAddress, port, Network.ConnectionType.Unreliable);

						if (clientNode >= 0) {
							m_network.SetClientNode(i, clientNode);
						}
						else {
							// エラーに強制移行.
							m_errorMessage = "ゲームを開始できませんでした.\n\nぼたんをおしてね.";
							this.step = STEP.ERROR;
						}
					}
				}
				break;

#if UNUSE_MATCHING_SERVER
			case STEP.WAIT_SYNC:
				{
					CharEquipment equip = new CharEquipment();
					
					equip.globalId = GlobalParam.get().global_account_id;
					equip.shotType = (int)SHOT_TYPE.NEGI;

					EquipmentPacket packet = new EquipmentPacket(equip);
					int serverNode = m_network.GetServerNode();
					m_network.SendReliable<CharEquipment>(serverNode, packet);
				}
				break;
#endif

			case STEP.GAME_START:
				{
					GlobalParam.getInstance().fadein_start = true;
					SceneManager.LoadScene("WeaponSelectScene");
				}
				break;

				
			case STEP.WAIT_RESTART:
				{
					if (m_isHost) {
						m_network.StopGameServer();
					}
					m_network.StopServer();
					m_network.Disconnect();
				}
				break;
			}
			this.step_timer = 0.0f;
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step) {

			case STEP.WAIT:
			{
			}
			break;
				
			case STEP.MATCHING:
			{
				this.next_step = STEP.WAIT_MATCHING;
			}
			break;

			case STEP.WAIT_MATCHING:
			{
#if !UNUSE_MATCHING_SERVER
				if (m_client != null && m_client.IsFinishedMatching()) {
				
					GlobalParam.get().global_account_id = m_client.GetPlayerId();

					m_isHost = m_client.IsRoomOwner();
#else
				{
#endif
					if (m_isHost) {
						GlobalParam.get().is_host = true;
					}

					this.next_step = STEP.SERVER_START;
				}
			}
			break;

			case STEP.SERVER_START:
			{
			// サーバが起動するのを待ちます.
#if UNUSE_MATCHING_SERVER
				if (this.step_timer > 5.0f){
#else
				if (this.step_timer > 3.0f){
#endif
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
#if UNUSE_MATCHING_SERVER
				this.next_step = STEP.WAIT_SYNC;
#else
				this.next_step = STEP.GAME_START;
#endif
			}
			break;
	
#if UNUSE_MATCHING_SERVER
			case STEP.WAIT_SYNC:
			{
				// クライアント同士が接続するのを待ちます.
				// サンプルコードでは待ち時間を入れることで接続するのを待っています.
				// 本来はUDPで接続処理を書いてコネクションを張れるようにするほうがよいでしょう.

				bool isConnected = true;
				string connect = "Connect:[sync:" + m_syncFlag;
				for (int i = 0; i < m_client.GetMemberNum(); ++i) {

					if (i == GlobalParam.get().global_account_id) {
						continue;
					}

					int node = m_network.GetClientNode(i);
					isConnected &= m_network.IsConnected(node);

					connect += "[" + i +"(" + node + "):" + m_network.IsConnected(node) + "] ";
				}
				//Debug.Log(connect);

				if (isConnected || this.step_timer > 10.0f) {
					this.next_step = STEP.GAME_START;
				}

				++count_;
			}
			break;
#endif

			case STEP.GAME_START:
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

		// 背景画像.
		int x = Screen.width/2 - 80;
		int y = Screen.height/2 - 20;

		GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), this.title_image);

		if (this.step == STEP.WAIT_RESTART) {
			NotifyError();
			return;
		}

		if (m_client != null &&
			this.step >= STEP.WAIT_MATCHING && 
			this.step <= STEP.GAME_START) {
			m_client.OnGUIMatching();
		}

#if UNUSE_MATCHING_SERVER
		if (this.step == STEP.WAIT_SYNC) {
			GUIStyle style = new GUIStyle();
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.black;
			style.fontSize = 25;

			string label = "Connecting ";
			for (int i = 0; i < (count_ % 600) / 60; ++i) {
				label += ".";
			}
			
			GUI.Label(new Rect(x, y, 160, 20), label, style);
		}
#endif
				
		if (this.step != STEP.WAIT) {
			return;
		}

#if UNUSE_MATCHING_SERVER
		// スタンドアロンでマッチングサーバを経由しない時に使います.
		if(GUI.Button(new Rect(x, y, 160, 20), "とうふやさんで遊ぶ")) {
			m_isHost = true;
			GlobalParam.getInstance().global_account_id = 0;
			this.next_step = STEP.MATCHING;
		}
		if(GUI.Button(new Rect(x, y+30, 160, 20), "だいずさんで遊ぶ")) {
			GlobalParam.getInstance().global_account_id = 1;
			this.next_step = STEP.MATCHING;
		}
		if(GUI.Button(new Rect(x, y+60, 160, 20), "ずんださんで遊ぶ")) {
			GlobalParam.getInstance().global_account_id = 2;
			this.next_step = STEP.MATCHING;
		}
		if(GUI.Button(new Rect(x, y+90, 160, 20), "いりまめさんで遊ぶ")) {
			GlobalParam.getInstance().global_account_id = 3;
			this.next_step = STEP.MATCHING;
		}

		if(is_single) {

			if(this.next_step == STEP.MATCHING) {
			
				this.next_step = STEP.GAME_START;
			}
		}

#else
		{
			GUIStyle style = new GUIStyle();
			style.fontSize = 14;
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.black;
			
			GUI.Label(new Rect(x, y-50, 160, 20), "マッチングサーバアドレス", style);
			m_serverAddress = GUI.TextField(new Rect(x, y-30, 160, 20), m_serverAddress);
		}
				
		if(GUI.Button(new Rect(x, y, 160, 20), "げーむであそぶ")) {
			this.next_step = STEP.MATCHING;
		}
#endif
	}

	// ================================================================ //
	
	
	public void NetEventHandling()
	{
		if (m_network == null) {
			return;
		}

		NetEventState state = m_network.GetEventState();
		
		if (state == null) {
			return;
		}
		
		switch (state.type) {
		case NetEventType.Connect:
			Debug.Log("[CLIENT]connect event handling:" + state.node);
			break;
			
		case NetEventType.Disconnect:
			Debug.Log("[CLIENT]disconnect event handling:" + state.node);
				if (this.step < STEP.SERVER_START) {
				m_errorMessage = "サーバと切断しました.\n\nぼたんをおしてね.";
				this.step = STEP.ERROR;
			}
			break;
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
		
		if (GUI.Button (new Rect (px, py, sx, sy), m_errorMessage, style)) {
			this.step      = STEP.WAIT;
			this.next_step = STEP.NONE;
		}
	}


	// 端末のIPアドレスを取得.
	public string GetServerIPAddress() {

		string hostAddress = "";
		string hostname = Dns.GetHostName();

		// ホスト名からIPアドレスを取得する.
		IPAddress[] adrList = Dns.GetHostAddresses(hostname);

		for (int i = 0; i < adrList.Length; ++i) {
				string addr = adrList[i].ToString();
				string [] c = addr.Split('.');

				if (c.Length == 4) {
						hostAddress = addr;
						break;
				}
		}

		return hostAddress;
	}
}
