// パケット定義の基底クラス
//
// ■プログラムの説明
// 送受信するパケットが継承するインターフェースクラスです.
// 送受信関数はインターフェースクラスの関数を使用してパケットのデータを取得します.
//

using System.Collections;
using System.IO;


public interface IPacket<T>
{
	// パケットIDを取得.
	PacketId 	GetPacketId();

	// T型で定義されたパケットデータを取得.
	T 			GetPacket();

	// パケットデータをバイナリ化したデータを取得.
	byte[] 		GetData();
}
