using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace VoiceroidDaemon
{
    [DataContract]
    internal class Configuration
    {
        /// <summary>
        /// VOICEROID2エディタの実行ファイル名
        /// </summary>
        [DataMember]
        public string VoiceroidEditorExe;

        /// <summary>
        /// インストールディレクトリのパス
        /// </summary>
        [DataMember]
        public string InstallPath;

        /// <summary>
        /// 認証コードのシード値
        /// </summary>
        [DataMember]
        public string AuthCodeSeed = "";

        /// <summary>
        /// 言語名
        /// </summary>
        [DataMember]
        public string LanguageName;

        /// <summary>
        /// フレーズ辞書のファイルパス
        /// </summary>
        [DataMember]
        public string PhraseDictionaryPath = "";

        /// <summary>
        /// 単語辞書のファイルパス
        /// </summary>
        [DataMember]
        public string WordDictionaryPath = "";

        /// <summary>
        /// 記号ポーズ辞書のファイルパス
        /// </summary>
        [DataMember]
        public string SymbolDictionaryPath = "";

        /// <summary>
        /// ボイスライブラリ名
        /// </summary>
        [DataMember]
        public string VoiceDbName = "";

        /// <summary>
        /// 話者名。単一話者のライブラリならボイスライブラリ名と同じだと思われる。
        /// </summary>
        [DataMember]
        public string VoiceName = "";
        
        /// <summary>
        /// 読み仮名変換のタイムアウト[ms]
        /// </summary>
        [DataMember]
        public int KanaTimeout = 0;

        /// <summary>
        /// 音声変換のタイムアウト[ms]
        /// </summary>
        [DataMember]
        public int SpeechTimeout = 0;

        /// <summary>
        /// 待ち受けアドレス
        /// </summary>
        [DataMember]
        public string ListeningAddress;

        /// <summary>
        /// デシリアライズ前に呼ばれる。
        /// 初期値を代入する。
        /// </summary>
        /// <param name="context"></param>
        [OnDeserializing]
        internal void OnDeserializing(StreamingContext context)
        {
            LoadInitialValues();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private Configuration() { }
        
        /// <summary>
        /// 既定の初期値でないメンバーに初期値を代入する
        /// </summary>
        private void LoadInitialValues()
        {
            VoiceroidEditorExe = "VoiceroidEditor.exe";
            InstallPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\AHS\\VOICEROID2";
            LanguageName = "standard";
            ListeningAddress = "http://127.0.0.1:8080/";
        }
        
        /// <summary>
        /// 設定ファイルを読み込む
        /// </summary>
        /// <param name="file_path">設定ファイルのパス</param>
        /// <returns>読み込まれた設定</returns>
        public static Configuration Load(string file_path, out bool not_exists)
        {
            not_exists = !File.Exists(file_path);
            try
            {
                using (Stream stream = new FileStream(file_path, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(Configuration));
                    return (Configuration)serializer.ReadObject(stream);
                }
            }
            catch (Exception)
            {
                // 初期値を返す
                var config = new Configuration();
                config.LoadInitialValues();
                return config;
            }
        }

        /// <summary>
        /// 設定ファイルを保存する
        /// </summary>
        /// <param name="file_path">設定ファイルのパス</param>
        /// <returns>保存できたらtrueを返す</returns>
        public bool Save(string file_path)
        {
            try
            {
                using (Stream stream = new FileStream(file_path, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  "))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(Configuration));
                        serializer.WriteObject(writer, this);
                        writer.Flush();
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    
    internal static class IniFileHandler
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern uint WritePrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName);
    }
}
