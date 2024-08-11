using UnityEngine;
using System;
using System.Collections;

public class StdMapInitializer : MapInitializer {

	public bool		UseRandomSeedForDebug = false;	//< LevelGeneratorSeedを使用せずに、
	public int		RandomSeedForDebugMin = 0;		//< UseRandomSeedForDebugが有効な時に使用するランダムのレンジ
	public int		RandomSeedForDebugMax = 1000;	//< UseRandomSeedForDebugが有効な時に使用するランダムのレンジ

	public override void initializeMap(GameRoot game_root)
	{
		MapCreator mapCreator = MapCreator.getInstance ();
		Vector3 playerStartPosition;
		int seed = 0;

		if (UseRandomSeedForDebug)
		{
			TimeSpan ts = new TimeSpan(DateTime.Now.Ticks);
			double seconds = ts.TotalSeconds;
			seed = (int) ((long)seconds - (long)(seconds/1000.0)*1000);
			//seed = Random.Range(RandomSeedForDebugMin, RandomSeedForDebugMax);
		}
		else
		{
			seed = GlobalParam.get().seed;
		}

		// マップを自動生成.
		Debug.Log("Use random seed:" + seed);
		mapCreator.generateLevel(seed);

		GameRoot.get().createLocalPlayer();
		GameRoot.get().createNetPlayers();

		chrBehaviorLocal	local_player = PartyControl.get().getLocalPlayer();

		// アイテムを作る.
		mapCreator.generateItems(local_player.getAcountID());
		
		mapCreator.createFloorDoor();

		// プレイヤーの位置をセットする.

		playerStartPosition = mapCreator.getPlayerStartPosition();

		local_player.transform.position = playerStartPosition + PartyControl.get().getPositionOffset(local_player.control.global_index);
	
		for(int i = 0;i < PartyControl.get().getFriendCount();i++) {

			chrBehaviorPlayer	friend = PartyControl.get().getFriend(i);

			friend.control.cmdSetPositionAnon(playerStartPosition + PartyControl.get().getPositionOffset(friend.control.global_index));
		}

		PartyControl.get().setCurrentRoom(mapCreator.getRoomFromPosition(playerStartPosition));

		// ステータスウインドウ.

		Navi.get().createStatusWindows();

		//

		//

#if false
		string	local_player_id = PartyControl.get().getLocalPlayer().getAcountID();

		ItemManager.get().createItem("candy00", local_player_id);
		ItemManager.get().setPositionToItem("candy00", playerStartPosition + new Vector3( 0.0f, 0.0f,  4.0f));

		ItemManager.get().createItem("ice00", local_player_id);
		ItemManager.get().setPositionToItem("ice00", playerStartPosition + new Vector3( 7.0f, 0.0f,  0.0f));

		ItemManager.get().createItem("key00", local_player_id);
		ItemManager.get().setPositionToItem("key00", playerStartPosition + new Vector3(10.0f, 0.0f,  0.0f));
		ItemManager.get().createItem("key01", local_player_id);
		ItemManager.get().setPositionToItem("key01", playerStartPosition + new Vector3(10.0f, 0.0f,  0.0f));
		ItemManager.get().createItem("key02", local_player_id);
		ItemManager.get().setPositionToItem("key02", playerStartPosition + new Vector3(10.0f, 0.0f,  0.0f));
		ItemManager.get().createItem("key03", local_player_id);
		ItemManager.get().setPositionToItem("key03", playerStartPosition + new Vector3(10.0f, 0.0f,  0.0f));
#endif
	}
}
