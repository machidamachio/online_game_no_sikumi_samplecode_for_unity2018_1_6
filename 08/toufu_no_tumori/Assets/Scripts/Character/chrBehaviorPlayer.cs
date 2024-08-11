using UnityEngine;
using System.Collections;

public class chrBehaviorPlayer : chrBehaviorBase {

	// 縺翫?縺｣縺薙＠荳ｭ縺ｮ諠??ｱ.
	protected struct StepHouseMove {
		
		public chrBehaviorNPC_House	house;
	};
	protected StepHouseMove		step_house_move;

	// ================================================================ //

	// 縺翫?縺｣縺薙＠髢句ｧ?
	public virtual void		beginHouseMove(chrBehaviorNPC_House house)
	{
		this.step_house_move.house = house;
	}

	// 縺翫?縺｣縺薙＠邨ゆｺ??縺ｨ縺阪?蜃ｦ逅?
	public virtual void		endHouseMove()
	{
		// 螳ｶ繧定?蛻??蟄蝉ｾ帙?繧?↑縺上☆.
		this.step_house_move.house.transform.parent = null;

		this.controll.game_input.clear();

		// 閾ｪ蛻?ｒ陦ｨ遉ｺ縺吶ｋ.
		this.controll.setVisible(true);

		// 螳ｶ縺ｮ繝懊ャ繧ｯ繧ｹ繧ｳ繝ｩ繧､繝?繝ｼ繧呈怏蜉ｹ縺ｫ縺励※縲∫黄逅??蜍輔ｒ髢句ｧ?
		this.step_house_move.house.GetComponent<BoxCollider>().enabled = true;
		this.step_house_move.house.GetComponent<Rigidbody>().useGravity = true;
		this.step_house_move.house.GetComponent<Rigidbody>().WakeUp();

		// 閾ｪ蛻??繝懊ャ繧ｯ繧ｹ繧ｳ繝ｩ繧､繝?繝ｼ繧貞炎髯､縺励※縲√き繝励そ繝ｫ繧ｳ繝ｩ繧､繝?繝ｼ繧?
		// 蠕ｩ蟶ｰ縺吶ｋ.
		GameObject.DestroyImmediate(this.gameObject.GetComponent<BoxCollider>());
		this.gameObject.GetComponent<CapsuleCollider>().enabled = true;
	}

	// 縺翫?縺｣縺薙＠荳ｭ??
	public virtual bool		isNowHouseMoving()
	{
		return(false);
	}

	// 繝ｭ繝ｼ繧ｫ繝ｫ繝励Ξ繧､繝､繝ｼ??
	public virtual bool		isLocal()
	{
		return(true);
	}

	// ================================================================ //

	// 豁ｩ縺阪Δ繝ｼ繧ｷ繝ｧ繝ｳ繧貞?逕溘☆繧?
	public void		playWalkMotion()
	{
		// 豁ｩ縺阪Δ繝ｼ繧ｷ繝ｧ繝ｳ.
		this.controll.cmdSetMotion("Take 001", 0);
		
		Sound.ID[]	ids = {Sound.ID.TFT_SE02A, Sound.ID.TFT_SE02B};

		// 雜ｳ髻ｳ SE.
		SoundManager.get().playSEInterval(ids, 0.5f, this.get_walk_se_slot());
	}
	
	// 遶九■豁｢縺ｾ繧翫Δ繝ｼ繧ｷ繝ｧ繝ｳ繧貞?逕溘☆繧?
	public void		stopWalkMotion()
	{
		// 遶九■豁｢縺ｾ繧翫Δ繝ｼ繧ｷ繝ｧ繝ｳ.
		this.controll.cmdSetMotion("Take 002", 0);
		
		// 雜ｳ髻ｳ SE.
		SoundManager.get().stopSEInterval(this.get_walk_se_slot());
	}

	protected Sound.SLOT	get_walk_se_slot()
	{
		Sound.SLOT	slot = Sound.SLOT.SE_WALK0;

		if(this.isLocal()) {

			slot = Sound.SLOT.SE_WALK0;

		} else {

			slot = Sound.SLOT.SE_WALK1;
		}

		return(slot);
	}

	// ================================================================ //

	// 隱ｿ蛛懊?邨ゅｏ縺｣縺溘け繧ｨ繝ｪ繝ｼ縺ｮ螳溯｡?
	protected void		execute_queries()
	{
		foreach(QueryBase query in this.controll.queries) {
			
			if(!query.isDone()) {
				
				continue;
			}
			
			switch(query.getType()) {
				
				case "item.pick":
				{
					QueryItemPick	query_pick = query as QueryItemPick;
					
					if(query.isSuccess()) {
						
						// 繧｢繧､繝?Β繧偵ｂ縺｣縺ｦ縺?◆繧峨☆縺ｦ繧?
						if(this.controll.item_carrier.isCarrying()) {
						Debug.Log("Pick:" + query_pick.target + " Carry:" + this.controll.item_carrier.item.id);
							if (query_pick.target != this.controll.item_carrier.item.id) {
								// 逶ｸ謇九?繝ｬ繧､繝､繝ｼ縺ｫ繝峨Ο繝??縺励◆縺薙→繧堤衍繧峨○縺ｪ縺代ｌ縺ｰ縺?￠縺ｪ縺??縺ｧ縲√け繧ｨ繝ｪ繝ｼ縺ｯ菴懊ｋ.
								// 蜷梧悄縺ｮ蠢?ｦ√?縺ｪ縺??縺ｧ縲√ラ繝ｭ繝??縺ｯ縺吶＄縺ｫ螳溯｡後☆繧?
							Debug.Log ("behavior:cmdItemQueryDrop");

								QueryItemDrop		query_drop = this.controll.cmdItemQueryDrop();

								query_drop.is_drop_done = true;

								this.controll.cmdItemDrop(this.controll.account_name);
							}
						}
						
						this.controll.cmdItemPick(query_pick, this.controll.account_name, query_pick.target);

						if(!query_pick.is_anon) {

							SoundManager.get().playSE(Sound.ID.TFT_SE01);
						}
					}
					
					query.set_expired(true);		
				}
				break;
				
				case "item.drop":
				{
					if(query.isSuccess()) {

						if((query as QueryItemDrop).is_drop_done) {

							// 縺吶〒縺ｫ繝峨Ο繝??貂医∩.
							Debug.Log("[CLIENT CHAR] Item already dropped.");
						} else {
							Debug.Log("[CLIENT CHAR] Item dropped.");

							this.controll.cmdItemDrop(this.controll.account_name);
						}
					}
					
					query.set_expired(true);					
				}
				break;
				
				case "house-move.start":
				{
					do {
						
						if(!query.isSuccess()) {
							
							break;
						}
						
						QueryHouseMoveStart		query_start = query as QueryHouseMoveStart;
						
						chrBehaviorNPC_House	house = CharacterRoot.get().findCharacter<chrBehaviorNPC_House>(query_start.target);
						
						if(house == null) {
							
							break;
						}
						
						var		start_event = EventRoot.get().startEvent<HouseMoveStartEvent>();
						
						start_event.setPrincipal(this);
						start_event.setHouse(house);
						
					} while(false);
					
					query.set_expired(true);
				}
				break;
				
				case "house-move.end":
				{
					do {
						
						if(!query.isSuccess()) {
							
							break;
						}
						
						chrBehaviorNPC_House	house = this.step_house_move.house;
						
						var		end_event = EventRoot.get().startEvent<HouseMoveEndEvent>();
						
						end_event.setPrincipal(this);
						end_event.setHouse(house);
						
					} while(false);
					
					query.set_expired(true);
				}
				break;
				
				case "talk":
				{
					if(query.isSuccess()) {
						
						QueryTalk		query_talk = query as QueryTalk;
						
						this.controll.cmdDispBalloon(query_talk.words);
					}
					query.set_expired(true);
				}
				break;
			}
			
			break;
		}
	}

	// ================================================================ //

	// STEP.HOUSE_MOVE 縺ｮ蛻晄悄蛹?
	protected void	initialize_step_house_move_common()
	{
		// 閾ｪ蛻??繧ｫ繝励そ繝ｫ繧ｳ繝ｩ繧､繝?繝ｼ繧堤┌蜉ｹ縺ｫ縺励?∝ｮｶ縺ｮ繝懊ャ繧ｯ繧ｹ繧ｳ繝ｩ繧､繝?繝ｼ繧?
		// 遘ｻ讀阪☆繧?
		this.gameObject.GetComponent<CapsuleCollider>().enabled = false;
		this.gameObject.AddComponent<BoxCollider>();
		this.gameObject.GetComponent<BoxCollider>().size   = this.step_house_move.house.GetComponent<BoxCollider>().size;
		this.gameObject.GetComponent<BoxCollider>().center = this.step_house_move.house.GetComponent<BoxCollider>().center;
	
		// 螳ｶ縺ｮ繝懊ャ繧ｯ繧ｹ繧ｳ繝ｩ繧､繝?繝ｼ繧堤┌蜉ｹ縺ｫ縺励※縲∫黄逅??蜍輔ｂ縺阪ｋ.
		this.step_house_move.house.GetComponent<BoxCollider>().enabled = false;
		this.step_house_move.house.GetComponent<Rigidbody>().useGravity = false;
		this.step_house_move.house.GetComponent<Rigidbody>().velocity = Vector3.zero;
		this.step_house_move.house.GetComponent<Rigidbody>().Sleep();

		// 繧ｭ繝｣繝ｩ繧帝撼陦ｨ遉ｺ縺ｫ縺吶ｋ.
		// 螳ｶ縺檎ｧｻ蜍輔＠縺ｦ縺?ｋ繧医≧縺ｫ隕九∴繧?
		// ?亥ｮｶ繧貞ｭ蝉ｾ帙↓縺吶ｋ蜑阪↓繧?ｉ縺ｪ縺?→縲∝ｮｶ縺ｾ縺ｧ隕九∴縺ｪ縺上↑縺｣縺｡繧?≧??
		//
		this.controll.setVisible(false);

		// 螳ｶ繧定?蛻??蟄蝉ｾ帙↓縺吶ｋ.
		this.transform.position = this.step_house_move.house.transform.position;
		this.transform.rotation = this.step_house_move.house.transform.rotation;

		this.step_house_move.house.transform.parent = this.transform;
	}

	// STEP.HOUSE_MOVE 縺ｮ螳溯｡?
	protected void	execute_step_house_move_common()
	{
		this.step_house_move.house.GetComponent<Rigidbody>().velocity        = Vector3.zero;
		this.step_house_move.house.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
		this.step_house_move.house.transform.localPosition = Vector3.zero;
		this.step_house_move.house.transform.localRotation = Quaternion.identity;
			
		//this.exec_step_move();
	}
}
