using UnityEngine;
using System.Collections;

// ひっこし開始イベントボックス.
public class EventBoxHouseMove : MonoBehaviour {

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	void	OnTriggerEnter(Collider other)
	{
		do {

			var		player = other.gameObject.GetComponent<chrBehaviorLocal>();

			if(player == null) {

				break;
			}

			if(!GlobalParam.get().is_in_my_home) {

				break;
			}

			player.onEnterHouseMoveEventBox();

		} while(false);
	}

	void	OnTriggerExit(Collider other)
	{
		do {

			var		player = other.gameObject.GetComponent<chrBehaviorLocal>();

			if(player == null) {

				break;
			}

			if(!GlobalParam.get().is_in_my_home) {

				break;
			}

			player.onLeaveHouseMoveEventBox();

		} while(false);
	}
}
