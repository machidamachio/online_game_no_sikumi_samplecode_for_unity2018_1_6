using System;
using System.Collections.Generic;

public class FieldGenerator
{
	private Random random;
	private int fieldWidth;
	private int fieldHeight;
	private int blockWidth;
	private int blockHeight;
	
	private class Direction
	{
		public const int Left = 1 << 0;
		public const int Right = 1 << 1;
		public const int Top = 1 << 2;
		public const int Bottom = 1 << 3;
	}
	
	public struct Position
	{
		public int fieldHeight;
		public int fieldWidth;
		public int blockHeight;
		public int blockWidth;
	}
	
	/// <summary>
	/// ブロックの最小構成単位チップ(適当に名づけた)の種類
	/// </summary>
	public enum ChipType
	{
		/// <summary>
		/// 通行可能な床
		/// </summary>
		Floor,
		
		/// <summary>
		/// 通行不可能な壁
		/// </summary>
		Wall,
		
		/// <summary>
		/// 左扉の鍵
		/// </summary>
		LeftKey,
		
		/// <summary>
		/// 右扉の鍵
		/// </summary>
		RightKey,
		
		/// <summary>
		/// 上扉の鍵
		/// </summary>
		TopKey,
		
		/// <summary>
		/// 下扉の鍵
		/// </summary>
		BottomKey,
		
		/// <summary>
		/// 扉
		/// </summary>
		Door,
		
		/// <summary>
		/// 敵の生成位置
		/// </summary>
		Spawn,
		
		/// <summary>
		/// ボス鍵.
		/// </summary>
		BossKey,
	}
	
	public FieldGenerator(int seed)
	{
		random = new Random(seed);
	}
	
	public void SetSize( int fieldHeight, int fieldWidth, int blockHeight, int blockWidth )
	{
		if (blockWidth < 5 || blockHeight < 5)
		{
			throw new ArgumentException("blockサイズは5位上");
		}
		
		if (blockWidth % 2 == 0 || blockHeight % 2 == 0)
		{
			throw new ArgumentException("blockサイズは奇数");
		}
		if (fieldHeight <= 1 || fieldWidth <= 1)
		{
			throw new ArgumentException( "fieldサイズは2以上" );
		}
		
		
		this.fieldWidth = fieldWidth;
		this.fieldHeight = fieldHeight;
		this.blockWidth = blockWidth;
		this.blockHeight = blockHeight;
	}
	
	/// <summary>
	/// doorを持つblockを作る。
	/// </summary>
	/// <param name="door">扉。最大4方向に設置できる。方向は和演算で指定する</param>
	private void CreateWall(int door)
	{
		if ((door & Direction.Left) != 0)
			Console.WriteLine("left");
		if ((door & Direction.Right) != 0)
			Console.WriteLine("right");
		if ((door & Direction.Top) != 0)
			Console.WriteLine("top");
		if ((door & Direction.Bottom) != 0)
			Console.WriteLine("bottom");
		
	}
	
	private ChipType[,] CreateEmptyBlock(int door, int height, int width)
	{
		var block = new ChipType[height, width];
		
		// 外周を全て埋める.
		for (int w = 0; w < width; ++w)
		{
			block[0, w] = ChipType.Wall;
			block[height-1,w] = ChipType.Wall;
		}
		for (int h = 1; h < height-1; ++h)
		{
			block[h, 0] = ChipType.Wall;
			block[h, width-1] = ChipType.Wall;
		}
		
		// 奇数行列だけ埋める.
		// （４タイルの中心。柱がおかれるところ）.
		for (int h = 2; h < height-1; h+=2) {
			for (int w = 2; w < width; w+=2) {
				block[h, w] = ChipType.Wall;
			}
		}
		
		return block;
	}
	
	/// <summary>
	/// Blockを作る
	/// </summary>
	/// <param name="emptyBlock"></param>
	/// <returns></returns>
	private void CreateWall(ref ChipType[,] emptyBlock)
	{
		int height = emptyBlock.GetLength(0);
		int width = emptyBlock.GetLength(1);
		
		for (int h = 2; h < height-1; h+=2)
		{
			for (int w = 2; w < width-1; w+=2)
			{
				int randMax = 3;
				var next = random.Next(randMax);
				if (next == 0) // 右方向
					emptyBlock[h,w+1] = ChipType.Wall; 
				else if (next == 1) // 左方向
					emptyBlock[h,w-1] = ChipType.Wall;
				else if (next == 2) // 下方向
					emptyBlock[h+1,w] = ChipType.Wall;
			}
		}
	}
	
	private bool HasTargetDoor(int door, int direction)
	{
		if ((door & direction) == 0)
			return false;
		return true;
	}
	
	private void CreateDoor(ref ChipType[,] block, int door, bool is_room)
	{
		int hd = 0;
		int wd = 0;
		if (((block.GetLength(0)-2) / 2) % 2 == 1)
			hd = 1;
		if (((block.GetLength(1)-2) / 2) % 2 == 1)
			wd = 1;
		
		ChipType doorChip = ChipType.Door;
		
		if ((door & Direction.Left) != 0)
		{
			if(is_room) {

				block[block.GetLength(0)/2-hd, 0] = ChipType.Wall;
				block[block.GetLength(0)/2-hd, 1] = doorChip;

			} else {

				block[block.GetLength(0)/2-hd, 0] = doorChip;
				block[block.GetLength(0)/2-hd, 1] = ChipType.Floor;
			}
		}
		
		if ((door & Direction.Right) != 0)
		{
			if(is_room) {

				block[block.GetLength(0)/2-hd, block.GetLength(1)-1] = ChipType.Wall;
				block[block.GetLength(0)/2-hd, block.GetLength(1)-2] = doorChip;

			} else {

				block[block.GetLength(0)/2-hd, block.GetLength(1)-1] = doorChip;
				block[block.GetLength(0)/2-hd, block.GetLength(1)-2] = ChipType.Floor;
			}
		}
		
		if ((door & Direction.Top) != 0)
		{
			if(is_room) {

				block[0, block.GetLength(1)/2+wd] = ChipType.Wall;
				block[1, block.GetLength(1)/2+wd] = doorChip;

			} else {

				block[0, block.GetLength(1)/2+wd] = doorChip;
				block[1, block.GetLength(1)/2+wd] = ChipType.Floor;
			}
		}
		
		if ((door & Direction.Bottom) != 0)
		{
			if(is_room) {

				block[block.GetLength(0)-1, block.GetLength(1)/2+wd] = ChipType.Wall;
				block[block.GetLength(0)-2, block.GetLength(1)/2+wd] = doorChip;

			} else {

				block[block.GetLength(0)-1, block.GetLength(1)/2+wd] = doorChip;
				block[block.GetLength(0)-2, block.GetLength(1)/2+wd] = ChipType.Floor;
			}
		}
	}
	
	private void CreateKey(ref ChipType[,] block, int door)
	{
		// 床の数カウント
		int nofFloor = 0;
		for (int h = 1; h < block.GetLength(0)-1; ++h)
		{
			for (int w = 1; w < block.GetLength(1)-1; ++w)
			{
				if (block[h, w] == ChipType.Floor)
					++nofFloor;
			}
		}
		
		// 鍵の設置
		for (int i = 0; i < 4; ++i)
		{
			ChipType key;
			if (i == 0) {
				key = ChipType.RightKey;
				if ((door & Direction.Right) == 0)
					continue;
			}
			else if (i == 1)
			{
				key = ChipType.LeftKey;
				if ((door & Direction.Left) == 0)
					continue;
			}
			else if (i == 2)
			{
				key = ChipType.BottomKey;
				if ((door & Direction.Bottom) == 0)
					continue;
			}
			else
			{
				key = ChipType.TopKey;     
				if ((door & Direction.Top) == 0)
					continue;
			}
			
			double total = 0.0;
			double p = random.NextDouble();
			for (int h = 1; h < block.GetLength(0)-1; ++h)
			{
				for (int w = 1; w < block.GetLength(1)-1; ++w)
				{
					total += 1.0 / nofFloor;
					if (block[h,w] != ChipType.Floor || p > total)
						continue;
					
					block[h, w] = key;
					--nofFloor;
					h = block.GetLength(0);
					break;
				}
			}
		}
	}
	
	public ChipType[,][,] CreateField()
	{
		if (this.blockHeight == 0)
			throw new Exception("サイズをセットしてください");
		
		// スタートとゴールをランダム選択
		int start = -1;
		int goal = -1;
		while (start == goal || goal < -1 || start < -1)
		{
			start = random.Next(4);
			goal = random.Next(4);
		}
		
		int door = 0;
		if (start == 0)
			door |= Direction.Top;
		else if (start == 1)
			door |= Direction.Bottom;
		else if (start == 2)
			door |= Direction.Left;
		else if (start == 3)
			door |= Direction.Right;
		
		if (goal == 0)
			door |= Direction.Top;
		else if (goal == 1)
			door |= Direction.Bottom;
		else if (goal == 2)
			door |= Direction.Left;
		else if (goal == 3)
			door |= Direction.Right;
		
		
		ChipType[,][,] field = new ChipType[this.fieldHeight, this.fieldWidth][,];
		
		var block = CreateEmptyBlock(door, fieldHeight * 2 + 1, fieldWidth * 2 + 1);
		
		// フィールドの扉を設置
		CreateDoor(ref block, door, false);
		
		// フィールドを隔てる壁配置
		CreateWall(ref block);
		
		//var doorInfo = new int[this.fieldHeight, this.fieldWidth];
		
		
		// フィールドを構築.
		for (int h = 0; h < this.fieldHeight; ++h)
		{
			int blockH = 2 * h + 1;
			for (int w = 0; w < this.fieldWidth; ++w)
			{
				int blockW = 2 * w + 1;
				int blockDoor = 0;
				
				if (block[blockH+1, blockW] != ChipType.Wall)
					blockDoor |= Direction.Bottom;
				if (block[blockH-1, blockW] != ChipType.Wall)
					blockDoor |= Direction.Top;
				if (block[blockH, blockW+1] != ChipType.Wall)
					blockDoor |= Direction.Right;
				if (block[blockH, blockW-1] != ChipType.Wall)
					blockDoor |= Direction.Left;
				
				var curBlock = CreateEmptyBlock(blockDoor, this.blockHeight, this.blockWidth);
				field[h, w] = curBlock;
				CreateWall(ref curBlock);
				CreateDoor(ref curBlock, blockDoor, false);
				//CreateKey(ref curBlock, blockDoor);
			}
		}
		
		// ボス鍵配置
		//SetBossKey(field);
		
		return field;
	}
	
	private void SetBossKey(ChipType[,][,] field)
	{
		var ep = GetEndPoints(field);
		for (int i = 0; i < ep.Length; ++i)
		{
			var block = field[ep[i].fieldHeight, ep[i].fieldWidth]; // Endpointがあるブロック.
			
			// endpointを開ける鍵を削除
			for (int bh = 0; bh < block.GetLength(0); ++bh)
			{
				for (int bw = 0; bw < block.GetLength(1); ++bw)
				{
					if ((ep[i].blockHeight == 0 && block[bh, bw] == ChipType.TopKey)
					    || (ep[i].blockHeight == block.GetLength(0)-1 && block[bh, bw] == ChipType.BottomKey)
					    || (ep[i].blockWidth == 0 && block[bh, bw] == ChipType.LeftKey)
					    || (ep[i].blockWidth == block.GetLength(1)-1 && block[bh, bw] == ChipType.RightKey))
					{
						block[bh, bw] = ChipType.Floor;
					}
				}
			}
		}
		
		var numFloor = 0;
		for (int fh = 0; fh < this.fieldHeight; ++fh)
		{
			for (int fw = 0; fw < this.fieldWidth; ++fw)
			{
				// 出入口には配置しないので床カウントしない.
				if ((ep[0].fieldHeight == fh && ep[0].fieldWidth == fw)
				    || (ep[1].fieldHeight == fh && ep[1].fieldWidth == fw))
					continue;
				
				for (int bh = 0; bh < this.blockHeight; ++bh)
					for (int bw = 0; bw < this.blockWidth; ++bw)
						if (field[fh, fw][bh, bw] == ChipType.Floor)
							++numFloor;               
			}
		}
		
		double r = random.NextDouble();
		double total = 0.0;
		
		for (int fh = 0; fh < this.fieldHeight; ++fh)
		{
			for (int fw = 0; fw < this.fieldWidth; ++fw)
			{
				// 出入口には配置しないのでスルー.
				if ((ep[0].fieldHeight == fh && ep[0].fieldWidth == fw)
				    || (ep[1].fieldHeight == fh && ep[1].fieldWidth == fw))
					continue;
				
				for (int bh = 0; bh < this.blockHeight; ++bh)
				{
					for (int bw = 0; bw < this.blockWidth; ++bw)
					{
						if (field[fh, fw][bh, bw] == ChipType.Floor)
						{
							total += 1.0/numFloor;
							if (r < total)
							{
								field[fh, fw][bh, bw] = ChipType.BossKey;
								fh = fw = bh = bw = int.MaxValue-1;
							}
						}
					}
				}
			}
		}
	}
	
	public void DebugPrint(ChipType[,] block)
	{
		for (int h = 0; h < block.GetLength(0); ++h)
		{
			for (int w = 0; w < block.GetLength(1); ++w)
			{
				switch (block[h, w]) 
				{
				case ChipType.Wall:
					Console.Write("@");
					break;
				case ChipType.Floor:
					Console.Write(" ");
					break;
				case ChipType.BottomKey:
					Console.Write("b");
					break;
				case ChipType.TopKey:
					Console.Write("t");
					break;
				case ChipType.RightKey:
					Console.Write("r");
					break;
				case ChipType.LeftKey:
					Console.Write("l");
					break;
				case ChipType.Door:
					Console.Write("d");
					break;
				case ChipType.BossKey:
					Console.Write("B");
					break;
				default:
					throw new Exception("そんなブロックないよ");
				}
			}
			Console.WriteLine(string.Empty);
		}
	}
	
	/// <summary>
	/// 出口と入口を取得する
	/// </summary>
	/// <remarks>出口と入口に違いはないので都合の良い用に使ってください</remarks>
	/// <returns>出口と入口の場所</returns>
	public Position[] GetEndPoints(ChipType[,][,] field)
	{
		Position[] endpoints = new Position[2];
		endpoints[0].fieldHeight = -1;
		
		// 左の壁を調べる        
		for (int fh = 0; fh < this.fieldHeight; ++fh)
		{
			for (int bh = 0; bh < this.blockHeight; ++bh)
			{
				if (field[fh, 0][bh, 0] == ChipType.Door)
				{
					endpoints[0].fieldHeight = fh;
					endpoints[0].fieldWidth = 0;
					endpoints[0].blockHeight = bh;
					endpoints[0].blockWidth = 0;
					fh = int.MaxValue-1;
					break;
				}
			}
		}
		
		// 上の壁を調べる
		for (int fw = 0; fw < this.fieldWidth; ++fw)
		{
			for (int bw = 0; bw < this.blockWidth; ++bw)
			{
				if (field[0, fw][0, bw] == ChipType.Door)
				{
					int target = endpoints[0].fieldHeight == -1 ? 0 : 1;
					endpoints[target].fieldHeight = 0;
					endpoints[target].fieldWidth = fw;
					endpoints[target].blockHeight = 0;
					endpoints[target].blockWidth = bw;
					if (target == 1)
					{
						return endpoints;
					}
					fw = int.MaxValue-1;
					break;
				}
			}
		}
		
		// 右の壁を調べる        
		for (int fh = 0; fh < this.fieldHeight; ++fh)
		{
			for (int bh = 0; bh < this.blockHeight; ++bh)
			{
				if (field[fh, this.fieldWidth-1][bh, this.blockWidth-1] == ChipType.Door)
				{
					int target = endpoints[0].fieldHeight == -1 ? 0 : 1;
					endpoints[target].fieldHeight = fh;
					endpoints[target].fieldWidth = this.fieldWidth-1;
					endpoints[target].blockHeight = bh;
					endpoints[target].blockWidth = this.blockWidth-1;
					if (target == 1)
					{
						return endpoints;
					}
					fh = int.MaxValue-1;
					break;
				}
			}
		}
		
		// 下の壁を調べる
		for (int fw = 0; fw < this.fieldWidth; ++fw)
		{
			for (int bw = 0; bw < this.blockWidth; ++bw)
			{
				if (field[this.fieldHeight-1, fw][this.blockHeight-1, bw] == ChipType.Door)
				{
					int target = endpoints[0].fieldHeight == -1 ? 0 : 1;
					endpoints[target].fieldHeight = this.fieldHeight-1;
					endpoints[target].fieldWidth = fw;
					endpoints[target].blockHeight = this.blockHeight-1;
					endpoints[target].blockWidth = bw;
					if (target == 1)
					{
						return endpoints;
					}
					fw = int.MaxValue-1;
					break;
				}
			}
		}
		return endpoints;
	}
	
	/// <summary>
	/// ブロックに敵の発生位置を1つランダムに生成する
	/// </summary>
	/// <param name="block">対象のブロック</param>
	public void CreateSpawnPointAtRandom(ChipType[,] block)
	{
		// 床の数をカウント.
		int candidate = 0; // スポーンポイントになりえる床の数.
		for (int bh = 1; bh < block.GetLength(0); ++bh)
		{
			for (int bw = 1; bw < block.GetLength(1); ++bw)
			{
				if (block[bh, bw] == ChipType.Floor)
					++candidate;
			}
		}
		
		// 床をランダム選択.
		double r = random.NextDouble();
		double total = 0.0;
		for (int bh = 1; bh < block.GetLength(0); ++bh)
		{
			for (int bw = 1; bw < block.GetLength(1); ++bw)
			{
				if (block[bh, bw] != ChipType.Floor)
					continue;
				total += 1.0/candidate;
				if (r < total)
				{
					block[bh, bw] = ChipType.Spawn;
					return;
				}
			}
		}
	}
	
	/// <summary>
	/// 指定位置が角か調べる.
	/// </summary>
	private bool IsCornner(ChipType[,] block, int h, int w)
	{
		// 角か調べる.
		if (block[h-1, w] == ChipType.Wall)
			if (block[h, w-1] == ChipType.Wall || block[h, w+1] == ChipType.Wall)
				return true;
		
		if (block[h+1, w] == ChipType.Wall)
			if (block[h, w-1] == ChipType.Wall || block[h, w+1] == ChipType.Wall)
				return true;
		
		return false;
	}
	
	/// <summary>
	/// ブロック内の角に敵の発生位置をランダムに生成する
	/// </summary>
	/// <param name="block">対象のブロック</param>
	public void CreateSpawnPointAtCornnerAtRandom(ChipType[,] block)
	{
		// 床の数をカウント.
		int candidate = 0; // スポーンポイントになりえ床の数.
		for (int bh = 1; bh < block.GetLength(0); ++bh)
		{
			for (int bw = 1; bw < block.GetLength(1); ++bw)
			{
				if (block[bh, bw] != ChipType.Floor || !IsCornner(block, bh, bw))
					continue;
				
				++candidate;
			}
		}
		
		if (candidate == 0)
			return;
		
		// 床をランダム選択.
		double r = random.NextDouble();
		double total = 0.0;
		for (int bh = 1; bh < block.GetLength(0); ++bh)
		{
			for (int bw = 1; bw < block.GetLength(1); ++bw)
			{
				if (block[bh, bw] != ChipType.Floor || !IsCornner(block, bh, bw))
					continue;
				total += 1.0/candidate;
				if (r < total)
				{
					block[bh, bw] = ChipType.Spawn;
					return;
				}
			}
		}
	}
	
	/// <summary>
	/// 指定したブロック間の距離()を求める.
	/// </summary>
	/// <param name="from_fh">ブロックの位置</param>
	/// <param name="from_fw">ブロックの位置</param>
	/// <param name="to_fh">ブロックの位置</param>
	/// <param name="to_fw">ブロックの位置</param>
	/// <returns>距離</returns>
	public int GetDistanceBetweenTwoBlock(ChipType[,][,] field, int from_fh, int from_fw, int to_fh, int to_fw)
	{
		// とりあえず迷路無視の距離を返す.
		// return Math.Abs(from_fh - to_fh) + Math.Abs(from_fw - to_fw);
		
		var h = field.GetLength(0);
		var w = field.GetLength(1);
		
		int[,] doorInfo = new int[h, w];
		for (int i = 0; i < h; ++i)
		{
			for (int j = 0; j < w; ++j)
			{
				var d = 0;
				var b = field[i, j];
				var bh = b.GetLength(0);
				var bw = b.GetLength(1);
				var bhc = (bh / 2) % 2 == 0 ? bh/2-1 : bh/2;
				var bwc = (bw / 2) % 2 == 0 ? bw/2+1 : bw/2;
				
				if (b[bhc, 0] == ChipType.Door)
					d |= Direction.Left;
				
				if (b[bhc, bw-1] == ChipType.Door)
					d |= Direction.Right;
				
				if (b[0, bwc] == ChipType.Door)
					d |= Direction.Top;
				
				if (b[bh-1, bwc] == ChipType.Door)
					d |= Direction.Bottom;
				
				doorInfo[i, j] = d;
			}
		}
		
		// DFS
		var distance = int.MaxValue;
		var closed = new bool[h, w];
		var open = new Queue<KeyValuePair<int, int>>();
		var d_buffer = new Queue<int>();
		open.Enqueue(new KeyValuePair<int,int>(from_fh, from_fw));
		d_buffer.Enqueue(0);
		
		Func<int, int, bool> RangeTest = (i, j) => {
			if (i < 0 || j < 0 || i >= h || j >= w)
			return false;
			return true;
		};
		
		while (open.Count != 0)
		{
			var pos = open.Dequeue();
			var i = pos.Key;
			var j = pos.Value;
			var d = d_buffer.Dequeue();
			
			if (closed[i, j])
				continue;
			
			if ((i == to_fh && j == to_fw) && distance > d)
			{
				distance = d;
				continue;
			}
			
			if (HasTargetDoor(doorInfo[i, j], Direction.Top) && RangeTest(i-1, j))
			{
				open.Enqueue(new KeyValuePair<int, int>(i-1, j));
				d_buffer.Enqueue(d+1);
			}
			
			if (HasTargetDoor(doorInfo[i, j], Direction.Bottom) && RangeTest(i+1, j))
			{
				open.Enqueue(new KeyValuePair<int, int>(i+1, j));
				d_buffer.Enqueue(d+1);
			}
			
			if (HasTargetDoor(doorInfo[i, j], Direction.Left) && RangeTest(i, j-1))
			{
				open.Enqueue(new KeyValuePair<int, int>(i, j-1));
				d_buffer.Enqueue(d+1);
			}
			
			if (HasTargetDoor(doorInfo[i, j], Direction.Right) && RangeTest(i, j+1))
			{
				open.Enqueue(new KeyValuePair<int, int>(i, j+1));
				d_buffer.Enqueue(d+1);
			}
			
			closed[i, j] = true;
		}
		
		return distance;
	}
	
	public void DebugPrint(ChipType[,][,] field)
	{
		for (int fh = 0; fh < this.fieldHeight; ++fh)
		{
			for (int bh = 0; bh < this.blockHeight; ++bh)
			{
				for (int fw = 0; fw < this.fieldWidth; ++fw)
				{
					for (int bw = 0; bw < this.blockWidth; ++bw)
					{
						var temp = field[fh, fw][bh, bw];
						switch (temp) 
						{
						case ChipType.Wall:
							Console.Write("@");
							break;
						case ChipType.Floor:
							Console.Write(" ");
							break;
						case ChipType.BottomKey:
							Console.Write("b");
							break;
						case ChipType.TopKey:
							Console.Write("t");
							break;
						case ChipType.RightKey:
							Console.Write("r");
							break;
						case ChipType.LeftKey:
							Console.Write("l");
							break;
						case ChipType.Door:
							Console.Write("d");
							break;
						case ChipType.Spawn:
							Console.Write("s");
							break;
						case ChipType.BossKey:
							Console.Write("B");
							break;
						default:
							throw new Exception("そんなブロックないよ");
						}
					}
				}
				Console.WriteLine(string.Empty);
			}
		}
	}
}
