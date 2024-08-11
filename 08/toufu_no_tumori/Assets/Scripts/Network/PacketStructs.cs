// 送受信するデータの構造体定義
//
// ■プログラムの説明
// 送受信する各種データの構造体を定義しています.
// これらの構造体を IPacket のテンプレートクラスを継承したパケットクラスに渡して使用します.
// 各パケットのIDがユニーク(唯一の値)になるようにIDを割り当てています.
//

using UnityEngine;
using System;
using System.Collections;
using System.Net;

// パケットID定義.
public enum PacketId
{
	// ゲーム用パケット.
	GameSyncInfo,
	GameSyncInfoHouse,
	CharacterData,
	ItemData,
	Moving,
	GoingOut,
	ChatMessage,
	
	Max,
}

//
//
// システム用パケットデータ定義.
//
//

//
// パケットヘッダー.
// ゲーム用、その他パケットデータに情報を付加して送信するデータです.
//
public struct PacketHeader
{
	// パケット種別.
	public PacketId 	packetId;
}


//
//
// ゲーム用パケットデータ定義.
//
//


//
// ゲーム前の同期情報.
//
public struct SyncGameData
{
	public int			version;	// パケットID.
	public MovingData	moving;		// 引越し情報.
	public int 			itemNum;	// アイテム情報数.
	public ItemData[]	items;		// アイテム情報.
}

//
// アイテム取得情報.
//
public struct ItemData
{
	public string 		itemId;		// アイテム識別子.
	public int			state;		// アイテムの取得状態.
	public string 		ownerId;	// 所有者ID.

	public const int 	itemNameLength = 32;		// アイテム名の長さ.
	public const int 	characterNameLength = 64;	// キャラクターIDの長さ.
}

//
// キャラクター座標情報.
//
public struct CharacterCoord
{
	public float	x;		// キャラクターのx座標.
	public float	z;		// キャラクターのz座標.
	
	// ゲームで扱う型のまま設定できるようにメソッドを定義しています.
	public CharacterCoord(float x, float z)
	{
		this.x = x;
		this.z = z;
	}
	public Vector3	ToVector3()
	{
		return(new Vector3(this.x, 0.0f, this.z));
	}
	public static CharacterCoord	FromVector3(Vector3 v)
	{
		return(new CharacterCoord(v.x, v.z));
	}
	
	public static CharacterCoord	Lerp(CharacterCoord c0, CharacterCoord c1, float rate)
	{
		CharacterCoord	c;
		
		c.x = Mathf.Lerp(c0.x, c1.x, rate);
		c.z = Mathf.Lerp(c0.z, c1.z, rate);
		
		return(c);
	}
}

//
// キャラクターの移動情報.
//
public struct CharacterData
{
	public string			characterId;	// キャラクターID.
	public int 				index;			// 位置座標のインデックス.
	public int				dataNum;		// 座標データ数.
	public CharacterCoord[]	coordinates;	// 座標データ.
	
	public const int 	characterNameLength = 64;	// キャラクターIDの長さ.
}

//
// 引越し情報.
//
public struct MovingData
{
	public string		characterId;	// キャラクターID.
	public string		houseId;		// 家のID.
	public bool 		moving;			// 引越し情報.

	public const int 	characterNameLength = 64;	// キャラクターIDの長さ.
	public const int 	houseIdLength = 32;			// 家IDの長さ.
}

//
// お庭移動情報.
//
public struct GoingOutData
{
	public string		characterId;	// キャラクターID.
	public bool 		goingOut;		// 遊びに行く情報.

	public const int 	characterNameLength = 64;	// キャラクターIDの長さ.
}

//
// チャットメッセージ.
//
public struct ChatMessage
{
	public string		characterId; // キャラクターID.
	public string		message;	 // チャットメッセージ.

	public const int 	characterNameLength = 64;	// キャラクターIDの長さ.
	public const int	messageLength = 64;
}

