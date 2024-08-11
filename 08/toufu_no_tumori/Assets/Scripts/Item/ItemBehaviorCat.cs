using UnityEngine;
using System.Collections;

// アイテムのビヘイビアー　おばさんねこ用.
public class ItemBehaviorCat : ItemBehaviorBase {

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	// 生成直後に呼ばれる.
	public override void	initialize_item()
	{
		this.item_favor.is_enable_house_move = true;
	}
}
