// UDPで送受信するセッション管理を行うクラス定義
//
// ■プログラムの説明
// UDPで送受信するセッションの管理を行うクラスです.
// Session クラスで override するメソッドの定義をしています.
// このクラス、ノードが通信を行う TransportUDP クラスをトランスポート(m_transports)として管理しています.
//

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

public class SessionUDP : Session<TransportUDP>
{

	// UDPで通信を行うためのリスニングソケットの生成.
	public override bool CreateListener(int port, int connectionMax)
	{
		try {
			m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			m_listener.Bind(new IPEndPoint(IPAddress.Any, port));

			string str = "Create UDP Listener " + "(Port:" + port + ")"; 
			Debug.Log(str);
		}
		catch {
			return false;
		}
		
		return true;
	}

	// UDPで通信を行うためのリスニングソケットの破棄.
	public override bool DestroyListener()
	{
		if (m_listener == null) {
			return false;
		}

		m_listener.Close();
		m_listener = null;
		
		return true;
	}

	// クライアントとの接続.
	public override void AcceptClient() 
	{
		// UDPで接続するため Accept 関連の処理は行いません.
	}

	// 受信処理.
	protected override void DispatchReceive()
	{
		// リスニングソケットで一括受信したデータを各ノードのトランスポートへ振り分けます.
		if (m_listener != null && m_listener.Poll(0, SelectMode.SelectRead)) {
			byte[] buffer = new byte[m_mtu];
			IPEndPoint address = new IPEndPoint(IPAddress.Any, 0);
			EndPoint endPoint =(EndPoint) address;
			
			int recvSize = m_listener.ReceiveFrom(buffer, SocketFlags.None, ref endPoint);

			int node = -1;
			// 同一端末で実行する際にポート番号で送信元を判別するあためにキープアライブの.
			// パケットにIPアドレスとポート番号を取り出します.
			string str = System.Text.Encoding.UTF8.GetString(buffer).Trim('\0');
			if (str.Contains(TransportUDP.m_requestData)) {
				string[] strArray = str.Split(':');
				IPEndPoint ep = new IPEndPoint(IPAddress.Parse(strArray[0]), int.Parse(strArray[1]));
				node = getNodeFromEndPoint(ep, true);
			}
			else {
				node = getNodeFromEndPoint((IPEndPoint) endPoint, false);
			}

			if (node >= 0) {
				TransportUDP transport = m_transports[node];
				transport.SetReceiveData(buffer, recvSize, (IPEndPoint) endPoint);
			}
		}
	}

	
	// EndPointからノード番号を取得.
	private int getNodeFromEndPoint(IPEndPoint endPoint, bool keepAlive)
	{
		foreach (int node in m_transports.Keys) {
			TransportUDP transport = m_transports[node];

			IPEndPoint transportEp = (keepAlive)? transport.GetLocalEndPoint() : transport.GetRemoteEndPoint();
			if (transportEp != null) {
				if (
					transportEp.Port == endPoint.Port &&
					transportEp.Address.ToString() == endPoint.Address.ToString()
				    ) {
					return node;
				}
			}
		}
		
		return -1;
	}
}

