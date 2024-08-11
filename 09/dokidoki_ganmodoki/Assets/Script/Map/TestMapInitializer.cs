using UnityEngine;
using System.Collections;

public class TestMapInitializer : MapInitializer {

	public int		LevelGeneratorSeed = 5;				//< ジェネレータにわたすシード.
	public bool		UseRandomSeedForDebug = false;		//< LevelGeneratorSeedを使用せずに、.
	public int		RandomSeedForDebugMin = 0;			//< UseRandomSeedForDebugが有効な時に使用するランダムのレンジ.
	public int		RandomSeedForDebugMax = 100;		//< UseRandomSeedForDebugが有効な時に使用するランダムのレンジ.

	public override void initializeMap(GameRoot game_root)
	{
		MapCreator mapCreator = MapCreator.getInstance ();

		Vector3 start_position;

		int seed = 0;

		if (UseRandomSeedForDebug)
		{
			seed = Random.Range(RandomSeedForDebugMin, RandomSeedForDebugMax);
		}
		else
		{
			seed = LevelGeneratorSeed;
		}

		// マップを自動生成.

		mapCreator.generateLevel(seed);

		// ローカルプレイヤーを作成. FIXME: 通信対応.
		PartyControl.getInstance().createLocalPlayer(GlobalParam.getInstance().global_account_id);
		
		// Put items including keys.
		mapCreator.generateItems(PartyControl.get().getLocalPlayer().getAcountID());
		
		// Put lairs in the map.
		//mapCreator.generateLairs();
		
		// FIXME: ここは座標書き込み命令になるのかな？
		// とりあえず座標を直接書き込む.
		start_position = mapCreator.getPlayerStartPosition();

		PartyControl.getInstance().getLocalPlayer().transform.position = start_position;
		
		mapCreator.createFloorDoor();
		
		// Activate the start room.
		mapCreator.SetCurrentRoom(mapCreator.getRoomFromPosition(start_position));

		{
			string	local_player_id = PartyControl.get().getLocalPlayer().getAcountID();

			ItemManager.getInstance().createItem("cake00",    local_player_id);
			ItemManager.getInstance().createItem("candy00",   local_player_id);
			ItemManager.getInstance().createItem("ice00",     local_player_id);
			
			ItemManager.getInstance().createItem("key00",     local_player_id);
			ItemManager.getInstance().createItem("key01",     local_player_id);
			ItemManager.getInstance().createItem("key02",     local_player_id);
			ItemManager.getInstance().createItem("key03",     local_player_id);
			//ItemManager.getInstance().createItem("key04",     local_player_id);
			
			ItemManager.getInstance().setPositionToItem("cake00",    start_position + new Vector3(2.0f, 0.0f, 0.0f));
			ItemManager.getInstance().setPositionToItem("candy00",   start_position + new Vector3(4.0f, 0.0f, 0.0f));
			ItemManager.getInstance().setPositionToItem("ice00",     start_position + new Vector3(6.0f, 0.0f, 0.0f));
		
			ItemManager.getInstance().setPositionToItem("key00",     start_position + new Vector3( 2.0f, 0.0f, -2.0f));
			ItemManager.getInstance().setPositionToItem("key01",     start_position + new Vector3( 4.0f, 0.0f, -2.0f));
			ItemManager.getInstance().setPositionToItem("key02",     start_position + new Vector3( 6.0f, 0.0f, -2.0f));
			ItemManager.getInstance().setPositionToItem("key03",     start_position + new Vector3( 8.0f, 0.0f, -2.0f));
			//ItemManager.getInstance().setPositionToItem("key04",     start_position + new Vector3(10.0f, 0.0f, -2.0f));
		}
		/*{
			EnemyRoot.getInstance().createEnemy("Enemy_Kumasan").transform.Translate(new Vector3(0.0f, 0.0f, 5.0f));
			EnemyRoot.getInstance().createEnemy("Enemy_Obake").transform.Translate(new Vector3(-5.0f, 0.0f, 5.0f));
		}*/
	}
}
