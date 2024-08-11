using UnityEngine;
using System.Collections;

public class EffectRoot : MonoBehaviour {

	public GameObject	itemGetEffect_prefab		= null;		// アイテムをひろったときのエフェクト.
	public GameObject	healEffect_prefab 			= null;		// 体力回復エフェクト.
	public GameObject	candleFireEffect_prefab		= null;		// ろうそくの火のエフェクト.
	public GameObject	doorMojiEffect_prefab		= null;		// ドア（ドーナッツ）の周りでまわる文字.
	public GameObject	bossFootSmoke_prefab		= null;		// ボスの足煙のエフェクト. 普通の足煙より大きかったりするかも.
	public GameObject	bossAngryMark_prefab		= null;		// ボスの怒りマーク.
	public GameObject	bossSnort_prefab			= null;		// ボスの鼻息のエフェクト.
	public GameObject	bossChargeAura_prefab		= null;		// ボスの突撃エフェクト.
	public GameObject	effectItePrefab				= null;		// 「いて」のエフェクト.
	public GameObject	effectHosiPrefab			= null;		// ☆のエフェクト.
	public GameObject	hitEffect_prefab			= null;		// ヒット（ダメージ受けた）エフェクト（「いて」と☆をまとめてつくる）.
	public GameObject	jinJinEffect_prefab			= null;		// アイスを食べすぎたときの頭じんじんエフェクト.
	public GameObject	atariOugi_prefab			= null;		// アイスが当たったときのおうぎ.
	public GameObject	yuzuExplode_prefab			= null;		// ゆずが着弾したときのエフェクト.
	public GameObject	effectSmokeMiddlePrefab		= null;		// ぼわん煙　中.
	public GameObject	effectSmokeSmallPrefab		= null;		// ぼわん煙　小.
	public GameObject	effectTearsPrefab			= null;		// 涙.

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	// アイテムを拾ったときのエフェクトを作る.
	public GameObject		createItemGetEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.itemGetEffect_prefab) as GameObject;

		effect.transform.position = position;
		effect.AddComponent<EffectSelfRelease>();

		return(effect);
	}

	// 体力回復エフェクトを作る.
	public GameObject		createHealEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.healEffect_prefab) as GameObject;

		effect.transform.position = position;
		effect.AddComponent<EffectSelfRelease>();

		return(effect);
	}

	// ろうそくの火エフェクトを作る.
	public GameObject		createCandleFireEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.candleFireEffect_prefab) as GameObject;

		effect.transform.position = position;

		return(effect);
	}

	// ドア（ドーナッツ）のまわりの文字のエフェクトを作る.
	public DoorMojiControl		createDoorMojisEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.doorMojiEffect_prefab) as GameObject;

		effect.transform.position = position;

		return(effect.GetComponent<DoorMojiControl>());
	}

	// ボス用の足煙のエフェクトを作る.
	public GameObject		createBossFootSmokeEffect(Vector3 position)
	{
		GameObject effect = null;

		if (bossFootSmoke_prefab != null)
		{
			effect = GameObject.Instantiate(this.bossFootSmoke_prefab) as GameObject;
			effect.transform.position = position;
		}

		return effect;
	}

	// ボスの怒りマークのエフェクトを作る.
	public GameObject		createAngryMarkEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.bossAngryMark_prefab) as GameObject;
		
		effect.transform.position = position;
		
		return(effect);
	}
	
	// ボスの鼻息のエフェクトを作る.
	public GameObject		createSnortEffect(Vector3 position, Quaternion rotation)
	{
		GameObject	effect = GameObject.Instantiate(this.bossSnort_prefab) as GameObject;
		
		effect.transform.position = position;
		effect.transform.rotation = rotation;
		
		return(effect);
	}

	// ボスの突撃オーラエフェクトを作って戻す. 座標は受け取った側で調整する.
	public GameObject		createChargeAura()
	{
		return GameObject.Instantiate(this.bossChargeAura_prefab) as GameObject;
	}
	
	// ---------------------------------------------------------------- //

	// 「いて」のエフェクトを作る.
	public GameObject		createHitIteEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.effectItePrefab) as GameObject;

		effect.transform.position = position;
		effect.AddComponent<EffectSelfRelease>();

		return(effect);
	}

	// ☆のエフェクトを作る.
	public GameObject		createHitHosiEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.effectHosiPrefab) as GameObject;

		effect.transform.position = position;
		effect.AddComponent<EffectSelfRelease>();

		return(effect);
	}

	// ヒット（ダメージ受けた）エフェクトを作る.
	public GameObject		createHitEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.hitEffect_prefab) as GameObject;

		effect.transform.position = position;

		return(effect);
	}

	// アイスを食べすぎたときの頭じんじんエフェクトを作る.
	public GameObject		createJinJinEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.jinJinEffect_prefab) as GameObject;

		effect.transform.position = position;

		return(effect);
	}

	// アイスが当たったときのおうぎを作る.
	public GameObject		createAtariOugi(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.atariOugi_prefab) as GameObject;

		effect.transform.position = position;

		return(effect);
	}

	// ゆずが着弾したときのエフェクトを作る.
	public GameObject		createYuzuExplode(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.yuzuExplode_prefab) as GameObject;

		effect.transform.position = position;
		effect.AddComponent<EffectSelfRelease>();

		return(effect);
	}

	// ぼわん煙（中サイズ）のエフェクトを作る.
	public GameObject		createSmokeMiddle(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.effectSmokeMiddlePrefab) as GameObject;

		effect.transform.position = position;
		effect.AddComponent<EffectSelfRelease>();

		return(effect);
	}

	// ぼわん煙（小サイズ）のエフェクトを作る.
	public GameObject		createSmokeSmall(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.effectSmokeSmallPrefab) as GameObject;

		effect.transform.position = position;
		effect.AddComponent<EffectSelfRelease>();

		return(effect);
	}

	// ゆずが着弾したときのエフェクトを作る.
	public GameObject		createTearsEffect(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.effectTearsPrefab) as GameObject;

		effect.transform.position = position;
		effect.transform.rotation = Quaternion.identity;

		return(effect);
	}

	// ================================================================ //
	// インスタンス.

	private	static EffectRoot	instance = null;

	public static EffectRoot	getInstance()
	{
		if(EffectRoot.instance == null) {

			EffectRoot.instance = GameObject.Find("EffectRoot").GetComponent<EffectRoot>();
		}

		return(EffectRoot.instance);
	}
	public static EffectRoot	get()
	{
		return(EffectRoot.getInstance());
	}
}
