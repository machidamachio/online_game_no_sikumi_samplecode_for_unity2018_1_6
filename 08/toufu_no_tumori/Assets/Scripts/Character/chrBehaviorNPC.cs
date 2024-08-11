using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ビヘイビアー　NPC用.
public class chrBehaviorNPC : chrBehaviorBase {

	private		List<string>	preset_texts = null;
	private		bool			is_texts_editable = false;	// preset_texts を編集できる？.

	protected float			timer = 0.0f;

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
	public override sealed void	initialize()
	{
		this.preset_texts = new List<string>();

		this.is_texts_editable = true;
		this.initialize_npc();
		this.is_texts_editable = false;
		this.controll.setPlayer(false);
	}

	// 生成直後に呼ばれる NPC 用.
	public virtual void	initialize_npc()
	{
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public override void	start()
	{
		this.controll.balloon.setColor(Color.red);
	}

	// 毎フレームよばれる.
	public override	void	execute()
	{
		this.timer += Time.deltaTime;

		int		text_id = (int)Mathf.Repeat(this.timer, (float)this.preset_texts.Count);

		this.controll.cmdDispBalloon(text_id);
	}

	// 定型文を返す.
	// 定型文のふきだし（NPCのふきだし）を表示するときによばれる.
	public override sealed string	getPresetText(int text_id)
	{
		string	text = "";

		if(0 <= text_id && text_id < this.preset_texts.Count) {

			text = this.preset_texts[text_id];
		}

		return(text);
	}

	// ================================================================ //
	// 継承先のクラス用

	protected void	addPresetText(string text)
	{
		if(this.is_texts_editable) {

			this.preset_texts.Add(text);

		} else {

			// initialize() メソッド以外ではテキストの追加はできない.
			Debug.LogError("addPresetText() can use only in initialize_npc().");
		}
	}
}