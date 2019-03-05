# voiceroid_daemon
VOICEROID2のHTTPサーバーデーモン

## 概要
VOICEROID2のDLL(aitalked.dll)を直接叩いて、音声データをHTTPで取得できるサーバーソフトです。  
よってエディターを起動しておく必要はありません。  
ライセンス認証はDLLレベルで行われているため当然ながら動作には有効なライセンスが必要です。  

## ビルド環境
Visual Studio 2017

## 使い方
コマンドプロンプトやPowerShellよりvoiceroidd.exeを起動します。  
初回起動時は  
```
voiceroidd.exe auth
```  
と打ってガイダンスに従いDLLの認証コードを取得して設定ファイルを保存してください。    
認証コードはおそらくDLLの不正使用を防止するためのコードで、ボイスライブラリのライセンスとは別のものです。  
次に生成された設定ファイル(既定では`config.json`)に必要なパラメータを入力してください。  
  
記入を終えたら以下のコマンドでHTTPサーバーを起動してください。  
```
voiceroidd.exe server
```  
サーバーが起動するとタスクトレイにアイコンが出て待ち受け状態になります。  
終了させるときはトレイアイコンのコンテキストメニューをからExitを選択して終了させてください。  
何か設定に問題があったりエラーが発生した場合はメッセージボックスでエラーが表示されます。  
そのときは設定ファイルや環境を見直してください。

正常にサーバーが起動したらWebブラウザなどからアクセスしてテストが可能です。  
以下にAPIの使用例を示します。
IPアドレスとポートはそれぞれの環境に読み替えてください。

- 文章をVOICEROIDの読み仮名に変換する。  
`http://127.0.0.1:8080/kana/fromtext/こんにちは`  
にアクセスすると`<S>(Irq MARK=_AI@5)コ^ンニチワ<F>`というテキストファイルが返ります。  
テキストファイルのエンコードはUTF-16形式です。

- 文章を音声に変換する。  
`http://127.0.0.1:8080/speech/fromtext/こんばんは`  
にアクセスするとwavファイルが返ります。  

- 読み仮名を音声に変換する。  
読み仮名には特殊記号が含まれるためUTF-8でURLエンコードしてください。  
`http://127.0.0.1:8080/speech/fromkana/%3cS%3e%28Irq%20MARK%3d_AI%405%29%e3%82%b3%5e%e3%83%b3%e3%83%8b%e3%83%81%e3%83%af%3cF%3e`  
にアクセスするとWAVEファイルが返ります。  
WAVEファイルのフォーマットは44.1kHz,16bit,モノラルです。

## 設定ファイル
設定ファイルはJSON形式で書かれています。  
既定では設定ファイルは`config.json`という名前でvoiceroidd.exe起動時のカレントディレクトリに生成されます。
```
{
  "AuthCodeSeed": "",
  "InstallPath": "C:\\Program Files (x86)\\AHS\\VOICEROID2",
  "KanaTimeout": 0,
  "LanguageName": "standard",
  "ListeningAddress": "http:\/\/127.0.0.1:8080\/",
  "PhraseDictionaryPath": "",
  "SpeechTimeout": 0,
  "SymbolDictionaryPath": "",
  "VoiceDbName": "",
  "VoiceName": "",
  "VoiceroidEditorExe": "VoiceroidEditor.exe",
  "WordDictionaryPath": ""
}
```

- AuthCodeSeed  
DLLの認証コードです。使い方の節を参照してください。
- InstallPath  
VOICEROID2のインストールパスです。  
もし標準の場所以外にインストールした場合はここを変更してください。
- VoiceroidEditorExe  
VOICEROID2エディタの実行ファイルの名前を指定します。
- ListeningAddress  
HTTPサーバーの待ち受けアドレスとポートです。  
もし外部からの接続を待ち受ける場合、  
待ち受けアドレスは`http://+:8080/`などに設定し、  
管理者権限でコマンドプロンプトやPowerShellを起動して以下のコマンドを入力しアクセスを許可してください。
```
netsh http add urlacl url=http://+:8080/ user=<ユーザー名をここに入れる>
```
- LanguageName  
言語名を指定します。
- VoiceDbName  
ボイスライブラリ名を指定します。  
インストールフォルダのVoiceフォルダ内のフォルダ名です。
- VoiceName  
話者名を指定します。  
単一話者のボイスライブラリの場合はおそらくボイスライブラリ名と同じです。
- PhraseDictionaryPath
- SymbolDictionaryPath
- WordDictionaryPath  
それぞれフレーズ辞書、記号ポーズ辞書、単語辞書のファイルパスを指定します。使わないのなら空欄にしてください。
- KanaTimeout  
テキスト→読み仮名変換時のタイムアウト(ミリ秒)です。  
0を指定するとタイムアウトせずに無制限に処理の完了を待ちます。
- SpeechTimeout  
読み仮名(テキスト)→音声変換時のタイムアウト(ミリ秒)です。  
0を指定するとタイムアウトせずに無制限に処理の完了を待ちます。

## ライセンス
本ソフトウェアの製作にあたって以下のライブラリを使用しています。  
- [Friendly](https://github.com/Codeer-Software/Friendly)  
- [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)  
