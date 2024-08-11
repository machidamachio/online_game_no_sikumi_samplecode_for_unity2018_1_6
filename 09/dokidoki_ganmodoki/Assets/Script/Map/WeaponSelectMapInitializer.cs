using UnityEngine;
using System.Collections;
using GameObjectExtension;

// 武器選択マップを初期化するためのクラス.
public class WeaponSelectMapInitializer : MapInitializer
{
	public Shader	map_shader;

	public override void initializeMap(GameRoot game_root)
	{
		MapCreator		map_creator   = MapCreator.get();
		PartyControl	party_control = PartyControl.get();

		map_creator.setRoomNum(1, 1);

		// Floorルートを生成.
		map_creator.floor_root_go = new GameObject("Floor");

		// 武器選択フロアーでは１ルームのブロックを 3x4 に変更.
		map_creator.setRoomGridNum(3, 4);

		// 部屋を作る.
		RoomController room = map_creator.createRoomFloor(new Map.RoomIndex(0, 0));

		// ダミーの部屋をつくる.
		RoomController	vacancy = map_creator.createVacancy(new Map.RoomIndex(0, -1));

		// ルーム同士の区切りの壁を作る.
		map_creator.createRoomWall();

		// 外壁を作る.
		GameObject	outer_walls = map_creator.createOuterWalls();

		// フロアー移動ドアをいっこだけ作る.
		map_creator.createFloorDoor(new Map.RoomIndex(0, 0), new Map.BlockIndex(1, 3), Map.EWSN.NORTH);

		// ---------------------------------------------------------------- //

		Renderer[]	renderers = outer_walls.GetComponentsInChildren<Renderer>();

		foreach(var render in renderers) {

			render.material.shader = this.map_shader;
		}

		//

		renderers = vacancy.GetComponentsInChildren<Renderer>();

		foreach(var render in renderers) {

			render.material.shader = this.map_shader;
		}

		renderers = room.GetComponentsInChildren<Renderer>();

		foreach(var render in renderers) {

			render.material.shader = this.map_shader;
		}

		// ---------------------------------------------------------------- //
		// かぶさんをつくる.

		chrController	kabusan = CharacterRoot.get().createNPC("NPC_Kabu_San");

		kabusan.cmdSetPositionAnon(chrBehaviorKabu.getStayPosition());
		kabusan.cmdSetDirectionAnon(chrBehaviorKabu.getStayDirection());

		// ---------------------------------------------------------------- //
		// ローカルプレイヤーを作成.

		party_control.createLocalPlayer(GlobalParam.getInstance().global_account_id);

		chrBehaviorLocal	player = PartyControl.get().getLocalPlayer();

		player.control.cmdSetPositionAnon(new Vector3( 0.0f, 0.0f, -9.0f));
		player.changeBulletShooter(SHOT_TYPE.EMPTY);

		// ---------------------------------------------------------------- //
		// アイテムを生成.

		this.generateItems(game_root);

		party_control.setCurrentRoom(room);

		ItemWindow.get().setActive(false);
	}

	// マップへのアイテム配置を行うプライベートメソッド.
	private void generateItems(GameRoot gameRoot)
	{
		string	local_player_id = PartyControl.get().getLocalPlayer().getAcountID();

		string item_type = "shot_negi";
		string item_name = item_type + "." + local_player_id;
		ItemManager.get().createItem(item_type, item_name, local_player_id);
		ItemManager.get().setPositionToItem(item_name, WeaponSelectMapInitializer.getNegiItemPosition());

		item_type = "shot_yuzu";
		item_name = item_type + "." + local_player_id;
		ItemManager.get().createItem(item_type, item_name, local_player_id);
		ItemManager.get().setPositionToItem(item_name, WeaponSelectMapInitializer.getYuzuItemPosition());
	}

	// ねぎアイテムの位置.
	public static Vector3	getNegiItemPosition()
	{
		return(new Vector3( 7.0f, 0.0f,  2.0f));
	}

	// ゆずアイテムの位置.
	public static Vector3	getYuzuItemPosition()
	{
		return(new Vector3(-7.0f, 0.0f,  2.0f));
	}
}
