# voiceroid_daemon
VOICEROID2のHTTPサーバーデーモン

## どんなソフトウェア？
VOICEROID2のDLL(aitalked.dll)を直接叩いて、音声データをHTTPで取得できるサーバーソフトです。  
よってエディターを起動しておく必要はありません。  
ライセンス認証はDLLレベルで行われているため当然ながら動作には有効なライセンスが必要です。

## ビルドする
Visual Studio 2017 の C#

## 起動させる
voiceroidd.exeをダブルクリックで起動するとconfig.iniが生成されます。  
config.iniに必要な設定を書き込んでもう一度、exeファイルをダブルクリックするとタスクトレイにアイコンが出て待ち受け状態になります。  
ウィンドウは出ませんので、終了させるときはトレイアイコンのコンテキストメニューをからExitを選択して終了させてください。  
エラーが発生した場合はメッセージボックスでエラーが表示されます。  

config.iniの書式は以下の通りです。
```
[Default]
InstallPath=<VOICEROID2のインストールパス> (C:\Program Files (x86)\AHS\VOICEROID2)
AuthCodeSeed=<認証コードのシード値> (おそらくどのコンピュータでもhttps://github.com/Nkyoku/aitalk_wrapper/raw/master/code.jpg)
LanguageName=<言語名> (standardもしくはstandard_kansai、関西弁は未テスト)
PhraseDictionaryPath=<フレーズ辞書のファイルパス> (使わないなら空欄)
WordDictionaryPath=<単語辞書のファイルパス> (使わないなら空欄)
SymbolDictionaryPath=<記号ポーズ辞書のファイルパス> (使わないなら空欄)
VoiceName=<ボイス名> (akari_44など、インストールフォルダのVoiceフォルダ内のフォルダ名)
ListeningAddress=<待ち受けアドレス> (http://127.0.0.1:80/など)
```

## 使い方
以下のサーバーアドレスとポート番号はconfig.iniの設定値に置き換えてください。

文章をVOICEROIDの読み仮名に変換する。  
`http://127.0.0.1:80/kana/fromtext/こんにちは`  
にアクセスすると`<S>(Irq MARK=_AI@5)コ^ンニチワ<F>`というテキストファイルが返ります。  

文章を音声に変換する。  
`http://127.0.0.1:80/speech/fromtext/こんばんは`  
にアクセスするとwavファイルが返ります。  

読み仮名を音声に変換する。  
読み仮名には特殊記号が含まれるためUTF-8でURLエンコードしてください。  
`http://127.0.0.1:80/speech/fromkana/%3cS%3e%28Irq%20MARK%3d_AI%405%29%e3%82%b3%5e%e3%83%b3%e3%83%8b%e3%83%81%e3%83%af%3cF%3e`  
にアクセスするとwavファイルが返ります。  

返ってくるテキストファイルはUTF-16形式、wavファイルは44.1kHz,16bit,モノラルです。


