using UnityEngine;
using System.Collections;

// ジャンケンの出す手種.
public enum RPSKind {
    None = -1,		// 未決定.
    Rock = 0,		// グー.
    Paper,			// パー.
    Scissor,		// チョキ.
};

// 攻撃/防御選択.
public enum ActionKind {
    None = 0,		// 未決定.
    Attack,			// 攻撃.
    Block,			// 防御.
};

// 攻撃/防御の情報構造体.
public struct AttackInfo {
    public ActionKind actionKind;
    public float actionTime;        //経過時間.

    public AttackInfo(ActionKind kind, float time) {
        actionKind = kind;
        actionTime = time;
    }
};



public struct InputData{
    public RPSKind rpsKind;         //ジャンケン選択.
    public AttackInfo attackInfo;   //攻防情報.
}





// 勝者の識別.
public enum Winner {
    None = 0,		// 未決定.
    ServerPlayer,	// サーバー側(1P)の勝ち.
    ClientPlayer,	// クライアント側(2P)の勝ち.
    Draw,			// 引き分け.
};


class ResultChecker {
    //ジャンケンの勝敗を求める.
    public static Winner GetRPSWinner(RPSKind server, RPSKind client) {
        // 1Pと2Pの手を数値化します.
        int serverRPS = (int)server;
        int clientRPS = (int)client;

        if (serverRPS == clientRPS) {
            return Winner.Draw; //引き分け.
        }

        // 数値の差分を用いて処理判定を行います.
        if (serverRPS == (clientRPS + 1) % 3) {
            return Winner.ServerPlayer;  //1Pの勝ち.
        }
        return Winner.ClientPlayer; //2Pの勝ち.
    }

    
    //ジャンケンの結果と、攻撃/防御から勝敗を求める
    public static Winner GetActionWinner(AttackInfo server, AttackInfo client, Winner rpsWinner) {
        string debugStr = "rpsWinner:" + rpsWinner.ToString();
        debugStr += "    server.actionKind:" + server.actionKind.ToString() + " time:" + server.actionTime.ToString();
        debugStr += "    client.actionKind:" + client.actionKind.ToString() + " time:" + client.actionTime.ToString();
        Debug.Log(debugStr);


        ActionKind serverAction = server.actionKind;
        ActionKind clientAction = client.actionKind;

        // 攻撃/防御が正しく行われたかを判定します.
        switch (rpsWinner) {
        case Winner.ServerPlayer:
            if (serverAction != ActionKind.Attack) {
                // 1Pが攻撃をしなかったので引き分け.
                return Winner.Draw;
            }
            else if (clientAction != ActionKind.Block) {
                // 2Pが間違えたので1Pの勝ちです.
                return Winner.ServerPlayer;
            }
            // 決着は時間になります.
            break;

        case Winner.ClientPlayer:
            if (clientAction != ActionKind.Attack) {
                // 2Pが攻撃をしなかったので引き分け.
                return Winner.Draw;
            }
            else if (serverAction != ActionKind.Block) {
                // 1Pが間違えたので2Pの勝ちです.
                return Winner.ClientPlayer;
            }
            // 決着は時間になります.
            break;

        case Winner.Draw:
            //引き分けのときは何をしても引き分けにしかならない.
            return Winner.Draw;
        }

        
        // 時間対決.
        float serverTime = server.actionTime;
        float clientTime = client.actionTime;

        if (serverAction == ActionKind.Attack) {
            // 1Pが攻撃の場合は2Pよりも早いときに勝ちになります.
            if (serverTime < clientTime) {
                // 1Pの方が早いので勝ちです.
                return Winner.ServerPlayer;
            }
        }
        else {
            // 2Pが攻撃の場合は2Pよりも早く防御しないと負けです.
            if (serverTime > clientTime) {
                return Winner.ClientPlayer;
            }
        }

        // 同じ時間なので引き分けです.
        return Winner.Draw;
    }



    //テストコード.
    static void Assert(bool condition) {
        if (!condition) {
            throw new System.Exception();
        }
    }
    public static void WinnerTest(){
        
        Assert(GetRPSWinner(RPSKind.Paper, RPSKind.Paper) == Winner.Draw);
        Assert(GetRPSWinner(RPSKind.Paper, RPSKind.Rock) == Winner.ServerPlayer);
        Assert(GetRPSWinner(RPSKind.Paper, RPSKind.Scissor) == Winner.ClientPlayer);
        Assert(GetRPSWinner(RPSKind.Rock, RPSKind.Paper) == Winner.ClientPlayer);
        Assert(GetRPSWinner(RPSKind.Rock, RPSKind.Rock) == Winner.Draw);
        Assert(GetRPSWinner(RPSKind.Rock, RPSKind.Scissor) == Winner.ServerPlayer);
        Assert(GetRPSWinner(RPSKind.Scissor, RPSKind.Paper) == Winner.ServerPlayer);
        Assert(GetRPSWinner(RPSKind.Scissor, RPSKind.Rock) == Winner.ClientPlayer);
        Assert(GetRPSWinner(RPSKind.Scissor, RPSKind.Scissor) == Winner.Draw);

        AttackInfo s;
        s.actionKind = ActionKind.Attack;
        s.actionTime = 1.0f;
        //時間：同じ、早い、遅い、を試す.
        //win & attack
        s.actionKind = ActionKind.Attack;
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 1), Winner.ServerPlayer) == Winner.ServerPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 1), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 1), Winner.ServerPlayer) == Winner.ServerPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 2), Winner.ServerPlayer) == Winner.ServerPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 2), Winner.ServerPlayer) == Winner.ServerPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 2), Winner.ServerPlayer) == Winner.ServerPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 0), Winner.ServerPlayer) == Winner.ServerPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 0), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 0), Winner.ServerPlayer) == Winner.ServerPlayer);
        //win & block
        s.actionKind = ActionKind.Block;
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 1), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 1), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 1), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 2), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 2), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 2), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 0), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 0), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 0), Winner.ServerPlayer) == Winner.Draw);
        //win & none
        s.actionKind = ActionKind.None;
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 1), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 1), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 1), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 2), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 2), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 2), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 0), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 0), Winner.ServerPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 0), Winner.ServerPlayer) == Winner.Draw);

        //lose & attack
        s.actionKind = ActionKind.Attack;
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 1), Winner.ClientPlayer) == Winner.ClientPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 1), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 1), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 2), Winner.ClientPlayer) == Winner.ClientPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 2), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 2), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 0), Winner.ClientPlayer) == Winner.ClientPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 0), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 0), Winner.ClientPlayer) == Winner.Draw);
        //lose & block
        s.actionKind = ActionKind.Block;
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 1), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 1), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 1), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 2), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 2), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 2), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 0), Winner.ClientPlayer) == Winner.ClientPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 0), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 0), Winner.ClientPlayer) == Winner.Draw);
        //lose & none
        s.actionKind = ActionKind.None;
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 1), Winner.ClientPlayer) == Winner.ClientPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 1), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 1), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 2), Winner.ClientPlayer) == Winner.ClientPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 2), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 2), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 0), Winner.ClientPlayer) == Winner.ClientPlayer);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 0), Winner.ClientPlayer) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 0), Winner.ClientPlayer) == Winner.Draw);

        //draw & attack
        s.actionKind = ActionKind.Attack;
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 1), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 1), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 1), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 2), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 2), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 2), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 0), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 0), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 0), Winner.Draw) == Winner.Draw);
        //draw & block
        s.actionKind = ActionKind.Block;
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 1), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 1), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 1), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 2), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 2), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 2), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 0), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 0), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 0), Winner.Draw) == Winner.Draw);
        //draw & none
        s.actionKind = ActionKind.None;
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 1), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 1), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 1), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 2), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 2), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 2), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Attack, 0), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.Block, 0), Winner.Draw) == Winner.Draw);
        Assert(GetActionWinner(s, new AttackInfo(ActionKind.None, 0), Winner.Draw) == Winner.Draw);
    }

}
