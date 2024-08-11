using UnityEngine;
using System.Collections;

// ビヘイビアーの基底クラス.
public class chrBehaviorBase : MonoBehaviour {

	public chrController control = null;

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
	public virtual void	initialize()
	{
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public virtual void	start()
	{
	}

	// 毎フレームよばれる.
	public virtual void	execute()
	{
	}

	// 定型文を返す
	// 定型文のふきだし（NPCのふきだし）を表示するときによばれる.
	public virtual string	getPresetText(int text_id)
	{
		return("");
	}

	// 他のキャラクターにタッチされた（そばでクリック）ときに呼ばれる.
	public virtual void		touchedBy(chrController toucher)
	{

	}

	// 近接攻撃がヒットしたときに呼ばれる.
	public virtual void		onMeleeAttackHitted(chrBehaviorBase other)
	{
	}

	// ダメージを受けたときに呼ばれる.
	public virtual void		onDamaged()
	{
	}

	// やられたときに呼ばれる.
	public virtual void		onVanished()
	{
	}

	// 削除する直前に呼ばれる.
	public virtual void		onDelete()
	{
	}

	// ================================================================ //

	// GameObject にくっついているビヘイビアーをゲットする.
	public static T getBehaviorFromGameObject<T>(GameObject go) where T : chrBehaviorBase
	{
		T	behavior = null;

		do {

			chrController	control = go.GetComponent<chrController>();

			if(control == null) {

				continue;
			}

			behavior = control.behavior as T;

			if(behavior == null) {

				continue;
			}

		} while(false);

		return(behavior);
	}
}
