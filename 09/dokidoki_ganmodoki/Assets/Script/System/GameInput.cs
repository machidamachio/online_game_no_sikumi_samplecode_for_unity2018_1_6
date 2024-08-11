using UnityEngine;
using System.Collections;

public class GameInput : MonoBehaviour {

	// クリックしたもの.
	public enum POINTEE {

		NONE = -1,

		TERRAIN = 0,		// 地形.
		ITEM,				// アイテム.
		CHARACTOR,			// キャラクター.
		TEXT_FIELD,			// テキスト入力エリア.

		NUM,
	}

	public struct Pointing {

		public bool	trigger_on;
		public bool	current;
		public bool	trigger_off;

		public POINTEE	pointee;
		public string	pointee_name;

		public Vector3	position_3d;
	}

	public struct Shot {

		public bool	trigger_on;
		public bool	current;

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
	public Shot			shot;
	public SerifText	serif_text;

	public bool		is_text_box_enable = true;		// セリフ入力用のテキストボックスを表示する？.

	// ================================================================ //

	protected bool	mouse_lock = false;

	protected Rect		text_field_pos = new Rect(20, 20, 100, 20);
	private string		editing_text = "";

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.pointing.trigger_on  = false;
		this.pointing.current     = false;
		this.pointing.trigger_off = false;
		this.pointing.pointee     = POINTEE.NONE;

		this.shot.trigger_on = false;
		this.shot.current    = false;
		this.shot.pointee    = POINTEE.NONE;

		this.serif_text.trigger_on = false;
		this.serif_text.current    = false;
		this.serif_text.text       = "";
	}
	
	void	Update()
	{
		bool	is_on_invalid_area = false;

		// ---------------------------------------------------------------- //

		// マウスの座標（GUIとの判定用）.
		//
		Vector2		mouse_position_gui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

		// デバッグウインドウがクリックされたとき.
		// （移動しないように）.
		//
		if(dbwin.root().isOcuppyRect(mouse_position_gui)) {

			is_on_invalid_area = true;
		}

		Vector3		mouse_position = Input.mousePosition;

		// 画面の左右と下にはみ出したときの対策.
		mouse_position.x = Mathf.Clamp(mouse_position.x, 0.0f, Screen.width);
		mouse_position.y = Mathf.Clamp(mouse_position.y, 0.0f, Screen.height);

		Ray		ray = Camera.main.ScreenPointToRay(mouse_position);

		// ---------------------------------------------------------------- //

		this.pointing.trigger_on  = Input.GetMouseButtonDown(0);
		this.pointing.current     = Input.GetMouseButton(0);
		this.pointing.trigger_off = Input.GetMouseButtonUp(0);

		this.shot.trigger_on = Input.GetMouseButtonDown(1);
		this.shot.current    = Input.GetMouseButton(1);

		// ---------------------------------------------------------------- //

		do {

			if(!this.pointing.current) {

				break;
			}

			if(this.pointing.trigger_on) {

				// テキスト入力エリアがクリックされたとき.
				// （移動しないように）.
				if(this.text_field_pos.Contains(mouse_position_gui)) {
	
					this.pointing.pointee = POINTEE.NONE;
					break;
	
				}

				if(ItemWindow.get().isPositionInWindow(mouse_position_gui)) {

					ItemWindow.get().clickWindow(mouse_position_gui);
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
			// レイヤーマスク.
	
			int		layer_mask = 0;
	
			layer_mask += 1 << LayerMask.NameToLayer("Terrain");
			layer_mask += 1 << LayerMask.NameToLayer("Default");
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
	
					case "Terrain":
					{
						this.pointing.pointee = POINTEE.TERRAIN;
					}
					break;
	
					case "Default":
					case "Player":
					{
						switch(hit.transform.gameObject.tag) {

							case "Item":
							{
								this.pointing.pointee      = POINTEE.ITEM;
								this.pointing.pointee_name = hit.transform.gameObject.name;
							}
							break;

							case "Player":
							case "Chatactor":
							{
								this.pointing.pointee      = POINTEE.CHARACTOR;
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
				}
			}

		} while(false);

		// ---------------------------------------------------------------- //
		// ショット　右クリック.

		do {

			if(!this.shot.current) {

				break;
			}

			if(this.shot.trigger_on) {

				// デバッグウインドウがクリックされたとき.
				// （移動しないように）.
				if(is_on_invalid_area) {
	
					this.shot.pointee = POINTEE.NONE;
					break;
				}
			}

			//

			chrController	player = CharacterRoot.getInstance().getPlayer();

			Plane	plane = new Plane(Vector3.up, player.transform.position);
	
			float	depth;
	
			if(plane.Raycast(ray, out depth)) {
	
				Vector3		xp = ray.origin + ray.direction*depth;
	
				if(this.shot.trigger_on) {

					this.shot.pointee = POINTEE.TERRAIN;
				}

				this.shot.position_3d = xp;
			}

		} while(false);
	}

	void	OnGUI()
	{
		if(this.is_text_box_enable) {

			if(Event.current.type == EventType.Layout) {
	
				this.serif_text.trigger_on = false;
			}
	
			this.editing_text = GUI.TextArea(this.text_field_pos, this.editing_text);
	
			// リターンキーが押されたら確定.
			if(this.editing_text.EndsWith("\n")) {
	
				this.editing_text = this.editing_text.Remove(this.editing_text.Length - 1);
	
				this.serif_text.trigger_on = true;
				this.serif_text.text       = this.editing_text;
				this.editing_text		   = "";
			}
		}
	}

	// ================================================================ //
	// インスタンス.

	private	static GameInput	instance = null;

	public static GameInput	getInstance()
	{
		if(GameInput.instance == null) {

			GameInput.instance = GameObject.Find("GameRoot").GetComponent<GameInput>();
		}

		return(GameInput.instance);
	}
}
