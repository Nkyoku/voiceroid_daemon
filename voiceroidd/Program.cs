using System;
using System.Text;
using System.IO;
using System.Net;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Aitalk;

namespace VoiceroidDaemon
{
    [Command(Name = "voiceroidd", Description = "VOICEROID2 HTTP Server Daemon", ThrowOnUnexpectedArgument = false)]
    [SuppressDefaultHelpOption]
    class Program
    {
        /// <summary>
        /// エントリーポイント
        /// </summary>
        /// <param name="args">コマンドライン引数</param>
        public static int Main(string[] args)
        {
            // コマンドライン引数をパースしてOnExecute()を呼び出す
            return CommandLineApplication.Execute<Program>(args);
        }
        
        /// <summary>
        /// 設定ファイルのパス
        /// </summary>
        [Option("-c", CommandOptionType.SingleValue)]
        private string ConfigFilePath { get; } = "config.json";

        /// <summary>
        /// 動作モード。
        /// "server"ならサーバー、"auth"なら認証コードの取得
        /// </summary>
        [Argument(0, Description = "auth or server")]
        public string OperationMode { get; } = "";
        
        /// <summary>
        /// プログラムの実行する
        /// </summary>
        private void OnExecute()
        {
            // 設定を読み込む
            Config = Configuration.Load(ConfigFilePath, out bool not_exists);
            if (not_exists == true)
            {
                Config.Save(ConfigFilePath);
            }

            try
            {
                // 動作モードに応じて処理を開始する
                switch (OperationMode.ToLower())
                {
                case "auth":
                    GetAuthCode();
                    break;

                case "server":
                    StartServer();
                    break;

                default:
                    // ヘルプテキストを表示する
                    MessageBox.Show(
$@"コマンド
・認証コードを取得する。
    voiceroidd auth
・HTTPサーバーを起動する。
    voiceroidd server

オプション
・設定ファイルのパスを明示的に指定する。未指定の場合は'config.json'が使用される。
    -c <filepath>"
                        , Caption);
                    break;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), Caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 認証コードを取得して設定ファイルに記録するモード
        /// </summary>
        private void GetAuthCode()
        {
            DialogResult result;
            result = MessageBox.Show(
                "DLLの初期化に必要な認証コードをVOICEROID2エディタから取得します。\nVOICEROID2エディタを起動してください。",
                Caption, MessageBoxButtons.OKCancel);
            if (result != DialogResult.OK)
            {
                MessageBox.Show("認証コードの取得は中断されました。", Caption);
            }
            else
            {
                string auth_code_seed = Injecter.GetKey();
                if (auth_code_seed == null)
                {
                    MessageBox.Show("認証コードの取得に失敗しました。", Caption);
                }
                else
                {
                    result = MessageBox.Show(
                        $"認証コード'{auth_code_seed}'を取得しました。\n設定を{ConfigFilePath}に保存しますか?",
                        Caption, MessageBoxButtons.YesNo);
                    if (result != DialogResult.Yes)
                    {
                        MessageBox.Show("認証コードは保存されませんでした。", Caption);
                    }
                    else
                    {
                        Config.AuthCodeSeed = auth_code_seed;
                        if (Config.Save(ConfigFilePath) == true)
                        {
                            MessageBox.Show("設定を保存しました。", Caption);
                        }
                        else
                        {
                            MessageBox.Show("設定の保存に失敗しました。", Caption);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// HTTPサーバーを起動するモード
        /// </summary>
        private void StartServer()
        {
#if DEBUG
            // Debugビルドの場合、ログファイルを出力する
            Trace.Listeners.Add(new TextWriterTraceListener("trace.log"));
            Trace.AutoFlush = true;
#endif

            // AITalkを初期化する
            AitalkWrapper.Initialize(Config.InstallPath, Config.AuthCodeSeed);

            try
            {
                // 言語ファイルを読み込む
                AitalkWrapper.LoadLanguage(Config.LanguageName);

                // フレーズ辞書が設定されていれば読み込む
                if (File.Exists(Config.PhraseDictionaryPath))
                {
                    AitalkWrapper.ReloadPhraseDictionary(Config.PhraseDictionaryPath);
                }

                // 単語辞書が設定されていれば読み込む
                if (File.Exists(Config.WordDictionaryPath))
                {
                    AitalkWrapper.ReloadWordDictionary(Config.WordDictionaryPath);
                }

                // 記号ポーズ辞書が設定されていれば読み込む
                if (File.Exists(Config.SymbolDictionaryPath))
                {
                    AitalkWrapper.ReloadSymbolDictionary(Config.SymbolDictionaryPath);
                }

                // 音声データベースを読み込む
                AitalkWrapper.LoadVoice(Config.VoiceDbName);

                // 話者を設定する
                AitalkWrapper.Parameter.CurrentVoiceName = Config.VoiceName;

                // 処理を別スレッドで実行する
                Task task = Task.Factory.StartNew(Run);

                // トレイアイコンを作成する
                // アイコンはVOICEROIDエディタのものを使用するが、ダメならこの実行ファイルのものを使用する
                NotifyIcon notify_icon = new NotifyIcon();
                try
                {
                    notify_icon.Icon = Icon.ExtractAssociatedIcon($"{Config.InstallPath}\\{Config.VoiceroidEditorExe}");
                }
                catch (Exception)
                {
                    notify_icon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
                notify_icon.Text = $"{Caption} : {Config.VoiceName}\nListening at {Config.ListeningAddress}";
                notify_icon.Visible = true;

                // トレイアイコンのコンテキストメニューを作成する
                ContextMenu menu = new ContextMenu();
                menu.MenuItems.Add(new MenuItem("Exit", new EventHandler((object sender, EventArgs e) =>
                {
                    StopServerCancelToken.Cancel();
                    task.Wait();
                    notify_icon.Visible = false;
                    Application.Exit();
                    Environment.Exit(1);
                })));
                notify_icon.ContextMenu = menu;

                // メッセージループを開始する
                Application.Run();
            }
            finally
            {
                AitalkWrapper.Finish();
            }
        }

        // 処理を本体
        private void Run()
        {
            try
            {
                // HTTPサーバーを開始する
                var server = new HttpListener();
                server.Prefixes.Add(Config.ListeningAddress);
                server.Start();
                Trace.WriteLine($"HTTP server is listening at {Config.ListeningAddress}");
                Task server_task = WaitConnections(server, StopServerCancelToken.Token);
                try
                {
                    server_task.Wait();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{DateTime.Now} : server_task.Wait()\n{ex}");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{DateTime.Now} : Run()\n{ex}");
                MessageBox.Show(ex.ToString(), Caption);
                return;
            }
        }

        // 接続を待ち受ける
        private async Task WaitConnections(HttpListener server, CancellationToken cancel_token)
        {
            cancel_token.Register(() => server.Stop());
            while (cancel_token.IsCancellationRequested == false)
            {
                // リクエストを取得する
                var context = await server.GetContextAsync();
                try
                {
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{DateTime.Now} : WaitConnections()\n{ex}");
                    MessageBox.Show(ex.ToString(), Caption);
                    return;
                }
            }
        }
        
        // リクエストを処理する
        private void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse responce = context.Response;
            
            try
            {
                if (request.HttpMethod != "GET")
                {
                    throw new NotImplementedException();
                }

                int offset = 0;
                string path = request.RawUrl.Substring(1);
                var query = HttpUtility.ParseQueryString(request.Url.Query);
                
                // メソッド名を調べる
                if (UrlMatch(path, "kana/", ref offset) == true)
                {
                    // 仮名変換メソッドを呼び出している
                    if (UrlMatch(path, "fromtext/", ref offset) == false)
                    {
                        throw new ArgumentException("変換するテキストが指定されていません。");
                    }

                    // 変換するテキストを取得する
                    string text_encoded = path.Substring(offset, path.Length - offset);
                    string text = HttpUtility.UrlDecode(text_encoded);
                    string kana = AitalkWrapper.TextToKana(text, Config.KanaTimeout);

                    // 仮名を返す
                    byte[] result = Encoding.UTF8.GetBytes(kana);
                    responce.OutputStream.Write(result, 0, result.Length);
                    responce.ContentEncoding = Encoding.UTF8;
                    responce.ContentType = "text/plain";
                }
                else if (UrlMatch(path, "speech/", ref offset) == true)
                {
                    // 音声変換メソッドを呼び出している
                    string kana = null;
                    if (UrlMatch(path, "fromtext/", ref offset) == true)
                    {
                        // テキストが入力されたときは仮名に変換する
                        string text_encoded = path.Substring(offset, path.Length - offset);
                        string text = HttpUtility.UrlDecode(text_encoded);
                        kana = AitalkWrapper.TextToKana(text, Config.KanaTimeout);
                    }
                    else if (UrlMatch(path, "fromkana/", ref offset) == true)
                    {
                        string kana_encoded = path.Substring(offset, path.Length - offset);
                        kana = HttpUtility.UrlDecode(kana_encoded);
                    }
                    else
                    {
                        throw new ArgumentException("変換するテキストが指定されていません。");
                    }

                    // 音声に変換する
                    var stream = new MemoryStream();
                    AitalkWrapper.KanaToSpeech(kana, stream, Config.SpeechTimeout);

                    // 音声を返す
                    byte[] result = stream.ToArray();
                    responce.OutputStream.Write(result, 0, result.Length);
                    responce.ContentType = "audio/wav";
                }
                else if (path == "voicedb.json")
                {
                    // ボイスライブラリの一覧を返す
                    string[] voice_db_list = AitalkWrapper.VoiceDbList;
                    using (var stream = new MemoryStream())
                    {
                        var serializer = new DataContractJsonSerializer(typeof(string[]));
                        serializer.WriteObject(stream, voice_db_list);
                        byte[] result = stream.ToArray();
                        responce.OutputStream.Write(result, 0, result.Length);
                        responce.ContentEncoding = Encoding.UTF8;
                        responce.ContentType = "application/json";
                    }
                }
                else if (path == "param.json")
                {
                    // TTSパラメータを返す
                    byte[] result = AitalkWrapper.Parameter.ToJson();
                    responce.OutputStream.Write(result, 0, result.Length);
                    responce.ContentEncoding = Encoding.UTF8;
                    responce.ContentType = "application/json";
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            catch(NotImplementedException)
            {
                responce.StatusCode = (int)HttpStatusCode.NotImplemented;
            }
            catch (FileNotFoundException)
            {
                responce.StatusCode = (int)HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                // 例外を文字列化して返す
                responce.StatusCode = (int)HttpStatusCode.InternalServerError;
                byte[] byte_data = Encoding.UTF8.GetBytes(ex.ToString());
                responce.OutputStream.Write(byte_data, 0, byte_data.Length);
                responce.ContentEncoding = Encoding.UTF8;
                responce.ContentType = "text/plain";
            }

            // レスポンスを返す
            responce.Close();
        }

        // subpathがurlのパスの一部に一致するか調べ、一致したら次の部分パスを比較できるようにインデックスをずらす
        private static bool UrlMatch(string url, string subpath, ref int offset)
        {
            if (string.Compare(url, offset, subpath, 0, subpath.Length) == 0)
            {
                offset += subpath.Length;
                return true;
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// 設定
        /// </summary>
        private Configuration Config;

        // サーバーを終了させるためのCancellationToken
        private CancellationTokenSource StopServerCancelToken = new CancellationTokenSource();

        /// <summary>
        /// メッセージボックスなどのキャプション
        /// </summary>
        private const string Caption = "Voiceroid Daemon";
    }
}
