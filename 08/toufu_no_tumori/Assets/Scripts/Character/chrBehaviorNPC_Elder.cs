using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ビヘイビアー　NPC Elder 用.
public class chrBehaviorNPC_Elder : chrBehaviorNPC {

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
		this.addPresetText("わしが村長じゃ");
		this.addPresetText("そんちょう（そんじょ）そこらの");
		this.addPresetText("村長じゃないぞっ");
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public override void	start()
	{
	}
}