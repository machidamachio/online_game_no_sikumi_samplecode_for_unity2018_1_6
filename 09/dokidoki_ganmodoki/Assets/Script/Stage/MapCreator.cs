using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;

public class MapCreator : MonoBehaviour {
	
	//  Unity 2018 で、コリジョンがおかしくなることの対処.
	// プレハブを実体化した時に、プレハブに含まれるコライダーのトランスフォームが
	// おかしくなるらしい？.
	protected void	force_off_on_colliders(GameObject go)
	{
		BoxCollider[]	colis = go.GetComponentsInChildren<BoxCollider>();

		foreach(BoxCollider coli in colis) {
			// こうすると、トランスフォームが正しい値に直る.
			coli.enabled = false;
			coli.enabled = true;
		}
	}

	// ================================================================ //

	public GameObject[]		floor_prefabs      = null;			// 床.
	public GameObject[]		outer_wall_prefabs = null;			// 外壁.
	public GameObject[]		room_wall_prefabs  = null;			// ルーム同士の区切りの壁.
	public GameObject[]		inner_wall_prefabs = null;			// ルーム内の内壁.
	public GameObject[]		door_prefabs = null;				// ドア.
	public GameObject		pillar_prefab = null;				// ルーム内壁のクロス位置に置くはしら.
	
	public static float		BLOCK_SIZE = 10.0f;				// １グリッドの大きさ.
	public static float		UNIT_SIZE = 1.0f;				// Size of a unit --- player start position and item.

	public static int		ROOM_COLUMNS_NUM	= 3;	// レベル内のルームの横の数.
	public static int		ROOM_ROWS_NUM		= 3;	// レベル内のルームの縦の数.

	public static int		BLOCK_GRID_COLUMNS_NUM	= 5;	// ルーム内のグリッド（ブロック）の横のマス目の数.
	public static int		BLOCK_GRID_ROWS_NUM		= 5;	// ルーム内のグリッド（ブロック）の縦のマス目の数.

	// Field Generator に渡すブロックの数.
	// 床と床の間（ルーム内壁がおかれるところ）も１ブロックと数える.
	public static int		BLOCK_COLUMNS_NUM = BLOCK_GRID_COLUMNS_NUM + (BLOCK_GRID_COLUMNS_NUM + 1);
	public static int		BLOCK_ROWS_NUM    = BLOCK_GRID_ROWS_NUM    + (BLOCK_GRID_ROWS_NUM + 1);

	public static float		ROOM_COLUMN_SIZE = BLOCK_SIZE * BLOCK_GRID_COLUMNS_NUM;
	public static float		ROOM_ROW_SIZE    = BLOCK_SIZE * BLOCK_GRID_ROWS_NUM;

	protected int		room_columns_num = ROOM_COLUMNS_NUM;
	protected int		room_rows_num    = ROOM_ROWS_NUM;

	protected int		block_grid_columns_num = BLOCK_GRID_COLUMNS_NUM;
	protected int		block_grid_rows_num    = BLOCK_GRID_ROWS_NUM;

	protected int		block_columns_num = BLOCK_GRID_COLUMNS_NUM + (BLOCK_GRID_COLUMNS_NUM + 1);
	protected int		block_rows_num    = BLOCK_GRID_ROWS_NUM    + (BLOCK_GRID_ROWS_NUM + 1);

	protected float	room_column_size = BLOCK_SIZE*BLOCK_GRID_COLUMNS_NUM;
	protected float	room_row_size    = BLOCK_SIZE*BLOCK_GRID_ROWS_NUM;

	public GameObject		floor_root_go = null;

	protected FieldGenerator	levelGenerator = null;

	protected FieldGenerator.ChipType[,][,]	level_data;

	protected PseudoRandom.Plant	random_plant;				// 乱数生成オブジェクト.

	// 階段　フロアーのスタート、ゴール地点.
	public struct Stairs {

		public Map.RoomIndex	room_index;
		public Map.BlockIndex	block_index;
	}
	public Stairs	start;
	public Stairs	goal;

	// ---------------------------------------------------------------- //
	// ルーム.

	private List<RoomController>	rooms = new List<RoomController>();
	private RoomController			currentRoom;
	private DoorControl				bossDoor;
	private string					bossKeyItemName;

	// ---------------------------------------------------------------- //

	// ルーム０からルーム１へのドア.
	public struct DoorData {
		
		public DoorControl.TYPE		type;
		
		public Map.RoomIndex	room0;		// ルーム０.
		public Map.RoomIndex	room1;		// ルーム１．

		
		public int	local_position;		// 縦 or 横の位置（グリッド単位）.
		// 北-南のドアならX位置（Z位置は一番上または一番下）.
		// 東-西のドアならZ位置（X位置は一番右または一番左）.
	};

	// ルーム内の内壁のデーター.
	public struct InnerWallData {

		public int		x;				// 床のグリッド番号（左下が (0, 0)）.
		public int		z;				// 床のグリッド番号（左下が (0, 0)）.

		public bool		is_horizontal;	
	}

	// 内壁のはしっこにおく柱.
	public struct PillarData {
		
		// 左下のグリッド.
		// ろうそくは .
		// (x, x) (x + 1, z) (x, z + 1) (x + 1, z + 1)
		// の４つのグリッドの交点に置かれます.
		public int		x;				// 床のグリッド番号（左下が (0, 0)）.
		public int		z;				// 床のグリッド番号（左下が (0, 0)）.
	}

	// 床の上に置くもの（ドア、アイテムなど）.
	public struct BlockInfo {

		public Map.CHIP		chip;
		public int			option0;
	}

	public BlockInfo[,][,]	block_infos;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		if(dbwin.root().getWindow("map") == null) {

			this.create_debug_window();
		}
	}
	
	//void	Update()
	//{
	//}

	// ================================================================ //

	public void		setRoomNum(int columns, int rows)
	{
		this.room_columns_num = columns;
		this.room_rows_num    = rows;
	}

	public void		setRoomGridNum(int columns, int rows)
	{
		this.block_grid_columns_num = columns;
		this.block_grid_rows_num    = rows;

		this.block_columns_num = this.block_grid_columns_num + (this.block_grid_columns_num + 1);
		this.block_rows_num    = this.block_grid_rows_num    + (this.block_grid_rows_num + 1);

		this.room_column_size = BLOCK_SIZE*this.block_grid_columns_num;
		this.room_row_size    = BLOCK_SIZE*this.block_grid_rows_num;
	}

	// ルームの数をゲットする.
	public Map.RoomIndex	getRoomNum()
	{
		return(new Map.RoomIndex(this.room_columns_num, this.room_rows_num));
	}

	// ルーム内のブロックの数（よこ）をゲットする.
	public int		getBlockColumnsNum()
	{
		return(this.block_columns_num);
	}

	// ルーム内のブロックの数（たて）をゲットする.
	public int		getBlockRowsNum()
	{
		return(this.block_rows_num);
	}

	// ルームの大きさ（よこ）をゲットする.
	public float	getRoomColumnSize()
	{
		return(BLOCK_SIZE*this.block_grid_columns_num);
	}

	// ルームの大きさ（たて）をゲットする.
	public float	getRoomRowSize()
	{
		return(BLOCK_SIZE*this.block_grid_rows_num);
	}

	// ルーム内のブロックの数（よこ）をゲットする.
	public int		getBlockGridColumnsNum()
	{
		return(this.block_grid_columns_num);
	}

	// ルーム内のブロックの数（たて）をゲットする.
	public int		getBlockGridRowsNum()
	{
		return(this.block_grid_rows_num);
	}

	// レベル生成 Kazuhisa Minato.
	public void		generateLevel(int seed)
	{
		if(this.levelGenerator == null) {

			this.random_plant = PseudoRandom.get().createPlant("MapCreator", 100);

			// ---------------------------------------------------------------- //

			// 乱数の設定をコンストラクタで行います。.
			this.levelGenerator = new FieldGenerator (seed);
			
			// フィールドの生成方法.
			// フィールドのw, hは2以上.
			// ブロックのw, hは5以上かつ奇数という制限があります.
			// 奇数列のグリッド幅はゼロとして解釈をする必要があります。.
			this.levelGenerator.SetSize (ROOM_ROWS_NUM, ROOM_COLUMNS_NUM, BLOCK_ROWS_NUM, BLOCK_COLUMNS_NUM);

			// フィールドを生成します.
			// 各要素がブロックである2次元配列を生成します.
			// したがって、ブロック(i,j)にはreturn_value[i,j]でアクセスし、.
			// ブロック(i,j)内の要素(u,v)にはreturn_value[i,j][u,v]でアクセスします.
			var	levelData = levelGenerator.CreateField();


			// FieldGenerator の出力を並び替える.
			// ・[z, x]		->	[x, z].
			// ・上が z = 0	->	下が z = 0.

			this.level_data = new FieldGenerator.ChipType[ROOM_COLUMNS_NUM, ROOM_ROWS_NUM][,];

			foreach(var ri in Map.RoomIndex.getRange(ROOM_COLUMNS_NUM, ROOM_ROWS_NUM)) {

				this.level_data[ri.x, ri.z] = new FieldGenerator.ChipType[BLOCK_COLUMNS_NUM, BLOCK_ROWS_NUM];

				foreach(var bi in Map.BlockIndex.getRange(this.block_columns_num, this.block_rows_num)) {

					this.level_data[ri.x, ri.z][bi.x, bi.z] = levelData[ROOM_ROWS_NUM - 1 - ri.z, ri.x][BLOCK_ROWS_NUM - 1 - bi.z, bi.x];
				}
			}

			// スタート、ゴール.

			var ep = levelGenerator.GetEndPoints(levelData);

			this.start.room_index.x  = ep[0].fieldWidth;
			this.start.room_index.z  = ROOM_ROWS_NUM - 1 - ep[0].fieldHeight;
			this.start.block_index.x = (ep[0].blockWidth - 1)/2;
			this.start.block_index.z = this.block_grid_rows_num - 1 - (ep[0].blockHeight - 1)/2;

			this.goal.room_index.x  = ep[1].fieldWidth;
			this.goal.room_index.z  = ROOM_ROWS_NUM - 1 - ep[1].fieldHeight;
			this.goal.block_index.x = (ep[1].blockWidth - 1)/2;
			this.goal.block_index.z = this.block_grid_rows_num - 1 - (ep[1].blockHeight - 1)/2;

			//

			// ---------------------------------------------------------------- //

			// ルームを作る.

			this.floor_root_go = new GameObject("Floor");

			foreach(var room_index in Map.RoomIndex.getRange(ROOM_COLUMNS_NUM, ROOM_ROWS_NUM)) {

				this.createRoom(room_index);
			}

			// ダミーの部屋.
			// （フロアーの一番下の列）.
			for(int i = 0;i < ROOM_COLUMNS_NUM;i++) {

				this.createVacancy(new Map.RoomIndex(i, -1));
			}

			// ルームの仕切りを作成.
			this.createRoomWall();

			this.createOuterWalls();


			// ---------------------------------------------------------------- //
			// 床の上に置くもの（ドア、アイテムなど）の情報をつくっておく.

			// ルーム移動ドア.
	
			this.block_infos = new MapCreator.BlockInfo[ROOM_COLUMNS_NUM, ROOM_ROWS_NUM][,];

			foreach(var ri in Map.RoomIndex.getRange(ROOM_COLUMNS_NUM, ROOM_ROWS_NUM)) {

				this.block_infos[ri.x, ri.z] = new BlockInfo[this.block_grid_columns_num, this.block_grid_rows_num];

				var		block_info_room = this.block_infos[ri.x, ri.z];
				var		level_data_room = this.level_data[ri.x, ri.z];

				foreach(var bi in Map.BlockIndex.getRange(this.block_grid_columns_num, this.block_grid_rows_num)) {

					block_info_room[bi.x, bi.z].chip = Map.CHIP.VACANT;

					if(level_data_room[bi.x*2 + 1 - 1, bi.z*2 + 1] == FieldGenerator.ChipType.Door) {

						block_info_room[bi.x, bi.z].chip    = Map.CHIP.DOOR;
						block_info_room[bi.x, bi.z].option0 = (int)Map.EWSN.WEST;

					} else if(level_data_room[bi.x*2 + 1 + 1, bi.z*2 + 1] == FieldGenerator.ChipType.Door) {

						block_info_room[bi.x, bi.z].chip    = Map.CHIP.DOOR;
						block_info_room[bi.x, bi.z].option0 = (int)Map.EWSN.EAST;

					} else if(level_data_room[bi.x*2 + 1, bi.z*2 + 1 - 1] == FieldGenerator.ChipType.Door) {

						block_info_room[bi.x, bi.z].chip    = Map.CHIP.DOOR;
						block_info_room[bi.x, bi.z].option0 = (int)Map.EWSN.SOUTH;

					} else if(level_data_room[bi.x*2 + 1, bi.z*2 + 1 + 1] == FieldGenerator.ChipType.Door) {

						block_info_room[bi.x, bi.z].chip    = Map.CHIP.DOOR;
						block_info_room[bi.x, bi.z].option0 = (int)Map.EWSN.NORTH;
					}
				}
			}

			// フロアー移動ドア.

			this.block_infos[this.start.room_index.x, this.start.room_index.z][this.start.block_index.x, this.start.block_index.z].chip    = Map.CHIP.STAIRS;
			this.block_infos[this.start.room_index.x, this.start.room_index.z][this.start.block_index.x, this.start.block_index.z].option0 = 0;
			this.block_infos[this.goal.room_index.x,   this.goal.room_index.z][this.goal.block_index.x,   this.goal.block_index.z].chip    = Map.CHIP.STAIRS;
			this.block_infos[this.goal.room_index.x,   this.goal.room_index.z][this.goal.block_index.x,   this.goal.block_index.z].option0 = 1;

			foreach(var ri in Map.RoomIndex.getRange(ROOM_COLUMNS_NUM, ROOM_ROWS_NUM)) {

				var		info_room = this.block_infos[ri.x, ri.z];

				RoomController	room = this.get_room_root_go(ri);

				List<Map.BlockIndex>	reserves = new List<Map.BlockIndex>();

				// ジェネレーター.
				for(int i = 0;i < 3;i++) {

					var		lair_places = this.allocateChipPlacesRoom(ri, Map.CHIP.LAIR, 1);

					if(lair_places.Count == 0) {

						break;
					}

					// ジェネレーターがとなりあったブロックに出ないよう、.
					// 周囲の８ブロックをリザーブしておく.

					Map.BlockIndex	bi = lair_places[0];

					foreach(var around in bi.getArounds8()) {

						if(this.allocateChipPlaceRoom(ri, around, Map.CHIP.LAIR)) {
	
							reserves.Add(around);
						}
					}
				}

				// リザーブしたブロックを戻す.
				foreach(var reserve in reserves) {

					this.putbackChipPlaceRoom(ri, reserve);
				}

				// ルーム移動キー.

				var		key_places = this.allocateChipPlacesRoom(ri, Map.CHIP.KEY, room.getDoorCount());

				for(int i = 0;i < key_places.Count;i++) {

					Map.BlockIndex	place = key_places[i];

					info_room[place.x, place.z].option0 = (int)room.getDoorByIndex(i).KeyType;
				}
			}

			// フロアー移動ドアのキー.

			bool	floor_key_created = false;

			for(int i = 0;i < ROOM_COLUMNS_NUM*ROOM_ROWS_NUM;i++) {

				Map.RoomIndex	ri = new Map.RoomIndex();

				ri.x = this.random_plant.getRandomInt(ROOM_COLUMNS_NUM);
				ri.z = this.random_plant.getRandomInt(ROOM_ROWS_NUM);

				var floor_key_places = this.allocateChipPlacesRoom(ri, Map.CHIP.KEY, 1);

				if(floor_key_places.Count == 0) {

					continue;
				}

				this.block_infos[ri.x, ri.z][floor_key_places[0].x, floor_key_places[0].z].option0 = (int)Item.KEY_COLOR.PURPLE;

				floor_key_created = true;
				break;
			}
			if(!floor_key_created) {

				Debug.LogError("can't create floor key.");
			}

			// ---------------------------------------------------------------- //

		}
	}
#if false
	public int	getPracticableCount(Map.RoomIndex room_index, Map.BlockIndex block_index)
	{
		int		count = 0;

		for(int i = 0;i < (int)Map.EWSN.NUM;i++) {

			if(this.isPracticable(room_index, block_index, (Map.EWSN)i)) {

				count++;
			}
		}

		return(count);
	}
#endif
	// 指定のブロックから、指定の方向に進める？.
	public bool	isPracticable(Map.RoomIndex room_index, Map.BlockIndex block_index, Map.EWSN eswn)
	{
		bool	ret = false;
		var		level_data_room = this.level_data[room_index.x, room_index.z];

		switch(eswn) {

			case Map.EWSN.NORTH:
			{
				if(block_index.z < this.block_grid_rows_num - 1) {

					if(level_data_room[block_index.x*2 + 1, block_index.z*2 + 1 + 1] != FieldGenerator.ChipType.Wall) {

						ret = true;
					}
				}
			}
			break;

			case Map.EWSN.SOUTH:
			{
				if(block_index.z > 0) {

					if(level_data_room[block_index.x*2 + 1, block_index.z*2 + 1 - 1] != FieldGenerator.ChipType.Wall) {

						ret = true;
					}
				}
			}
			break;

			case Map.EWSN.EAST:
			{
				if(block_index.x < this.block_grid_columns_num - 1) {

					if(level_data_room[block_index.x*2 + 1 + 1, block_index.z*2 + 1] != FieldGenerator.ChipType.Wall) {

						ret = true;
					}
				}
			}
			break;

			case Map.EWSN.WEST:
			{
				if(block_index.x > 0) {

					if(level_data_room[block_index.x*2 + 1 - 1, block_index.z*2 + 1] != FieldGenerator.ChipType.Wall) {

						ret = true;
					}
				}
			}
			break;
		}

		return(ret);
	}

	// ================================================================ //
	// ルームを作る.

	// ルームを作る.
	public RoomController	createRoom(Map.RoomIndex room_index)
	{
		Vector3		room_center = Vector3.zero;
		Vector3		position    = Vector3.zero;
		int			n = 0;
		
		// ---------------------------------------------------------------- //
		// ルーム管理オブジェクトを作る.
	
		RoomController	room = this.get_room_root_go(room_index);

		room_center = this.getRoomCenterPosition(room_index);

		room.transform.position = room_center;
		
		// ---------------------------------------------------------------- //
		// 床をつくる.
		
		GameObject	floors = new GameObject("floors");
		
		floors.transform.position = room_center;

		room.floor_objects = new GameObject[this.block_grid_columns_num, this.block_grid_rows_num];

		foreach(var bi in Map.BlockIndex.getRange(this.block_grid_columns_num, this.block_grid_rows_num)) {

			GameObject	prefab = this.floor_prefabs[(bi.x + bi.z)%this.floor_prefabs.Length];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position = this.getBlockCenterPosition(bi);
			position += room_center;
			
			go.transform.position = position;
			go.transform.parent   = floors.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);

			room.floor_objects[bi.x, bi.z] = go;
		}

		// ---------------------------------------------------------------- //
		// ルーム移動ドアを作る.

		List<DoorData>	door_datas = new List<DoorData>();

		//  フロアーの右端の部屋じゃないなら…….
		if(room_index.x < ROOM_COLUMNS_NUM - 1) {

			for(int bz = 0; bz < this.block_rows_num; bz++) {
				int bx = this.block_columns_num - 1;

				if (this.level_data[room_index.x, room_index.z][bx, bz] == FieldGenerator.ChipType.Door)
				{
					// 左:右の部屋を結ぶドア.
					DoorData	data = new DoorData();

					data.type  = DoorControl.TYPE.ROOM;
					data.room0 = room_index;
					data.room1 = room_index.get_next(1, 0);
					data.local_position = bz/2;
					door_datas.Add(data);
				}
			}
		}

		//  フロアーの上端の部屋じゃないなら…….
		if(room_index.z < ROOM_ROWS_NUM - 1) {

			for(int bx = 0; bx < this.block_columns_num; bx++) {
				int bz = this.block_rows_num - 1;
				if (this.level_data[room_index.x, room_index.z][bx, bz] == FieldGenerator.ChipType.Door)
				{
					// 下:上の部屋を結ぶドア.
					DoorData	data = new DoorData();

					data.type  = DoorControl.TYPE.ROOM;
					data.room0 = room_index;				
					data.room1 = room_index.get_next(0, 1);
					data.local_position = bx/2;
					door_datas.Add(data);
				}
			}
		}

		this.create_doors(door_datas);

		// ---------------------------------------------------------------- //
		// ルーム内壁を作る.

		List<InnerWallData>		wall_datas = new List<InnerWallData>();

		// 右壁の登録.
		for(int bz = 1;bz < this.block_rows_num;bz+=2) {

			for(int bx = 2;bx < this.block_columns_num - 2;bx+=2) {

				// 壁かどうか調べる.
				if(this.level_data[room_index.x, room_index.z][bx, bz] == FieldGenerator.ChipType.Wall) {
					InnerWallData	data = new InnerWallData();
					
					data.x = (bx - 2) / 2;
					data.z = (bz - 1) / 2;
					data.is_horizontal = false; // たて壁.
					wall_datas.Add(data);
				}
			}
		}
		// 上壁の登録.
		for(int bx = 1;bx < this.block_columns_num;bx+=2) {

			for(int bz = 2;bz < this.block_rows_num - 2;bz+=2) {

				// 壁かどうか調べる.
				if(this.level_data[room_index.x, room_index.z][bx, bz] == FieldGenerator.ChipType.Wall) {

					InnerWallData	data = new InnerWallData();
					
					data.x = (bx - 1) / 2;
					data.z = (bz - 2) / 2;
					data.is_horizontal = true; // よこ壁.
					wall_datas.Add(data);
				}
			}
		}
		this.create_inner_wall(room_index, wall_datas);

		// ---------------------------------------------------------------- //
		// 柱をつくる.
		// 壁が２枚以上接しているところにつくる.
		
		List<PillarData>	pillar_datas = new List<PillarData>();
		PillarData			pillar_data  = new PillarData();
		
		for(int x = 0;x < BLOCK_GRID_COLUMNS_NUM - 1;x++) {
			
			for(int z = 0;z < BLOCK_GRID_ROWS_NUM - 1;z++) {
				
				n = 0;
				
				// 左.
				if(wall_datas.Exists(wall => (wall.is_horizontal && wall.x == x && wall.z == z))) {
					
					n++;
				}
				// 右.
				if(wall_datas.Exists(wall => (wall.is_horizontal && wall.x == x + 1 && wall.z == z))) {
					
					n++;
				}
				
				// 下.
				if(wall_datas.Exists(wall => (!wall.is_horizontal && wall.x == x && wall.z == z))) {
					
					n++;
				}
				// 上.
				if(wall_datas.Exists(wall => (!wall.is_horizontal && wall.x == x && wall.z == z + 1))) {
					
					n++;
				}
				
				if(n < 2) {
					
					continue;
				}
				
				pillar_data.x = x;
				pillar_data.z = z;
				
				pillar_datas.Add(pillar_data);
			}
		}
		
		this.create_pillar(room_index, pillar_datas);

		// ---------------------------------------------------------------- //

		floors.transform.parent = room.transform;

		return(room.GetComponent<RoomController>());
	}

	// ルーム（ダミーのあき部屋）を作る.
	public RoomController	createVacancy(Map.RoomIndex room_index)
	{
		Vector3		room_center = Vector3.zero;
		Vector3		position    = Vector3.zero;
		int			n = 0;
		
		// ---------------------------------------------------------------- //
		// ルーム管理オブジェクトを作る.
	
		RoomController	room = this.get_room_root_go(room_index);

		room_center = this.getRoomCenterPosition(room_index);

		room.transform.position = room_center;
		
		// ---------------------------------------------------------------- //
		// 床をつくる.
		
		GameObject	floors = new GameObject("floors");
		
		floors.transform.position = room_center;

		for(int z = 0;z < this.block_grid_rows_num;z++) {

			for(int x = 0;x < this.block_grid_columns_num;x++) {

				GameObject	prefab = this.floor_prefabs[n%this.floor_prefabs.Length];
				GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

				position = this.getBlockCenterPosition(new Map.BlockIndex(x, z));

				position += room_center;
				
				go.transform.position = position;
				
				go.transform.parent = floors.transform;

				//  Unity 2018 で、コリジョンがおかしくなることの対処.
				this.force_off_on_colliders(go);
				
				n++;
			}
		}

		floors.transform.parent = room.transform;

		return(room);
	}

	// ルーム管理オブジェクトを取得する. なければ作成する.
	protected RoomController	get_room_root_go(Map.RoomIndex index)
	{
		string	name = "room" + index.x + index.z;

		GameObject	go = GameObject.Find(name);

		if(go == null) {

			go = new GameObject(name);

			// 管理用のコンポーネントを追加する.
			RoomController room = go.AddComponent<RoomController>();

			room.setIndex(index);

			// リストに登録しておく.
			this.rooms.Add(room);

			go.transform.parent = this.floor_root_go.transform;
		}

		return(go.GetComponent<RoomController>());
	}

	// ドアを作る（１ルームぶん）.
	public void	create_doors(List<DoorData> door_datas)
	{
		DoorControl		door0, door1;
		Map.BlockIndex	block0_index, block1_index;

		int		n = 0;

		foreach(var data in door_datas) {

			// 両方の部屋で使われていない色のなかから、ランダムに選ぶ.

			RoomController room0 = get_room_root_go(data.room0).GetComponent<RoomController>();
			RoomController room1 = get_room_root_go(data.room1).GetComponent<RoomController>();

			List<bool>	color_used0 = room0.checkKeyColorsUsed();
			List<bool>	color_used1 = room1.checkKeyColorsUsed();

			List<int>	key_colors = new List<int>();

			for(int i = 0;i < color_used0.Count;i++) {

				if(!color_used0[i] && !color_used1[i]) {

					key_colors.Add(i);
				}
			}

			int		key_type = key_colors[this.random_plant.getRandomInt(key_colors.Count)];

			door0 = null;
			door1 = null;

			// 上下の部屋を結ぶドア.
			if(data.room0.x == data.room1.x) {

				block0_index = new Map.BlockIndex(data.local_position, BLOCK_GRID_ROWS_NUM - 1);
				block1_index = new Map.BlockIndex(data.local_position, 0);

				// 上（北）.
				if(data.room1.z == data.room0.z + 1) {

					door0 = this.create_door(data.type, data.room0, block0_index, Map.EWSN.NORTH, key_type);
					door1 = this.create_door(data.type, data.room1, block1_index, Map.EWSN.SOUTH, key_type);
				}
				// 下（南）.
				if(data.room1.z == data.room0.z - 1) {

					door0 = this.create_door(data.type, data.room0, block1_index, Map.EWSN.SOUTH, key_type);
					door1 = this.create_door(data.type, data.room1, block0_index, Map.EWSN.NORTH, key_type);
				}
			}

			// 左右の部屋を結ぶドア.
			if(data.room0.z == data.room1.z) {

				block0_index = new Map.BlockIndex(BLOCK_GRID_COLUMNS_NUM - 1, data.local_position);
				block1_index = new Map.BlockIndex(0,                          data.local_position);

				// 東（右）.
				if(data.room1.x == data.room0.x + 1) {

					door0 = this.create_door(data.type, data.room0, block0_index, Map.EWSN.EAST, key_type);
					door1 = this.create_door(data.type, data.room1, block1_index, Map.EWSN.WEST, key_type);
				}
				// 西（左）.
				if(data.room1.x == data.room0.x - 1) {

					door0 = this.create_door(data.type, data.room0, block1_index, Map.EWSN.WEST, key_type);
					door1 = this.create_door(data.type, data.room1, block0_index, Map.EWSN.EAST, key_type);
				}
			}

			if(door0 != null && door1 != null) {

				door0.connect_to = door1;
				door1.connect_to = door0;
			}

			n++;
		}
	}

	// ドアを作る（ひとつ）.
	public DoorControl		create_door(DoorControl.TYPE type, Map.RoomIndex room_index, Map.BlockIndex block_index, Map.EWSN dir, int key_type)
	{
		GameObject	prefab;

		if(type == DoorControl.TYPE.ROOM) {

			prefab = this.door_prefabs[key_type%(this.door_prefabs.Length - 1)];

		} else {

			prefab = this.door_prefabs[this.door_prefabs.Length - 1];
		}

		GameObject		go   = GameObject.Instantiate(prefab) as GameObject;
		
		DoorControl		door = go.AddComponent<DoorControl>();
		
		door.room_index = room_index;
		
		door.type     = type;
		door.KeyType  = key_type;
		door.door_dir = dir;

		//
		
		RoomController	room = this.get_room_root_go(room_index);

		go.transform.parent = room.transform;
		go.transform.localPosition = this.getBlockCenterPosition(block_index);

		room.RegisterDoor(door);
		
		return(door);
	}

	// ルーム内の内壁を作る.
	public void		create_inner_wall(Map.RoomIndex room_index, List<InnerWallData> datas)
	{
		Vector3		room_center = Vector3.zero;
		Vector3		position    = Vector3.zero;
		int			n = 0;

		//
		room_center = this.getRoomCenterPosition(room_index);

		// 床

		GameObject	inner_walls = new GameObject("inner walls");

		inner_walls.transform.position = room_center;

		foreach(var data in datas) {

			int		x = data.x;
			int		z = data.z;

			GameObject	prefab = this.inner_wall_prefabs[n%this.inner_wall_prefabs.Length];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position = this.getBlockCenterPosition(new Map.BlockIndex(x, z));
			
			if(data.is_horizontal) {
				
				// よこ（上）.
				position.z += BLOCK_SIZE/2.0f;
				
			} else {
				
				// たて（右）.
				position.x += BLOCK_SIZE/2.0f;
			}

			go.transform.position = position + room_center;

			// もとのモデルが縦方向なので、横方向のときは９０°回転する.
			if(data.is_horizontal) {

				go.transform.rotation = Quaternion.AngleAxis(90.0f, Vector3.up);
			}

			go.transform.parent = inner_walls.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);

			n++;
		}

		RoomController	room = this.get_room_root_go(room_index);

		inner_walls.transform.parent = room.gameObject.transform;
	}

	// ---------------------------------------------------------------- //
	// ろうそくの柱　ルーム内の内壁が交差するばしょ.

	public void		create_pillar(Map.RoomIndex room_index, List<PillarData> datas)
	{
		Vector3		room_center = Vector3.zero;
		Vector3		position    = Vector3.zero;
		int			n = 0;
		
		// ルームの中心の座標を求める.
		room_center = this.getRoomCenterPosition(room_index);
		
		// 柱
		
		GameObject	pillars = new GameObject("pillars");
		
		pillars.transform.position = room_center;
		
		foreach(var data in datas) {
			
			int		x = data.x;
			int		z = data.z;
			
			GameObject	prefab = this.pillar_prefab;
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;
			
			position = this.getBlockCenterPosition(new Map.BlockIndex(x, z));
			
			position.x += BLOCK_SIZE/2.0f;
			position.z += BLOCK_SIZE/2.0f;
			
			go.transform.position = position + room_center;
			go.transform.parent   = pillars.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);

			n++;
		}
		
		RoomController	room = this.get_room_root_go(room_index);
		
		pillars.transform.parent = room.transform;
		
	}
	
	// ---------------------------------------------------------------- //
	// ルーム同士の区切りの壁を作る.

	public void		createRoomWall()
	{
		Vector3		position    = Vector3.zero;

		GameObject	room_walls = new GameObject("room walls");

		int		room_wall_columns_num = this.room_columns_num*this.block_grid_columns_num;
		int		room_wall_rows_num    = (this.room_rows_num + 1)*this.block_grid_rows_num;

		// よこ.

		int		n = 0;

		for(int z = 0;z < this.room_rows_num;z++) {

			for(int x = 0;x < room_wall_columns_num;x++) {

				n++;

				GameObject	prefab = this.room_wall_prefabs[n%this.room_wall_prefabs.Length];
				GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

				position.x = (-((float)room_wall_columns_num/2.0f - 0.5f) + (float)x)*BLOCK_SIZE;
				position.y = 0.0f;
				position.z = (-((float)this.room_rows_num/2.0f - 0.5f) - 1.0f + (float)z)*this.room_row_size + this.room_row_size/2.0f;

				go.transform.position = position;
				go.transform.rotation = Quaternion.AngleAxis(90.0f, Vector3.up);
				go.transform.parent   = room_walls.transform;

				//  Unity 2018 で、コリジョンがおかしくなることの対処.
				this.force_off_on_colliders(go);

				// ルーム間ウォールをルームに登録しておく.

				int rx = x/this.block_grid_columns_num;

				RoomController room = get_room_root_go(new Map.RoomIndex(rx, z));

				if(room != null) {

					room.RegisterRoomWall(Map.EWSN.SOUTH, go);
				}

				if(z > 0) {

					room = get_room_root_go(new Map.RoomIndex(rx, z - 1));
	
					if(room != null) {
	
						room.RegisterRoomWall(Map.EWSN.NORTH, go);
					}
				}
			}
		}

		// たて.

		n = 0;

		for(int x = 0;x < this.room_columns_num - 1;x++) {

			for(int z = 0;z < room_wall_rows_num;z++) {

				n++;

				GameObject	prefab = this.room_wall_prefabs[n%this.room_wall_prefabs.Length];
				GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

				position.x = (-((float)this.room_columns_num/2.0f - 0.5f) + (float)x)*this.room_column_size + this.room_column_size/2.0f;
				position.y = 0.0f;
				position.z = (-((float)room_wall_rows_num/2.0f - 0.5f) - this.block_grid_rows_num/2.0f + (float)z)*BLOCK_SIZE;

				go.transform.position = position;
				go.transform.parent   = room_walls.transform;

				//  Unity 2018 で、コリジョンがおかしくなることの対処.
				this.force_off_on_colliders(go);

				int		rz = z/this.block_grid_rows_num;

				RoomController room = get_room_root_go(new Map.RoomIndex(x, rz));
				if (room != null) {
					room.RegisterRoomWall(Map.EWSN.EAST, go);
				}
				room = get_room_root_go(new Map.RoomIndex(x + 1, rz));
				if (room != null) {
					room.RegisterRoomWall(Map.EWSN.WEST, go);
				}
			}
		}

		room_walls.transform.parent = this.floor_root_go.transform;
	}

	// ---------------------------------------------------------------- //
	// 床を作る.

	// rx と rz で指定されたルームの床を敷く.
	public RoomController		createRoomFloor(Map.RoomIndex room_index)
	{
		Vector3		room_center = Vector3.zero;
		Vector3		position    = Vector3.zero;
		int			n = 0;
		
		//
		
		RoomController	room = this.get_room_root_go(room_index);
		
		room_center = this.getRoomCenterPosition(room_index);
		
		room.transform.position = room_center;
		
		// 床.
		GameObject	floors = new GameObject("floors");
		
		floors.transform.position = room_center;
		
		for(int z = 0;z < this.block_grid_rows_num;z++) {
			
			for(int x = 0;x < this.block_grid_columns_num;x++) {

				GameObject	prefab = this.floor_prefabs[n%this.floor_prefabs.Length];
				GameObject	go     = GameObject.Instantiate(prefab) as GameObject;
				
				go.isStatic = true;
				
				position = this.getBlockCenterPosition(new Map.BlockIndex(x, z));
				position += room_center;

				go.transform.position = position;
				go.transform.parent = floors.transform;

				//  Unity 2018 で、コリジョンがおかしくなることの対処.
				this.force_off_on_colliders(go);

				n++;
			}
		}
		
		floors.transform.parent = room.transform;

		return room.GetComponent<RoomController>();
	}

	// ---------------------------------------------------------------- //
	// フロアーの外壁をつくる.

	public GameObject		createOuterWalls()
	{
		Vector3		position    = Vector3.zero;

		// フロアーの南側にダミーの部屋を１列つくるので、その分外壁も大きくする.

		GameObject	outer_walls = new GameObject("outer walls");

		int		outer_wall_columns_num = this.room_columns_num*this.block_grid_columns_num;
		int		outer_wall_rows_num    = (this.room_rows_num + 1)*this.block_grid_rows_num;

		// ---------------------------------------------------------------- //

		// 上.
		for(int x = 0;x < outer_wall_columns_num - 2;x++) {

			GameObject	prefab = this.outer_wall_prefabs[0];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position.x = (-((float)outer_wall_columns_num/2.0f - 0.5f) + (float)(x + 1))*BLOCK_SIZE;
			position.y = 0.0f;
			position.z = ((float)outer_wall_rows_num/2.0f - this.block_grid_rows_num/2.0f + 0.5f)*BLOCK_SIZE;

			go.transform.position = position;
			go.transform.localRotation = Quaternion.AngleAxis( 90.0f, Vector3.up);
			go.transform.parent   = outer_walls.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);
		}

		// 下.
		for(int x = 0;x < outer_wall_columns_num - 2;x++) {

			GameObject	prefab = this.outer_wall_prefabs[0];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position.x = (-((float)outer_wall_columns_num/2.0f - 0.5f) + (float)(x + 1))*BLOCK_SIZE;
			position.y = 0.0f;
			position.z = -((float)outer_wall_rows_num/2.0f + this.block_grid_rows_num/2.0f + 0.5f)*BLOCK_SIZE;

			go.transform.position = position;
			go.transform.localRotation = Quaternion.AngleAxis(-90.0f, Vector3.up);
			go.transform.parent   = outer_walls.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);
		}

		// 右.
		for(int z = 0;z < outer_wall_rows_num - 2;z++) {

			GameObject	prefab = this.outer_wall_prefabs[0];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position.x =  ((float)outer_wall_columns_num/2.0f + 0.5f)*BLOCK_SIZE;
			position.y = 0.0f;
			position.z = (-((float)outer_wall_rows_num/2.0f + this.block_grid_rows_num/2.0f - 0.5f) + (float)(z + 1))*BLOCK_SIZE;

			go.transform.position = position;
			go.transform.localRotation = Quaternion.AngleAxis( 180.0f, Vector3.up);
			go.transform.parent   = outer_walls.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);
		}

		// 左.
		for(int z = 0;z < outer_wall_rows_num - 2;z++) {

			GameObject	prefab = this.outer_wall_prefabs[0];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position.x = (-((float)outer_wall_columns_num/2.0f + 0.5f))*BLOCK_SIZE;
			position.y = 0.0f;
			position.z = (-((float)outer_wall_rows_num/2.0f + this.block_grid_rows_num/2.0f - 0.5f) + (float)(z + 1))*BLOCK_SIZE;

			go.transform.position = position;
			go.transform.localRotation = Quaternion.AngleAxis( 0.0f, Vector3.up);
			go.transform.parent   = outer_walls.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);
		}

		// ---------------------------------------------------------------- //
		// コーナー.

		// 右上.
		{
			GameObject	prefab = this.outer_wall_prefabs[1];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position.x = ((float)outer_wall_columns_num/2.0f + 0.5f)*BLOCK_SIZE;
			position.y = 0.0f;
			position.z = ((float)outer_wall_rows_num/2.0f - this.block_grid_rows_num/2.0f + 0.5f)*BLOCK_SIZE;

			go.transform.position = position;
			go.transform.localRotation = Quaternion.AngleAxis(-90.0f, Vector3.up);
			go.transform.parent   = outer_walls.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);
		}
		// 左上.
		{
			GameObject	prefab = this.outer_wall_prefabs[1];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position.x = -((float)outer_wall_columns_num/2.0f + 0.5f)*BLOCK_SIZE;
			position.y = 0.0f;
			position.z =  ((float)outer_wall_rows_num/2.0f - this.block_grid_rows_num/2.0f + 0.5f)*BLOCK_SIZE;

			go.transform.position = position;
			go.transform.localRotation = Quaternion.AngleAxis(180.0f, Vector3.up);
			go.transform.parent   = outer_walls.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);
		}
		// 右下.
		{
			GameObject	prefab = this.outer_wall_prefabs[1];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position.x =  ((float)outer_wall_columns_num/2.0f + 0.5f)*BLOCK_SIZE;
			position.y = 0.0f;
			position.z = -((float)outer_wall_rows_num/2.0f + this.block_grid_rows_num/2.0f + 0.5f)*BLOCK_SIZE;

			go.transform.position = position;
			go.transform.localRotation = Quaternion.AngleAxis( 0.0f, Vector3.up);
			go.transform.parent   = outer_walls.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);
		}
		// 左下.
		{
			GameObject	prefab = this.outer_wall_prefabs[1];
			GameObject	go     = GameObject.Instantiate(prefab) as GameObject;

			position.x = -((float)outer_wall_columns_num/2.0f + 0.5f)*BLOCK_SIZE;
			position.y = 0.0f;
			position.z = -((float)outer_wall_rows_num/2.0f + this.block_grid_rows_num/2.0f + 0.5f)*BLOCK_SIZE;

			go.transform.position = position;
			go.transform.localRotation = Quaternion.AngleAxis(90.0f, Vector3.up);
			go.transform.parent   = outer_walls.transform;

			//  Unity 2018 で、コリジョンがおかしくなることの対処.
			this.force_off_on_colliders(go);
		}

		outer_walls.transform.parent = this.floor_root_go.transform;

		return(outer_walls);
	}

	// フロアー移動ドアを作る.
	public void		createFloorDoor()
	{
		do {

			if (levelGenerator == null) {
				Debug.LogError("levelGenerator hasn't been initialized.");
				break;
			}

			Map.RoomIndex	room_index  = this.goal.room_index;
			Map.BlockIndex	block_index = this.goal.block_index;
	
			Map.EWSN	door_dir = Map.EWSN.NONE;
	
			if (block_index.x == 0) {
				door_dir = Map.EWSN.WEST;
			}
			else if (block_index.x == (BLOCK_GRID_COLUMNS_NUM - 1)) {
				door_dir = Map.EWSN.EAST;
			}
	
			if (block_index.z == 0) {
				door_dir = Map.EWSN.NORTH;
			}
			else if (block_index.z == (BLOCK_GRID_ROWS_NUM - 1)) {
				door_dir = Map.EWSN.SOUTH;
			}

			if(door_dir == Map.EWSN.NONE) {

				Debug.LogError("illegal floor door position.");
				break;
			}

			this.createFloorDoor(room_index, block_index, door_dir);

		} while(false);
	}

	// フロアー移動ドアを作る.
	public void		createFloorDoor(Map.RoomIndex room_index, Map.BlockIndex block_index, Map.EWSN door_dir)
	{
		bossDoor = this.create_door(DoorControl.TYPE.FLOOR, room_index, block_index, door_dir, 4);
	}

	// ルームドアのキー（全ルーム）とフロアードアのキーをつくる.
	public void		generateItems(string account_name)
	{
		// ルームドアのキーをつくる（１ルームぶん、フロアーキーも含む）.

		if(levelGenerator != null) {

			foreach(var ri in Map.RoomIndex.getRange(ROOM_COLUMNS_NUM, ROOM_ROWS_NUM)) {

				this.generateItemsInRoom(ri.x, ri.z, account_name);
			}
		}
	}

	// ルームドアのキーをつくる（１ルームぶん　ルームキー、フロアーキー）.
	private void generateItemsInRoom(int rx, int rz, string account_name)
	{
		ItemManager itemMgr = ItemManager.getInstance();
		if (levelGenerator == null) {
			Debug.LogError("levelGenerator isn't initialized.");
			return;
		}

		Map.RoomIndex	ri = new Map.RoomIndex(rx, rz);

		RoomController	room = get_room_root_go(ri).GetComponent<RoomController>();

		BlockInfo[,]	block_info_room = this.block_infos[ri.x, ri.z];

		foreach(var bi in Map.BlockIndex.getRange(this.block_grid_columns_num, this.block_grid_rows_num)) {

			if(block_info_room[bi.x, bi.z].chip != Map.CHIP.KEY) {

				continue;
			}

			int		key_type = block_info_room[bi.x, bi.z].option0;

			if(key_type == -1) {

				continue;
			}

			// かぎアイテムのインスタンスをつくる.

			string	type_name = Item.Key.getTypeName((Item.KEY_COLOR)key_type);
			string	item_name = Item.Key.getInstanceName((Item.KEY_COLOR)key_type, ri);

			itemMgr.createItem(type_name, item_name, account_name);

			// 位置を求める.

			Vector3 initialPosition = this.getRoomCenterPosition(ri) + this.getBlockCenterPosition(bi);

			// 中心からちょっとずらす.
			initialPosition.x += BLOCK_SIZE/4;
			initialPosition.z += BLOCK_SIZE/4;

			itemMgr.setPositionToItem(item_name, initialPosition);

			room.RegisterKey(item_name, key_type);
		}
	}

	// ================================================================ //

	// カギ、ドアなどをおく場所（ブロック）を確保する.
	public List<Map.BlockIndex>		allocateChipPlacesRoom(Map.RoomIndex room_index, Map.CHIP chip, int count)
	{
		List<Map.BlockIndex>	places = new List<Map.BlockIndex>();

		do {

			if(this.block_infos == null) {

				break;
			}
			BlockInfo[,]	info_room = this.block_infos[room_index.x, room_index.z];
	
			// 空いている場所を数える.
	
			int		vacant_count = 0;
	
			foreach(var bi in Map.BlockIndex.getRange(this.block_grid_columns_num, this.block_grid_rows_num)) {
	
				if(info_room[bi.x, bi.z].chip != Map.CHIP.VACANT) {
	
					continue;
				}
				vacant_count++;
			}

			count = Mathf.Min(count, vacant_count);

			if(count <= 0) {

				break;
			}

			// 0 ～ vacant_count までの乱数を count 個、重複なく選ぶ.
	
			List<int>	candidates = new List<int>();
	
			for(int i = 0;i < count;i++) {
	
				int		new_candidate = this.random_plant.getRandomInt(vacant_count - i);
	
				foreach(var candidate in candidates) {
	
					if(new_candidate >= candidate) {
	
						new_candidate++;
					}
				}
				candidates.Add(new_candidate);
			}

			candidates.Sort();

			// candidates[] 番目の要素を places にコピー.

			vacant_count = 0;
	
			foreach(var bi in Map.BlockIndex.getRange(this.block_grid_columns_num, this.block_grid_rows_num)) {
	
				if(info_room[bi.x, bi.z].chip != Map.CHIP.VACANT) {
	
					continue;
				}

				if(vacant_count == candidates[0]) {

					info_room[bi.x, bi.z].chip = chip;

					places.Add(bi);
					candidates.RemoveAt(0);

					if(candidates.Count == 0) {

						break;
					}
				}
				vacant_count++;
			}

		} while(false);

		return(places);
	}

	// 特定の場所一か所にチップをおく.
	public bool		allocateChipPlaceRoom(Map.RoomIndex room_index, Map.BlockIndex block_index, Map.CHIP chip)
	{
		bool			ret = false;
		BlockInfo[,]	info_room = this.block_infos[room_index.x, room_index.z];

		do {

			if(block_index.x < 0 || this.block_grid_columns_num <= block_index.x) {

				break;
			}
			if(block_index.z < 0 || this.block_grid_rows_num <= block_index.z) {

				break;
			}

			if(info_room[block_index.x, block_index.z].chip != Map.CHIP.VACANT) {

				break;
			}

			info_room[block_index.x, block_index.z].chip = chip;
			ret = true;

		} while(false);

		return(ret);
	}

	// 特定の場所からチップをとりのぞく.
	public void		putbackChipPlaceRoom(Map.RoomIndex room_index, Map.BlockIndex block_index)
	{
		BlockInfo[,]	info_room = this.block_infos[room_index.x, room_index.z];

		do {

			if(block_index.x < 0 || this.block_grid_columns_num <= block_index.x) {

				break;
			}
			if(block_index.z < 0 || this.block_grid_rows_num <= block_index.z) {

				break;
			}

			info_room[block_index.x, block_index.z].chip = Map.CHIP.VACANT;

		} while(false);
	}

	// 指定のルームの、敵のジェネレーター（Lair、SpawnPoint）を設置するブロックをゲットする.
	public List<Map.BlockIndex>	getLairPlacesRoom(Map.RoomIndex room_index)
	{
		List<Map.BlockIndex>		places = new List<Map.BlockIndex>();

		BlockInfo[,]	block_info_map = this.block_infos[room_index.x, room_index.z];

		foreach(var bi in Map.BlockIndex.getRange(this.block_grid_columns_num, this.block_grid_rows_num)) {

			BlockInfo	info = block_info_map[bi.x, bi.z];

			if(info.chip != Map.CHIP.LAIR) {

				continue;
			}

			places.Add(bi);
		}

		return(places);
	}

	// フロアーのスタート地点をゲットする.
	public Vector3	getPlayerStartPosition()
	{
		Map.RoomIndex	ri = this.start.room_index;
		Map.BlockIndex	bi = this.start.block_index;

		Vector3		position = this.getRoomCenterPosition(ri) + this.getBlockCenterPosition(bi);

		return(position);
	}

	// フロアーのゴール地点をゲットする.
	public Vector3	getBossDoorPosition()
	{
		Map.RoomIndex	ri = this.goal.room_index;
		Map.BlockIndex	bi = this.goal.block_index;

		Vector3		position = this.getRoomCenterPosition(ri) + this.getBlockCenterPosition(bi);

		return(position);
	}

	// ルームをゲットする.
	public RoomController	getRoom(Map.RoomIndex room_index)
	{
		return(this.get_room_root_go(room_index));
	}

	// ワールド座標から、ルームをゲットする.
	public RoomController	getRoomFromPosition(Vector3 world_position)
	{
		Map.RoomIndex	ri = this.getRoomIndexFromPosition(world_position);

		RoomController	room = this.get_room_root_go(ri);

		if (room == null) {
			Debug.LogError("Invaild world position.");
		}

		return(room);
	}

	// ワールド座標から、RoomIndex を求める.
	public Map.RoomIndex	getRoomIndexFromPosition(Vector3 world_position)
	{
		Map.RoomIndex	ri;

		world_position.x -= (-(float)ROOM_COLUMNS_NUM/2.0f)*ROOM_COLUMN_SIZE;
		world_position.z -= (-(float)ROOM_ROWS_NUM/2.0f)*ROOM_ROW_SIZE;

		ri.x = Mathf.FloorToInt(world_position.x/ROOM_COLUMN_SIZE);
		ri.z = Mathf.FloorToInt(world_position.z/ROOM_ROW_SIZE);

		return(ri);
	}

	// ワールド座標から、RoomIndex と BlockIndex を求める.
	public void		getBlockIndexFromPosition(out Map.RoomIndex room_index, out Map.BlockIndex block_index, Vector3 world_position)
	{
		room_index = this.getRoomIndexFromPosition(world_position);

		Vector3		room_center = this.getRoomCenterPosition(room_index);

		world_position -= room_center;

		Vector3		room_size = Vector3.zero;

		room_size.x = BLOCK_SIZE*this.block_grid_columns_num;
		room_size.z = BLOCK_SIZE*this.block_grid_rows_num;

		world_position += room_size/2.0f;

		block_index.x = Mathf.FloorToInt(world_position.x/BLOCK_SIZE);
		block_index.z = Mathf.FloorToInt(world_position.z/BLOCK_SIZE);
	}

	// ================================================================ //

	// ルームの中心の座標を求める.
	public Vector3 getRoomCenterPosition(Map.RoomIndex room_index)
	{
		Vector3		room_center = Vector3.zero;

		room_center.x = (-((float)this.room_columns_num/2.0f - 0.5f) + (float)room_index.x)*this.room_column_size;
		room_center.y = 0.0f;
		room_center.z = (-((float)this.room_rows_num/2.0f    - 0.5f) + (float)room_index.z)*this.room_row_size;

		return(room_center);
	}

	// ルームのXZ座標の矩形をゲットする.
	public Rect		getRoomRect(Map.RoomIndex room_index)
	{
		Vector3	center = this.getRoomCenterPosition(room_index);

		float	w = this.getRoomColumnSize();
		float	h = this.getRoomRowSize();

		Rect	rect = new Rect(center.x - w/2.0f, center.z - h/2.0f, w, h);

		return(rect);
	}

	// ブロック（床グリッド）の中心の座標を求める.
	public Vector3	getBlockCenterPosition(Map.BlockIndex block_index)
	{
		Vector3		block_center = Vector3.zero;
		
		block_center.x = (-(float)this.block_grid_columns_num/2.0f + 0.5f + (float)block_index.x)*BLOCK_SIZE;
		block_center.y = 0.0f;
		block_center.z = (-(float)this.block_grid_rows_num/2.0f    + 0.5f + (float)block_index.z)*BLOCK_SIZE;
		
		return(block_center);
	}

	// 床の高さをゲットする.
	public float	getFloorHeight()
	{
		return(0.0f);
	}

	// ================================================================ //
	// ルーム関連.

	public void SetCurrentRoom(RoomController newCurrentRoom)
	{
		//Debug.Log ("Party " + currentRoom + " ---> " + newCurrentRoom);
		if (currentRoom != null) {
			currentRoom.NotifyPartyLeave();
		}
		currentRoom = newCurrentRoom;
		currentRoom.NotifyPartyEnter();
	}
	
	public RoomController	GetCurrentRoom()
	{
		return this.currentRoom;
	}

	public RoomController FindRoomByDoor(DoorControl door)
	{
		return door.GetRoom();
	}

	public void		UnlockBossDoor()
	{
		if (bossDoor != null) {
			bossDoor.Unlock();
			bossDoor.beginWaitEnter();
		}
		else {
			//Debug.LogError("The current level doesn't have the boss door, but someone would like to unlock it.");
		}
	}
	
	// ================================================================ //

	protected void	create_debug_window()
	{
		var		window = dbwin.root().createWindow("map");

		window.createButton("door")
			.setOnPress(() => 
			{
				var		current_room = PartyControl.get().getCurrentRoom();

				if(current_room != null) {

					for(int i = 0;i < (int)Map.EWSN.NUM;i++) {

						var		door = current_room.getDoor((Map.EWSN)i);

						if(door == null) {

							continue;
						}

						if(door.IsUnlocked()) {

							door.dbLock();
							door.connect_to.dbLock();

						} else {

							door.Unlock();
							door.connect_to.Unlock();
						}
					}
				}
			});

		window.createButton("点火")
			.setOnPress(() => 
			{
				RoomController		current_room = PartyControl.get().getCurrentRoom();

				current_room.igniteCandles();
			});

		window.createButton("消化")
			.setOnPress(() => 
			{
				RoomController		current_room = PartyControl.get().getCurrentRoom();

				current_room.puffOutCandles();
			});
	}

	// ================================================================ //
	// インスタンス.

	private	static MapCreator	instance = null;

	public static MapCreator	getInstance()
	{
		if(MapCreator.instance == null) {

			MapCreator.instance = GameObject.Find("GameRoot").GetComponent<MapCreator>();
		}

		return(MapCreator.instance);
	}
	public static MapCreator	get()
	{
		return(MapCreator.getInstance());
	}
}
