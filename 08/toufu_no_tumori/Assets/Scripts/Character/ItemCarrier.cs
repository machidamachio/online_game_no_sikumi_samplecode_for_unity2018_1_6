using UnityEngine;
using System.Collections;

// 持ち運び中のアイテム.
public class ItemCarrier {

	public chrController	character;

	public	ItemController		item  = null;
	public	Vector3				pivot;

	public	float				omega;
	public	float				angle;
	public	float				spin_center;

	public	bool				is_landed;

	public ipModule.Jump		ip_jump = new ipModule.Jump();

	public const float	MIN_OMEGA = 360.0f;			// [degree/sec].
	public const float	ROTATE_RATE = 0.1f*60.0f;
	
	// ================================================================ //

	public ItemCarrier(chrController character)
	{
		this.character = character;
	}

	// 運び初め演出中？.
	public bool		isInAttachAction()
	{
		return(!this.is_landed);
	}

	// ================================================================ //

	public void		execute()
	{
		do {

			if(this.item == null) {

				break;
			}
			if(this.is_landed) {

				break;
			}

			ItemController	item = this.item;

			this.ip_jump.execute(Time.deltaTime);

			if(!this.ip_jump.isMoving()) {

				this.is_landed = true;
			}

			item.transform.position = this.character.transform.position + this.ip_jump.position;

			// 回転.

			this.angle += this.omega*Time.deltaTime;

			item.transform.rotation = Quaternion.identity;
			item.transform.Translate(this.spin_center*Vector3.up);
			item.transform.Rotate(this.pivot, this.angle);
			item.transform.Translate(-this.spin_center*Vector3.up);

			// 終了処理.

			if(this.is_landed) {

				item.gameObject.transform.parent      = this.character.gameObject.transform;
				item.gameObject.GetComponent<Rigidbody>().isKinematic = true;
				item.gameObject.GetComponent<Collider>().enabled      = true;
			}

		} while(false);

		if(this.is_landed && this.item != null) {

			var q =		Quaternion.FromToRotation(item.transform.forward, this.character.gameObject.transform.forward);

			float	angle;
			Vector3	axis;

			q.ToAngleAxis(out angle, out axis);

			float	min_omega = MIN_OMEGA*Time.deltaTime;

			if(angle <= min_omega) {

				item.transform.rotation = item.transform.rotation;

			} else {

			   	float	rotate_angle = angle*ROTATE_RATE*Time.deltaTime;

				rotate_angle = Mathf.Max(rotate_angle, min_omega);

				q = Quaternion.AngleAxis(rotate_angle, axis);

				item.transform.rotation = q*item.transform.rotation;
			}
		}
	}

	// ================================================================ //

	// 持ち運び開始.
	public void		beginCarry(ItemController item)
	{
		item.gameObject.GetComponent<Rigidbody>().isKinematic   = true;
		item.gameObject.GetComponent<Collider>().enabled        = false;

		Vector3		start = item.transform.position - this.character.transform.position;
		Vector3		goal  = new Vector3(0.0f, chrController.CARRIED_ITEM_HEIGHT, 0.0f);

		this.ip_jump.start(start, goal, chrController.CARRIED_ITEM_HEIGHT + 1.0f);

		this.item = item;

		this.pivot = Quaternion.AngleAxis(90.0f, Vector3.up)*this.ip_jump.xz_velocity();
		this.pivot.Normalize();
		this.omega = 360.0f/(this.ip_jump.t0 + this.ip_jump.t1);
		this.angle = 0.0f;

		this.spin_center = 0.0f;

		switch(this.item.name.ToLower()) {

			case "tarai":	this.spin_center = 0.0f;	break;
			case "negi":	this.spin_center = 0.5f;	break;
			case "yuzu":	this.spin_center = 0.25f;	break;
			case "wan":		this.spin_center = 0.5f;	break;
			case "cat":		this.spin_center = 0.5f;	break;
		}

		this.is_landed = false;
	}

	// 持ち運び開始（演出はキャンセル）.
	public void		beginCarryAnon(ItemController item)
	{
		this.beginCarry(item);

		this.item.gameObject.transform.parent      = this.character.gameObject.transform;
		this.item.gameObject.GetComponent<Rigidbody>().isKinematic = true;
		this.item.gameObject.GetComponent<Collider>().enabled      = true;

		this.item.transform.localPosition = this.ip_jump.goal;
		this.item.transform.rotation      = Quaternion.identity;

		this.is_landed = true;
	}

	// アイテム持ってる？.
	public bool		isCarrying()
	{
		return(this.item != null);
	}

	// 運び中のアイテムをゲットする.
	public ItemController	getItem()
	{
		return(this.item);
	}

	// 持ち運びおしまい.
	public void		endCarry()
	{
		this.item = null;
	}

}
