using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ビヘイビアーの基底クラス.
public class ItemBehaviorBase : MonoBehaviour {

	public ItemController controll = null;

	public ItemFavor	item_favor = null;						// 特典　アイテムを持っているキャラクターにつく、特殊効果.

	private		List<string>	preset_texts = null;			// プリセットなセリフ.
	private		bool			is_texts_editable = false;		// preset_texts を編集できる？.

	public bool			is_active = true;						// アクティブ/非アクティブ設定

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
	public void	initialize()
	{
		this.item_favor   = new ItemFavor();
		this.preset_texts = new List<string>();

		this.is_texts_editable = true;
		this.initialize_item();
		this.is_texts_editable = false;

		// タライは影をちょっとずらす.
		// （隠れちゃうので）.
		do {

			if(this.gameObject.name != "Tarai") {

				break;
			}

			var	shadow = this.gameObject.GetComponentInChildren<Projector>();

			if(shadow == null) {

				break;
			}
	
			shadow.transform.localPosition += new Vector3(0.1f, 0.0f, -0.1f);

		} while(false);
	}

	// 定型文を返す
	// 定型文のふきだし（NPCのふきだし）を表示するときによばれる.
	public string	getPresetText(int text_id)
	{
		string	text = "";

		if(0 <= text_id && text_id < this.preset_texts.Count) {

			text = this.preset_texts[text_id];
		}

		return(text);
	}

	// ================================================================ //

	// 生成直後に呼ばれる 派生クラス用.
	public virtual void	initialize_item()
	{
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public virtual void	start()
	{
	}

	// 毎フレームよばれる.
	public virtual void	execute()
	{
	}

	// 拾われたときに呼ばれる.
	public virtual void		onPicked()
	{
	}

	// リスポーンしたときに呼ばれる.
	public virtual void		onRespawn()
	{
	}

	// アイテムを成長状態にする（拾えるようにする）.
	public virtual void		finishGrowing()
	{

	}

	// アイテムのアクティブ/非アクティブ設定.
	public virtual void		activeItem(bool active)
	{
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
