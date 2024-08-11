using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;

public class RoomController : MonoBehaviour {

	private List<GameObject> northRoomWalls = new List<GameObject>();
	private List<GameObject> eastRoomWalls  = new List<GameObject>();
	private List<GameObject> southRoomWalls = new List<GameObject>();
	private List<GameObject> westRoomWalls  = new List<GameObject>();

	private List<DoorControl>	doors = new List<DoorControl>();

	public GameObject[,]		floor_objects;		// 床のゲームオブジェクト.

	protected Map.RoomIndex		index;				// この部屋の番号.

	// ================================================================ //

	// インデックスをセットする.
	public void		setIndex(Map.RoomIndex index)
	{
		this.index = index;
	}

	// インデックスをゲットする.
	public Map.RoomIndex	getIndex()
	{
		return(this.index);
	}

	public void RegisterRoomWall(Map.EWSN dir, GameObject roomWallGO)
	{
		switch (dir)
		{
		case Map.EWSN.NORTH:
			northRoomWalls.Add (roomWallGO);
			break;
		case Map.EWSN.EAST:
			eastRoomWalls.Add (roomWallGO);
			break;
		case Map.EWSN.SOUTH:
			southRoomWalls.Add (roomWallGO);
			break;
		case Map.EWSN.WEST:
			westRoomWalls.Add (roomWallGO);
			break;
		}
	}

	// ルーム間の壁をゲットする.
	public List<RoomWallControl>	GetRoomWalls(Map.EWSN dir)
	{
		List<RoomWallControl>	walls = new List<RoomWallControl>();
		List<GameObject>		wall_gos = null;

		switch (dir)
		{
		case Map.EWSN.NORTH:
			wall_gos = northRoomWalls;
			break;
		case Map.EWSN.EAST:
			wall_gos = eastRoomWalls;
			break;
		case Map.EWSN.SOUTH:
			wall_gos = southRoomWalls;
			break;
		case Map.EWSN.WEST:
			wall_gos = westRoomWalls;
			break;
		}

		if(wall_gos != null) {
			foreach(var go in wall_gos) {
				if(go.GetComponent<RoomWallControl>() != null) {
					walls.Add(go.GetComponent<RoomWallControl>());
				}
			}
		}
		return(walls);
	}

	public void RegisterDoor(DoorControl door)
	{
		doors.Add(door);
		door.SetRoom(this);
	}

	// ドアの数をゲットする.
	public int		getDoorCount()
	{
		return(this.doors.Count);
	}

	// ドアをゲットする.
	public DoorControl	getDoor(Map.EWSN dir)
	{
		return(this.doors.Find(x => x.door_dir == dir));
	}

	// index 番目のドアをゲットする.
	public DoorControl	getDoorByIndex(int index)
	{
		return(this.doors[index]);
	}

	// 位置を取得する.
	public Vector3	getPosition()
	{
		return(this.gameObject.transform.position);
	}

	public void RegisterKey(string itemName, int keyType)
	{
		foreach (var door in doors) {
			if (door.KeyType == keyType) {
				door.SetKey(itemName);
				break;
			}
		}
	}
	
	// ドア（キー）の各色が使われているか調べる.
	public List<bool>	checkKeyColorsUsed()
	{
		List<bool>	is_used = new List<bool>();

		for(int i = 0;i < 4;i++) {

			is_used.Add(false);
		}

		for(int i = 0;i < this.doors.Count;i++) {

			int		key_color = this.doors[i].KeyType;

			if(0 <= key_color && key_color < is_used.Count) {

				is_used[key_color] = true;
			}
		}

		return(is_used);
	}

	public int GetUnusedKeyType()
	{
		for (int i = 0; i < 4; ++i) {
			bool alreadyUsed = false;
			foreach (DoorControl door in doors) {
				alreadyUsed |= (door.KeyType == i);
			}
			if (!alreadyUsed) {
				return i;
			}
		}
		Debug.LogError("The room has more doors than 4.");
		return -1;
	}

	public int GetKeyType(Map.EWSN door_dir)
	{
		foreach (var door in doors) {
			if (door.door_dir == door_dir) {
				return door.KeyType;
			}
		}
		Debug.LogError("The room doesn't the door having the " + door_dir + " door.");
		return -1;
	}

	public void OnConsumedKey(string key_type)
	{
		//Debug.Log ("consumed key --- " + type);

		DoorControl		door = null;

		do {

			// キーに対応するドアを探す.

			Item.KEY_COLOR	key_color = Item.Key.getColorFromTypeName(key_type);

			if(key_color == Item.KEY_COLOR.NONE) {

				break;
			}

			door = this.doors.Find(x => x.KeyType == (int)key_color);

			if(door == null) {

				break;
			}

			// アンロックする.
			door.Unlock();

			// つながっているドアもアンロックする.
			if(door.connect_to != null) {

				door.connect_to.Unlock();
			}

		} while(false);
	}

	// パーティーが部屋に入ったとき（カレントルームになったとき）に呼ばれる.
	public void NotifyPartyEnter()
	{
		// 南のルーム壁を半透明に戻す.
		foreach(var wall in southRoomWalls) {

			wall.GetComponent<RoomWallControl>().FadeOut();
		}

		// ドアのスリープを解除する.
		foreach(var door in this.doors) {

			door.endSleep();
		}

		// ろうそくに火をともす（エフェクト）.
		this.igniteCandles();
	}

	// パーティーが部屋から出たとき（カレントルーじゃなくなったとき）に呼ばれる.
	public void NotifyPartyLeave()
	{
		// 南のルーム壁を不透明に戻す.
		foreach(var wall in southRoomWalls) {

			wall.GetComponent<RoomWallControl>().FadeIn();
		}

		// ドアをスリープする.
		foreach(var door in this.doors) {

			door.beginSleep();
		}

		this.puffOutCandles();
	}

	// ろうそくの火をつける（エフェクト）.
	public void		igniteCandles()
	{
		do {
			// もう火がついていた（エフェクトを束ねる GameObjec があった）ときは
			// なにもしない.
			if(this.transform.Find("fires") != null) {

				break;
			}

			// 柱を束ねる GameObject を探す.

			Transform childPillars = this.transform.Find("pillars");

			if(childPillars == null) {

				break;
			}

			GameObject	pillars_root = this.transform.Find("pillars").gameObject;

			// エフェクトを作る.
	
			GameObject	fire_root = new GameObject("fires");
	
			fire_root.transform.parent = this.gameObject.transform;
	
			float		height = 3.0f;

			// 全ての子供（＝すべての柱）にエフェクトをつける.
			for(int i = 0;i < pillars_root.transform.childCount;i++) {
	
				GameObject	pillar = pillars_root.transform.GetChild(i).gameObject;
	
				GameObject	effect = EffectRoot.get().createCandleFireEffect(pillar.transform.position + Vector3.up*height);
	
				effect.transform.parent = fire_root.transform;
			}

		} while(false);
	}

	// ろうそくの火を消す.
	public void		puffOutCandles()
	{
		do {

			Transform	child = this.transform.Find("fires");

			if(child == null) {

				break;
			}

			GameObject.Destroy(child.gameObject);

		} while(false);
	}

	// カレントルーム（パーティーがいる部屋）？
	public bool	isCurrent()
	{
		return(PartyControl.get().getCurrentRoom() == this);
	}

	// ================================================================ //
	// デバッグ用

	// 床のデバッグ用プレーンを表示する.
	public void		dbSetFloorColor(Map.BlockIndex bi, Color color)
	{
		GameObject	debug_go = this.floor_objects[bi.x, bi.z].findChildGameObject("Debug");

		debug_go.SetActive(true);

		debug_go.GetComponent<MeshRenderer>().material.color = color;
	}

	// 床のデバッグ用プレーンを非表示にする.
	public void		dbHideFloorColor(Map.BlockIndex bi)
	{
		GameObject	debug_go = this.floor_objects[bi.x, bi.z].findChildGameObject("Debug");

		debug_go.SetActive(false);
	}

	public void		dbArrowSetVisible(Map.BlockIndex bi, Map.EWSN ewsn, bool is_visible)
	{
		string		arrow_name = "";

		switch(ewsn) {

			case Map.EWSN.EAST:		arrow_name = "DebugE";	break;
			case Map.EWSN.WEST:		arrow_name = "DebugW";	break;
			case Map.EWSN.SOUTH:	arrow_name = "DebugS";	break;
			case Map.EWSN.NORTH:	arrow_name = "DebugN";	break;
		}

		if(arrow_name != "") {

			GameObject	arrow_go = this.floor_objects[bi.x, bi.z].findChildGameObject(arrow_name);

			arrow_go.SetActive(is_visible);
		}
	}

	public void		dbArrowHideAll(Map.BlockIndex bi)
	{
		for(int i = 0;i < (int)Map.EWSN.NUM;i++) {

			this.dbArrowSetVisible(bi, (Map.EWSN)i, false);
		}
	}
}