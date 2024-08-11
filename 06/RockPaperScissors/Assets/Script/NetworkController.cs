// 選択したじゃんけんやアクションの送受信
//
// ■プログラムの説明
// 通信の接続、切断、送受信の定型処理をまとめたクラスです.
// 待ち受け、接続の処理を NetworkController() 関数(コンストラクタ)で行います.
// 選択されたジャンケンの種類 SendRPSData() 関数で送信します. 
// ゲームプログラムからはジャンケンの種類を選択する関数として呼び出すだけでパケットの構造を意識せずに済みます.
// 受信してたらジャンケンの種類を ReceiveRPSData() 関数で返します. 
// ゲームプログラムからはジャンケンの種類を受信する関数として呼び出すだけでパケットの構造を意識せずに済みます.
// ジャンケン後のアクションは SendActionData()、ReceiveActionData() 関数で送受信をします.
// これらもゲームプログラムからはアクションの種類と経過時間を送受信する関数として呼び出すだけでパケットの構造を意識せずに済みます.
//

//#define EMURATE_INPUT //デバッグ中は入力をこれでエミュレートします.

using UnityEngine;
using System;
using System.Collections;
using System.Net;

public class NetworkController {
    const int USE_PORT = 50765;
	TransportTCP m_network;         //よく使うので捕まえておく.

    // サーバー・クライアントの判定用.
    public enum HostType {
        Server,
        Client,
    };
    HostType m_hostType;


    // サーバーで使うときの呼び出し.
    public NetworkController() {
        m_hostType = HostType.Server;

        GameObject nObj = GameObject.Find("Network");
        m_network = nObj.GetComponent<TransportTCP>();
        m_network.StartServer(USE_PORT, 1);
    }

	// クライアントで使うときの呼び出し.
	public NetworkController(string serverAddress) {
        m_hostType = HostType.Client;

        GameObject nObj = GameObject.Find("Network");
		m_network = nObj.GetComponent<TransportTCP>();
        m_network.Connect(serverAddress, USE_PORT);
    }

	// ネットワークの終了.
	public void Disconnect() {

		if (m_hostType == HostType.Server) {

			m_network.StopServer();
		}
		else {

			m_network.Disconnect();
		}
	}

    // ネットワークの状態を取得.
    public bool IsConnected() {
#if EMURATE_INPUT
        return true;    //デバッグ中は接続してるものとして偽装します.
#endif

        return m_network.IsConnected();
    }

    public HostType GetHostType() {
        return m_hostType;
    }

	// 選択されたジャンケンの種類を送信する.
	public void SendRPSData(RPSKind rpsKind)
	{
		// 構造体をbyte配列に変換します.
		byte[] data = new byte[1];
		data[0] = (byte) rpsKind;
		
		// データを送信します.
		m_network.Send(data, data.Length);
	}
	
	// 受信してたらジャンケンの種類を返す.
	public RPSKind ReceiveRPSData()
	{
#if EMURATE_INPUT
		return RPSKind.Rock;    // デバッグ中はグーを選択しているものとして偽装します.
#endif
		
		byte[] data = new byte[1024];
		
		// データを受信します.
		int recvSize = m_network.Receive(ref data, data.Length);
		if (recvSize < 0) {
			// 入力情報を受信していない.
			return RPSKind.None;
		}
		
		// byte配列を構造体に変換します.
		RPSKind rps = (RPSKind) data[0];
		
		return rps;
	}
	
	// アクション送信.
	public void SendActionData(ActionKind actionKind, float actionTime)
	{
		// 構造体をbyte配列に変換します.
		byte[] data = new byte[3];
		data[0] = (byte) actionKind;
		
		// 整数化します.
		short actTime = (short)(actionTime * 1000.0f);
		// ネットワークバイトオーダーへ変換します.
		short netOrder = IPAddress.HostToNetworkOrder(actTime);
		// byte[] 型に変換します.
		byte[] conv = BitConverter.GetBytes(netOrder);
		data[1] = conv[0];
		data[2] = conv[1];
		
		// データを送信します.
		m_network.Send(data, data.Length);
	}
	
	// アクション受信.
	public bool ReceiveActionData(ref ActionKind actionKind, ref float actionTime)
	{	
		byte[] data = new byte[1024];
		
		// データを受信します.
		int recvSize = m_network.Receive(ref data, data.Length);
		if (recvSize < 0) {
			// 入力情報を受信していない.
			return false;
		}
		
		// byte配列を構造体に変換します.
		actionKind = (ActionKind) data[0];
		// byte[] 型から short 型へ変換します.
		short netOrder = (short) BitConverter.ToUInt16(data, 1);
		// ホストバイトオーダーへ変換します.
		short hostOrder = IPAddress.NetworkToHostOrder(netOrder);
		// float 単位の時間に戻します.
		actionTime = hostOrder / 1000.0f;
		
		return true;
	}
}
