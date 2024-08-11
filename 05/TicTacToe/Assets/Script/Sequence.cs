// ゲーム全体のシーケンス制御
//
// ■プログラムの説明
// シーケンスプログラムは対戦相手との接続、ゲーム、切断に分かれています.
// 対戦相手との接続は OnUpdateSelectHost() 関数で行っています.
// ゲームは OnUpdateGame() 関数でを行っています. ゲーム本体のプログラムは TicTacToe.cs で処理しています.
// 切断は OnUpdateDisconnection() 関数で行っています.
// 

using UnityEngine;
using System.Collections;
using System.Net;

public class Sequence : MonoBehaviour {

	private Mode			m_mode;
	
	private string			serverAddress;
	
	private HostType		hostType;

	private const int 		m_port = 50765;

	private TransportTCP 	m_transport = null;

	private int				m_counter = 0;
	
	public GUITexture		bgTexture;
	public GUITexture		pushTexture;

	private static float WINDOW_WIDTH = 640.0f;
	private static float WINDOW_HEIGHT = 480.0f;

	enum Mode {
		SelectHost = 0,
		Connection, 
		Game,
		Disconnection,
		Error,
	};

	enum HostType {
		None = 0,
		Server,
		Client,
	};

	// 初期化.
	void Awake () {
		m_mode = Mode.SelectHost;
		hostType = HostType.None;
		serverAddress = "";

		// Networkクラスのコンポーネントを取得.
		GameObject obj = new GameObject("Network");
		m_transport  = obj.AddComponent<TransportTCP>();
		DontDestroyOnLoad(obj);

		// ホスト名を取得する.
		serverAddress = GetServerIPAddress();
	}

	void Update () {

		switch (m_mode) {
		case Mode.SelectHost:
			OnUpdateSelectHost();
			break;
			
		case Mode.Connection:
			OnUpdateConnection();
			break;
			
		case Mode.Game:
			OnUpdateGame();
			break;
			
		case Mode.Disconnection:
			OnUpdateDisconnection();
			break;
		}

		++m_counter;
	}


	void OnGUI()
	{
		switch (m_mode) {
		case Mode.SelectHost:
			OnGUISelectHost();
			break;
			
		case Mode.Connection:
			OnGUIConnection();
			break;
			
		case Mode.Game:
			break;
			
		case Mode.Disconnection:
			break;

		case Mode.Error:
			OnGUICError();
			break;
		}
	}
	
	
	// SeverまたはClientの選択画面
	void OnUpdateSelectHost()
	{

		switch (hostType) {
		case HostType.Server:
			{
				bool ret = m_transport.StartServer(m_port, 1);
				m_mode = ret? Mode.Connection : Mode.Error;
			}
			break;
			
		case HostType.Client: 
			{
				bool ret = m_transport.Connect(serverAddress, m_port);
				m_mode = ret? Mode.Connection : Mode.Error;
			}
			break;
			
		default:
			break;
		}
	}

	// 接続処理.
	void OnUpdateConnection()
	{
		if (m_transport.IsConnected() == true) {
			m_mode = Mode.Game;
			
			GameObject game = GameObject.Find("TicTacToe");
			game.GetComponent<TicTacToe>().GameStart();
		}
	}

	// ゲーム中.
	void OnUpdateGame()
	{
		GameObject game = GameObject.Find("TicTacToe");

		if (game.GetComponent<TicTacToe>().IsGameOver() == true) {
			m_mode = Mode.Disconnection;
		}
	}

	// 切断処理.
	void OnUpdateDisconnection()
	{
		switch (hostType) {
		case HostType.Server:
			m_transport.StopServer();
			break;
			
		case HostType.Client:
			m_transport.Disconnect();
			break;
			
		default:
			break;
		}

		m_mode = Mode.SelectHost;
		hostType = HostType.None;
		// ホストのIPアドレスを取得する.
		serverAddress = GetServerIPAddress();
	}

	// ホスト/クライアント選択画面.
	void OnGUISelectHost()
	{
		// 背景表示.
		DrawBg(true);

		if (GUI.Button (new Rect (20,290, 150,20), "対戦相手を待ちます")) {
			hostType = HostType.Server;
		}
		
		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		if (GUI.Button (new Rect (20,380,150,20), "対戦相手と接続します")) {
			hostType = HostType.Client;
		}

		Rect labelRect = new Rect(20, 410, 200, 30);
		GUIStyle style = new GUIStyle();
		style.fontStyle = FontStyle.Bold;
		style.normal.textColor = Color.white;
		GUI.Label(labelRect, "あいてのIPあどれす", style);
		labelRect.y -= 2;
		style.fontStyle = FontStyle.Normal;
		style.normal.textColor = Color.black;
		GUI.Label(labelRect, "あいてのIPアドレス", style);

		serverAddress = GUI.TextField(new Rect(20, 430, 200, 20), serverAddress);
	}
	
	// 接続処理中.
	void OnGUIConnection()
	{
		// 背景表示.
		DrawBg(false);

		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		GUI.Button (new Rect (84,335,160,20), "対戦相手をまっています");
	}

	// エラー表示.
	void OnGUICError()
	{
		// 背景表示.
		DrawBg(false);

		float px = Screen.width * 0.5f - 150.0f;
		float py = Screen.height * 0.5f;

		if (GUI.Button(new Rect(px, py, 300, 80), "接続できませんでした.\n\nぼたんをおしてね")) {
			m_mode = Mode.SelectHost;
			hostType = HostType.None;
		}
	}

	// 背景表示.
	void DrawBg(bool blink)
	{
		// 背景を描画します.
		Rect bgRect = new Rect(Screen.width / 2 - WINDOW_WIDTH * 0.5f, 
		                     Screen.height / 2 - WINDOW_HEIGHT * 0.5f, 
		                     WINDOW_WIDTH, 
		                     WINDOW_HEIGHT);
		Graphics.DrawTexture(bgRect, bgTexture.texture);

		// ぼたんおしてね.
		if (blink && m_counter % 120 > 20) {
			Rect pushRect = new Rect(84, 335, 220, 25);
			Graphics.DrawTexture(pushRect, pushTexture.texture);
		}
	}

	// 端末のIPアドレスを取得.
	public string GetServerIPAddress() {

		string hostAddress = "";
		string hostname = Dns.GetHostName();

		// ホスト名からIPアドレスを取得します.
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
