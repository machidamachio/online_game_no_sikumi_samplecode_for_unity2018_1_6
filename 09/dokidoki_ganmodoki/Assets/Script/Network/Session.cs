// セッション管理を行う基底クラス定義
//
// ■プログラムの説明
// セッション管理を行う基底クラスです.
// ITransport クラスを継承したクラスをテンプレートクラスとして引数に取ります.
// 引数で受け取った送受信クラス(TransportTCP, TransportUDP)で通信をするノードとしてを管理します.
// このクラスを使用するプログラムはノードおよびトランスポートをノードIDで指定して使用します.
// このクラスおよび継承したクラスは、ノードが通信を行う ITransport クラスを継承した Transport*** クラス
// をトランスポート(m_transports)として管理しています.
//

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;


public abstract class Session<T>
	where T : ITransport, new()
{

	// リスニングソケット.
	protected Socket				m_listener = null;

	// 使用するポート番号.
	protected int					m_port = 0;

	// 現在の接続ID.
	protected int					m_nodeIndex = 0;
	

	// ノード管理用のDictionary.
	protected Dictionary<int, T>	m_transports = null;

	//
	// スレッド関連のメンバ変数.
	//

	protected bool					m_threadLoop = false;
	
	protected Thread				m_thread = null;


	// ノード追加用のインデックスのロックオブジェクト.
	protected System.Object 		m_transportLock = new System.Object();

	// ノードID割り振り用のインデックスのロックオブジェクト.
	protected System.Object 		m_nodeIndexLock = new System.Object();

	// サーバーフラグ.	
	protected bool	 				m_isServer = false;

	// 送受信用のパケットの最大サイズ.
	protected int 					m_mtu;

	// バッファのサイズはMTUの設定によって決まります.(MTU:1回に送信できる最大のデータサイズ)
	// イーサネットの最大MTUは1500bytesです.
	// この値はOSや端末などで異なるものですのでバッファのサイズは
	// 動作させる環境のMTUを調べて設定しましょう.
	protected int					defaultMTUSize = 1500;


	// イベント通知のデリゲート.
	public delegate void 		EventHandler(int node, NetEventState state);
	// イベントハンドラー.
	protected EventHandler		m_handler;
	
	// Constractor
	public Session()
	{
		try {
			// ノード管理情報を生成します.
			m_transports = new Dictionary<int, T>();
			// MTUサイズをデフォルトで設定します.
			// 起動するOS等に合わせてサイズを変更してください.
			m_mtu = defaultMTUSize;
		}
		catch (Exception e) {
			Debug.Log(e.ToString());
		}
	}
	
	// デストラクタ.
	~Session() 
	{
		// このクラスを破棄した際に切断したことが通信相手にわかるように明示的に切断処理を行います.
		Disconnect();
	}


	// 待ち受け開始.
	public bool StartServer(int port, int connectionMax)
	{
		// リスニングソケットを生成します.
		bool ret = CreateListener(port, connectionMax);
		if (ret == false) {
			return false;
		}

		// スレッドを生成します.
		if (m_threadLoop == false) {
			CreateThread();
		}

		m_port = port;
		m_isServer = true;
		
		return true;
	}

	// 待ち受け終了.
	public void StopServer()
	{
		m_isServer = false;

		DestroyThread();

		DestroyListener();

		Debug.Log("Server stopped.");
	}


	// スレッドを生成.
	protected bool CreateThread()
	{
		Debug.Log("CreateThread called.");

		// 受信処理のスレッド起動.
		try {
			m_thread = new Thread(new ThreadStart(ThreadDispatch));
			m_threadLoop = true;
			m_thread.Start();
		}
		catch {
			return false;
		}


		Debug.Log("Thread launched.");

		return true;
	}

	// スレッドを破棄.
	protected bool DestroyThread()
	{
		Debug.Log("DestroyThread called.");

		if (m_threadLoop == true) {

			m_threadLoop = false;

			if (m_thread != null) {
				// 受信処理スレッド終了.
				m_thread.Join();
				// 受信処理スレッド破棄.
				m_thread = null;
			}
		}

		return true;
	}

	// セッションに参加(Socket指定).
	protected int JoinSession(Socket socket)
	{
		// セッションに参加します.
		T transport = new T();

		if (socket != null) {
			// ソケットの設定をします.
			transport.Initialize(socket);
		}

		return JoinSession(transport);
	}

	// セッションに参加(Transport指定).
	protected int JoinSession(T transport)
	{
		int node = -1;
		lock (m_nodeIndexLock) {
			node = m_nodeIndex;
			++m_nodeIndex;
		}
		
		// このtransportのノードIDを決定.
		transport.SetNodeId(node);
		
		// イベントの通知を受ける関数を登録します.
		transport.RegisterEventHandler(OnEventHandling);
		
		try {
			lock (m_transportLock) {
				m_transports.Add(node, transport);
			}
		}
		catch { 
			return -1;
		}
		
		return node;
	}


	// セッションから退出.
	protected bool LeaveSession(int node)
	{
		if (node < 0) {
			return false;	
		}
					
		T transport = (T) m_transports[node];
		if (transport == null) {
			// ノードが存在しない.
			return false;
		}

		lock (m_transportLock) {
			// Transportを破棄します.
			transport.Terminate();

			m_transports.Remove(node);
		}

		return true;
	}

	// サーバー動作設定確認.
	public bool IsServer()
	{
		return m_isServer;
	}

	// 接続確認.
	public bool IsConnected(int node)
	{
		if (m_transports.ContainsKey(node) == false) {
			return false;
		}

		return m_transports[node].IsConnected(); 
	}

	// 管理セッション数取得.
	public int GetSessionNum()
	{
		return m_transports.Count;
	}

	// 接続元(ローカル側)のエンドポイント取得.
	public IPEndPoint GetLocalEndPoint(int node)
	{
		if (m_transports.ContainsKey(node) == false) {
			return default(IPEndPoint);
		}

		IPEndPoint ep;
		T transport = m_transports[node];
		ep = transport.GetLocalEndPoint();

		return ep;
	}

	// 接続先(リモート側)のエンドポイント取得
	public IPEndPoint GetRemoteEndPoint(int node)
	{
		if (m_transports.ContainsKey(node) == false) {
			return default(IPEndPoint);
		}

		IPEndPoint ep;
		T transport = m_transports[node];
		ep = transport.GetRemoteEndPoint();

		return ep;
	}

	// 接続要求監視.
	int FindTransoprt(IPEndPoint sender)
	{
		foreach (int node in m_transports.Keys) {
			T transport = m_transports[node];
			IPEndPoint ep = transport.GetLocalEndPoint();
			if (ep.Address.ToString() == sender.Address.ToString()) {
				return node;
			}
		}
		
		return -1;
	}

	//
	public virtual void ThreadDispatch()
	{	
		
		string str = "ThreadDispatch:" + m_threadLoop.ToString();
		Debug.Log(str);
		
		while (m_threadLoop) {
			// 接続要求監視.
			AcceptClient();
			
			// セッション内のノードの送受信処理.
			Dispatch();
			
			// 他のスレッドへ処理を譲ります.
			Thread.Sleep(3);		
		}
		
		Debug.Log("Thread end.");
	}


	// 接続処理.
	public virtual int Connect(string address, int port)
	{
		Debug.Log("Connect call");

		if (m_threadLoop == false) {
			Debug.Log("CreateThread");
			CreateThread();
		}
	
		int node = -1;
		bool ret = false;
		try {
			Debug.Log("transport Connect");
			T transport = new T();
			// 同一端末で実行する際にポート番号で送信元を判別するあためのポート番号を設定します.
			transport.SetServerPort(m_port);
			ret = transport.Connect(address, port);
			if (ret) {

				node = JoinSession(transport);
				Debug.Log("JoinSession node:" + node);
			}
		}
		catch {
			Debug.Log("Connect fail.[exception]");
		}

		// 接続を通知します.
		if (m_handler != null) {
			// ゲームアプリケーションは他のプレイヤーが入室したときにユーザーへ通知するのがよいでしょう.
			// そのため入室したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = (ret)? NetEventResult.Success : NetEventResult.Failure;
			m_handler(node, state);
		}

		return node;
	}

	// 切断処理(ノードID指定).
	public virtual bool Disconnect(int node)
	{
		if (node < 0) {
			return false;
		}

		if (m_transports == null) {
			return false;
		}

		if (m_transports.ContainsKey(node) == false) {
			return false;
		}

		T transport = m_transports[node];
		if (transport != null) {
			transport.Disconnect();
			LeaveSession(node);
		}

		// 切断を通知します.
		if (m_handler != null) {
			// ゲームアプリケーションは他のプレイヤーが切断したときにユーザーに通知するのがよいでしょう.
			// そのため切断したことをアプリケーションがわかるようにアプリケーション側の関数を呼び出すようにします.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Disconnect;
			state.result = NetEventResult.Success;
			m_handler(node, state);
		}

		return true;
	}

	// 全ノードの切断処理.
	public virtual bool Disconnect()
	{
		// スレッドの停止
		DestroyThread();
		
		// 接続中のTransportを切断する.
		lock (m_transportLock) {

			Dictionary<int, T> transports = new Dictionary<int, T>(m_transports);

			foreach (T trans in transports.Values) {
				trans.Disconnect();
				trans.Terminate();
			}
		}

		return true;
	}

	// 送信処理.
	public virtual int Send(int node, byte[] data, int size)
	{
		if (node < 0) {
			return -1;
		}

		int sendSize = 0;
		try {
			T transport = (T)m_transports[node];
			sendSize = transport.Send(data, size);
		}
		catch {
			return -1;
		}

		return sendSize;	
	}

	// 受信処理.
	public virtual int Receive(int node, ref byte[] buffer)
	{
		if (node < 0) {
			return -1;
		}

		int recvSize = 0;
		try { 
			T transport = m_transports[node];
			recvSize = transport.Receive(ref buffer, buffer.Length);
		}
		catch {
			return -1;
		}

		return recvSize;
	}

	// 通信スレッド側の送受信処理.
	public virtual void Dispatch()
	{
		Dictionary<int, T> transports = new Dictionary<int, T>(m_transports);
		
		// 送信処理.
		foreach (T trans in transports.Values) {
			if (trans != null) {
				trans.Dispatch();
			}
		}

		// 受信処理.
		DispatchReceive();

	}

	// 受信処理 (このクラスを継承したクラスで実装します).
	protected virtual void DispatchReceive()
	{
		// リスニングソケットで一括受信したデータを各トランスポートへ振り分ける.
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


	// イベントハンドラー.
	public virtual void OnEventHandling(ITransport itransport, NetEventState state)
	{
		int node = itransport.GetNodeId();

		string str = "SignalNotification[" + node + "] :" + state.type.ToString() + " state:" + state.result.ToString();
		Debug.Log(str);

		do {
			if (m_transports.ContainsKey(node) == false) {
				// 見つからなかった.
				string msg = "NodeId[" + node + "] is not founded.";
				Debug.Log(msg);
				break;
			}

			switch (state.type) {
			case NetEventType.Connect:
				break;

			case NetEventType.Disconnect:
				// 通信相手が切断した場合、実際に接続している通信相手との整合をとるために
				// ノードの管理情報から切断した通信相手のノード情報を削除します.
				LeaveSession(node);
				break;
			}
		} while (false);

		// イベント通知関数が登録されていたらコールバックします.
		if (m_handler != null) {
			m_handler(node, state);
		}
	}

	// リスニングソケットの生成 (このクラスを継承したクラスで実装します).
	public abstract bool	CreateListener(int port, int connectionMax);

	// リスニングソケットの破棄 (このクラスを継承したクラスで実装します).
	public abstract bool 	DestroyListener();

	// クライアントとの接続 (このクラスを継承したクラスで実装します).
	public abstract void	AcceptClient();
	
}

