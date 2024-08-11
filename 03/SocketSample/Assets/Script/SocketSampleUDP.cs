// UDPソケットのサンプルプログラム
//
// ■プログラムの説明
// ”Launch server.”ボタンを押すとサーバーの動作を行います.
// ”Connect to server”を押すとクライアントの動作をします.
//
// ●サーバーの動作
//   受信用ソケットを生成し、クライアントからのパケットを受信するまで待ち受けを続けます.
//   クライアントから送信されたメッセージを受信してConsoleウインドウに出力します.
//   メッセージを出力した後、ソケットを閉じて動作を終了します.
//   サーバーの動作は次の関数で処理しています.
//     CreateListener() 関数で受信用ソケットを生成します.
//     ReceiveMessage() 関数でクライアントからのメッセージを受信します.メッセージが受信されるまでこの関数で待ち続けます.
//     CloseListener() 関数で受信用ソケットを閉じて動作を終了します.
//
// ●クライアントの動作
//   UDP通信用のソケットを生成します.
//   テキストボックスに設定されたサーバーのアドレスへ"Hello, this is client."という文字列のメッセージを送信します.
//   メッセージ送信後、ソケットを閉じて動作を終了します.
//   クライアントは一連の動作を SendMessage() 関数で処理しています.
//

using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class SocketSampleUDP : MonoBehaviour
{
	
	// 接続先のIPアドレス.
	private string			m_address = "";
	
	// 接続先のポート番号.
	private const int 		m_port = 50765;

	// 通信用変数.
	private Socket			m_socket = null;

	// 状態.
	private State			m_state;

	// 状態定義.
	private enum State
	{
		SelectHost = 0,
		CreateListener,
		ReceiveMessage,
		CloseListener,
		SendMessage,
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
		case State.CreateListener:
			CreateListener();
			break;

		case State.ReceiveMessage:
			ReceiveMessage();
			break;

		case State.CloseListener:
			CloseListener();
			break;

		case State.SendMessage:
			SendMessage();
			break;

		default:
			break;
		}
	}

	// メッセージ受信用ソケットの生成.
	void CreateListener()
	{
		Debug.Log("[UDP]Start communication.");
		
		// ソケットを生成します.
		m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		// 使用するポート番号を割り当てます.
		m_socket.Bind(new IPEndPoint(IPAddress.Any, m_port));

		// UDPはTCPと異なり待ち受けの開始のListen関数を呼び出しません.
		// Bind関数を呼び出すことでパケットを受信できます.

		m_state = State.ReceiveMessage;
	}

	// 他の端末からのメッセージ受信.
	void ReceiveMessage()
	{
		// バッファのサイズはMTUの設定によって決まります.(MTU:1回に送信できる最大のデータサイズ)
		// イーサネットの最大MTUは1500bytesです.
		// この値はOSや端末などで異なるものですのでバッファのサイズは
		// 動作させる環境のMTUを調べて設定しましょう.
		byte[] buffer = new byte[1400];

		IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
		EndPoint senderRemote = (EndPoint)sender;

		// クライアントからのパケットの受信があるときは生成したソケット(m_socket)にデータが届きます.
		if (m_socket.Poll(0, SelectMode.SelectRead)) {
			int recvSize = m_socket.ReceiveFrom(buffer, SocketFlags.None, ref senderRemote);
			if (recvSize > 0) {
				string message = System.Text.Encoding.UTF8.GetString(buffer);
				Debug.Log(message);
				m_state = State.CloseListener;
			}
		}
	}
	
	// 待ち受け終了.
	void CloseListener()
	{	
		// 待ち受けを終了します.
		if (m_socket != null) {
			m_socket.Close();
			m_socket = null;
		}

		m_state = State.Endcommunication;

		Debug.Log("[UDP]End communication.");
	}

	// クライアント側の接続, 送信, 切断.
	void SendMessage()
	{
		Debug.Log("[UDP]Start communication.");

		// 送信用ソケットを生成します.
		m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		// サーバーへメッセージ送信します.
		byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Hello, this is client.");
		IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(m_address), m_port);
		// UDP通信は明示的な接続を行わない(コネクションレス)ためパケット送信時に送信先のIPアドレスとポート番号を
		// 指定して送信します.
		m_socket.SendTo(buffer, buffer.Length, SocketFlags.None, endpoint);

		// 切断します.
		m_socket.Shutdown(SocketShutdown.Both);
		m_socket.Close();

		m_state = State.Endcommunication;

		Debug.Log("[UDP]End communication.");
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
			m_state = State.CreateListener;
		}
		
		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		m_address = GUI.TextField(new Rect(20, 100, 200, 20), m_address);
		if (GUI.Button (new Rect (20,70,150,20), "Connect to server")) {
			m_state = State.SendMessage;
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
