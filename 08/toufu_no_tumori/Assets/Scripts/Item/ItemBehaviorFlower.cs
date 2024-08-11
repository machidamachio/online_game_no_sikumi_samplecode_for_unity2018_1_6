using UnityEngine;
using System.Collections;

// アイテムのビヘイビアー　花用.
public class ItemBehaviorFlower : ItemBehaviorBase {

	private GameObject		flower = null;		// 花　地面に生えてるとき.
	private GameObject		brooch = null;		// ブローチ　拾ったときs.

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
		this.flower = this.gameObject.transform.Find("Flower").gameObject;
		this.brooch = this.gameObject.transform.Find("Brooch").gameObject;

		this.flower.GetComponent<Renderer>().enabled = true;
		this.brooch.GetComponent<Renderer>().enabled = false;
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public override void	start()
	{
		this.controll.balloon.setColor(Color.red);
	}

	// 毎フレームよばれる.
	public override void	execute()
	{
	}

	// 拾われたときに呼ばれる.
	public override void	onPicked()
	{
		this.flower.GetComponent<Renderer>().enabled = false;
		this.brooch.GetComponent<Renderer>().enabled = true;
	}

	// リスポーンしたときに呼ばれる.
	public override void		onRespawn()
	{
		this.flower.GetComponent<Renderer>().enabled = true;
		this.brooch.GetComponent<Renderer>().enabled = false;
	}
}
