// TCP通信を行う通信モジュール
//
// ■プログラムの説明
// TCPプロトコルを用いたデータの送受信を行うモジュールです.
// 通信相手が Connect() 関数を呼び出して接続したらお互いに送受信できる状態になります.
// 通信相手と Send() 関数、Recieve() 関数で双方向のデータ通信を行います.
// 
// ●接続の動作
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
//    イベント発生時の関数呼び出しは Connect() 関数、Disconnect() 関数を参照してください.
// 

using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;


public class TransportTCP : ITransport
{
	// ノードID.
	private	int				m_nodeId = -1;

	// 通信用ソケット.
	private Socket			m_socket = null;

	// 接続先ポート番号.
	// ログ出力用に使用します.
	//private int				m_port = -1;

	// 接続フラグ.
	private	bool			m_isConnected = false;

	// 送信バッファ.
	private PacketQueue		m_sendQueue = new PacketQueue();
	
	// 受信バッファ.
	private PacketQueue		m_recvQueue = new PacketQueue();
	
	// イベントハンドラー.
	private EventHandler	m_handler;
	
	// 送受信用のパケットの最大サイズ.
	// バッファのサイズはMTUの設定によって決まります.(MTU:1回に送信できる最大のデータサイズ)
	// イーサネットの最大MTUは1500bytesです.
	// この値はOSや端末などで異なるものですのでバッファのサイズは
	// 動作させる環境のMTUを調べて設定しましょう.
	private const int		m_packetSize = 1400;

	public string	transportName = "";

	// デフォルトコンストラクタ.
	public TransportTCP()
	{

	}

	// コンストラクタ.
	public TransportTCP(Socket socket, string name)
	{
		m_socket = socket;
		transportName = name;
	}

	// 初期化.
	public bool Initialize(Socket socket)
	{
		m_socket = socket;
		m_isConnected = true;

		return true;
	}

	// 終了処理.
	public bool Terminate()
	{
		m_socket = null;

		return true;
	}

	// ノードID取得.
	public int GetNodeId()
	{
		return m_nodeId;
	}

	// ノードIDの設定.
	public void SetNodeId(int node)
	{
		m_nodeId = node;
	}

	// 接続元(ローカル側)のエンドポイント取得.
	public IPEndPoint GetLocalEndPoint()
	{
		if (m_socket == null) {
			return default(IPEndPoint);
		}
		
		return m_socket.LocalEndPoint as IPEndPoint;
	}

	// 接続先(リモート側)のエンドポイント取得
	public IPEndPoint GetRemoteEndPoint()
	{
		if (m_socket == null) {
			return default(IPEndPoint);
		}
		
		return m_socket.RemoteEndPoint as IPEndPoint;
	}

	// サーバーとの通信ポート設定.
	public void SetServerPort(int port)
	{
	}

	// 接続処理.
	public bool Connect(string address, int port)
	{
		Debug.Log("Transport connect called");

		if (m_socket != null) {
			return false;
		}

		try {
			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_socket.Connect(address, port);
			m_socket.NoDelay = true;
			// ログ出力用に使用します.
			//m_port = port;
			m_isConnected = true;
			Debug.Log("Connection success");
		}
		catch (SocketException e) {
			m_socket = null;
			m_isConnected = false;
			Debug.Log("Connect fail");
			Debug.Log(e.ToString());
		}

		string str = "TransportTCP connect:" + m_isConnected.ToString(); 
		Debug.Log(str);
		if (m_handler != null) {
			// 接続結果を通知します.
			// ゲームアプリケーションは他のプレイヤーが入室したときにユーザーへ通知するのがよいでしょう.
			// そのため入室したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = (m_isConnected == true)? NetEventResult.Success : NetEventResult.Failure;
			m_handler(this, state);
			Debug.Log("event handler called");
		}

		return m_isConnected;
	}

	// 切断処理.
	public void Disconnect()
	{
		m_isConnected = false;

		if (m_socket != null) {
			// ソケットのクローズ.
			m_socket.Shutdown(SocketShutdown.Both);
			m_socket.Close();
			m_socket = null;
		}

		// 切断を通知します.
		if (m_handler != null) {
			// ゲームアプリケーションは他のプレイヤーが切断したときにユーザーに通知するのがよいでしょう.
			// そのため切断したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Disconnect;
			state.result = NetEventResult.Success;
			m_handler(this, state);
		}
	}

	// 送信処理.
	public int Send(byte[] data, int size)
	{
		// 送信データは一旦キューにバッファリングするだけで送信はしていません.
		// 実際の送信は通信スレッド側(DispatchSend() 関数)で行います.
		// ゲームスレッド側の処理をできるだけ軽くするために直接 Send() 関数で送信していません.
		return m_sendQueue.Enqueue(data, size);
	}

	// 受信処理.
	public int Receive(ref byte[] buffer, int size) 
	{
		// 実際の受信は通信スレッド側(DispatchReceive() 関数)で行います.
		// ゲームスレッド側の処理をできるだけ軽くするために直接　Receive() 関数で受信していません.
		return m_recvQueue.Dequeue(ref buffer, size);
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

	// 通信スレッド側の送受信処理.
	public void Dispatch()
	{
		// クライアントとの小受信を処理します.
		if (m_isConnected == true && m_socket != null) {

			// 送信処理.
			DispatchSend();
			
			// 受信処理.
			DispatchReceive();
		}
	}

	// 通信スレッド側の送信処理.
	void DispatchSend()
	{
		if (m_socket == null) {
			return;
		}

		try {
			// 送信処理.
			if (m_socket.Poll(0, SelectMode.SelectWrite)) {
				byte[] buffer = new byte[m_packetSize];
				
				int sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				while (sendSize > 0) {
					try {
						int sendResult = m_socket.Send(buffer, sendSize, SocketFlags.None);
						if (sendResult < 0) {
							Debug.Log("Transport send error send size:" + sendResult);
						}
					}
					catch (SocketException e) {
						Debug.Log("Transport send error:" + e.Message);

						if (m_handler != null) {
							NetEventState state = new NetEventState();
							state.node = m_nodeId;
							state.type = NetEventType.SendError;
							state.result = NetEventResult.Failure;
							m_handler(this, state);
						}
					}
					sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				}
			}
		}
		catch {
			return;
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
			while (m_socket.Poll(0, SelectMode.SelectRead)) {
				byte[] buffer = new byte[m_packetSize];

				int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);

				// 通信相手と切断したことにReceive関数の関数値は0が返されます.
				if (recvSize == 0) {
					// 切断.
					Debug.Log("[TCP]Disconnect recv from other.");
					Disconnect();
				}
				else if (recvSize > 0) {
					// ゲームスレッド側に受信したデータを渡すために受信データをキューに追加します.
					m_recvQueue.Enqueue(buffer, recvSize);
				}
			}
		}
		catch {
			return;
		}
	}

	// 受信したデータを保存.
	public void SetReceiveData(byte[] data, int size)
	{	
		// 受信データをバッファに追加.
		if (size > 0) {
			m_recvQueue.Enqueue(data, size);
		}
	}

	// 接続確認.
	public bool IsConnected()
	{
		return	m_isConnected;
	}

}
