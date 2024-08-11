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
// マッチングパケット定義.
//

// マッチングリクエストパケット定義.
public class MatchingRequestPacket : IPacket<MatchingRequest>
{
	class MatchingRequestSerializer : Serializer
	{
		// MatchingRequest 構造体をバイナリデータへシリアライズします.
		public bool Serialize(MatchingRequest packet)
		{
			bool ret = true;

			ret &= Serialize(packet.version);
			int request = (int)packet.request;
			ret &= Serialize(request);
			ret &= Serialize(packet.roomId);
			ret &= Serialize(packet.name, MatchingRequest.roomNameLength);
			ret &= Serialize(packet.level);
			
			return ret;
		}

		// バイナリデータを MatchingRequest 構造体へデシリアライズします.
		public bool Deserialize(ref MatchingRequest element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.version);

			int request = 0;
			ret &= Deserialize(ref request);
			element.request = (MatchingRequestId) request;
			
			ret &= Deserialize(ref element.roomId);
			ret &= Deserialize(ref element.name, MatchingRequest.roomNameLength);
			ret &= Deserialize(ref element.level);
			
			return ret;
		}
	}
	
	// パケットデータの実体.
	MatchingRequest	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public MatchingRequestPacket(MatchingRequest data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public MatchingRequestPacket(byte[] data)
	{
		MatchingRequestSerializer serializer = new MatchingRequestSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.MatchingRequest;
	}

	// ゲームで使用する MatchingRequest 型で定義されたパケットデータを取得.
	public MatchingRequest	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		MatchingRequestSerializer serializer = new MatchingRequestSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// マッチングリクエストパケット定義.
public class MatchingResponsePacket : IPacket<MatchingResponse>
{
	class MatchingResponseSerializer : Serializer
	{
		// MatchingResponse 構造体をバイナリデータへシリアライズします.
		public bool Serialize(MatchingResponse packet)
		{
			bool ret = true;

			int result = (int)packet.result;
			ret &= Serialize(result);
			
			int request = (int)packet.request;
			ret &= Serialize(request);

			ret &= Serialize(packet.roomId);
			ret &= Serialize(packet.name, MatchingResponse.roomNameLength);
			ret &= Serialize(packet.members);
			
			return ret;
		}

		// バイナリデータを MatchingResponse 構造体へデシリアライズします.
		public bool Deserialize(ref MatchingResponse element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}
		
			bool ret = true;

			int result = 0;
			ret &= Deserialize(ref result);
			element.result = (MatchingResult) result;
			
			int request = 0;
			ret &= Deserialize(ref request);
			element.request = (MatchingRequestId) request;
			
			ret &= Deserialize(ref element.roomId);
			ret &= Deserialize(ref element.name, MatchingResponse.roomNameLength);
			ret &= Deserialize(ref element.members);
			
			return ret;
		}
	}
	
	// パケットデータの実体.
	MatchingResponse	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public MatchingResponsePacket(MatchingResponse data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public MatchingResponsePacket(byte[] data)
	{
		MatchingResponseSerializer serializer = new MatchingResponseSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// 同じパケットでIDだけ変更します.
	public PacketId	GetPacketId()
	{
		return PacketId.MatchingResponse;
	}

	// ゲームで使用する MatchingResponse 型で定義されたパケットデータを取得.
	public MatchingResponse	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		MatchingResponseSerializer serializer = new MatchingResponseSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// ルーム検索結果パケット定義.
public class SearchRoomPacket : IPacket<SearchRoomResponse>
{
	class SearchRoomSerializer : Serializer
	{
		// SearchRoomResponse 構造体をバイナリデータへシリアライズします.
		public bool Serialize(SearchRoomResponse packet)
		{
			bool ret = true;

			ret &= Serialize(packet.roomNum);
			
			for (int i = 0; i < packet.roomNum; ++i) {
				
				ret &= Serialize(packet.rooms[i].roomId);
				
				ret &= Serialize(packet.rooms[i].name, MatchingResponse.roomNameLength);
				
				ret &= Serialize(packet.rooms[i].members);
			}
			
			return ret;
		}

		// バイナリデータを SearchRoomResponse 構造体へデシリアライズします.
		public bool Deserialize(ref SearchRoomResponse element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.roomNum);
			
			element.rooms = new RoomInfo[element.roomNum];
			for (int i = 0; i < element.roomNum; ++i) {
				
				ret &= Deserialize(ref element.rooms[i].roomId);
				
				ret &= Deserialize(ref element.rooms[i].name, MatchingResponse.roomNameLength);
				
				ret &= Deserialize(ref element.rooms[i].members);
			}

			return ret;
		}
	}
	
	// パケットデータの実体.
	SearchRoomResponse	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public SearchRoomPacket(SearchRoomResponse data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public SearchRoomPacket(byte[] data)
	{
		SearchRoomSerializer serializer = new SearchRoomSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.SearchRoomResponse;
	}

	// ゲームで使用する SearchRoomResponse 型で定義されたパケットデータを取得.
	public SearchRoomResponse	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		SearchRoomSerializer serializer = new SearchRoomSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}


//
// セッション通知パケット定義.
//
public class SessionPacket : IPacket<SessionData>
{
	class SessionSerializer : Serializer
	{
		// SessionData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(SessionData packet)
		{
			bool ret = true;

			int result = (int)packet.result;
			ret &= Serialize(result);
			ret &= Serialize(packet.playerId);
			ret &= Serialize(packet.members);

			for (int i = 0; i < packet.members; ++i) {
				
				ret &= Serialize(packet.endPoints[i].ipAddress, EndPointData.ipAddressLength);
				ret &= Serialize(packet.endPoints[i].port);
			}
			
			return ret;
		}

		// バイナリデータを SessionData 構造体へデシリアライズします.
		public bool Deserialize(ref SessionData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;
			
			int result = 0;
			ret &= Deserialize(ref result);
			element.result = (MatchingResult) result;
			
			ret &= Deserialize(ref element.playerId);
			ret &= Deserialize(ref element.members);

			element.endPoints = new EndPointData[element.members];
			for (int i = 0; i < element.members; ++i) {
				
				ret &= Deserialize(ref element.endPoints[i].ipAddress, EndPointData.ipAddressLength);
				ret &= Deserialize(ref element.endPoints[i].port);
			}

			return ret;
		}
	}
	
	// パケットデータの実体.
	SessionData	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public SessionPacket(SessionData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public SessionPacket(byte[] data)
	{
		SessionSerializer serializer = new SessionSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.StartSessionNotify;
	}

	// ゲームで使用する SessionData 型で定義されたパケットデータを取得.
	public SessionData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		SessionSerializer serializer = new SessionSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}






//
//
// ゲーム用パケットデータ定義.
//
//

// ゲーム前の同期情報パケット定義.
public class EquipmentPacket : IPacket<CharEquipment>
{
	class EquipmentSerializer : Serializer
	{
		// CharEquipment 構造体をバイナリデータへシリアライズします.
		public bool Serialize(CharEquipment packet)
		{
			bool ret = true;
			
			ret &= Serialize(packet.globalId);
			ret &= Serialize(packet.shotType);

			return ret;
		}

		// バイナリデータを CharEquipment 構造体へデシリアライズします.
		public bool Deserialize(ref CharEquipment element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.globalId);
			ret &= Deserialize(ref element.shotType);

			return ret;
		}
	}
	
	// パケットデータの実体.
	CharEquipment	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public EquipmentPacket(CharEquipment data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public EquipmentPacket(byte[] data)
	{
		EquipmentSerializer serializer = new EquipmentSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.Equip;
	}

	// ゲームで使用する CharEquipment 型で定義されたパケットデータを取得.
	public CharEquipment	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		EquipmentSerializer serializer = new EquipmentSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}



// ゲーム前の同期情報パケット定義.
public class GameSyncPacket : IPacket<GameSyncInfo>
{
	class GameSyncerializer : Serializer
	{
		// GameSyncInfo 構造体をバイナリデータへシリアライズします.
		public bool Serialize(GameSyncInfo packet)
		{
			bool ret = true;

			// 同期する乱数の種.
			ret &= Serialize(packet.seed);

			// 同期する装備情報.
			for (int i = 0; i < NetConfig.PLAYER_MAX; ++i) {
				// キャラクターのグローバルID.
				ret &= Serialize(packet.items[i].globalId);	
				// キャラクターのグローバルID.
				ret &= Serialize(packet.items[i].shotType);
			}

			return ret;
		}

		// バイナリデータを GameSyncInfo 構造体へデシリアライズします.
		public bool Deserialize(ref GameSyncInfo element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}
	
			bool ret = true;

			// 同期する乱数の種.
			ret &= Deserialize(ref element.seed);
			
			// 同期する装備情報.
			element.items = new CharEquipment[NetConfig.PLAYER_MAX];
			for (int i = 0; i < NetConfig.PLAYER_MAX; ++i) {
				// キャラクターのグローバルID.
				ret &= Deserialize(ref element.items[i].globalId);	
				// キャラクターのグローバルID.
				ret &= Deserialize(ref element.items[i].shotType);
			}

			return ret;
		}
	}
	
	
	// パケットデータの実体.
	GameSyncInfo	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public GameSyncPacket(GameSyncInfo data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public GameSyncPacket(byte[] data)
	{
		GameSyncerializer serializer = new GameSyncerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.GameSyncInfo;
	}

	// ゲームで使用する GameSyncInfo 型で定義されたパケットデータを取得.
	public GameSyncInfo	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		GameSyncerializer serializer = new GameSyncerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
	
}

// キャラクター座標パケット定義.
public class CharacterDataPacket : IPacket<CharacterData>
{
	class CharactorDataSerializer : Serializer
	{
		// CharacterData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(CharacterData packet)
		{
		
			bool ret = true;

			ret &= Serialize(packet.characterId, CharacterData.characterNameLength);
			ret &= Serialize(packet.index);
			ret &= Serialize(packet.dataNum);

			for (int i = 0; i < packet.dataNum; ++i) {
				// CharactorCoord
				ret &= Serialize(packet.coordinates[i].x);
				ret &= Serialize(packet.coordinates[i].z);
			}	
			
			return ret;
		}

		// バイナリデータを CharacterData 構造体へデシリアライズします.
		public bool Deserialize(ref CharacterData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.characterId, CharacterData.characterNameLength);
			ret &= Deserialize(ref element.index);
			ret &= Deserialize(ref element.dataNum);

			element.coordinates = new CharacterCoord[element.dataNum];
			for (int i = 0; i < element.dataNum; ++i) {
				// CharactorCoord
				ret &= Deserialize(ref element.coordinates[i].x);
				ret &= Deserialize(ref element.coordinates[i].z);
			}
			
			return ret;
		}
	}
	
	// パケットデータの実体.
	CharacterData		m_packet;
	
	public CharacterDataPacket(CharacterData data)
	{
		m_packet = data;
	}
	
	public CharacterDataPacket(byte[] data)
	{
		CharactorDataSerializer serializer = new CharactorDataSerializer();
		
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
		CharactorDataSerializer serializer = new CharactorDataSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// キャラクター攻撃パケット定義.
public class AttackPacket : IPacket<AttackData>
{
	protected class AttackDataSerializer : Serializer
	{
		// AttackData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(AttackData packet)
		{
			bool ret = true;
			
			ret &= Serialize(packet.characterId, AttackData.characterNameLength);
			ret &= Serialize(packet.attackKind);

			return ret;
		}

		// バイナリデータを AttackData 構造体へデシリアライズします.
		public bool Deserialize(ref AttackData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}
			
			bool ret = true;
			
			ret &= Deserialize(ref element.characterId, AttackData.characterNameLength);
			ret &= Deserialize(ref element.attackKind);

			return true;
		}
	}
	
	// パケットデータの実体.
	AttackData m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public AttackPacket(AttackData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public AttackPacket(byte[] data)
	{
		AttackDataSerializer serializer = new AttackDataSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.AttackData;
	}

	// ゲームで使用する AttackData 型で定義されたパケットデータを取得.
	public AttackData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		AttackDataSerializer serializer = new AttackDataSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}



// アイテムパケット定義.
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
			ret &= Serialize(packet.ownerId, ItemData.charactorNameLength);
			
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
			ret &= Deserialize(ref element.ownerId, ItemData.charactorNameLength);
			
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
	
	// 送信用のbyte[] 型のデータを取得.
	public byte[] GetData()
	{
		ItemSerializer serializer = new ItemSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// ドーナツに入った状態のパケット.
public class DoorPacket : IPacket<CharDoorState>
{
	class DoorSerializer : Serializer
	{
		// CharDoorState 構造体をバイナリデータへシリアライズします.
		public bool Serialize(CharDoorState packet)
		{
			bool ret = true;

			ret &= Serialize(packet.globalId);
			ret &= Serialize(packet.keyId, CharDoorState.keyNameLength);
			ret &= Serialize(packet.isInTrigger);
			ret &= Serialize(packet.hasKey);

			return ret;
		}

		// バイナリデータを CharDoorState 構造体へデシリアライズします.
		public bool Deserialize(ref CharDoorState element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.globalId);
			ret &= Deserialize(ref element.keyId, CharDoorState.keyNameLength);
			ret &= Deserialize(ref element.isInTrigger);
			ret &= Deserialize(ref element.hasKey);

			return ret;
		}
	}
	
	// パケットデータの実体.
	CharDoorState	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public DoorPacket(CharDoorState data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public DoorPacket(byte[] data)
	{
		DoorSerializer serializer = new DoorSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.DoorState;
	}

	// ゲームで使用する CharDoorState 型で定義されたパケットデータを取得.
	public CharDoorState	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		DoorSerializer serializer = new DoorSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}


// ルーム移動パケット定義.
public class RoomPacket : IPacket<MovingRoom>
{
	class RoomSerializer : Serializer
	{
		// MovingRoom 構造体をバイナリデータへシリアライズします.
		public bool Serialize(MovingRoom packet)
		{
			bool ret = true;

			ret &= Serialize(packet.keyId, MovingRoom.keyNameLength);
			
			return ret;
		}

		// バイナリデータを MovingRoom 構造体へデシリアライズします.
		public bool Deserialize(ref MovingRoom element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.keyId, MovingRoom.keyNameLength);
			
			return ret;
		}
	}
	
	
	// パケットデータの実体.
	MovingRoom	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public RoomPacket(MovingRoom data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public RoomPacket(byte[] data)
	{
		RoomSerializer serializer = new RoomSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.MovingRoom;
	}

	// ゲームで使用する MovingRoom 型で定義されたパケットデータを取得.
	public MovingRoom	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		RoomSerializer serializer = new RoomSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
	
}


// アイテム使用パケット定義.
public class ItemUsePacket : IPacket<ItemUseData>
{
	class ItemUseSerializer : Serializer
	{
		// ItemUseData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(ItemUseData packet)
		{
			bool ret = true;
			ret &= Serialize(packet.itemFavor);
			ret &= Serialize(packet.targetId, ItemUseData.characterNameLength);
			ret &= Serialize(packet.userId, ItemUseData.characterNameLength);
			ret &= Serialize(packet.itemCategory);

			return true;
		}

		// バイナリデータを ItemUseData 構造体へデシリアライズします.
		public bool Deserialize(ref ItemUseData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;
			ret &= Deserialize(ref element.itemFavor);
			ret &= Deserialize(ref element.targetId, ItemUseData.characterNameLength);
			ret &= Deserialize(ref element.userId, ItemUseData.characterNameLength);
			ret &= Deserialize(ref element.itemCategory);

			return true;
		}
	}
	
	// パケットデータの実体.
	ItemUseData	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public ItemUsePacket(ItemUseData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public ItemUsePacket(byte[] data)
	{
		ItemUseSerializer serializer = new ItemUseSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}
	
	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.UseItem;
	}

	// ゲームで使用する ItemUseData 型で定義されたパケッ
	public ItemUseData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		ItemUseSerializer serializer = new ItemUseSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// モンスターリスポーンパケット定義.
public class MonsterPacket : IPacket<MonsterData>
{
	protected class MonsterDataSerializer : Serializer
	{
		// MonsterData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(MonsterData packet)
		{
			bool ret = true;

			ret &= Serialize(packet.lairId, MonsterData.monsterNameLength);
			ret &= Serialize(packet.monsterId, MonsterData.monsterNameLength);

			return ret;
		}

		// バイナリデータを MonsterData 構造体へデシリアライズします.
		public bool Deserialize(ref MonsterData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.lairId, MonsterData.monsterNameLength);
			ret &= Deserialize(ref element.monsterId, MonsterData.monsterNameLength);

			return true;
		}
	}
	
	// パケットデータの実体.
	MonsterData m_packet;

	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public MonsterPacket(MonsterData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public MonsterPacket(byte[] data)
	{
		MonsterDataSerializer serializer = new MonsterDataSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}
	

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.MonsterData;
	}

	// ゲームで使用する MonsterData 型で定義されたパケットデータを取得.
	public MonsterData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		MonsterDataSerializer serializer = new MonsterDataSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// HP通知パケット定義.
public class HitPointPacket : IPacket<HpData>
{
	protected class HpDataSerializer : Serializer
	{
		// HpData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(HpData packet)
		{
			bool ret = true;

			ret &= Serialize(packet.characterId, HpData.characterNameLength);
			ret &= Serialize (packet.hp);
			
			return ret;
		}

		// バイナリデータを HpData 構造体へデシリアライズします.
		public bool Deserialize(ref HpData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.characterId, HpData.characterNameLength);
			ret &= Deserialize (ref element.hp);
			
			return ret;
		}
	}
	
	// パケットデータの実体.
	HpData m_packet;

	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public HitPointPacket(HpData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public HitPointPacket(byte[] data)
	{
		HpDataSerializer serializer = new HpDataSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.HpData;
	}

	// ゲームで使用する HpData 型で定義されたパケットデータを取得.
	public HpData	GetPacket()
	{
		return m_packet;
	}


	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		HpDataSerializer serializer = new HpDataSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}


// ダメージ量パケット定義.
public class DamageDataPacket : IPacket<DamageData>
{
	protected class DamageDataSerializer : Serializer
	{
		// DamageData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(DamageData packet)
		{
			bool ret = true;

			ret &= Serialize(packet.target, DamageData.characterNameLength);
			ret &= Serialize(packet.attacker);
			ret &= Serialize (packet.damage);
			
			return ret;
		}

		// バイナリデータを DamageData 構造体へデシリアライズします.
		public bool Deserialize(ref DamageData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.target, DamageData.characterNameLength);
			ret &= Deserialize(ref element.attacker);
			ret &= Deserialize (ref element.damage);
			
			return ret;
		}
	}

	// パケットデータの実体.
	protected DamageData m_packet;
	
	// 継承用ののコンストラクタ.
	public DamageDataPacket()
	{
	}

	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public DamageDataPacket(DamageData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public DamageDataPacket(byte[] data)
	{
		DamageDataSerializer serializer = new DamageDataSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	virtual public PacketId	GetPacketId()
	{
		return PacketId.DamageData;
	}

	// ゲームで使用する DamageData 型で定義されたパケットデータを取得.
	public DamageData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		DamageDataSerializer serializer = new DamageDataSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

public class DamageNotifyPacket : DamageDataPacket
{

	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public DamageNotifyPacket(DamageData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public DamageNotifyPacket(byte[] data)
	{
		DamageDataSerializer serializer = new DamageDataSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public override PacketId	GetPacketId()
	{
		return PacketId.DamageNotify;
	}
}

// 召喚獣の出現パケット定義.
public class SummonPacket : IPacket<SummonData>
{
	class SummonSerializer : Serializer
	{
		// SummonData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(SummonData packet)
		{
			bool ret = true;

			ret &= Serialize(packet.summon, SummonData.summonNameLength);
			
			return ret;
		}

		// バイナリデータを SummonData 構造体へデシリアライズします.
		public bool Deserialize(ref SummonData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.summon, SummonData.summonNameLength);
			
			return ret;
		}
	}
	
	// パケットデータの実体.
	SummonData	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public SummonPacket(SummonData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public SummonPacket(byte[] data)
	{
		SummonSerializer serializer = new SummonSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}


	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.Summon;
	}

	// ゲームで使用する SummonData 型で定義されたパケットデータを取得.
	public SummonData	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		SummonSerializer serializer = new SummonSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// ボス直接攻撃パケット定義.
public class BossDirectPacket : IPacket<BossDirectAttack>
{
	class DirectAttackSerializer : Serializer
	{
		// BossDirectAttack 構造体をバイナリデータへシリアライズします.
		public bool Serialize(BossDirectAttack packet)
		{
			bool ret = true;

			ret &= Serialize(packet.target, BossDirectAttack.characterNameLength);
			ret &= Serialize(packet.power);

			return ret;
		}

		// バイナリデータを BossDirectAttack 構造体へデシリアライズします.
		public bool Deserialize(ref BossDirectAttack element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.target, BossDirectAttack.characterNameLength);
			ret &= Deserialize(ref element.power);

			return ret;
		}
	}
	
	// パケットデータの実体.
	BossDirectAttack	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public BossDirectPacket(BossDirectAttack data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public BossDirectPacket(byte[] data)
	{
		DirectAttackSerializer serializer = new DirectAttackSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.BossDirectAttack;
	}

	// ゲームで使用する BossDirectAttack 型で定義されたパケットデータを取得.
	public BossDirectAttack	GetPacket()
	{
		return m_packet;
	}

	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		DirectAttackSerializer serializer = new DirectAttackSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// ボス範囲攻撃パケット定義.
public class BossRangePacket : IPacket<BossRangeAttack>
{
	class RangeAttackSerializer : Serializer
	{
		// BossRangeAttack 構造体をバイナリデータへシリアライズします.
		public bool Serialize(BossRangeAttack packet)
		{
			bool ret = true;

			ret &= Serialize(packet.power);
			ret &= Serialize(packet.range);

			return ret;
		}

		// バイナリデータを BossRangeAttack 構造体へデシリアライズします.
		public bool Deserialize(ref BossRangeAttack element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			ret &= Deserialize(ref element.power);
			ret &= Deserialize(ref element.range);

			return ret;
		}
	}
	
	// パケットデータの実体.
	BossRangeAttack	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public BossRangePacket(BossRangeAttack data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public BossRangePacket(byte[] data)
	{
		RangeAttackSerializer serializer = new RangeAttackSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}


	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.BossRangeAttack;
	}

	// ゲームで使用する BossRangeAttack 型で定義されたパケットデータを取得.
	public BossRangeAttack	GetPacket()
	{
		return m_packet;
	}
	
	
	public byte[] GetData()
	{
		RangeAttackSerializer serializer = new RangeAttackSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// ボスクイック攻撃パケット定義.
public class BossQuickPacket : IPacket<BossQuickAttack>
{
	class DirectQuickSerializer : Serializer
	{
		// BossQuickAttack 構造体をバイナリデータへシリアライズします.
		public bool Serialize(BossQuickAttack packet)
		{
			bool ret = true;
			
			ret &= Serialize(packet.target, BossQuickAttack.characterNameLength);
			ret &= Serialize(packet.power);
			
			return ret;
		}

		// バイナリデータを BossQuickAttack 構造体へデシリアライズします.
		public bool Deserialize(ref BossQuickAttack element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}
			
			bool ret = true;
			
			ret &= Deserialize(ref element.target, BossQuickAttack.characterNameLength);
			ret &= Deserialize(ref element.power);
			
			return ret;
		}
	}
	
	// パケットデータの実体.
	BossQuickAttack	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public BossQuickPacket(BossQuickAttack data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public BossQuickPacket(byte[] data)
	{
		DirectQuickSerializer serializer = new DirectQuickSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.BossQuickAttack;
	}

	// ゲームで使用する BossQuickAttack 型で定義されたパケットデータを取得.
	public BossQuickAttack	GetPacket()
	{
		return m_packet;
	}


	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		DirectQuickSerializer serializer = new DirectQuickSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// ボス死亡通知パケット定義.
public class BossDeadPacket : IPacket<BossDead>
{
	class BossDeadSerializer : Serializer
	{
		// BossDead 構造体をバイナリデータへシリアライズします.
		public bool Serialize(BossDead packet)
		{
			bool ret = true;
			
			ret &= Serialize(packet.bossId, BossDead.bossNameLength);
			
			return ret;
		}

		// バイナリデータを BossDead 構造体へデシリアライズします.
		public bool Deserialize(ref BossDead element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}
			
			bool ret = true;
			
			ret &= Deserialize(ref element.bossId, BossDead.bossNameLength);
			
			return ret;
		}
	}
	
	// パケットデータの実体.
	BossDead	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public BossDeadPacket(BossDead data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public BossDeadPacket(byte[] data)
	{
		BossDeadSerializer serializer = new BossDeadSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}

	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.BossDead;
	}

	// ゲームで使用する BossDead 型で定義されたパケットデータを取得.
	public BossDead	GetPacket()
	{
		return m_packet;
	}


	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		BossDeadSerializer serializer = new BossDeadSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}


// ご褒美ケーキ情報パケット定義.
public class PrizePacket : IPacket<PrizeData>
{
	class PrizeSerializer : Serializer
	{
		// PrizeData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(PrizeData packet)
		{
			bool ret = true;

			// キャラクターID.	
			ret &= Serialize(packet.characterId, PrizeData.characterNameLength);
			// ケーキの数.
			ret &= Serialize(packet.cakeNum);

			return ret;
		}

		// バイナリデータを PrizeData 構造体へデシリアライズします.
		public bool Deserialize(ref PrizeData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;

			// キャラクターID.	
			ret &= Deserialize(ref element.characterId, PrizeData.characterNameLength);
			// ケーキの数.
			ret &= Deserialize(ref element.cakeNum);

			return ret;
		}
	}
	
	// パケットデータの実体.
	PrizeData	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public PrizePacket(PrizeData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public PrizePacket(byte[] data)
	{
		PrizeSerializer serializer = new PrizeSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}


	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.Prize;
	}

	// ゲームで使用する PrizeData 型で定義されたパケットデータを取得.
	public PrizeData	GetPacket()
	{
		return m_packet;
	}


	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		PrizeSerializer serializer = new PrizeSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// ご褒美ケーキの結果情報パケット定義.
public class PrizeResultPacket : IPacket<PrizeResultData>
{
	class PrizeResultSerializer : Serializer
	{
		// PrizeResultData 構造体をバイナリデータへシリアライズします.
		public bool Serialize(PrizeResultData packet)
		{
			bool ret = true;

			// ケーキデータの数.	
			ret &= Serialize(packet.cakeDataNum);
			// ケーキの数.
			for (int i = 0; i < 4; ++i) {
				ret &= Serialize(packet.cakeNum[i]);
			}

			return ret;
		}

		// バイナリデータを PrizeResultData 構造体へデシリアライズします.
		public bool Deserialize(ref PrizeResultData element)
		{
			if (GetDataSize() == 0) {
				// データが設定されていない.
				return false;
			}

			bool ret = true;
			
			// ケーキデータの数.	
			ret &= Deserialize(ref element.cakeDataNum);
			// ケーキの数.
			element.cakeNum = new int[element.cakeDataNum];
			for (int i = 0; i < element.cakeDataNum; ++i) {
				ret &= Deserialize(ref element.cakeNum[i]);
			}

			return ret;
		}
	}
	
	// パケットデータの実体.
	PrizeResultData	m_packet;
	
	
	// パケットデータをバイナリデータにシリアライズするためのコンストラクタ.
	public PrizeResultPacket(PrizeResultData data)
	{
		m_packet = data;
	}
	
	// バイナリデータをパケットデータにデシリアライズするためのコンストラクタ.
	public PrizeResultPacket(byte[] data)
	{
		PrizeResultSerializer serializer = new PrizeResultSerializer();
		
		serializer.SetDeserializedData(data);
		serializer.Deserialize(ref m_packet);
	}


	// パケットIDを取得.
	public PacketId	GetPacketId()
	{
		return PacketId.PrizeResult;
	}

	// ゲームで使用する PrizeResultData 型で定義されたパケットデータを取得.
	public PrizeResultData	GetPacket()
	{
		return m_packet;
	}


	// 送信用の byte[] 型のデータを取得.
	public byte[] GetData()
	{
		PrizeResultSerializer serializer = new PrizeResultSerializer();
		
		serializer.Serialize(m_packet);
		
		return serializer.GetSerializedData();
	}
}

// チャットパケット定義.
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

			return ret;
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
