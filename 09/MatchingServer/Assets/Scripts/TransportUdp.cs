// UDP通信を行う通信モジュール
//
// ■プログラムの説明
// UDPプロトコルを用いたデータの送受信を行うモジュールです.
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
//   実際の送信は通信スレッド側で行います.これはデータの祖受信によるオーバーヘッドでゲームの処理時間を消費しないようにしています.
//   ソケットによる送信は DispatchSend() 関数で処理しています.
//
// ●イベント処理
//	  通信相手の接続や切断をゲームプログラムに通知するための仕組みをイベントハンドラーとして登録、削除をします.
//    ゲームプログラム側に EventHandler 型の関数 [例： void foo(NetEventState state) { ... } ]を定義します.
//    定義した関数を RegisterEventHandler() 関数、UnregisterEventHandler() 関数に渡してイベントハンドラーに登録、解除をします.
//    通信相手が接続、切断した時にイベントハンドラーに登録した関数が呼び出され、ゲームプログラムの関数がデリゲートからが呼び出されます.
//    イベント発生時の関数呼び出しは Connect() 関数、Disconnect() 関数を参照してください.
//

using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class TransportUDP : ITransport
{
	// ノードID.
	private int				m_nodeId = -1;

	// 通信用ソケット.
	private Socket			m_socket = null;
	
	private IPEndPoint		m_localEndPoint = null;

	private IPEndPoint		m_remoteEndPoint = null;
	// 送信バッファ.
	private PacketQueue		m_sendQueue = new PacketQueue();
	
	// 受信バッファ.
	private PacketQueue		m_recvQueue = new PacketQueue();

	// 送受信用のパケットの最大サイズ.
	// バッファのサイズはMTUの設定によって決まります.(MTU:1回に送信できる最大のデータサイズ)
	// イーサネットの最大MTUは1500bytesです.
	// この値はOSや端末などで異なるものですのでバッファのサイズは
	// 動作させる環境のMTUを調べて設定しましょう.
	private int				m_packetSize = 1400;
	
	// 接続フラグ.
	private	bool			m_isRequested = false;
	
	// 受信フラグ.
	private	bool			m_isConnected = false;

	// タイムアウト時間.
	private const int 		m_timeOutSec = 10;
	
	// タイムアウトのティッカー.
	private DateTime		m_timeOutTicker;
	
	// キープアライブインターバル.
	private const int		m_keepAliveInter = 2; 

	// キープアライブティッカー.
	private DateTime		m_keepAliveTicker;
	
	// 接続確認用のダミーパケットデータ.
	public const string 	m_requestData = "KeepAlive.";
	
	// イベントハンドラー.
	private EventHandler	m_handler;

	
	// 同一端末実行時の判別用にリスニングソケットのポート番頭を保存.
	private int				m_serverPort = -1;
	
	// デフォルトコンストラクタ.
	public TransportUDP()
	{

	}

	// コンストラクタ.
	public TransportUDP(Socket socket) 
	{
		m_socket = socket;
	}

	// 初期化.
	public bool Initialize(Socket socket)
	{
		m_socket = socket;
		m_isRequested = true;
		
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
		return m_localEndPoint;
	}

	// 接続先(リモート側)のエンドポイント取得
	public IPEndPoint GetRemoteEndPoint()
	{
		return m_remoteEndPoint;
	}

	// サーバーとの通信ポート設定.
	public void SetServerPort(int port)
	{
		m_serverPort = port;
	}
	
	// 接続処理.
	public bool Connect(string ipAddress, int port)
	{
		if (m_socket == null) {
			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			Debug.Log("Create new socket.");
		}

		try {			
			m_localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
			m_isRequested = true;
			Debug.Log("Connection success");
		}
		catch {
			m_isRequested = false;
			Debug.Log("Connect fail");
		}

		string str = "TransportUDP connect:" + m_isRequested.ToString(); 
		Debug.Log(str);
		if (m_handler != null) {
			// 接続結果を通知します.
	        // ゲームアプリケーションは他のプレイヤーが入室したときにユーザーへ通知するのがよいでしょう.
	        // そのため入室したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = (m_isRequested == true)? NetEventResult.Success : NetEventResult.Failure;
			m_handler(this, state);
			Debug.Log("event handler called");
		}

		return m_isRequested;
	}
	
	// 切断処理.
	public void Disconnect() 
	{
		m_isRequested = false;

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
		if (m_sendQueue == null) {
			return 0;
		}

		// 送信データは一旦キューにバッファリングするだけで送信はしていません.
		// 実際の送信は通信スレッド側(DispatchSend() 関数)で行います.
		// ゲームスレッド側の処理をできるだけ軽くするために直接 Send() 関数で送信していません.
		return m_sendQueue.Enqueue(data, size);
	}
	
    // 受信処理.
	public int Receive(ref byte[] buffer, int size) 
	{
		if (m_recvQueue == null) {
			return 0;
		}

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
	
	// 接続要求をした.
	public bool IsRequested()
	{
		return	m_isRequested;
	}
	
	// 接続確認.
	public bool IsConnected()
	{
		return	m_isConnected;
	}

	// 通信スレッド側の送受信処理.
	public void Dispatch()
	{
		// 送信処理.
		DispatchSend();

		// タイムアウト処理.
		CheckTimeout();

		// キープアライブのチェックをします.
		if (m_socket != null) {
			// 通信相手に接続を開始したことを定期的に通知します.
			TimeSpan ts = DateTime.Now - m_keepAliveTicker;
			
			if (ts.Seconds > m_keepAliveInter) {
				// UDPの接続に関して, サンプルコードではハンドシェイクを行わないため.
				// 同一端末で実行する際にポート番号で送信元を判別しなければなりません.
				// このため, 接続のトリガーとなるキープアライブのパケットにIPアドレスと.
				// ポート番号を載せて判別させるようにしています.
				string message = m_localEndPoint.Address.ToString() + ":" + m_serverPort + ":" + m_requestData;
				byte[] request = System.Text.Encoding.UTF8.GetBytes(message);
				m_socket.SendTo(request, request.Length, SocketFlags.None, m_localEndPoint);	
				m_keepAliveTicker = DateTime.Now;
			}
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
					m_socket.SendTo(buffer, sendSize, SocketFlags.None, m_localEndPoint);	
					sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				}
			}
		}
		catch {
			return;
		}
	}
	
	// 接続要求パケットの設定.
	public void SetReceiveData(byte[] data, int size, IPEndPoint endPoint)
	{	
		string str = System.Text.Encoding.UTF8.GetString(data).Trim('\0');
		if (str.Contains(m_requestData)) {
			// 接続要求パケット受信.
			if (m_isConnected == false && m_handler != null) {
				NetEventState state = new NetEventState();
				state.type = NetEventType.Connect;
				state.result = NetEventResult.Success;
				m_handler(this, state);
			}

			m_isConnected = true;
			m_remoteEndPoint = endPoint;
			m_timeOutTicker = DateTime.Now;
		}
		else if (size > 0) {
			m_recvQueue.Enqueue(data, size);
		}
	}

	// タイムアウトチェック.
	void CheckTimeout()
	{
		TimeSpan ts = DateTime.Now - m_timeOutTicker;
		
		if (m_isRequested && m_isConnected && ts.Seconds > m_timeOutSec) {
			Debug.Log("Disconnect because of timeout.");
			// タイムアウトする時間までにデータが届かなかった.
			// 理解を簡単にするために,あえて通信スレッドからメインスレッドを呼び出しています.
			// 本来ならば切断リクエストを発行して,メインスレッド側でリクエストを監視して.
			// メインスレッド側の処理で切断を行うようにしましょう.
			Disconnect();
		}
	}
}

