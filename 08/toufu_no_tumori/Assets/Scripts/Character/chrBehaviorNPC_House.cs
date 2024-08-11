using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ビヘイビアー　NPC House 用.
public class chrBehaviorNPC_House : chrBehaviorNPC {

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	// 生成直後に呼ばれる NPC 用.
	public override void	initialize_npc()
	{
      	GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

		this.addPresetText("Ｗｅｌｌｃｏｍｅ！");
		this.addPresetText("ひっこし中");
		this.addPresetText("いってらっしゃ～い");
		this.addPresetText("ばいば～い"); 
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public override void	start()
	{
	}

	// 毎フレームよばれる.
	public override	void	execute()
	{
		GameObject go = GameObject.Find("Network");
		if (go != null && 
		    go.GetComponent<Network>().IsConnected() == false) {
			// 切断時に家にかえったときは吹き出しを変更.
			this.controll.cmdDispBalloon(3);
		}
	}

	// 玄関の戸をあける.
	public void		openDoor()
	{
		this.controll.cmdSetMotion("Open", 0);
	}

	// 玄関の戸をしめる.
	public void		closeDoor()
	{
		this.controll.cmdSetMotionRewind("Open", 0);
	}

	// おひっこしはじめ.
	public void		startHouseMove()
	{
		GameObject go = GameObject.Find("Network");
		if (go != null && 
		    go.GetComponent<Network>().IsConnected() == false) {
			// 切断時に家にかえったときは吹き出しを変更.
			this.controll.cmdDispBalloon(3);
			return;
		}

		this.controll.cmdDispBalloon(1);
	}

	// おひっこしおしまい.
	public void		endHouseMove()
	{
		this.controll.cmdDispBalloon(2);
	}
}