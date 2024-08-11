using UnityEngine;
using System.Collections;

// 開発/デバッグ用に人間がコントロールできるようにしたものです.
public class chrBehaviorEnemyBoss_Human : chrBehaviorEnemyBoss {
	public override	void	execute()
	{
		base.execute ();
		
		// スペースバーかJキーを押すとジャンプ攻撃を行う
		if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J))
		{
			EnemyRoot.getInstance().RequestBossRangeAttack(1.0f, 5.0f);
		}
		
		// Cキーを押すと攻撃を行う
		if (Input.GetKey(KeyCode.C))
		{
			// ダッシュで突撃
            EnemyRoot.getInstance().RequestBossDirectAttack(focus.getAcountID(), 3.0f);
		}

		// Qキーを押すとクイック攻撃を行う
		if(Input.GetKey(KeyCode.Q)) {

			// クイック攻撃
            EnemyRoot.getInstance().RequestBossQuickAttack(focus.getAcountID(), 1.0f);
		}
	}
	
	// [開発用] 最新のパーティ情報を取得してプレイヤーのリストを更新する。
	public override sealed void updateTargetPlayers()
	{
		initializeTargetPlayers();
	}
}
