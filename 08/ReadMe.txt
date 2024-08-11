
■「オンラインゲームのしくみ」サンプルコード

●フォルダ構成
本サンプルのフォルダ構成は下記のようになっています。
本書の章に合わせてフォルダを参照してください。
					
▼フォルダ構成					▼フォルダ説明
----------------------------------------------------------------------------------------------------------
+08						●第8章サンプルプログラム
 \---toufu_no_tumori				「とうふのつもり」サンプルプログラム
    +---Assets
     |   +---Scenes				　・シーンファイル
     |   +---Scripts				　・サンプルスクリプトコード
     +---bin					　・「とうふのつもり」サンプル実行ファイル
         \---toufu_no_tumori_Data


■「とうふのつもり」サンプルプログラム

●実行ファイル
08\toufu_no_tumori\bin\toufu_no_tumori.exe

●遊び方
タイトル画面で、「おともだちのアドレス」のテキストフィールドに一緒にあそぶおともだちの
お互いのIPアドレスを入力してください。

タイトル画面で「おともだちのアドレス」のテキストフィールドに一緒に遊ぶおともだちのお互い
のIPアドレスを入力します。
先にお庭に行っておともだちを待つ場合は「おともだちを待つ」ボタンを押してゲームを開始します。
先に遊んでいるおともだちと遊ぶ場合は「遊びに行く」ボタンを押します。


おともだちを待つプレイヤーは「とうふやさん」で遊びます。
おともだちのところへ遊びに行くプレイヤーは「だいずやさん」で遊びます。
画面上をマウスの左ボタンをクリックまたはドラッグするとキャラクターがマウスポインタの位置に向かって移動します。
地面に置いてあるアイテムは拾うことができます。アイテムをダブルクリックすると取得できます。
アイテムを取得した状態で他のアイテムを取得すると、所有していたアイテムは破棄され、新しいアイテムを取得します。
アイテムはおともだちの庭へ運んで行くことができます。おともだちにあげることもできます。
ただし、元の庭にない植物は枯れてしまいますので誰かが元の庭に戻してあげなければなりません。
ネコを取得した状態で家をクリックするとキャラクターの移動が引越しモードに変わります。
もう一度家をクリックすると引越しが終了します。
画面右上のテキストフィールドにメッセージを入力するとおともだちにメッセージを送ることができます。


●サンプルプログラム
プロジェクトファイル：08\toufu_no_tumori\Assets\Scenes\TitleScene.unity
プログラム：08\toufu_no_tumori\Assets\Script


●通信が関係するファイルの構成
Character			キャラクター制御スクリプト群
    CharacterRoot.cs		キャラクターマネージャ
    chrBehaviorBase.cs		キャラクタービヘイビアの基底クラス
    chrBehaviorLocal.cs		ローカルキャラクターのビヘイビア
    chrBehaviorNet.cs		リモートキャラクターのビヘイビア
    chrBehaviorNPC_House.cs	家のビヘイビア
    chrBehaviorPlayer.cs	キャラクター(とうふやさん、だいずやさん)の共通ビヘイビア
    chrController.cs		キャラクターコントローラ
    ItemCarrier.cs		アイテム持ち運びの制御

Event				イベント制御スクリプト群
    EventBoxLeave.cs		庭移動時の制御(ローカルキャラクターが移動するとき)
    EventEnter.cs		庭移動時の制御(リモートキャラクターが遊びに来るとき)
    EventLeave.cs		庭移動時のアイテム持ち運び制御

Item				アイテム制御スクリプト群
    ItemBehaviorBase.cs		アイテムビヘイビアの基底クラス
    ItemBehaviorFruit.cs	ネギ、ゆずのビヘイビア
    ItemController.cs		アイテムコントローラ
    ItemManager.cs		アイテムマネージャ


Network				通信制御スクリプト群
    GameServer.cs		ゲームサーバ
    IPacket.cs			パケットインターフェース
    Network.cs			通信モジュール(TransportTCP,TransportUDPクラスの制御)
    NetworkDef.cs		通信に関する定義
    Packet.cs			パケットクラス定義
    PacketQueue.cs		パケットキュークラス
    PacketSerializer.cs		パケットシリアライザ(パケットヘッダのシリアライザ)
    PacketStructs.cs		パケットデータ定義
    Serializer.cs		シリアライザ基底クラス
    SplineData.cs		キャラクター移動用スプライン補間クラス
    TransportTcp.cs		TCPのソケット通信プログラム
    TransportUDP.cs		UDPのソケット通信プログラム

System				ゲームシステム制御スクリプト群
    EventRoot.cs		イベントマネージャ
    GameRoot.cs			ゲーム制御
    GlobalParam.cs		シーンをまたぐ情報を管理
    MapCreator.cs		マップ生成制御
    QueryManager.cs		クエリ制御
    TitleControl.cs		ゲームサーバ、ホスト-ゲスト間接続シーケンス制御


※Assets\Script\Networkフォルダ内の通信処理を行うプログラム内にプログラムの動作に関するコメントが記述してあります。
　プログラム内のコメントも参照してください。


●通信プログラム補足
▼キャラクターの移動
　・ローカルキャラクターの座標送信：chrBehaviorLocal.execute
  　ローカルキャラクターの座標は10フレーム間隔でバッファ m_culling に貯められ、過去4点分の情報を
　　CharacterRoot.SendCharacterCoord に渡されます。(SendCharacterCoord の処理は本書を参照)

　・リモートキャラクターの座標受信：chrBehaviorNet.CalcCoordinates
　　リモートキャラクターの座標は通信モジュールで受信したデータをCharacterRoot.OnReceiveCharacterPacket
　　で処理し、CalcCoordinates に渡されます。


▼アイテムの取得・破棄
　・ゲームサーバ
　　アイテムの取得調停：GameServer.MediatePickupItem
　　　アイテムの取得問い合わせは GameServer.OnReceiveItemPacket で受信を行い、MediatePickupItem が
　　　呼び出されます。(MediatePickupItem の処理は本書を参照)
　　
　　アイテムの破棄調停：GameServer.MediateDropItem
　　　アイテムの取得問い合わせは GameServer.OnReceiveItemPacket で受信を行い、MediateDropItem が
　　　呼び出されます。MediateDropItem の処理は本書を参照)
	
　・アイテム取得の問い合わせ：ItemManager.queryPickItem
　　　アイテムがクリックされたときに、chrBehaviorLocal.exec_step_move で chrController.cmdItemQueryPick
　　　から ItemManager.queryPickItem へクエリを発行してサーバへの問い合わせを行います。
　　　(各処理は本書を参照)

　・アイテム破棄の問い合わせ：ItemManager.queryDropItem
　　　アイテムを所有しているときに別のアイテムを取得した場合、chrBehaviorPlayer.execute_queries で
　　　chrController.cmdItemQueryDrop から ItemManager.queryDropItem クエリを発行してサーバへの問い合わせを行います。

　・持ち運べないアイテム：MapCreator.loadLevel
　　　アイテムは別のお庭へ持ち運びができるものとできないものがあります。
　　　とうふやさんのお庭にはネギが生え、だいずやさんのお庭にはゆずが生えます。それぞれのお庭にはネギかゆずしか
　　　持ち運べません。このため、生成するアイテムの active 状態の設定を MapCreator.loadLevel で行います。
　　

▼引越し
　・引越し開始/終了送信：chrBehaviorLocal.execute
　　　家がクリックされると chrBehaviorLocal.execute で chrController.cmdQueryHouseMoveStart でクエリが発行
　　　されます。このクエリにより、CharacterRoot.queryHouseMoveStart でゲームサーバのリフレクターに送信されます。

　・引越し開始/終了受信：CharacterRoot.OnReceiveMovingPacket
　　　ゲームサーバのリフレクターにより、リモート端末は情報を受信すると CharacterRoot.OnReceiveMovingPacket で引越し
　　　の処理が行われます。(本書を参照)


▼チャット
　・チャットメッセージ送信：chrBehaviorLocal.execute
　　　チャットメッセージのテキストフィールドに入力が行われると、chrBehaviorLocal.execute で chrController.cmdQueryTalk
　　　が呼び出されます。この関数から CharacterRoot.queryTalk でメッセージが送信されます。

　・チャットメッセージ受信：CharacterRoot.OnReceiveChatMessage
　　　チャットメッセージを受信したリモート端末は、ローカル端末と同様にクエリを発行してバルーンにメッセージを表示します。
 　　　(本書を参照)

▼お庭の移動
　・お出かけ/戻るときのアイテム制御：LeaveEvent.execute
　　　相手のお庭に遊びに行くときや戻るときは LeaveEvent.execute 内で this.step.do_transition() が  STEP.START に
　　　なったときに持ち運びに関するアイテムの制御を行っています。

　・お出かけ/戻るの送信：GameRoot.NotifyFieldMoving
　　　お出かけするときは EventBoxLeave.Update内で出発のトリガーをチェックして GameRoot.NotifyFieldMoving で
　　　お出かけや戻るときの情報をゲームサーバへ送信します。
　　　ゲームサーバはリフレクターで各端末へ送信します。

　・お出かけ/戻るの受信：GameRoot.OnReceiveGoingOutPacket
　　　リモート端末がお出かけ/戻るの情報を受信すると、GameRoot.OnReceiveGoingOutPacket が呼び出されます。
　　　この関数内で、イベントを発生させるキャラクターとお庭、リモートキャラクタの位置により次に行う制御を切り替えます。


●MonoDevelopでのビルド
サンプルコードはC#のデフォルト引数を使用しています。
そのため、MonoDevelopの設定によってはビルドエラーになる場合があります。
このような場合は、MonoDevelopのSolusion ウインドウの Assembly-CSharp を選択して[右クリック]-[Options]を選択します。
Project-Options の[Build]-[General]-[Target framework] の項目を .NET 4.0 以上を選択して OK を押してください。
