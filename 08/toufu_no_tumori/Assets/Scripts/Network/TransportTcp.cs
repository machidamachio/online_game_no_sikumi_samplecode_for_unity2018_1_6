// TCP通信を行う通信モジュール
//
// ■プログラムの説明
// TCPプロトコルを用いたデータの送受信を行うモジュールです.
// データを受信するための動作をサーバーのように処理しています.
// 通信相手のデータを受信するためのサーバー処理を実行して待ち受けを行います.
// 通信相手が Connect() 関数を呼び出して接続したらお互いに送受信できる状態になります.
// 通信相手と Send() 関数、Recieve() 関数で双方向のデータ通信を行います.
// 
// ●待ち受けの動作
//   受信用ソケットを生成し、クライアントと送受信を行う送受信関数をゲームスレッドとは別のスレッド(通信スレッド)として起動します.
//   通信相手から送信されたメッセージを受信してゲームスレッドに渡すためにキューにバッファリングします.
//   待ち受けの動作は次の関数で処理しています.
//     StartServer() 関数でクライアントとの通信を開始します.
//     AcceptClient() 関数でクライアントとの接続を行います.
//     StopServer() 関数でクライアントとの通信を終了します.
//
// ●通信相手への接続の動作
//   Connect() 関数で待ち受けをしてる通信相手に接続を行います.
//   Disconnect() 関数で通信相手と切断します.
//
// ●送受信処理
//　　ゲームプログラムからデータを送受信を行うために　Send() 関数、Receive() 関数を使用します.
//   これらの関数はソケットによる送受信をしていません.
//   実際の送受信は通信スレッド側で行います.これはデータの祖受信によるオーバーヘッドでゲームの処理時間を消費しないようにしています.
//   ソケットによる送受信は DispatchSend() 関数、DispatchReceive() 関数で処理しています.
// ●イベント処理
//	  通信相手の接続や切断をゲームプログラムに通知するための仕組みをイベントハンドラーとして登録、削除をします.
//    ゲームプログラム側に EventHandler 型の関数 [例： void foo(NetEventState state) { ... } ]を定義します.
//    定義した関数を RegisterEventHandler() 関数、UnregisterEventHandler() 関数に渡してイベントハンドラーに登録、解除をします.
//    通信相手が接続、切断した時にイベントハンドラーに登録した関数が呼び出され、ゲームプログラムの関数がデリゲートからが呼び出されます.
//    イベント発生時の関数呼び出しは AcceptClient() 関数、Connect() 関数、Disconnect() 関数を参照してください.
//
 

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class TransportTcp {
	//
	// ソケット接続関連.
	//

	// リスニングソケット.
	private Socket			m_listener = null;
	
	// 通信用ソケット.
	private List<Socket>	m_socket = null;
	private int				m_port = -1;

	// サーバーフラグ.
	private bool 			m_isServer = false;
	
	// 接続フラグ.
	private	bool			m_isConnected = false;

	// 送信バッファ.
	private PacketQueue		m_sendQueue = new PacketQueue();
	
	// 受信バッファ.
	private PacketQueue		m_recvQueue = new PacketQueue();
	
	//
	// イベント関連のメンバ変数.
	//

	// イベント通知のデリゲート.
	public delegate void 	EventHandler(NetEventState state);
	// イベントハンドラー.
	private EventHandler	m_handler;
	

	// 送受信用のパケットの最大サイズ.
	// バッファのサイズはMTUの設定によって決まります.(MTU:1回に送信できる最大のデータサイズ)
	// イーサネットの最大MTUは1500bytesです.
	// この値はOSや端末などで異なるものですのでバッファのサイズは
	// 動作させる環境のMTUを調べて設定しましょう.
	private const int		m_packetSize = 1400;

	private System.Object 	lockObj = new System.Object();

	public TransportTcp()
	{
		// クライアントとの接続用ソケットリスト生成.
		m_socket = new List<Socket>();
	}

	// 待ち受け開始.
	public bool StartServer(int port)
	{
		Debug.Log("TransportTcp::StartServer called. port:" + port);

		try {
			// リスニングソケットを生成します.
			m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_listener.NoDelay = true;
			m_listener.Bind(new IPEndPoint(IPAddress.Any, port));
			m_listener.Listen(2);
			m_port = port;
		}
		catch {
			return false;
		}

		m_isServer = true;

		return true;
	}

	// 待ち受け終了.
	public void StopServer()
	{
		Debug.Log("TransportTcp::StopServer called.");

		Disconnect ();

		if (m_listener != null) {
			m_listener.Close();
			m_listener = null;
		}		

		m_isServer = false;
	}

    // 接続処理.
	public bool Connect(string address, int port)
	{
		try {
			lock (lockObj) {
				Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.NoDelay = true;
				socket.Connect(address, port);
				m_socket.Add(socket);
			}
		}
		catch (SocketException e) {
			Debug.Log(e.Message);
			return false;
		}

		m_isConnected = true;
		Debug.Log("TransportTcp::Connect called.");

		return true;
	}

	// 切断処理.
	public bool Disconnect()
	{
		if (m_socket != null) {
			lock (lockObj) {
				// ソケットをクローズします.
				foreach (Socket socket in m_socket) {
					try {
						socket.Shutdown(SocketShutdown.Both);
						socket.Close();
					}
					catch (SocketException e) {
						Debug.Log(e.Message);
					}
				}
				m_socket.Clear();
				m_socket = null;
			}
			
			// 切断を通知します.
			// ゲームアプリケーションは他のプレイヤーが切断したときにユーザーに通知するのがよいでしょう.
			// そのため切断したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			if (m_handler != null) {
				NetEventState state = new NetEventState();
				state.type = NetEventType.Disconnect;
				state.result = NetEventResult.Success;
				m_handler(state);
			}
		}

		m_isConnected = false;
		Debug.Log("TransportTcp::Disconnect called.");

		return true;
	}

    // 送信処理.
	public int Send(byte[] data, int size)
	{
		// 実際の送信は通信スレッド側(DispatchSend関数)で行います.
		// ゲームスレッド側の処理をできるだけ軽くするために直接Send関数で送信していません.
		return m_sendQueue.Enqueue(data, size);
	}
	
    // 受信処理.
	public int Receive(ref byte[] buffer, int size) 
	{
		// 実際の受信は通信スレッド側(DispatchReceive() 関数)で行います.
		// ゲームスレッド側の処理をできるだけ軽くするために直接　Receive() 関数で受信していません.
		return m_recvQueue.Dequeue(ref buffer, size);
	}

	// 接続確認.
	public bool IsConnected()
	{
		return	m_isConnected;
	}

	// サーバー動作設定確認.
	public bool IsServer()
	{
		return	m_isServer;
	}

	// イベント通知関数登録.
	public void RegisterEventHandler(EventHandler handler)
	{
		m_handler += handler;
	}
	
	// イベント通知関数削除.
	public void UnregisterEventHandler(EventHandler handler)
	{
		m_handler -= handler;
	}

	// スレッド側の送受信処理.
	public void Dispatch()
	{
		// クライアントからの接続を待ちます.
		AcceptClient();

		// クライアントとの小受信を処理します.
		if (m_isConnected == true && m_socket != null) {
			lock (lockObj) {
				// 送信処理.
				DispatchSend();
				
				// 受信処理.
				DispatchReceive();
			}
		}
	}

	// クライアントとの接続.
	void AcceptClient()
	{
		if (m_listener != null && m_listener.Poll(0, SelectMode.SelectRead)) {
			// クライアントから接続されました.
			Socket socket = m_listener.Accept();
			m_socket.Add(socket);
			m_isConnected = true;
			// ゲームなどのアプリケーションは他のプレイヤーが入室したときにユーザーに通知するのがよいでしょう.
			// そのため入室したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			if (m_handler != null) {
				NetEventState state = new NetEventState();
				state.type = NetEventType.Connect;
				state.result = NetEventResult.Success;
				m_handler(state);
			}
			Debug.Log("Connected from client. [port:" + m_port + "]");
		}
	}

	// スレッド側の送信処理.
	void DispatchSend()
	{
		if (m_socket == null) {
			return;
		}

		try {
			// 送信処理.
            //if (m_socket.Poll(0, SelectMode.SelectWrite)) {
				byte[] buffer = new byte[m_packetSize];
				
				// Send関数でバッファリングされたデータを取り出して送信を行います.
				int sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				// 送信データがなくなるまで送信を続けます.
				while (sendSize > 0) {
					foreach (Socket socket in m_socket) {
						socket.Send(buffer, sendSize, SocketFlags.None);	
					}
					sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				}
			//}
		}
		catch {
			// 送信エラーがはっせいしたことをアプリケーションに通知します.
			if (m_handler != null) {
				NetEventState state = new NetEventState();
				state.type = NetEventType.SendError;
				state.result = NetEventResult.Failure;
				m_handler(state);
			}
		}
	}

	// スレッド側の受信処理.
	void DispatchReceive()
	{
		if (m_socket == null) {
			return;
		}

		// 受信処理.
		try {
			foreach (Socket socket in m_socket) {
				if (socket.Poll(0, SelectMode.SelectRead)) {
					byte[] buffer = new byte[m_packetSize];

					int recvSize = socket.Receive(buffer, buffer.Length, SocketFlags.None);

					Debug.Log("TransportTcp Receive data [size:" + recvSize + "][port:" + m_port +"]");
					// 通信相手と切断したことにReceive関数の関数値は0が返されます.
					if (recvSize == 0) {
						// 切断.
						Disconnect();
					}
					else if (recvSize > 0) {
						// ゲームスレッド側に受信したデータを渡すために受信データをキューに追加します.
						m_recvQueue.Enqueue(buffer, recvSize);
					}
				}
			}
		}
		catch {
			// 送信エラーがはっせいしたことをアプリケーションに通知します.
			if (m_handler != null) {
				NetEventState state = new NetEventState();
				state.type = NetEventType.ReceiveError;
				state.result = NetEventResult.Failure;
				m_handler(state);
			}
		}
	}
}
