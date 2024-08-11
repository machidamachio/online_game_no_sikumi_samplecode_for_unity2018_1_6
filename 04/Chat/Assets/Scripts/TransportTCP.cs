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
//
// ●イベント処理
//	  通信相手の接続や切断をゲームプログラムに通知するための仕組みをイベントハンドラーとして登録、削除をします.
//    ゲームプログラム側に EventHandler 型の関数 [例： void foo(NetEventState state) { ... } ]を定義します.
//    定義した関数を RegisterEventHandler() 関数、UnregisterEventHandler() 関数に渡してイベントハンドラーに登録、解除をします.
//    通信相手が接続、切断した時にイベントハンドラーに登録した関数が呼び出され、ゲームプログラムの関数がデリゲートからが呼び出されます.
//    イベント発生時の関数呼び出しは AcceptClient() 関数、Connect() 関数、Disconnect() 関数を参照してください.
// 

using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;



public class TransportTCP : MonoBehaviour {

	//
	// ソケット接続関連.
	//

	// リスニングソケット.
	private Socket			m_listener = null;

	// クライアントとの接続用ソケット.
	private Socket			m_socket = null;

	// 送信バッファ.
	private PacketQueue		m_sendQueue;
	
	// 受信バッファ.
	private PacketQueue		m_recvQueue;
	
	// サーバーフラグ.	
	private bool	 		m_isServer = false;

	// 接続フラグ.
	private	bool			m_isConnected = false;

	//
	// イベント関連のメンバ変数.
	//

	// イベント通知のデリゲート.
	public delegate void 	EventHandler(NetEventState state);

	private EventHandler	m_handler;

	//
	// スレッド関連のメンバ変数.
	//

	// スレッド実行フラグ.
	protected bool			m_threadLoop = false;
	
	protected Thread		m_thread = null;

	// バッファのサイズはMTUの設定によって決まります.(MTU:1回に送信できる最大のデータサイズ)
	// イーサネットの最大MTUは1500bytesです.
	// この値はOSや端末などで異なるものですのでバッファのサイズは
	// 動作させる環境のMTUを調べて設定しましょう.
	private static int 		s_mtu = 1400;


	// Use this for initialization
	void Start ()
    {
        // 送受信バッファを作成します.
        m_sendQueue = new PacketQueue();
        m_recvQueue = new PacketQueue();	
	}
	
	// Update is called once per frame
	void Update ()
    {
	}

	// 待ち受け開始.
	public bool StartServer(int port, int connectionNum)
	{
        Debug.Log("StartServer called.!");

        // リスニングソケットを生成します.
        try {
			// ソケットを生成します.
			m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			// 使用するポート番号を割り当てます.
			m_listener.Bind(new IPEndPoint(IPAddress.Any, port));
			// 待ち受けを開始します.
			m_listener.Listen(connectionNum);
        }
        catch {
			Debug.Log("StartServer fail");
            return false;
        }

        m_isServer = true;

        return LaunchThread();
    }

	// 待ち受け終了.
    public void StopServer()
    {
		m_threadLoop = false;
        if (m_thread != null) {
            m_thread.Join();
            m_thread = null;
        }

        Disconnect();

        if (m_listener != null) {
            m_listener.Close();
            m_listener = null;
        }

        m_isServer = false;

        Debug.Log("Server stopped.");
    }


	// 接続処理.
	public bool Connect(string address, int port)
    {
        Debug.Log("TransportTCP connect called.");

        if (m_listener != null) {
            return false;
        }

		bool ret = false;
        try {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_socket.NoDelay = true;
            m_socket.Connect(address, port);
			ret = LaunchThread();
		}
        catch {
            m_socket = null;
        }

		if (ret == true) {
			m_isConnected = true;
			Debug.Log("Connection success.");
		}
		else {
			m_isConnected = false;
			Debug.Log("Connect fail");
		}

        if (m_handler != null) {
			// 接続結果を通知します.
			// ゲームアプリケーションは他のプレイヤーが入室したときにユーザーへ通知するのがよいでしょう.
			// そのため入室したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = (m_isConnected == true) ? NetEventResult.Success : NetEventResult.Failure;
            m_handler(state);
			Debug.Log("event handler called");
        }

        return m_isConnected;
    }

	// 切断処理.
	public void Disconnect() {
        m_isConnected = false;

        if (m_socket != null) {
			// ソケットのクローズします.
			try {
				m_socket.Shutdown(SocketShutdown.Both);
				m_socket.Close();
				m_socket = null;
			}
			catch (SocketException e) {
				Debug.Log(e.Message);
			}
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

    // 送信処理.
    public int Send(byte[] data, int size)
	{
		if (m_sendQueue == null) {
			return 0;
		}

		// 実際の送信は通信スレッド側(DispatchSend関数)で行います.
		// ゲームスレッド側の処理をできるだけ軽くするために直接Send関数で送信していません.
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

	// スレッド起動関数.
	bool LaunchThread()
	{
		try {
			// Dispatch用のスレッド起動.
			m_threadLoop = true;
			m_thread = new Thread(new ThreadStart(Dispatch));
			m_thread.Start();
		}
		catch {
			Debug.Log("Cannot launch thread.");
			return false;
		}
		
		return true;
	}

	// スレッド側の送受信処理.
    public void Dispatch()
	{
		Debug.Log("Dispatch thread started.");

		while (m_threadLoop) {
			// クライアントからの接続を待ちます.
			AcceptClient();

			// クライアントとの小受信を処理します.
			if (m_socket != null && m_isConnected == true) {

	            // 送信処理.
	            DispatchSend();

	            // 受信処理.
	            DispatchReceive();
	        }

			Thread.Sleep(5);
		}

		Debug.Log("Dispatch thread ended.");
    }

	// クライアントとの接続.
	void AcceptClient()
	{
		if (m_listener != null && m_listener.Poll(0, SelectMode.SelectRead)) {
			// クライアントから接続されました.
			m_socket = m_listener.Accept();
			m_isConnected = true;
	
			// 接続を通知します.
			// ゲームなどのアプリケーションは他のプレイヤーが入室したときにユーザーに通知するのがよいでしょう.
			// そのため入室したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = NetEventResult.Success;
			m_handler(state);
			Debug.Log("Connected from client.");
		}
	}

	// スレッド側の送信処理.
    void DispatchSend()
	{
        try {
            // 送信処理.
            if (m_socket.Poll(0, SelectMode.SelectWrite)) {
				byte[] buffer = new byte[s_mtu];

				// Send関数でバッファリングされたデータを取り出して送信を行います.
				int sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				// 送信データがなくなるまで送信を続けます.
				while (sendSize > 0) {
                    m_socket.Send(buffer, sendSize, SocketFlags.None);
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
        // 受信処理.
        try {
            while (m_socket.Poll(0, SelectMode.SelectRead)) {
				byte[] buffer = new byte[s_mtu];

                int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);
				// 通信相手と切断したことにReceive関数の関数値は0が返されます.
				if (recvSize == 0) {
                    // 切断.
                    Debug.Log("Disconnect recv from client.");
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

	// サーバーか確認.
	public bool IsServer() {
		return m_isServer;
	}
	
    // 接続確認.
    public bool IsConnected() {
        return m_isConnected;
    }

}
