using System;
using System.Text;
using System.IO;
using System.Net;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace voiceroid_daemon
{
    class Program
    {
        // パスの最大長
        private const int MaxPathLength = 1024;

        // 設定ファイルのパス
        private const string ConfigFilePath = ".\\config.ini";

        // 設定ファイルのセクション名
        private const string ConfigFileSection = "Default";

        // VOICEROIDエディタの実行ファイル名
        private const string VoiceroidEditor = "VoiceroidEditor.exe";

        // インストールディレクトリのパス
        private static string InstallPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\AHS\\VOICEROID2";

        // 認証コードのシード値
        private static string AuthCodeSeed = "";

        // VOICEROID2の言語名
        private static string LanguageName = "standard";

        // フレーズ辞書へのパス
        private static string PhraseDictionaryPath = "";

        // 単語辞書へのパス
        private static string WordDictionaryPath = "";

        // 記号ポーズ辞書へのパス
        private static string SymbolDictionaryPath = "";

        // VOICEROID2の音声データベース名
        private static string VoiceName = "";

        // 待ち受けアドレス
        private static string ListeningAddress = "http://127.0.0.1:80/";
        
        // AITalkラッパーライブラリ
        private static AITalkWrapper.AITalkWrapper SpeechEngine = null;

        // サーバーを終了させるためのCancellationToken
        private static CancellationTokenSource StopServerCancelToken = new CancellationTokenSource();

        // 設定ファイルを読み込む
        private static void LoadConfiguration(string config_path)
        {
            var sb = new StringBuilder(MaxPathLength);

            IniFileHandler.GetPrivateProfileString(ConfigFileSection, "InstallPath", "", sb, MaxPathLength, ConfigFilePath);
            InstallPath = sb.ToString();

            IniFileHandler.GetPrivateProfileString(ConfigFileSection, "AuthCodeSeed", "", sb, MaxPathLength, ConfigFilePath);
            AuthCodeSeed = sb.ToString();

            IniFileHandler.GetPrivateProfileString(ConfigFileSection, "LanguageName", "", sb, MaxPathLength, ConfigFilePath);
            LanguageName = sb.ToString();

            IniFileHandler.GetPrivateProfileString(ConfigFileSection, "PhraseDictionaryPath", "", sb, MaxPathLength, ConfigFilePath);
            PhraseDictionaryPath = sb.ToString();

            IniFileHandler.GetPrivateProfileString(ConfigFileSection, "WordDictionaryPath", "", sb, MaxPathLength, ConfigFilePath);
            WordDictionaryPath = sb.ToString();

            IniFileHandler.GetPrivateProfileString(ConfigFileSection, "SymbolDictionaryPath", "", sb, MaxPathLength, ConfigFilePath);
            SymbolDictionaryPath = sb.ToString();

            IniFileHandler.GetPrivateProfileString(ConfigFileSection, "VoiceName", "", sb, MaxPathLength, ConfigFilePath);
            VoiceName = sb.ToString();

            IniFileHandler.GetPrivateProfileString(ConfigFileSection, "ListeningAddress", "", sb, MaxPathLength, ConfigFilePath);
            ListeningAddress = sb.ToString();
        }

        // 設定ファイルを保存する
        private static void SaveConfiguration(string config_path)
        {
            IniFileHandler.WritePrivateProfileString(ConfigFileSection, "InstallPath", InstallPath, ConfigFilePath);
            IniFileHandler.WritePrivateProfileString(ConfigFileSection, "AuthCodeSeed", AuthCodeSeed, ConfigFilePath);
            IniFileHandler.WritePrivateProfileString(ConfigFileSection, "LanguageName", LanguageName, ConfigFilePath);
            IniFileHandler.WritePrivateProfileString(ConfigFileSection, "PhraseDictionaryPath", PhraseDictionaryPath, ConfigFilePath);
            IniFileHandler.WritePrivateProfileString(ConfigFileSection, "WordDictionaryPath", WordDictionaryPath, ConfigFilePath);
            IniFileHandler.WritePrivateProfileString(ConfigFileSection, "SymbolDictionaryPath", SymbolDictionaryPath, ConfigFilePath);
            IniFileHandler.WritePrivateProfileString(ConfigFileSection, "VoiceName", VoiceName, ConfigFilePath);
            IniFileHandler.WritePrivateProfileString(ConfigFileSection, "ListeningAddress", ListeningAddress, ConfigFilePath);
        }
        
        // エントリーポイント
        static void Main(string[] args)
        {
            // 設定ファイルを読み込む
            // ファイルが存在しない場合は作成し、一旦終了する
            if (File.Exists(ConfigFilePath) == true)
            {
                LoadConfiguration(ConfigFilePath);
            }
            else
            {
                MessageBox.Show(String.Format("設定ファイル'{0}'の読み込みに失敗しました。", ConfigFilePath));
                SaveConfiguration(ConfigFilePath);
                if (File.Exists(ConfigFilePath) == true)
                {
                    MessageBox.Show(String.Format("設定ファイル'{0}'を新規に作成しましたので、設定を入力して再び起動してください。", ConfigFilePath));
                }
                else
                {
                    MessageBox.Show(String.Format("設定ファイル'{0}'を新規に作成しようと試みましたが失敗しました。", ConfigFilePath));
                }
                return;
            }
            
            // AITalkの初期化を行う
            try
            {
                // AITalkを開く
                SpeechEngine = new AITalkWrapper.AITalkWrapper();
                if (SpeechEngine.OpenLibrary(InstallPath, AuthCodeSeed, AITalkWrapper.AITalkWrapper.DefaultTimeOut) == false)
                    throw new Exception(SpeechEngine.GetLastError());

                // 言語ファイルを読み込む
                if (SpeechEngine.LoadLanguage(LanguageName) == false)
                    throw new Exception(SpeechEngine.GetLastError());

                // フレーズ辞書が設定されていれば読み込む
                if ((0 < PhraseDictionaryPath.Length) && (SpeechEngine.LoadPhraseDictionary(PhraseDictionaryPath) == false))
                    throw new Exception(SpeechEngine.GetLastError());

                // 単語辞書が設定されていれば読み込む
                if ((0 < WordDictionaryPath.Length) && (SpeechEngine.LoadWordDictionary(WordDictionaryPath) == false))
                    throw new Exception(SpeechEngine.GetLastError());

                // 記号ポーズ辞書が設定されていれば読み込む
                if ((0 < SymbolDictionaryPath.Length) && (SpeechEngine.LoadSymbolDictionary(SymbolDictionaryPath) == false))
                    throw new Exception(SpeechEngine.GetLastError());

                // 音声データベースを読み込む
                if (SpeechEngine.LoadVoice(VoiceName) == false)
                    throw new Exception(SpeechEngine.GetLastError());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }

            // 処理を別スレッドで実行する
            Task task = Task.Factory.StartNew(Run);

            // トレイアイコンを作成する
            // アイコンはVOICEROIDエディタのものを使用するが、ダメならこの実行ファイルのものを使用する
            NotifyIcon notify_icon = new NotifyIcon();
            try
            {
                notify_icon.Icon = Icon.ExtractAssociatedIcon(InstallPath + "\\" + VoiceroidEditor);
            }
            catch (Exception)
            {
                notify_icon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            notify_icon.Text = $"voiceroidd : {VoiceName}\nListening at {ListeningAddress}";
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

        // 処理を本体
        private static void Run()
        {
            try
            {
                // HTTPサーバーがサポートされていることを確認する
                if (HttpListener.IsSupported == false)
                    throw new Exception("HttpListenerがサポートされていません。");

                // HTTPサーバーを開始する
                Task server_task = WaitConnections(StopServerCancelToken.Token);
                Console.WriteLine("HTTP server is listening at " + ListeningAddress);
                try
                {
                    server_task.Wait();
                }
                catch (Exception) { }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return;
            }
        }

        // 接続を待ち受ける
        private static async Task WaitConnections(CancellationToken cancel_token)
        {
            // HTTPサーバーを開始する
            var server = new HttpListener();
            server.Prefixes.Add(ListeningAddress);
            server.Start();
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
                    Console.Error.WriteLine(ex);
                    return;
                }
            }
        }
        
        // リクエストを処理する
        private static void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse responce = context.Response;
            
            try
            {
                if (request.HttpMethod != "GET")
                {
                    throw new NotImplementedException();
                }

                int index = 1;
                string raw_url = request.RawUrl;

                // メソッド名を調べる
                if (UrlMatch(raw_url, "kana/", ref index) == true)
                {
                    // 仮名変換メソッドを呼び出している
                    if (UrlMatch(raw_url, "fromtext/", ref index) == false)
                    {
                        throw new ArgumentException("変換するテキストが指定されていません。");
                    }

                    // 変換するテキストを取得する
                    string text_encoded = raw_url.Substring(index, raw_url.Length - index);
                    string text = HttpUtility.UrlDecode(text_encoded, Encoding.UTF8);
                    string kana = SpeechEngine.TextToKana(text, AITalkWrapper.AITalkWrapper.DefaultTimeOut);
                    if (kana == null)
                        throw new Exception(SpeechEngine.GetLastError());

                    // 仮名を返す
                    byte[] byte_data = Encoding.Unicode.GetBytes(kana);
                    responce.OutputStream.Write(byte_data, 0, byte_data.Length);
                    responce.ContentEncoding = Encoding.Unicode;
                    responce.ContentType = "text/plain";
                }
                else if (UrlMatch(raw_url, "speech/", ref index) == true)
                {
                    // 音声変換メソッドを呼び出している
                    string kana = null;
                    if (UrlMatch(raw_url, "fromtext/", ref index) == true)
                    {
                        // テキストが入力されたときは仮名に変換する
                        string text_encoded = raw_url.Substring(index, raw_url.Length - index);
                        string text = HttpUtility.UrlDecode(text_encoded, Encoding.UTF8);
                        kana = SpeechEngine.TextToKana(text, AITalkWrapper.AITalkWrapper.DefaultTimeOut);
                    }
                    else if (UrlMatch(raw_url, "fromkana/", ref index) == true)
                    {
                        string kana_encoded = raw_url.Substring(index, raw_url.Length - index);
                        kana = HttpUtility.UrlDecode(kana_encoded, Encoding.UTF8);
                    }
                    else
                    {
                        throw new ArgumentException("変換するテキストが指定されていません。");
                    }
                    if (kana == null)
                        throw new Exception(SpeechEngine.GetLastError());

                    // 音声に変換する
                    byte[] speech = SpeechEngine.KanaToSpeech(kana, AITalkWrapper.AITalkWrapper.DefaultTimeOut);
                    if (speech == null)
                        throw new Exception(SpeechEngine.GetLastError());

                    // 音声を返す
                    responce.OutputStream.Write(speech, 0, speech.Length);
                    responce.ContentType = "audio/wav";
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
                byte[] byte_data = Encoding.Unicode.GetBytes(ex.ToString());
                responce.OutputStream.Write(byte_data, 0, byte_data.Length);
                responce.ContentEncoding = Encoding.Unicode;
                responce.ContentType = "text/plain";
                responce.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            // レスポンスを返す
            responce.Close();
        }

        // subpathがurlのパスの一部に一致するか調べ、一致したら次の部分パスを比較できるようにインデックスをずらす
        private static bool UrlMatch(string url, string subpath, ref int index)
        {
            if (string.Compare(url, index, subpath, 0, subpath.Length) == 0)
            {
                index += subpath.Length;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
