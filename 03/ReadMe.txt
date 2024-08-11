
■「オンラインゲームのしくみ」サンプルコード

●フォルダ構成
本サンプルのフォルダ構成は下記のようになっています。
本書の章に合わせてフォルダを参照してください。
					
▼フォルダ構成					▼フォルダ説明
----------------------------------------------------------------------------------------------------------
+03						●第3章サンプルプログラム
 +---NetworkLibrary				通信ライブラリサンプル
 |   +---Assets					
 |   |   +---Scene				　・シーンファイル
 |   |   \---Script				　・サンプルスクリプトコード
 |   +---bin					　・通信ライブラリサンプル実行ファイル
 |       +---LibrarySampleTCP_Data		　・TCP用サンプルデータ
 |       \---LibrarySampleUDP_Data		　・UDP用サンプルデータ
 |　　
 \---SocketSample				はじめての通信プログラムサンプル
     +---Assets
     |   +---Scene				　・シーンファイル
     |   \---Script				　・サンプルスクリプトコード
     +---bin					　・はじめての通信プログラムサンプル実行ファイル
         +---SocketSampleTCP_Data		　・TCP用サンプルデータ
         \---SocketSampleUDP_Data		　・UDP用サンプルデータ


■ 通信サンプルプログラム

★はじめての通信プログラム
●実行ファイル
03\SocketSample\bin\SocketSample.exe		TCPサンプルプログラム
03\SocketSample\bin\SocketSample*.exe		UDPサンプルプログラム

●実行の仕方
Unity3D でプロジェクトファイルを起動してプログラム実行します。この時Consoleウインドウを表示してください。
Unity3D ではサーバー側で「Launch server」を選択してください。
クライアント側は実行ファイルを起動します。サーバー起動後、「Connect to server」を押してください。
クライアントのウインドウを閉じるとConsoleウインドウに"Hello, this is client."と表示されます。
メッセージが表示されれば通信が成功しています。


●サンプルプログラム
プロジェクトファイル：03\SocketSample\Assets\Scene\SocketSample.unity
プログラム：03\SocketSample\Assets\Script

SocketSampleTCP.cs		TCPソケットのサンプルプログラム
SocketSampleUDP.cs		UDPソケットのサンプルプログラム

※サンプルプログラム内にプログラムの動作に関するコメントが記述してあります。
　プログラム内のコメントも参照してください。


★通信ライブラリ
●サンプルプログラム
プロジェクトファイル：03\NetworkLibrary\Assets\Scene\NetworkLibrary.unity
プログラム：03\NetworkLibrary\Assets\Script

LibrarySample.cs		ライブラリの動作確認プログラム
TransportTCP.cs			TCP通信を行う通信モジュール
TransportUDP.cs			UDP通信を行う通信モジュール
PacketQueue.cs			パケットデータをスレッド間で共有するためのバッファ
NetworkDef.cs			通信イベント関連の定義
			
※サンプルプログラム内にプログラムの動作に関するコメントが記述してあります。
　プログラム内のコメントも参照してください。
