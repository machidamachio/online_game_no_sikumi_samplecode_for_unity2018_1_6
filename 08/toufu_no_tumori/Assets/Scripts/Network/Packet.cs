// 送受信するパケット定義
//
// ■プログラムの説明
// 送受信する各種パケットを定義しています.
// これらのパケットは IPacket のテンプレートクラスを継承したています.
// 各パケットの基底クラスである IPacket に送受信するデータの構造体を渡します.
// 定義されたパケットクラスは Serializer クラスを継承したそのクラス専用のシリアライザークラスを内部に持つようにしています.
// パケットデータの設定時や取得に専用のシリアライザーを処理させることにより
// 送信時はネットワークバイトオーダーに、受信時はホストバイトオーダーに変化しています.
// このようにすることでアプリケーションはデータの構造体をパケットに設定するだけで
// 通信に関するバイトオーダー変換、シリアライズを意識することなく送受信することができるようになります.
//

using System.Collections;
using System.IO;

//
// ゲーム前同期パケット定義(アイテム用).
//
public class SyncGamePacket : IPacket<SyncGameData>
{
	public class GameSyncSerializer : Serializer
	{
		// SyncGameData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(SyncGameData packet)
		{
			
			bool ret = true;
			ret &= Serialize(packet.version);

			ret &= Serialize(packet.moving.characterId, MovingData.characterNameLength);
			ret &= Serialize(packet.moving.houseId, MovingData.houseIdLength);
			ret &= Serialize(packet.moving.moving);
		
			ret &= Serialize(packet.itemNum);		
			for (int i = 0; i < packet.itemNum; ++i) {
				// CharacterCoord
				ret &= Serialize(packet.items[i].itemId, ItemData.itemNameLength);
				ret &= Serialize(packet.items[i].state);
				ret &= Serialize(packet.items[i].ownerId, ItemData.characterNameLength);
			}	
			
			return ret;
		}

		// バイナリデータを SyncGameData 構造体へデシリアライズします.
		public bool Deserialize(ref SyncGameData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;
			ret &= Deserialize(ref element.version);

			// MovingData構造体.
			ret &= Deserialize(ref element.moving.characterId, MovingData.characterNameLength);
			ret &= Deserialize(ref element.moving.houseId, MovingData.houseIdLength);
			ret &= Deserialize(ref element.moving.moving);

			ret &= Deserialize(ref element.itemNum);
			element.items = new ItemData[element.itemNum];
			for (int i = 0; i < element.itemNum; ++i) {
				// ItemData
				ret &= Deserialize(ref element.items[i].itemId, ItemData.itemNameLength);
				ret &= Deserialize(ref element.items[i].state);
				ret &= Deserialize(ref element.items[i].ownerId, ItemData.characterNameLength);
			}
			
			return ret;
		}
	}
	
	// パケットデータの実体.
	SyncGameData		m_packet;

	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public SyncGamePacket(SyncGameData data)
	{
		m_packet = data;
	}

	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public SyncGamePacket(byte[] data)
	{
		GameSyncSerializer serializer = new GameSyncSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.GameSyncInfo;
	}

	// ゲームで使用する SyncGameData 型で定義されたパケットデータを取得.
	public SyncGameData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		GameSyncSerializer serializer = new GameSyncSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

//
// ゲーム前同期パケット定義(引越し用).
//
public class SyncGamePacketHouse : IPacket<SyncGameData>
{
	// パケットデータの実体.
	SyncGameData		m_packet;

	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public SyncGamePacketHouse(SyncGameData data)
	{
		m_packet = data;
	}

	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public SyncGamePacketHouse(byte[] data)
	{
		SyncGamePacket.GameSyncSerializer serializer = new SyncGamePacket.GameSyncSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// 同じパケットでIDだけ変更します.
	public PacketId	GetPacketId()
	{
		return PacketId.GameSyncInfoHouse;
	}

	// ゲームで使用する SyncGameData 型で定義されたパケットデータを取得.
	public SyncGameData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		SyncGamePacket.GameSyncSerializer serializer = new SyncGamePacket.GameSyncSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

//
// アイテムパケット定義.
//
public class ItemPacket : IPacket<ItemData>
{
	class ItemSerializer : Serializer
	{
		// ItemData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(ItemData packet)
		{
			bool ret = true;

			ret &= Serialize(packet.itemId, ItemData.itemNameLength);
			ret &= Serialize(packet.state);
			ret &= Serialize(packet.ownerId, ItemData.characterNameLength);
			
			return ret;
		}

		// バイナリデータを ItemData 構造体へデシリアライズします.
		public bool Deserialize(ref ItemData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.itemId, ItemData.itemNameLength);
			ret &= Deserialize(ref element.state);
			ret &= Deserialize(ref element.ownerId, ItemData.characterNameLength);
			
			return ret;
		}
	}
	
	// パケットデータの実体.
	ItemData	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public ItemPacket(ItemData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public ItemPacket(byte[] data)
	{
		ItemSerializer serializer = new ItemSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.ItemData;
	}

	// ゲームで使用する ItemData 型で定義されたパケットデータを取得.
	public ItemData	GetPacket()
	{
		return m_packet;
	}
	
	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		ItemSerializer serializer = new ItemSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

//
// キャラクター座標パケット定義.
//
public class CharacterDataPacket : IPacket<CharacterData>
{
	class CharacterDataSerializer : Serializer
	{
		// CharacterData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(CharacterData packet)
		{
			
			Serialize(packet.characterId, CharacterData.characterNameLength);
			
			Serialize(packet.index);
			Serialize(packet.dataNum);
			
			for (int i = 0; i < packet.dataNum; ++i) {
				// CharacterCoord
				Serialize(packet.coordinates[i].x);
				Serialize(packet.coordinates[i].z);
			}	
			
			return true;
		}

		// バイナリデータを CharacterData 構造体へデシリアライズします.
		public bool Deserialize(ref CharacterData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}
			
			Deserialize(ref element.characterId, CharacterData.characterNameLength);
			
			Deserialize(ref element.index);
			Deserialize(ref element.dataNum);
			
			element.coordinates = new CharacterCoord[element.dataNum];
			for (int i = 0; i < element.dataNum; ++i) {
				// CharacterCoord
				Deserialize(ref element.coordinates[i].x);
				Deserialize(ref element.coordinates[i].z);
			}
			
			return true;
		}
	}
	
	// パケットデータの実体.
	CharacterData		m_packet;

	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public CharacterDataPacket(CharacterData data)
	{
		m_packet = data;
	}

	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public CharacterDataPacket(byte[] data)
	{
		CharacterDataSerializer serializer = new CharacterDataSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.CharacterData;
	}

	// ゲームで使用する CharacterData 型で定義されたパケットデータを取得.
	public CharacterData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		CharacterDataSerializer serializer = new CharacterDataSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

//
// 引越しパケット定義.
//
public class MovingPacket : IPacket<MovingData>
{
	class MovingSerializer : Serializer
	{
		// MovingData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(MovingData packet)
		{
			
			bool ret = true;
			
			ret &= Serialize(packet.characterId, MovingData.characterNameLength);
			ret &= Serialize(packet.houseId, MovingData.houseIdLength);
			ret &= Serialize(packet.moving);

			return ret;
		}

		// バイナリデータを MovingData 構造体へデシリアライズします.
		public bool Deserialize(ref MovingData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}
			
			bool ret = true;
			ret &= Deserialize(ref element.characterId, MovingData.characterNameLength);
			ret &= Deserialize(ref element.houseId, MovingData.houseIdLength);
			ret &= Deserialize(ref element.moving);

			return ret;
		}
	}
	
	// パケットデータの実体.
	MovingData		m_packet;

	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public MovingPacket(MovingData data)
	{
		m_packet = data;
	}

	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public MovingPacket(byte[] data)
	{
		MovingSerializer serializer = new MovingSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.Moving;
	}

	// ゲームで使用する MovingData 型で定義されたパケットデータを取得.
	public MovingData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		MovingSerializer serializer = new MovingSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

//
// 引越しパケット定義.
//
public class GoingOutPacket : IPacket<GoingOutData>
{
	class GoingOutDataSerializer : Serializer
	{
		// GoingOutData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(GoingOutData packet)
		{
			
			bool ret = true;
			
			ret &= Serialize(packet.characterId, MovingData.characterNameLength);
			ret &= Serialize(packet.goingOut);

			return ret;
		}

		// バイナリデータを GoingOutData 構造体へデシリアライズします.
		public bool Deserialize(ref GoingOutData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}
			
			bool ret = true;
			ret &= Deserialize(ref element.characterId, MovingData.characterNameLength);
			ret &= Deserialize(ref element.goingOut);

			return ret;
		}
	}
	
	// パケットデータの実体.
	GoingOutData		m_packet;

	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public GoingOutPacket(GoingOutData data)
	{
		m_packet = data;
	}

	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public GoingOutPacket(byte[] data)
	{
		GoingOutDataSerializer serializer = new GoingOutDataSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.GoingOut;
	}

	// ゲームで使用する GoingOutData 型で定義されたパケットデータを取得.
	public GoingOutData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		GoingOutDataSerializer serializer = new GoingOutDataSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

//
// チャットパケット定義.
//
public class ChatPacket : IPacket<ChatMessage>
{
	class ChatSerializer : Serializer
	{
		// ChatMessage 構造体をバイナリデータへシリアライズします.
		public bool Serialize(ChatMessage packet)
		{
			bool ret = true;

			ret &= Serialize(packet.characterId, ChatMessage.characterNameLength);
			ret &= Serialize(packet.message, ChatMessage.messageLength);

			return ret;
		}

		// バイナリデータを ChatMessage 構造体へデシリアライズします.
		public bool Deserialize(ref ChatMessage element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.characterId, ChatMessage.characterNameLength);
			ret &= Deserialize(ref element.message, ChatMessage.messageLength);

			return true;
		}
	}
	
	// パケットデータの実体.
	ChatMessage	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public ChatPacket(ChatMessage data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public ChatPacket(byte[] data)
	{
		ChatSerializer serializer = new ChatSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}


	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.ChatMessage;
	}

	// ゲームで使用する ChatMessage 型で定義されたパケットデータを取得.
	public ChatMessage	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		ChatSerializer serializer = new ChatSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}
