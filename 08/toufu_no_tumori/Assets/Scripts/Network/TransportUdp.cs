// UDP通信を行う通信モジュール
//
// ■プログラムの説明
// UDPプロトコルを用いたデータの送受信を行うモジュールです.
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
//
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
using System.Net;
using System.Net.Sockets;

public class TransportUdp {

	// リスニングソケット.
	// UDPでは m_socket を使用することができますが便宜上
	// リスニングソケットを別のソケットを使用します.
	// 次章でTransportUDPクラスの拡張のために分けています.
	private Socket			m_listener = null;

	// 通信用ソケット.
	private Socket			m_socket = null;

	private IPEndPoint		m_endPoint;

	private IPEndPoint		m_remoteEndPoint;

	// 送信バッファ.
	private PacketQueue		m_sendQueue = new PacketQueue();
	
	// 受信バッファ.
	private PacketQueue		m_recvQueue = new PacketQueue();

	// 接続フラグ.
	private	bool			m_icConnected = false;

	// 受信フラグ.
	private	bool			m_isCommunicating = false;
	
	// 送受信用のパケットの最大サイズ.
	// バッファのサイズはMTUの設定によって決まります.(MTU:1回に送信できる最大のデータサイズ)
	// イーサネットの最大MTUは1500bytesです.
	// この値はOSや端末などで異なるものですのでバッファのサイズは
	// 動作させる環境のMTUを調べて設定しましょう.
	private const int		m_packetSize = 1400;
	
	
	// タイムアウト時間.
	private const int 		m_timeOutSec = 60 * 3;
	
	// タイムアウトのティッカー.
	private DateTime		m_timeOutTicker;

	// キープアライブインターバル.
	private const int		m_keepAliveInter = 10; 

	// キープアライブティッカー.
	private DateTime		m_keepAliveTicker;

	//
	// イベント関連のメンバ変数.
	//
	// イベント通知のデリゲート.
	public delegate void 	EventHandler(NetEventState state);
	// イベントハンドラー.
	private EventHandler	m_handler;

	// 接続確認用のダミーパケットデータ.
	private const string 	m_requestData = "KeepAlive.";

	// 待ち受け開始.
	public bool StartServer(int port)
	{		
		Debug.Log("TransportUdp::StartServer called. port:" + port);
		
		// リスニングソケットを生成します.
		try {
			m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			m_listener.Bind(new IPEndPoint(IPAddress.Any, port));
			m_isCommunicating = false;
		}
		catch {
			return false;
		}

		return true;
	}
	
	// 待ち受け終了.
	public void StopServer()
	{
		Disconnect ();

		if (m_listener != null) {
			// リスナーとして使用していたソケットを閉じます.
			m_listener.Close();
			m_listener = null;
		}		

		m_isCommunicating = false;
		Debug.Log("TransportUdp::StopServer called.");
	}	
	
	// 接続処理.
	public bool Connect(string ipAddress, int port)
	{
		try {
			IPAddress addr = IPAddress.Parse(ipAddress);
			
			m_endPoint = new IPEndPoint(addr, port);
			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}
		catch {
			return false;
		}

		// 接続を通知します.
		if (m_handler != null) {
	        // ゲームアプリケーションは他のプレイヤーが入室したときにユーザーへ通知するのがよいでしょう.
	        // そのため入室したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = NetEventResult.Success;
			m_handler(state);
		}

		m_icConnected = true;
		Debug.Log("TransportUdp::Connect called.");

		return true;
	}
	
	// 切断処理.
	public bool Disconnect() 
	{
		if (m_socket != null) {
            // ソケットをクローズします.
			try {
				m_socket.Close();
				m_socket = null;
			}
			catch (SocketException e) {
				Debug.Log(e.Message);
			}

			// 切断を通知します.
			if (m_handler != null) {
		        // ゲームアプリケーションは他のプレイヤーが切断したときにユーザーに通知するのがよいでしょう.
   				// そのため切断したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
				NetEventState state = new NetEventState();
				state.type = NetEventType.Disconnect;
				state.result = NetEventResult.Success;
				m_handler(state);
			}
		}

		m_icConnected = false;
		Debug.Log("TransportTcp::Disconnect called.");

		return true;
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

	// 接続確認.
	public bool IsConnected()
	{
		return	m_icConnected;
	}

	// 接続中であるか確認.
	public bool IsCommunicating()
	{
		return	m_isCommunicating;
	}

	// 接続先のエンドポイントを取得.
	public IPEndPoint GetRemoteEndPoint()
	{
		return m_remoteEndPoint;
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
		// クライアントからの接続を待ちます.
		AcceptClient();

		// クライアントとの送受信を処理します.
		if (m_socket != null || m_listener != null) {				
			// 送信処理.
			DispatchSend();
			// 受信処理.
			DispatchReceive();
			// タイムアウト処理.
			CheckTimeout();
		}

		// キープアライブ.
		if (m_socket != null) {
			// 通信相手に接続を開始したことを定期的に通知します.
			TimeSpan ts = DateTime.Now - m_keepAliveTicker;
			
			if (ts.Seconds > m_keepAliveInter) {
				byte[] request = System.Text.Encoding.UTF8.GetBytes(m_requestData);
				m_socket.SendTo(request, request.Length, SocketFlags.None, m_endPoint);	
				m_keepAliveTicker = DateTime.Now;
			}
		}
	}

	// 通信相手の待ち受け.
	void AcceptClient()
	{
		if (m_isCommunicating == false &&
		    m_listener != null && m_listener.Poll(0, SelectMode.SelectRead)) {
			// クライアントから接続されました.
			m_isCommunicating = true;
			// 通信開始時刻を記録します.
			m_timeOutTicker = DateTime.Now;
			
			// 接続を通知します.
			if (m_handler != null) {
				NetEventState state = new NetEventState();
				state.type = NetEventType.Connect;
				state.result = NetEventResult.Success;
				m_handler(state);
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
					m_socket.SendTo(buffer, sendSize, SocketFlags.None, m_endPoint);	
					sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				}
			}
		}
		catch {
			return;
		}
	}

	// 通信スレッド側の受信処理.
	void DispatchReceive()
	{
		if (m_listener == null) {
			return;
		}

		// 受信処理.
		try {
			while (m_listener.Poll(0, SelectMode.SelectRead)) {
				byte[] buffer = new byte[m_packetSize];
				IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
				EndPoint remoteEp = (EndPoint)sender;
				
				int recvSize = m_listener.ReceiveFrom(buffer, SocketFlags.None, ref remoteEp);

				m_remoteEndPoint = (IPEndPoint)remoteEp;
				if (m_endPoint == null) {
					m_endPoint = m_remoteEndPoint;
				}

				//Debug.Log("remote:" + m_remoteEndPoint.Address.ToString());

				string str = System.Text.Encoding.UTF8.GetString(buffer);
				if (m_requestData.CompareTo(str.Trim('\0')) == 0) {
					// 接続要求のパケットを受信しました.
					;
				}
				// 通信相手と切断したことにReceive関数の関数値は0が返されます.
				else if (recvSize == 0) {
					// 切断します.
					Disconnect();
				}
				else if (recvSize > 0) {
					// データを受信しました.
					m_recvQueue.Enqueue(buffer, recvSize);
					// 受信時刻を更新ます.
					m_timeOutTicker = DateTime.Now;
				}
			}
		}
		catch {
			return;
		}
	}
	
	// タイムアウトチェック.
	void CheckTimeout()
	{
		TimeSpan ts = DateTime.Now - m_timeOutTicker;
		
		if (m_icConnected && m_isCommunicating && ts.Seconds > m_timeOutSec) {
			Debug.Log("Disconnect because of timeout.");
			// タイムアウトする時間までにデータが届かなかった.
			// 理解を簡単にするために,あえて通信スレッドからメインスレッドを呼び出しています.
			// 本来ならば切断リクエストを発行して,メインスレッド側でリクエストを監視して.
			// メインスレッド側の処理で切断を行うようにしましょう.
			Disconnect();
		}
	}
}

