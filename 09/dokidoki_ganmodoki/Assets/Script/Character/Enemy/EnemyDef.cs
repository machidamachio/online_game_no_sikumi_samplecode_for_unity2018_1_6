using UnityEngine;
using System.Collections;

namespace Enemy {

	public enum BEHAVE_KIND {

		NONE = -1,

		BOTTACHI = 0,		// 立ってるだけ。デバッグ用.
		OUFUKU,				// ２か所を往復する.
		UROURO,				// 止まる→歩く.
		TOTUGEKI,			// プレイヤーに近寄って近接攻撃.
		SONOBA_DE_FIRE,		// その場でショット.
		WARP_DE_FIRE,		// ワープを繰り返す.
		JUMBO,				// ジャンボ.
		GOROGORO,			// ごろごろ転がって、壁で反射.

		NUM,
	}

} // namespace Map

public class EnemyDef : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
