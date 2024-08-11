using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// レベル生成（敵の配置とか）.
public class LevelControl : MonoBehaviour {

	public static int	enemies_max = 30;	// １ルームあたりの敵の最大同時出現数.

	// ---------------------------------------------------------------- //

	protected class Room {

		public List<chrController>			enemies = new List<chrController>();
		public List<chrBehaviorEnemy_Lair>	lairs   = new List<chrBehaviorEnemy_Lair>();

		public int		enemy_index = 0;	// 敵の名まえの後ろにつけるインデックス.
		public int		spawner = 0;		// 敵を作ったジェネレーター.

		public List<Map.BlockIndex>			places  = new List<Map.BlockIndex>();

		public List<string>	items = new List<string>();	// ルーム内で生成したアイテム.

		public int		enter_count = 0;				// この部屋に入った回数.
	};

	protected Room[,]	rooms;

	protected float		spawn_timer = 0.0f;				// エネミースポーン用タイマー.s

	protected int		move_room_in_level_count = 0;	// レベル内で部屋移動した回数.

	// 乱数による抽選（いつかは当たる）.
	protected class Lottery {

		public Lottery(float add)
		{
			this.current = 0.0f;
			this.add     = add;
		}

		// 抽選.
		public bool	draw(float value)
		{
			bool	is_atari = false;

			if(value < this.current) {

				is_atari = true;

				// 当たったら確率リセット.
				this.current = 0.0f;

			} else {

				is_atari = false;

				// はずれたら次回当たる確率を上昇させる.
				// （いつかは必ず当たるように）.
				this.current += this.add;
			}

			return(is_atari);
		}

		public float	current;
		public float	add;		// はずれだったときに確率が上昇する値.
	}

	protected struct SpecialItemLottery {

		public Lottery	ice_atari;
	}
	protected SpecialItemLottery	item_lot;

	protected PseudoRandom.Plant	rand; 

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
		this.rooms = new Room[3, 3];

		for(int x = 0;x < 3;x++) {

			for(int z = 0;z < 3;z++) {

				this.rooms[x, z] = new Room();
			}
		}
	}

	void	Start()
	{
		this.item_lot.ice_atari = new Lottery(1.0f/3.0f);

		this.rand = PseudoRandom.get().createPlant("level:item", 40);
	}

	void 	Update()
	{
		RoomController	room_control = PartyControl.get().getCurrentRoom();

		do {

			if(room_control == null) {
	
				break;
			}

			Room	level_room   = this.rooms[room_control.getIndex().x, room_control.getIndex().z];

			if(GameRoot.get().isHost()) {
	
				// ローカルは自分で作る.
	
				//if(Input.GetMouseButtonDown(1)) {
				if(this.spawn_timer > 5.0f) {

					chrBehaviorEnemy_Lair	lair = null;
	
					for(int i = 0;i < level_room.lairs.Count;i++) {
	
						level_room.spawner = (level_room.spawner + 1)%level_room.lairs.Count;
	
						lair = level_room.lairs[level_room.spawner];
	
						if(lair != null) {
	
							break;
						}
					}
	
					if(lair != null) {

						dbwin.console().print("LairName:" + lair.name);

						lair.spawnEnemy();

					}

					this.spawn_timer = 0.0f;
				}
	
				this.spawn_timer += Time.deltaTime;

			} else {

				// リモートはホストからの指令で作る.
				foreach(var lair in level_room.lairs) {
	
					if(lair == null) {
	
						continue;
					}

					// 調停の終わったクエリーを探す.
					List<QueryBase> queries = QueryManager.get().findDoneQuery(lair.name);
	
					List<QueryBase>	spawn_queries = queries.FindAll(x => (x as QuerySpawn) != null);
	
					foreach(var query in spawn_queries) {
		
						QuerySpawn query_spawn = query as QuerySpawn;

						// 用済みになったので、削除する.
						query_spawn.set_expired(true);
	
						if(!query_spawn.isSuccess()) {
	
							continue;
						}
						dbwin.console().print("LairName:" + lair.name);
						dbwin.console().print("EnemyName:" + query_spawn.monster_id);

						//Debug.Log("LairName:" + lair.name);
						//Debug.Log("EnemyName:" + query_spawn.monster_id);

						lair.spawnEnemyFromPedigree(query_spawn.monster_id);
					}
				}
	
			}

		} while(false);
	}

	// ジェネレーターを探す.
	protected chrBehaviorEnemy_Lair		find_lair(string lair_name)
	{
		chrBehaviorEnemy_Lair	lair = null;

		do {

			RoomController	room_control = PartyControl.get().getCurrentRoom();
		
			Room	room = this.rooms[room_control.getIndex().x, room_control.getIndex().z];
	
			var	lairs = room.enemies.FindAll(x => ((x.behavior as chrBehaviorEnemy_Lair) != null));

			if(lairs.Count == 0) {

				break;
			}

			var		lair_control = lairs.Find(x => (x.name == lair_name));

			if(lair_control == null) {

				break;
			}

			lair = lair_control.behavior as chrBehaviorEnemy_Lair;

		} while(false);

		return(lair);
	}

	// ================================================================ //

	// カレントルーム内に敵を作れる？（敵の数制限のチェック）.
	public bool		canCreateCurrentRoomEnemy()
	{
		RoomController	room = PartyControl.get().getCurrentRoom();

		return(this.canCreateRoomEnemy(room));
	}

	// カレントルーム内に敵を作る.
	public T		createCurrentRoomEnemy<T>(string name) where T : chrBehaviorEnemy
	{
		RoomController	room = PartyControl.get().getCurrentRoom();

		return(this.createRoomEnemy<T>(room, name));
	}

	// ルーム内に敵を作れる？（敵の数制限のチェック）.
	public bool		canCreateRoomEnemy(RoomController room)
	{
		bool	ret = (this.rooms[room.getIndex().x, room.getIndex().z].enemies.Count < enemies_max);

		return(ret);
	}

	// ルーム内に敵を作る.
	public T		createRoomEnemy<T>(RoomController room, string name) where T : chrBehaviorEnemy
	{
		chrController	chr = null;
		T				enemy = null;

		do {

			string[]	tokens = name.Split('.');
			string		enemy_kind = name;

			if(tokens.Length > 1) {
				enemy_kind = tokens[0];
			}
		
			// ルーム内に敵を作れる（敵の数制限のチェック）？.
			if(!this.canCreateRoomEnemy(room)) {

				break;
			}

			if( (chr = EnemyRoot.get().createEnemy(enemy_kind)) == null) {

				break;
			}

			if((enemy = chr.behavior as T) == null) {

				break;
			}

			enemy.setRoom(room);

			Room	level_room = this.rooms[room.getIndex().x, room.getIndex().z];

			if (GameRoot.get().isHost() && tokens.Length == 1) {
				enemy.name += "." + level_room.enemy_index.ToString("D3");
			}
			else {
				enemy.name = name;
			}
			level_room.enemies.Add(chr);

			dbwin.console().print("**EnemyName" + enemy.name);
			//Debug.Log("**EnemyName" + enemy.name);

			var		as_lair = chr.behavior as chrBehaviorEnemy_Lair;

			if(as_lair != null) {

				level_room.lairs.Add(as_lair);
			}

			level_room.enemy_index++;

		} while(false);

		return(enemy);
	}

	//ルーム内に初期配置の敵を作る.
	public void		createRoomEnemies(RoomController room)
	{
#if true
		MapCreator	map_creater   = MapCreator.get();
		Vector3		room_position = map_creater.getRoomCenterPosition(room.getIndex());

		string 		room_index = room.getIndex().x.ToString("D1") + room.getIndex().z.ToString("D1");
		string		item_type, item_name;

		Room		level_room = this.rooms[room.getIndex().x, room.getIndex().z];

		level_room.items.Clear();

		List<Map.BlockIndex>	lair_places = map_creater.getLairPlacesRoom(room.getIndex());

		// FieldGenerator の設定したランダムな位置に、ジェネレーターを配置する.

		int		n = 0;

		foreach(var lair_place in lair_places) {

			string enemy_name = "Enemy_Lair" + "." + room_index + "." + n.ToString("D3");

			chrBehaviorEnemy	behavior = this.createRoomEnemy<chrBehaviorEnemy>(room, enemy_name);

			if (behavior == null) {
				continue;
			}

			chrController	chr = behavior.control;

			chrBehaviorEnemy_Lair	lair = chr.behavior as chrBehaviorEnemy_Lair;

			// 倒したときに出現するアイテム.

			object	item_option0 = null;

			if(n == 0) {

				item_type = "ice00";

				// 当たり、はずれの抽選.
				item_option0 = this.item_lot.ice_atari.draw(this.rand.getRandom());

			} else {

				item_type = "cake00";
			}

			item_name = item_type + "_" + room_index + "_"  + level_room.enter_count + "_"  + n.ToString("D2");

			lair.setRewardItem(item_type, item_name, item_option0);
			level_room.items.Add(item_name);

			// スポーンするエネミー.

			chrBehaviorEnemy_Lair.SpawnEnemy	spawn;

			spawn = lair.resisterSpawnEnemy();
			spawn.enemy_name  = "Enemy_Obake";
			spawn.behave_kind = Enemy.BEHAVE_KIND.UROURO;
			spawn.behave_desc = null;
			spawn.frequency   = 1.0f;

			spawn = lair.resisterSpawnEnemy();
			spawn.enemy_name  = "Enemy_Kumasan";
			spawn.behave_kind = Enemy.BEHAVE_KIND.TOTUGEKI;
			spawn.behave_desc = null;
			spawn.frequency   = 0.5f;

			spawn = lair.resisterSpawnEnemy();
			spawn.enemy_name  = "Enemy_Obake";
			spawn.behave_kind = Enemy.BEHAVE_KIND.WARP_DE_FIRE;
			spawn.behave_desc = null;
			spawn.frequency   = 0.5f;

			spawn = lair.resisterSpawnEnemy();
			spawn.enemy_name  = "Enemy_Kumasan";
			spawn.behave_kind = Enemy.BEHAVE_KIND.JUMBO;
			spawn.behave_desc = null;
			spawn.frequency   = 0.5f;

			spawn = lair.resisterSpawnEnemy();
			spawn.enemy_name  = "Enemy_Obake";
			spawn.behave_kind = Enemy.BEHAVE_KIND.GOROGORO;
			spawn.behave_desc = null;
			spawn.frequency   = 0.5f;

			Vector3		position = map_creater.getBlockCenterPosition(lair_place);

			lair.control.cmdSetPositionAnon(room_position + position);
			lair.control.cmdSetDirectionAnon(135.0f);

			n++;
		}

		// ドアの前に番人っぽく配置.
		for(int i = 0;i < (int)Map.EWSN.NUM;i++) {

			DoorControl	door = room.getDoor((Map.EWSN)i);

			if(door == null) {

				continue;
			}

			string enemy_name = "Enemy_Kumasan" + "." + room_index + "." + i.ToString("D3");

			chrBehaviorEnemy	enemy = this.createRoomEnemy<chrBehaviorEnemy>(room, enemy_name);

			Map.RoomIndex	ri;
			Map.BlockIndex	bi;

			map_creater.getBlockIndexFromPosition(out ri, out bi, door.getPosition());

			Vector3		position = door.getPosition();
			float		angle = 0.0f;

			Map.EWSN	eswn = door.door_dir;

			for(int j = 0;j < 4;j++) {

				if(map_creater.isPracticable(ri, bi, eswn)) {

					break;
				}
				eswn = Map.eswn.next_cw(eswn);
			}

			float	offset = 7.0f;

			switch(eswn) {

				case Map.EWSN.NORTH:
				{
					position += Vector3.forward*offset;
					angle = 180.0f;
				}
				break;
				case Map.EWSN.SOUTH:
				{
					position += Vector3.back*offset;
					angle = 0.0f;
				}
				break;
				case Map.EWSN.EAST:
				{
					position += Vector3.right*offset;
					angle = 90.0f;
				}
				break;
				case Map.EWSN.WEST:
				{
					position += Vector3.left*offset;
					angle = -90.0f;
				}
				break;
			}

			enemy.control.cmdSetPositionAnon(position);
			enemy.control.cmdSetDirectionAnon(angle);	

			var		desc = new Character.OufukuAction.Desc();

			Vector3		line = Quaternion.AngleAxis(angle, Vector3.up)*Vector3.right;

			desc.position0 = position + line*2.0f;
			desc.position1 = position - line*2.0f;

			enemy.setBehaveKind(Enemy.BEHAVE_KIND.OUFUKU, desc);
		}

		// その場で弾をうつおばけ/武器チェンジアイテム.

		int		item_place    = this.rand.getRandomInt(level_room.places.Count);
		int		shot_item_sel = this.rand.getRandomInt(10)%2;

		for(int i = 0;i < level_room.places.Count;i++) {

			var	place = level_room.places[i];

			Vector3		position = room_position + map_creater.getBlockCenterPosition(place);

			if(i == item_place) {

				// 武器チェンジアイテム.

				if(shot_item_sel == 0) {

					item_type = "shot_negi";

				} else {

					item_type = "shot_yuzu";
				}

				item_name = item_type + "_" + room_index + "_"  + level_room.enter_count + "_"  + n.ToString("D2");
	
				level_room.items.Add(item_name);

				ItemManager.get().createItem(item_type, item_name, PartyControl.get().getLocalPlayer().getAcountID());

				ItemManager.get().setPositionToItem(item_name, position);


			} else {

				// その場で弾をうつおばけ.

				string enemy_name = "Enemy_Obake" + "." + room_index + "." + i.ToString("D3");

				chrBehaviorEnemy	enemy = this.createRoomEnemy<chrBehaviorEnemy>(room, enemy_name);
	
				if(enemy == null) {
	
					continue;
				}
	
				enemy.setBehaveKind(Enemy.BEHAVE_KIND.SONOBA_DE_FIRE);
	
				enemy.control.cmdSetPositionAnon(position);
			}
		}
#else
	#if false
			chrController	chr0 = this.createRoomEnemy<chrBehaviorEnemy>(room, "Enemy_Lair").control;
			//chrController	chr1 = this.createRoomEnemy<chrBehaviorEnemy>(room, "Enemy_Lair").control;
	
			chrBehaviorEnemy_Lair	lair0 = chr0.behavior as chrBehaviorEnemy_Lair;
			//chrBehaviorEnemy_Lair	lair1 = chr1.behavior as chrBehaviorEnemy_Lair;

			lair0.name += "00";

			//lair0.setRewardItem("cake00", "cake01");
			//lair1.setRewardItem("ice00" , "ice01");

			chrBehaviorEnemy_Lair.SpawnEnemy	spawn;

			spawn = lair0.resisterSpawnEnemy();
			spawn.enemy_name  = "Enemy_Kumasan";
			spawn.behave_kind = Enemy.BEHAVE_KIND.JUMBO;
			spawn.behave_desc = null;
			spawn.frequency   = 1.0f;

			//spawn = lair0.resisterSpawnEnemy();
			//spawn.enemy_name  = "Enemy_Kumasan";
			//spawn.behave_kind = Enemy.BEHAVE_KIND.TOTUGEKI;
			//spawn.behave_desc = null;
			//spawn.frequency   = 0.5f;

			//lair0.spawn_enemy.enemy_name  = "Enemy_Obake";
			//lair0.spawn_enemy.behave_kind = Enemy.BEHAVE_KIND.UROURO;
			//lair1.spawn_enemy = "Enemy_Obake";
	
			Vector3	player_position = room.gameObject.transform.position;
	
			lair0.control.cmdSetPositionAnon(player_position + Vector3.forward*2.0f);
			//lair1.control.cmdSetPositionAnon(player_position + Vector3.forward*2.0f + Vector3.right*4.0f);
	
			lair0.control.cmdSetDirectionAnon(135.0f);
			//lair1.control.cmdSetDirectionAnon(135.0f);
	
	
	
			//
	
			if(room.getIndex().x == 2 && room.getIndex().z == 1) {
			
				player_position = MapCreator.get().getPlayerStartPosition();
			
				lair0.control.cmdSetPositionAnon(player_position + Vector3.forward*4.0f);
				//lair1.control.cmdSetPositionAnon(player_position + Vector3.forward*2.0f + Vector3.right*4.0f);
			}
	#else
			for(int i = 0;i < 1;i++) {

				//chrBehaviorEnemy	enemy = this.createRoomEnemy<chrBehaviorEnemy>(room, i == 0 ? "Enemy_Obake" : "Enemy_Kumasan");
				chrBehaviorEnemy	enemy = this.createRoomEnemy<chrBehaviorEnemy>(room, "Enemy_Kumasan");
				//chrController	obake = this.createRoomEnemy<chrBehaviorEnemy>(room, "Enemy_Lair").controll;
		
		
				//
		
				Vector3	player_position = room.gameObject.transform.position;
		
				enemy.control.cmdSetPositionAnon(player_position + Vector3.forward*3.0f);
				enemy.control.cmdSetDirectionAnon(180.0f - 45.0f);	
	
				if(room.getIndex().x == 2 && room.getIndex().z == 1) {
				
					player_position = MapCreator.get().getPlayerStartPosition();
				
					enemy.control.cmdSetPositionAnon(player_position + Vector3.forward*2.0f + Vector3.right*2.0f*(float)i);
				}

				if(i == 0) {

					//var		desc = new Character.OufukuAction.Desc();
		
					//Vector3		line = Quaternion.AngleAxis(180.0f, Vector3.up)*Vector3.back;
		
					//desc.position0 = player_position + line*2.0f;
					//desc.position1 = player_position - line*2.0f;
	
					//enemy.setBehaveKind(Enemy.BEHAVE_KIND.GOROGORO);
					enemy.setBehaveKind(Enemy.BEHAVE_KIND.JUMBO);
break;
				} else {

					enemy.setBehaveKind(Enemy.BEHAVE_KIND.TOTUGEKI);
				}
			}
	#endif
#endif

	}

	// 敵がその場で立ち止まる.
	public void		beginStillEnemies(RoomController room_control = null)
	{
		if(room_control == null) {

			room_control = PartyControl.get().getCurrentRoom();
		}

		Room	room = this.rooms[room_control.getIndex().x, room_control.getIndex().z];

		foreach(var chr in room.enemies) {

			chrBehaviorEnemy	enemy = chr.behavior as chrBehaviorEnemy;

			enemy.beginStill();
		}
	}

	// 敵のその場で立ち止まりを解除する.
	public void		endStillEnemies(RoomController room_control, float delay)
	{
		if(room_control == null) {

			room_control = PartyControl.get().getCurrentRoom();
		}

		Room	room = this.rooms[room_control.getIndex().x, room_control.getIndex().z];

		foreach(var chr in room.enemies) {

			chrBehaviorEnemy	enemy = chr.behavior as chrBehaviorEnemy;

			enemy.endStill(delay);
		}
	}

	// ルーム内の敵を全部削除する.
	public void		deleteRoomEnemies(RoomController room)
	{
		Room	level_room = this.rooms[room.getIndex().x, room.getIndex().z];

		var		enemies = this.rooms[room.getIndex().x, room.getIndex().z].enemies;

		foreach(var enemy in enemies) {

			if(enemy == null) {

				continue;
			}

			EnemyRoot.getInstance().deleteEnemy(enemy);
		}
	
		level_room.enemies.Clear();
		level_room.lairs.Clear();
		level_room.enemy_index = 0;
		level_room.spawner     = 0;

		this.spawn_timer = 0.0f;
	}

	// 指定の範囲内にいるエネミーを探す.
	public List<chrController> getRoomEnemiesInRange(RoomController room, Vector3 center, float radius)
	{
		if(room == null) {

			room = PartyControl.get().getCurrentRoom();
		}

		var		enemies = this.rooms[room.getIndex().x, room.getIndex().z].enemies;

		enemies = enemies.FindAll(x => x != null);

		enemies = enemies.FindAll(x => MathUtility.calcDistanceXZ(x.getPosition(), center) < radius);

		return(enemies);
	}

	// ================================================================ //

	// フロアー（マップ）が作られた直後に呼ばれる.
	public void		onFloorCreated()
	{
		Map.RoomIndex	room_count = MapCreator.get().getRoomNum();

		foreach(var ri in Map.RoomIndex.getRange(room_count.x, room_count.z)) {

			this.rooms[ri.x, ri.z].places = MapCreator.get().allocateChipPlacesRoom(ri, Map.CHIP.ANY, 5);
		}
	}

	// 敵が削除する直前に（chrBehaviorEnemy.deleteSelf）呼ばれる.
	public void		onDeleteEnemy(RoomController room, chrController enemy)
	{
		Room	level_room = this.rooms[room.getIndex().x, room.getIndex().z];

		chrBehaviorEnemy_Lair	as_lair = enemy.behavior as chrBehaviorEnemy_Lair;

		if(as_lair != null) {

			level_room.lairs.Remove(as_lair);
		}
		level_room.enemies.Remove(enemy);
	}

	// ルームに入るときに呼ばれる.
	public void		onEnterRoom(RoomController room)
	{
		Room	level_room = this.rooms[room.getIndex().x, room.getIndex().z];

		level_room.enter_count++;

		this.move_room_in_level_count++;
	}

	// ルームから出るときに呼ばれる.
	public void		onLeaveRoom(RoomController room)
	{
		// アイテムを削除する.

		Room	level_room = this.rooms[room.getIndex().x, room.getIndex().z];

		foreach(var item_name in level_room.items) {

			ItemController	item = ItemManager.get().findItem(item_name);

			if(item == null) {

				continue;
			}

			ItemManager.get().deleteItem(item_name);
		}

		level_room.items.Clear();
	}

	// ================================================================ //
	// インスタンス.

	private	static LevelControl	instance = null;

	public static LevelControl	get()
	{
		if(LevelControl.instance == null) {

			LevelControl.instance = GameObject.Find("LevelControl").GetComponent<LevelControl>();
		}

		return(LevelControl.instance);
	}
}
