
■「オンラインゲームのしくみ」サンプルコード

●フォルダ構成
本サンプルのフォルダ構成は下記のようになっています。
本書の章に合わせてフォルダを参照してください。
					
▼フォルダ構成					▼フォルダ説明
----------------------------------------------------------------------------------------------------------
+09						●第9章サンプルプログラム
 \---MatchingServer				「マッチングサーバ」サンプルプログラム
     +---Assets
     |   +---Scenes				　・シーンファイル
     |   +---Scripts				　・サンプルスクリプトコード
     +---bin					　・「マッチングサーバ」サンプル実行ファイル


■「マッチングサーバ」サンプルプログラム

●実行ファイル
09\MatchingServer\bin\MatchingServer.exe

●使い方
実行ファイルを起動すると、マッチングサーバが待ち受けを開始します。
「どきどき?がんもどき」でゲームを開始する前に起動しておく必要があります。
ルームは最大4部屋作成できます。
起動後の画面にマッチングサーバのIPアドレスと待ち受けポート番号、現在の接続数が表示されます。
マッチングクライアントから接続され、部屋が作成されると、作成された部屋の情報が表示されます。
ゲームを開始したセッションは、一定時間が経過すると部屋情報が削除されます。


●サンプルプログラム
プロジェクトファイル：09\MatchingServer\Assets\Scenes\MatchingServer.unity
プログラム：09\MatchingServer\Assets\Script


●通信が関係するファイルの構成
IPacket.cs		パケットインターフェース
MatchingServer.cs	マッチングサーバ
Network.cs		通信モジュール(TransportTCP,TransportUDPクラスの制御)
NetworkDef.cs		通信に関する定義
Packet.cs		パケットクラス定義
PacketQueue.cs		パケットキュークラス
PacketSerializer.cs	パケットシリアライザ(パケットヘッダのシリアライザ)
PacketStructs.cs	パケットデータ定義
Serializer.cs		シリアライザ基底クラス
Session.cs		セッション管理の基底クラス
SessionUDP.cs		TCP用セッション管理クラス
SessionTCP.cs		UDP用セッション管理クラス
TransportTcp.cs		TCPのソケット通信プログラム
TransportUDP.cs		UDPのソケット通信プログラム


※Assets\Script\Networkフォルダ内の通信処理を行うプログラム内にプログラムの動作に関するコメントが記述してあります。
　プログラム内のコメントも参照してください。