// TCPソケットのサンプルプログラム
//
// ■プログラムの説明
// TCPプロトコルを使用したソケットのサンプルプログラムです.
// ”Launch server.”ボタンを押すとサーバーの動作を行います.
// ”Connect to server”を押すとクライアントの動作をします.
//
// ●サーバーの動作
//   リスニングソケットを生成し、待ち受けを開始します.
//   クライアントから接続されるまで待ち受けを続けます.
//   クライアントから送信されたメッセージを受信してConsoleウインドウに出力します.
//   メッセージを出力した後、リスニングソケットを閉じて動作を終了します.
//   サーバーの動作は次の関数で処理しています.
//     StartListener() 関数でリスニングソケットの生成、待ち受けを開始します.
//     AcceptClient() 関数でクライアントからの接続を待ちます.
//     ServerCommunication() 関数でクライアントからのメッセージを受信します.メッセージが受信されるまでこの関数で待ち続けます.
//     StopListener() 関数でリスニングソケットを閉じて動作を終了します.
//
// ●クライアントの動作
//   TCP通信用のソケットを生成し、テキストボックスに設定されたサーバのアドレスへ接続します.
//   サーバーと接続した後に、"Hello, this is client."という文字列のメッセージを送信します.
//   メッセージ送信後、サーバーと切断をして動作を終了します.
//   クライアントは一連の動作を ClientProcess() 関数で処理しています.
//

using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class SocketSampleTCP : MonoBehaviour
{
	// 接続先のIPアドレス.
	private string			m_address = "";
	
	// 接続先のポート番号.
	private const int 		m_port = 50765;

	// リスニングソケット.
	private Socket			m_listener = null;

	// 通信用変数
	private Socket			m_socket = null;

	// 状態.
	private State			m_state;

	// 状態定義.
	private enum State
	{
		SelectHost = 0,
		StartListener,
		AcceptClient,
		ServerCommunication,
		StopListener,
		ClientCommunication,
		Endcommunication,
	}


	// Use this for initialization
	void Start ()
	{
		m_state = State.SelectHost;

		// サーバーとなる端末のIPアドレスを表示するためにIPアドレスを取得保存します.
		m_address = GetServerIPAddress();
	}
	
	// Update is called once per frame
	void Update ()
	{
		switch (m_state) {
		case State.StartListener:
			StartListener();
			break;

		case State.AcceptClient:
			AcceptClient();
			break;

		case State.ServerCommunication:
			ServerCommunication();
			break;

		case State.StopListener:
			StopListener();
			break;

		case State.ClientCommunication:
			ClientProcess();
			break;

		default:
			break;
		}
	}

	// 待ち受け開始.
	void StartListener()
	{
		Debug.Log("Start server communication.");
		
		// ソケットを生成します.
		m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		// 使用するポート番号を割り当てます.
		m_listener.Bind(new IPEndPoint(IPAddress.Any, m_port));
		// 待ち受けを開始します.
		m_listener.Listen(1);

		m_state = State.AcceptClient;
	}

	// クライアントからの接続待ち.
	void AcceptClient()
	{
		// クライアントからの接続があるときはリスニングソケット(m_listener)にデータが届きます.
		// データが届いたことはPoll関数を使用することでチェックできます.
		if (m_listener != null && m_listener.Poll(0, SelectMode.SelectRead)) {
			// クライアントから接続されました.
			m_socket = m_listener.Accept();
			Debug.Log("[TCP]Connected from client.");
			m_state = State.ServerCommunication;
		}
	}

	// クライアントからのメッセージ受信.
	void ServerCommunication()
	{
		// バッファのサイズはMTUの設定によって決まります.(MTU:1回に送信できる最大のデータサイズ)
		// イーサネットの最大MTUは1500bytesです.
		// この値はOSや端末などで異なるものですのでバッファのサイズは
		// 動作させる環境のMTUを調べて設定しましょう.
		byte[] buffer = new byte[1400];

		int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);
		if (recvSize > 0) {
			string message = System.Text.Encoding.UTF8.GetString(buffer);
			Debug.Log(message);
			m_state = State.StopListener;
		}
	}

	// 待ち受け終了.
	void StopListener()
	{	
		// 待ち受けを終了します.
		if (m_listener != null) {
			m_listener.Close();
			m_listener = null;
		}

		m_state = State.Endcommunication;

		Debug.Log("[TCP]End server communication.");
	}

	// クライアント側の接続, 送信, 切断.
	void ClientProcess()
	{
		Debug.Log("[TCP]Start client communication.");

		// サーバーへ接続します.
		m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		// TCPのバッファをストリームではなくパケット毎に即時送信するように設定します.
		m_socket.NoDelay = true;
#if UNITY_STANDALONE_WIN
		m_socket.SendBufferSize = 0;
#endif
		m_socket.Connect(m_address, m_port);

		// メッセージを送信します.
		byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Hello, this is client.");	
		m_socket.Send(buffer, buffer.Length, SocketFlags.None);

		// 切断します.
		m_socket.Shutdown(SocketShutdown.Both);
		m_socket.Close();

		Debug.Log("[TCP]End client communication.");
	}

	void OnGUI()
	{
		if (m_state == State.SelectHost) {
			OnGUISelectHost();
		}
	}

	// サーバーまたはクライアントの動作を選択.
	void OnGUISelectHost()
	{
		if (GUI.Button (new Rect (20,40, 150,20), "Launch server.")) {
			m_state = State.StartListener;
		}
		
		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		m_address = GUI.TextField(new Rect(20, 100, 200, 20), m_address);
		if (GUI.Button (new Rect (20,70,150,20), "Connect to server")) {
			m_state = State.ClientCommunication;
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
