// システムで使用するパケットのシリアライザー
//
// ■プログラムの説明
// システムで使用するパケットヘッダーのシリアライザーの定義です.
//

using UnityEngine;
using System.Collections;

// PacketHeader 構造体のシリアライザー.
public class HeaderSerializer : Serializer
{
	// PacketHeader 構造体をバイナリデータへシリアライズします.
	public bool Serialize(PacketHeader data)
	{
		// 既存のデータをクリアします.
		Clear();
		
		// 各要素を順番にシリアライズします.
		bool ret = true;
		ret &= Serialize((int)data.packetId);

		if (ret == false) {
			return false;
		}

		return true;	
	}

	// バイナリデータを PacketHeader 構造体へデシリアライズします.
	public bool Deserialize(ref PacketHeader serialized)
	{
		// デシリアライズするデータを設定します.
		bool ret = (GetDataSize() > 0)? true : false;
		if (ret == false) {
			return false;
		}
		
		// データの要素ごとにデシリアライズします.
		int packetId = 0;
		ret &= Deserialize(ref packetId);
		serialized.packetId = (PacketId)packetId;

		return ret;
	}	
}
