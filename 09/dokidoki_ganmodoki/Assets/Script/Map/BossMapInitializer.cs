using UnityEngine;
using System.Collections;

public class BossMapInitializer : MapInitializer
{
	public BossLevelSequence sequencer;

	// 開発用にボスをヒューマン入力で動かすかどうか
	// TODO 出版前に削除
	public bool UsesHumanControlForBoss;

	public override void initializeMap(GameRoot game_root)
	{
		MapCreator		map_creator   = MapCreator.get();
		PartyControl	party_control = PartyControl.get();

		map_creator.setRoomNum(1, 1);

		map_creator.floor_root_go = new GameObject("Floor");
		
		// 部屋を作る.
		RoomController room = map_creator.createRoomFloor(new Map.RoomIndex(0, 0));

		// ダミーの部屋をつくる.
		map_creator.createVacancy(new Map.RoomIndex(0, -1));

		// ルーム同士の区切りの壁を作る.
		map_creator.createRoomWall();

		// 外壁を作る.
		map_creator.createOuterWalls();

		GameRoot.get().createLocalPlayer();
		GameRoot.get().createNetPlayers();

		// プレイヤーの位置をセットする.

		chrBehaviorLocal	local_player = PartyControl.get().getLocalPlayer();

		Vector3		playerStartPosition = Vector3.zero;

		local_player.transform.position = playerStartPosition + PartyControl.get().getPositionOffset(local_player.control.global_index);
	
		for(int i = 0;i < PartyControl.get().getFriendCount();i++) {

			chrBehaviorPlayer	friend = PartyControl.get().getFriend(i);

			friend.control.cmdSetPositionAnon(playerStartPosition + PartyControl.get().getPositionOffset(friend.control.global_index));
		}

		party_control.setCurrentRoom(room);

		// ボスの作成.

		chrControllerEnemyBase	enemy;

		if(UsesHumanControlForBoss) {

			enemy = CharacterRoot.get().createEnemy("Boss1", "chrControllerEnemyBoss", "chrBehaviorEnemyBoss_Human") as chrControllerEnemyBase;

		} else {

			enemy = CharacterRoot.get().createEnemy("Boss1", "chrControllerEnemyBoss", "chrBehaviorEnemyBoss") as chrControllerEnemyBase;
		}

		enemy.cmdSetPosition(new Vector3 (0.0f, 0.0f, 20.0f));

		// ステータスウインドウ.

		Navi.get().createStatusWindows();
	}
}
