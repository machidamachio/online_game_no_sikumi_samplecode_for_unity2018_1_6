using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameInput : MonoBehaviour {

	// クリックしたもの.
	public enum POINTEE {

		NONE = -1,

		TERRAIN = 0,		// 地形.
		ITEM,				// アイテム.
		CHARACTER,			// キャラクター.
		TEXT_FIELD,			// テキスト入力エリア.

		NUM,
	}

	public struct Pointing {

		public bool	trigger_on;
		public bool	current;
		public bool	clear_after;		// clear() 後　ボタンが離されるまで待ち中.

		public POINTEE	pointee;
		public string	pointee_name;

		public Vector3	position_3d;
	}

	public struct SerifText {

		public bool	trigger_on;
		public bool	current;

		public string	text;
	}

	public Pointing		pointing;
	public SerifText	serif_text;

	public Texture		white_texture;

	// ================================================================ //

	protected Rect	text_field_pos = new Rect(Screen.width - 120, 50, 100, 20);
	protected bool	mouse_lock = false;

	protected	List<Rect>	forbidden_area = new List<Rect>();		// 入力禁止エリア（クリック等を拾わない）.
	public bool		disp_forbidden_area = false;

	private string		editing_text = "";

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.pointing.trigger_on  = false;
		this.pointing.current     = false;
		this.pointing.clear_after = false;
		this.pointing.pointee     = POINTEE.NONE;

		this.serif_text.trigger_on = false;
		this.serif_text.current    = false;
		this.serif_text.text       = "";
	}
	
	void	Update()
	{
		bool	is_on_invalid_area = false;

		// ---------------------------------------------------------------- //

		if(this.pointing.clear_after) {

			// クリアー後、すぐに入力になってしまわないよう、ボタンが
			// 離されるまで待つ.
			if(!Input.GetMouseButton(0)) {

				this.pointing.clear_after = false;
			}

		} else if(!this.pointing.current) {

			if(Input.GetMouseButton(0)) {

				this.pointing.trigger_on = true;
				this.pointing.current    = true;
			}

		} else {

			this.pointing.trigger_on = false;

			if(!Input.GetMouseButton(0)) {

				this.pointing.current    = false;
			}
		}

		do {

			if(!this.pointing.current) {

				break;
			}

			Vector2		mouse_position_2d = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

			// デバッグウインドウがクリックされたとき.
			// （移動しないように）.
			//
			if(dbwin.root().isOcuppyRect(mouse_position_2d)) {
	
				is_on_invalid_area = true;
			}

			if(this.forbidden_area.Exists(x => x.Contains(mouse_position_2d))) {

				is_on_invalid_area = true;
			}

			if(this.pointing.trigger_on) {

				// テキスト入力エリアがクリックされたとき.
				// （移動しないように）.
				if(this.text_field_pos.Contains(mouse_position_2d)) {
	
					this.pointing.pointee = POINTEE.NONE;
					break;
	
				}

				// デバッグウインドウがクリックされたとき.
				// （移動しないように）.
				if(is_on_invalid_area) {
	
					this.pointing.pointee = POINTEE.NONE;
					break;
				}
			}

			// マウスカーソル位置にある Terrain の座標を求める
			Vector3		mouse_position = Input.mousePosition;
	
			// 画面の左右と下にはみ出したときの対策.
			mouse_position.x = Mathf.Clamp(mouse_position.x, 0.0f, Screen.width);
			mouse_position.y = Mathf.Clamp(mouse_position.y, 0.0f, Screen.height);
	
			Ray		ray = Camera.main.ScreenPointToRay(mouse_position);
	
			// レイヤーマスク.
	
			int		layer_mask = 0;
	
			layer_mask += 1 << LayerMask.NameToLayer("Terrain");
			layer_mask += 1 << LayerMask.NameToLayer("Clickable");
			layer_mask += 1 << LayerMask.NameToLayer("Player");
	
			RaycastHit 	hit;
	
			if(!Physics.Raycast(ray, out hit, float.PositiveInfinity, layer_mask)) {

				break;
			}

			this.pointing.position_3d = hit.point;

			string	layer_name = LayerMask.LayerToName(hit.transform.gameObject.layer).ToString();

			// 『ポインティングされたもの』は、クリックした瞬間だけ更新する.
			// ドラッグでアイテムが拾えないように.
			if(this.pointing.trigger_on) {

				switch(layer_name) {
	
					case "Player":
					{
						this.pointing.pointee      = POINTEE.CHARACTER;
						this.pointing.pointee_name = hit.transform.gameObject.name;
					}
					break;
	
					case "Terrain":
					{
						this.pointing.pointee = POINTEE.TERRAIN;
					}
					break;

					case "Clickable":
					case "Default":
					{
						switch(hit.transform.gameObject.tag) {

							case "Item":
							{
								this.pointing.pointee      = POINTEE.ITEM;
								this.pointing.pointee_name = hit.transform.gameObject.name;
							}
							break;

							case "Charactor":
							{
								this.pointing.pointee      = POINTEE.CHARACTER;
								this.pointing.pointee_name = hit.transform.gameObject.name;
							}
							break;
						}
					}
					break;
	

					default:
					{
						this.pointing.pointee = POINTEE.NONE;
					}
					break;

				} // switch(layer_name) {

			} else if(this.pointing.current) {

				switch(layer_name) {
	
					case "Player":
					{
						// 地面との交点にしておく.
						this.pointing.position_3d = ray.origin + ray.direction*Mathf.Abs(ray.origin.y/ray.direction.y);
					}
					break;
				}
			}

		} while(false);
	}

	void	OnGUI()
	{

		if(Event.current.type == EventType.Layout) {

			this.serif_text.trigger_on = false;
		}

		this.editing_text = GUI.TextArea(this.text_field_pos, this.editing_text);

		// リターンキーが押されたら確定.
		if(this.editing_text.EndsWith("\n")) {

			this.editing_text = this.editing_text.Remove(this.editing_text.Length - 1);

			this.serif_text.trigger_on = true;
			this.serif_text.text       = this.editing_text;
		}

		// 入力禁止エリアのデバッグ表示.

		if(this.disp_forbidden_area) {

			foreach(var area in this.forbidden_area) {
	
				GUI.color = new Color(1.0f, 0.5f, 0.5f, 0.4f);
				GUI.DrawTexture(area, this.white_texture);
			}
		}
	}

	// ================================================================ //

	// 入力をクリアーする（強制的衣何も入力されてないことにする）.
	public void		clear()
	{
		this.pointing.current      = false;
		this.pointing.clear_after  = true;
		this.pointing.pointee      = POINTEE.NONE;
		this.pointing.pointee_name = "";
	}

	// 入力禁止エリア（クリック等を拾わない）を追加する.
	public void		appendForbiddenArea(Rect area)
	{
		this.forbidden_area.Add(area);
	}

	// 入力禁止エリア（クリック等を拾わない）を削除する.
	public void		removeForbiddenArea(Rect area)
	{
		this.forbidden_area.Remove(area);
	}

}
