
■「オンラインゲームのしくみ」サンプルコード

●フォルダ構成
本サンプルのフォルダ構成は下記のようになっています。
本書の章に合わせてフォルダを参照してください。
					
▼フォルダ構成					▼フォルダ説明
----------------------------------------------------------------------------------------------------------
+09						●第9章サンプルプログラム
 \---dokidoki_ganmodoki				「どきどき・がんもどき」サンプルプログラム
     +---Assets					　・シーンファイル
     |   +---Scripts				　・サンプルスクリプトコード
     +---bin					　・「どきどき・がんもどき」サンプル実行ファイル


■「どきどき・がんもどき」サンプルログラム

●実行ファイル
09\dokidoki_ganmodoki\bin\dokidoki_ganmodoki.exe

●遊び方
▽マッチング

タイトル画面で、「マッチングサーバのアドレス」のテキストフィールドにマッチングサーバを
起動している端末のIPアドレスを入力してください。
マッチングサーバと接続すると、新たに部屋を作成するか、既存の部屋へ参加するのかを決めます。
新たに部屋を作る場合は、テキストフィールドに作成する部屋の名前を入力して「部屋を作る"」
ボタンを押します。この時テキストフィールド下のレベル選択のトグルボタンで選択されている
レベルで作成されます。作成した部屋の参加状況を確認できる表示がされますので、ゲームを
開始したいときに「ゲームを始める」ボタンを押してゲームを開始することができます。
既存の部屋へ参加化する場合は、テキストフィールド下のレベル選択のトグルボタンでレベルを
指定して「部屋を探す」ボタンを押します。検索条件に合致した部屋の一覧のボタンが表示され
ますので、参加する部屋のボタンを押して部屋へ入ることができます。

※一度参加した部屋からは退出できません。


▽ゲーム
マッチングサーバでマッチングが行われるとゲームが開始されます。
マッチングしたプレイヤーとダンジョンへ進みます。
ダンジョン内はマウスの左ボタンをドラッグすることでキャラクターを移動させることができます。
マウスの右クリックで弾(ネギ)を発射することができます。
ダンジョン内にある鍵を拾って隣のルームへ移動出来ます。鍵を保持した状態で鍵と同色のドーナツ
に全員が乗ると隣の部屋へ行けます。
ダンジョンにはモンスターが徘徊しています。モンスターに近づいての近接攻撃や遠方からの攻撃で
討伐します。また、ジェネレータから発生することもあります。ジェネレータは破壊しない限りモン
スターを生成し続けます。
ダンジョン内にはケーキやアイスなどのアイテムがあります。これらはモンスターから受けたダメージ
を回復します。
モンスターにやられてしまった場合(HPが0になったとき)は、現在のルームのスタート地点に戻さ
れてしまいます。せっかくダンジョンを進んでも元に戻されてしまいますので、モンスターから
攻撃されないようにしましょう。
画面左上に仲間とチャットするためのテキストフィールドがあります。仲間と会話をしながらダンジョン
を進みましょう。


※このサンプルでの制限事項
・初期装備の選択、武器の切り替えはできません
・アイスの抽選は行われません
・ボスのダンジョンへは進めません


●サンプルプログラム
プロジェクトファイル：09\dokidoki_ganmodoki\Assets\Scenes\TitleScene.unity
プログラム：09\dokidoki_ganmodoki\Assets\Script


●通信が関係するファイルの構成
Character
    Player			キャラクター制御スクリプト群
        chrBehaviorLocal.cs		ローカルキャラクターのビヘイビア
        chrBehaviorNet.cs		リモートキャラクターのビヘイビア
        chrBehaviorPlayer.cs	キャラクターの共通ビヘイビア
    
    CharacterRoot.cs		キャラクターマネージャ
    chrBehaviorBase.cs		キャラクタービヘイビアの基底クラス
    chrController.cs		キャラクターコントローラ
    MeleeAttack.cs		攻撃に関するの制御

Event				イベント制御スクリプト群
    EventBoxLeave.cs		庭移動時の制御(ローカルキャラクターが移動するとき)
    EventEnter.cs		庭移動時の制御(リモートキャラクターが遊びに来るとき)
    EventLeave.cs		庭移動時のアイテム持ち運び制御

Item				アイテム制御スクリプト群
    ItemBehaviorBase.cs		アイテムビヘイビアの基底クラス
    ItemController.cs		アイテムコントローラ
    ItemManager.cs		アイテムマネージャ

Level
    LevelController.cs		ダンジョン生成に関する制御

Network				通信制御スクリプト群
    GameServer.cs		ゲームサーバ
    IPacket.cs			パケットインターフェース
    MatchingClient.cs		マッチングクライアント
    Network.cs			通信モジュール(TransportTCP,TransportUDPクラスの制御)
    NetworkDef.cs		通信に関する定義
    Packet.cs			パケットクラス定義
    PacketQueue.cs		パケットキュークラス
    PacketSerializer.cs		パケットシリアライザ(パケットヘッダのシリアライザ)
    PacketStructs.cs		パケットデータ定義
    Serializer.cs		シリアライザ基底クラス
    SplineData.cs		キャラクター移動用スプライン補間クラス
    Session.cs			セッション管理の基底クラス
    SessionUDP.cs		TCP用セッション管理クラス
    SessionTCP.cs		UDP用セッション管理クラス
    TransportTcp.cs		TCPのソケット通信プログラム
    TransportUDP.cs		UDPのソケット通信プログラム

Stage
    DoorControl.cs		部屋移動制御
    MapCreator.cs		マップ生成制御

System				ゲームシステム制御スクリプト群
    GameRoot.cs			ゲーム制御
    GlobalParam.cs		シーンをまたぐ情報を管理
    QueryManager.cs		クエリ制御
    TitleControl.cs		ゲームサーバ、ホスト-ゲスト間接続シーケンス制御


※Assets\Script\Networkフォルダ内の通信処理を行うプログラム内にプログラムの動作に関するコメントが記述してあります。
　プログラム内のコメントも参照してください。


●通信プログラム補足
▼キャラクターの移動
　・ローカルキャラクターの座標送信：chrBehaviorLocal.execute
  　ローカルキャラクターの座標は10フレーム間隔でバッファ m_culling に貯められ、過去4点分の情報を
　　CharacterRoot.SendCharacterCoord に渡されます。(SendCharacterCoord の処理は本書を参照してください)

　・リモートキャラクターの座標受信：chrBehaviorNet.CalcCoordinates
　　リモートキャラクターの座標は通信モジュールで受信したデータをCharacterRoot.OnReceiveCharacterPacket
　　で処理し、CalcCoordinates に渡されます。


▼アイテムの取得・使用
　・ゲームサーバ
　　アイテムの取得調停：GameServer.MediatePickupItem
　　　アイテムの取得問い合わせは GameServer.OnReceiveItemPacket で受信を行い、MediatePickupItem が
　　　呼び出されます。(MediatePickupItem の処理は本書を参照してください)
　　
　　アイテムの破棄調停：GameServer.MediateDropItem
　　　アイテムの取得問い合わせは GameServer.OnReceiveItemPacket で受信を行い、MediateDropItem が
　　　呼び出されます。MediateDropItem の処理は本書を参照してください)
	
　・アイテム取得の問い合わせ：ItemManager.queryPickItem
　　　アイテムがクリックされたときに、chrBehaviorLocal.exec_step_move で chrController.cmdItemQueryPick
　　　から ItemManager.queryPickItem へクエリを発行してサーバへの問い合わせを行います。
　　　(各処理は本書を参照してください)


▼アイテムの使用　　
　・アイテムの使用：chrController.cmdUseItemSelf, chrController.cmdUseItemToFriend
　　　ケーキを取得したり、アイスを使用するときは、chrController.cmdUseItemSelf, chrController.cmdUseItemToFriend
　　　から ItemManager.useItem が呼び出されます。この関数でアイテムを使用し、情報を送信します。
　　　アイテムの使用情報をじゅしんすると ItemManager.OnReceiveUseItemPacket が呼び出され、リモート側で ItemManager.useItem
　　　でアイテムの使用を行います。
 

▼キャラクターの攻撃
　・攻撃：chrBehaviorLocal.execute, MeleeAttack.attack
　　　キャラクターが攻撃を行うと CharacterRoot.SendAttackData が呼び出されます。攻撃の種類を判別して
　　　パケットを送信します。この通信はキャラクターが攻撃をしているように見せるための演出のため、データ
　　　がロストしてもゲームの進行に影響を及ぼさないためUDPで通信をしています。
　　　データを受信すると CharactorRoot.OnReceiveAttackPacket が呼び出されます。この関数で送信時に
　　　指定された攻撃種別に応じて chrBehaviorNet.cmdShotAttack, chrBehaviorNet.cmdMeleeAttack を呼び出して
　　　キャラクターに攻撃をさせます。ただし、リモートキャラクターはコリジョンを持ちませんので攻撃をしている
　　　ふりをするだけになります。
　　

▼ダメージ・HP通知
　・ダメージ通知：chrController.caseDamage
　　　各端末でモンスターへの攻撃が行われた時に chrController.caseDamage が呼び出されます。
　　　この関数で受けたダメージ量をホスト端末へ通信します。
　　　ゲスト端末からモンスターへのダメージ通知を受け取ったホスト端末は、受信したダメージ量を各端末へ
　　　CharacterRoot.NotifyDamage を呼び出すことで通知を行います。
　　　この NotifyHitPoint 関数でゲームサーバへリフレクターを介して各端末へ通知を行います。

　・HPの通信：chrController.caseDamage
　　　各キャラクターがモンスターから攻撃を受けた時に chrController.caseDamage が呼び出されます。
　　　この関数内で各端末への通知を行うために CharacterRoot.NotifyHitPoint を呼び出します。
　　　この NotifyHitPoint 関数でゲームサーバへリフレクターを介して各端末へ通知を行います。

▼モンスターの発生通知
　・モンスターのリスポーン：EnemyRoot.RequestSpawnEnemy
　　　ホスト端末の LevelControl.Update でモンスターのリスポーンが行われると EnemyRoot.RequestSpawnEnemy が呼び出されます。
　　　この RequestSpawnEnemy 関数で発生させるジェネレータを指定し、ゲームサーバのリフレクターを介して全端末へ通知
　　　を行います。受信した各端末で発生をさせることで発生の同期がとれます。
　　　この通知を受ける間にジェネレータを攻撃してもダメージ量はホスト端末へ送信したのち、全端末へ通知されますので発生の
　　　同期がとれることになります。(詳細は本文を参照してください)

▼チャット
　・チャットメッセージ送信：chrBehaviorLocal.execute
　　　チャットメッセージのテキストフィールドに入力が行われると、chrBehaviorLocal.execute で chrController.cmdQueryTalk
　　　が呼び出されます。この関数から CharacterRoot.queryTalk でメッセージが送信されます。

　・チャットメッセージ受信：CharacterRoot.OnReceiveChatMessage
　　　チャットメッセージを受信したリモート端末は、ローカル端末と同様にクエリを発行してバルーンにメッセージを表示します。
 　　　(詳細は本文を参照してください)


▼武器選択/同期待ち
　・武器選択通知送信：WeaponSelectLevelSequence.execute
　　　武器選択は WeaponSelectLevelSequence で行われます。武器を選択後、execute 関数内で、グローバルIDと選択した武器の種類
　　　をまとめてゲームサーバへ通知を行います。

　・武器選択同期待ち：GameServer.checkInitialEquipment
　　　ゲームサーバが各端末から選択した武器情報を受信すると GameServer.OnReceiveEquipmentPacket 関数が呼び出されます。
　　　この関数で受信した端末で選択された武器の種類を保存します。その後、checkInitialEquipment 関数で全端末で武器選択
　　　の情報を受信したか監視を行い、すべての端末で選択されたときに全員の武器選択情報を全端末へ送信を行います。
　　　全員の武器選択情報を受信した各端末では、WeaponSelectLevelSequence.OnReceiveSyncPacket 関数が呼び出されます。
　　　この情報を GlobalParam へ保存を行います。武器選択同期のパケットは GameScene へ移行するシグナルにもなっています。
　　　このパケットの受信後は次のシーンへの遷移を行う処理をします。


▼アイスのあたり
　・アイスのあたり処理：
　　　アイスのあたりは本文の説明の通り、通信を行いません。あたりが出たときは通常時の消費を行わず、再度使用できる
　　　ようにするだけで端末間で同期がとれている状態になります。


▼ボスの移動/攻撃/死亡通知
　・ボスの移動座標送信：chrBehaviorEnemyBoss.sendCharacterCoordinates
 　 　ローカルキャラクターの座標は10フレーム間隔でバッファ m_culling に貯められ、過去4点分の情報を
　　　CharacterRoot.SendCharacterCoord に渡されます。(SendCharacterCoord の処理は本文を参照してください)


　・ボスの直接攻撃送信：chrBehaviorEnemyBoss.decideNextStep
　　　直接攻撃は decideNextStep 関数内で端末がホストの場合のみ実行されます。decideNextStep 関数で EnemyRoot.RequestBossDirectAttack
　　　関数で攻撃対象のキャラクターIDと攻撃力を指定し、 ゲームサーバのリフレクターを介して各ゲスト端末へ通知されます。
　　　各ゲスト端末は EnemyRoot.OnReceiveDirectAttackPacket 関数でキャラクターIDと攻撃力を受信します。この情報を
　　　chrControllerEnemyBoss.cmdBossDirectAttack 関数へ通知してボスの攻撃を行います。


　・ボスの範囲攻撃送信：chrBehaviorEnemyBoss.decideNextStep
　　　範囲攻撃も、直接攻撃と同様に decideNextStep 関数内で端末がホストの場合のみ実行されます。decideNextStep 関数で
　　　EnemyRoot.RequestBossRangeAttack 関数で攻撃範囲と攻撃力を指定し、 ゲームサーバのリフレクターを介して各ゲスト
　　　端末へ通知されます。各ゲスト端末は EnemyRoot.OnReceiveRangeAttackPacket 関数で攻撃範囲と攻撃力を受信します。
　　　この情報を chrControllerEnemyBoss.cmdBossRangeAttack 関数へ通知してボスの攻撃を行います。


　・ボスの死亡通知：EnemyRoot.RequestBossDead
　　　ボスとの戦闘時に端末のシーンロードや移動時の微小な同期のずれによりボスのダメージ通知の送受信にずれが生じることが
　　　あります。ボスシーンに入ったら同期待ちをしてもよいのですが、モンスターなどの死亡通知を送受信する方法もあるので
　　　同期ずれの安全装置としてこちらを実装しました。(ゲームとしては演出などで同期待ちをするのがよい方法だと思います)
　　　ボスが死亡した時に、chrControllerEnemyBase.goToVanishState 関数で死亡時の処理が開始されます。この時、
　　　EnemyRoot.RequestBossDead 関数を呼び出してゲームサーバのリフレクターを介して死亡通知を行います。
　　　ボスの死亡通知を受信した端末は、EnemyRoot.OnReceiveBossDeadPacket 関数が呼び出されます。この関数から
　　　chrControllerEnemyBase.causeVanish 関数を呼び出すことでボスを死亡させることができます。


▼ご褒美のケーキバイキング
　・ケーキの取得数通知：BossLevelSequenceResult.sendPrizeData
　　　ケーキの取得が終了するとシーケンスが BossLevelSequenceResult に移行します。このシーケンスに移行した時の Start 関数
　　　で sendPrizeData 関数を呼び出し、ケーキ取得数の通知を行います。


　・ケーキ取得結果：GameServer.checkReceivePrizePacket
　　　ケーキの取得数を受信したゲームサーバは、GameServer.OnReceivePrizePacket 関数が呼び出され、各端末のケーキ取得数を
　　　保存します。全端末の取得数を受信するまで checkReceivePrizePacket 関数で監視を行います。全端末の取得数を受信したら
　　　取得結果をまとめて端末へ送信します。
　　　取得結果を受信した端末は、BossLevelSequenceResult.OnReceivePrizeResultPacket 関数が呼び出され、各キャラクターの
　　　取得したケーキ数を保存します。受信が完了すると execute 関数で取得結果を集計して、表示を行います。



●1台の端末での実行について
本サンプルは、実行する端末を複数台用意できない方のために1台の端末で動作するように作成してあります。
このため、UDPで使用するポート番号を、基準となるポート番号からプレイヤー番号を足したものを使用して
います。別の端末で通信を行う場合は、通常は、すべての端末で同一のポート番号を使用します。

また、デバッグ用途としてマッチングサーバを使用しないゲームプレイもできます。TitleControl.cs, MatchingClient.cs で定義され
ている UNUSE_MATCHING_SERVER の定義を有効にしてください。この場合は、同一の端末ですべてのアプリケーションを実行してください。


●MonoDevelopでのビルド
サンプルコードはC#のデフォルト引数を使用しています。
そのため、MonoDevelopの設定によってはビルドエラーになる場合があります。
このような場合は、MonoDevelopのSolusion ウインドウの Assembly-CSharp を選択して[右クリック]-[Options]を選択します。
Project-Options の[Build]-[General]-[Target framework] の項目を .NET 4.0 以上を選択して OK を押してください。
