using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollisionResult {

	public GameObject	object0 = null;
	public GameObject	object1 = null;
	public bool			is_trigger = false;
	public object		option0 = null;
}

public class CollisionManager : MonoBehaviour {

	public List<CollisionResult>	results = new List<CollisionResult>();

	protected bool	collision_updated = false;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		// 『確実にコリジョン計算が行われた』ことを検出するためのダミーコリジョン.
		//
		// ダミーコリジョンは CollisionManager と必ずコリジョンを発生する位置におくので、
		// 『コリジョンが発生した』＝『シーン内のコリジョン計算が一周した』とみなせる.

		GameObject	go = new GameObject("DummyCollision");

		go.gameObject.layer = this.gameObject.layer;

		go.AddComponent<SphereCollider>().isTrigger = true;
	}
	
	void	LateUpdate()
	{
	}

	void	FixedUpdate()
	{
		// OnCollisionStay が呼ばれる間隔は Update() の間隔より長い.
		// 確実にコリジョン計算が行われたタイミングで、結果をクリアーする.
		if(this.collision_updated) {

			this.clearResults();

			this.collision_updated = false;
		}
	}

	// トリガーにヒットしている間中よばれるメソッド.
	void	OnTriggerStay(Collider other)
	{
		this.collision_updated = true;
	}

	// ================================================================ //

	public void		clearResults()
	{
		this.results.Clear();
	}

	public void		removeResult(CollisionResult result)
	{
		this.results.Remove(result);
	}

	// ================================================================ //
	// インスタンス.

	private	static CollisionManager	instance = null;

	public static CollisionManager	getInstance()
	{
		if(CollisionManager.instance == null) {

			CollisionManager.instance = GameObject.Find("CollisionManager").GetComponent<CollisionManager>();
		}

		return(CollisionManager.instance);
	}
}
