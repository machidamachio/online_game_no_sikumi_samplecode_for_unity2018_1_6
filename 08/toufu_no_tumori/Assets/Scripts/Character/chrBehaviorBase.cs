using UnityEngine;
using System.Collections;

// ビヘイビアーの基底クラス.
public class chrBehaviorBase : MonoBehaviour {

	public chrController controll = null;

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

	// 毎フレーム LateUpdate() からよばれる.
	public virtual void	lateExecute()
	{
	}

	// 定型文を返す.
	// 定型文のふきだし（NPCのふきだし）を表示するときによばれる.
	public virtual string	getPresetText(int text_id)
	{
		return("");
	}

	// 他のキャラクターにタッチされた（そばでクリック）ときに呼ばれる.
	public virtual void		touchedBy(chrController toucher)
	{

	}

	// 外部からのコントロールを開始する.
	public virtual void 	beginOuterControll()
	{
	}

	// 外部からのコントロールを終了する.
	public virtual void		endOuterControll()
	{
	}
}
