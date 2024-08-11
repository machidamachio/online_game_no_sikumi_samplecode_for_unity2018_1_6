using UnityEngine;
using System.Collections;

public class DebugRadar : MonoBehaviour {

	public GameObject	underlay = null;

	// ================================================================ //

	protected enum STEP {

		NONE = -1,

		OFF = 0,			// 非表示.
		ROOM,				// ルームレーダー.
		FLOOR,

		NUM,
	};
	protected Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		if(this.underlay != null) {

			this.underlay.SetActive(true);
		}

		this.step.set_next(STEP.OFF);
	}
	
	void	Update()
	{
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.OFF:
			{
				if(Input.GetKeyDown(KeyCode.M)) {

					this.step.set_next(STEP.ROOM);
				}
			}
			break;

			case STEP.ROOM:
			{
				if(Input.GetKeyDown(KeyCode.M)) {

					this.step.set_next(STEP.FLOOR);
				}
			}
			break;

			case STEP.FLOOR:
			{
				if(Input.GetKeyDown(KeyCode.M)) {

					this.step.set_next(STEP.OFF);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.OFF:
				{
				this.GetComponent<Camera>().enabled = false;
				}
				break;

				case STEP.ROOM:
				{
					this.GetComponent<Camera>().enabled = true;
					this.GetComponent<Camera>().orthographicSize = 25.0f;
					this.GetComponent<Camera>().rect = new Rect(0.6f, 0.1f, 0.3f, 0.4f);
				}
				break;

				case STEP.FLOOR:
				{
					this.GetComponent<Camera>().enabled = true;
					this.GetComponent<Camera>().orthographicSize = 25.0f*3.0f;
					this.GetComponent<Camera>().rect = new Rect(0.3f, 0.1f, 0.6f, 0.8f);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.ROOM:
			{
				RoomController	room = PartyControl.get().getCurrentRoom();
		
				Vector3	room_center = MapCreator.get().getRoomCenterPosition(room.getIndex());
		
				Vector3	camera_position = this.transform.position;
		
				camera_position.x = room_center.x;
				camera_position.z = room_center.z;
		
				this.transform.position = camera_position;
			}
			break;

			case STEP.FLOOR:
			{
				Vector3	room_center = MapCreator.get().getRoomCenterPosition(new Map.RoomIndex(1, 1));
		
				Vector3	camera_position = this.transform.position;
		
				camera_position.x = room_center.x;
				camera_position.z = room_center.z;
		
				this.transform.position = camera_position;
			}
			break;

		}

		// ---------------------------------------------------------------- //

	}
}
