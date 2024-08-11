// ライブラリの動作確認プログラム
//
// ■プログラムの説明
// TransportTCPクラスとTransportUDPクラスの動作確認を行うプログラムです.
// "Launch server."ボタンを押すとサーバーの動作を行います.
// "Connect to server"を押すとクライアントの動作をします.
//
// ●サーバーの動作
//   待ち受けを開始して、クライアントからの接続を待ちます.
//   クライアントから接続されるまで待ち受けを続けます.
//   SocketSample{TCP, UDP}.csにある AcceptClient() 関数は Transport{TCP, UDP}.cs 内で
//   呼ばれていますのでサンプルプログラム側では呼び出す必要はありません.
//   クライアントから接続されると、Update() 関数で送信されたメッセージを受信してConsoleウインドウに出力します.
//   "Stop server"ボタンを押すと OnGUIServer() 関数内でサーバー動作を終了します.
//   サーバーの動作は次の関数で処理しています.
//     StartServer() 関数で待ち受けを開始します.
//     StopServer() 関数で待ち受けを終了します.
//
// ●クライアントの動作
//   "Connect to server"ボタンを押すと OnGUISelectHost() 関数内でテキストボックスに設定されたサーバのアドレスへ接続します.
//   "Send message"ボタンを押すと OnGUIClient() 関数内で"Hello, this is client."という文字列のメッセージを送信します.
//   "Disconnect"ボタンを押すとサーバーと切断をして一連の動作を終了します.
//

// TCPで通信する場合は定義を有効にしてください.UDPで通信する場合は"//"でコメントしてください.
#define USE_TRANSPORT_TCP

using UnityEngine;
using System.Collections;
using System.Net;


public class LibrarySample : MonoBehaviour {

	// 通信モジュール.
	public GameObject	transportTcpPrefab;
	public GameObject	transportUdpPrefab;

	// 通信用変数
	// TransportTCPクラスとTransportUDPクラスのメソッド(関数)の書式は両方のクラスで一致しています.
	// このように送受信を行うプロトコル(TCPまたはUDP)をクラスの宣言を切り替えるだけ変更できます.
#if USE_TRANSPORT_TCP
	TransportTCP		m_transport = null;
#else
	TransportUDP		m_transport = null;
#endif

	// 接続先のIPアドレス.
	private string		m_strings = "";

	// 接続先のポート番号.
	private const int 	m_port = 50765;

	private const int 	m_mtu = 1400;

	private bool 		isSelected = false;


	// Use this for initialization
	void Start ()
	{
		// Transportクラスのコンポーネントを取得.
#if USE_TRANSPORT_TCP
		GameObject obj = GameObject.Instantiate(transportTcpPrefab) as GameObject;
		m_transport = obj.GetComponent<TransportTCP>();
#else
		GameObject obj = GameObject.Instantiate(transportUdpPrefab) as GameObject;
		m_transport = obj.GetComponent<TransportUDP>();
#endif

		m_strings = GetServerIPAddress();
		Debug.Log(m_strings);
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (m_transport != null && m_transport.IsConnected() == true) {
			byte[] buffer = new byte[m_mtu];
			int recvSize = m_transport.Receive(ref buffer, buffer.Length);
			if (recvSize > 0) {
				string message = System.Text.Encoding.UTF8.GetString(buffer);
				Debug.Log(message);
			}
		}
	}

	void OnGUI()
	{
		if (isSelected == false) {
			OnGUISelectHost();
		}
		else {
			if (m_transport.IsServer() == true) {
				OnGUIServer();
			}
			else {
				OnGUIClient();
			}
		}
	}

	void OnGUISelectHost()
	{
#if USE_TRANSPORT_TCP
		if (GUI.Button (new Rect (20,40, 150,20), "Launch server.")) {
#else
		if (GUI.Button (new Rect (20,40, 150,20), "Launch Listener.")) {
#endif
			m_transport.StartServer(m_port, 1);
			isSelected = true;
		}
		
		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		m_strings = GUI.TextField(new Rect(20, 100, 200, 20), m_strings);
#if USE_TRANSPORT_TCP
		if (GUI.Button (new Rect (20,70,150,20), "Connect to server")) {
#else
		if (GUI.Button (new Rect (20,70,150,20), "Connect to terminal")) {
#endif
			m_transport.Connect(m_strings, m_port);
			isSelected = true;
			m_strings = "";
		}	
	}

	void OnGUIServer()
	{
#if USE_TRANSPORT_TCP
		if (GUI.Button (new Rect (20,60, 150,20), "Stop server")) {
#else
		if (GUI.Button (new Rect (20,60, 150,20), "Stop Listener")) {
#endif
			m_transport.StopServer();
			isSelected = false;
			m_strings = GetServerIPAddress();
		}
	}


	void OnGUIClient()
	{
		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		if (GUI.Button (new Rect (20,70,150,20), "Send message")) {
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Hello, this is client.");	
			m_transport.Send(buffer, buffer.Length);
		}

		if (GUI.Button (new Rect (20,100, 150,20), "Disconnect")) {
			m_transport.Disconnect();
			isSelected = false;
			m_strings = GetServerIPAddress();
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
