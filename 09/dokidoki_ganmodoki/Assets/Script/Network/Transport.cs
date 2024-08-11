// ネットワークライブラリ
//
// ■プログラムの説明
// 送受信処理を行うためのインタフェースクラスです.
// このクラスに宣言されているメソッドを継承したクラスにて定義します.
// このクラスのメソッドを Session クラスまたは Session クラスを継承したクラスで利用します. 
//


using System.Collections;
using System.Net;
using System.Net.Sockets;


// イベント通知のデリゲート.
public delegate void 	EventHandler(ITransport transport, NetEventState state);


public interface ITransport
{
	// 初期化.
	bool		Initialize(Socket socket);

	// 終了処理.
	bool		Terminate();

	// ノードIDを取得.
	int			GetNodeId();

	// ノードIDの設定.
	void		SetNodeId(int node);

	// 接続元エンドポイント取得.
	IPEndPoint	GetLocalEndPoint();

	// 接続先エンドポイント取得.
	IPEndPoint	GetRemoteEndPoint();

	// 送信処理.
	int			Send(byte[] data, int size);

	// 受信処理.
	int			Receive(ref byte[] buffer, int size);

	// 接続処理.
	bool		Connect(string ipAddress, int port);

	// 切断処理.
	void		Disconnect();
	
	// 送受信処理.
	void		Dispatch();

	// 接続確認関数.
	bool		IsConnected();

	// イベント通知関数登録.
	void		RegisterEventHandler(EventHandler handler);

	// イベント通知関数削除.
	void		UnregisterEventHandler(EventHandler handler);


	// 同一端末で実行する際にポート番号で送信元を判別するあためにキープアライブ用の.
	// ポート番号を設定します.
	void 		SetServerPort(int port);
}

