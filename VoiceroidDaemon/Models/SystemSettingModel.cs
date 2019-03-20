using System;
using System.Runtime.Serialization;

namespace VoiceroidDaemon.Models
{
    [DataContract]
    public class SystemSettingModel
    {
        /// <summary>
        /// VOICEROID2エディタの実行ファイル名
        /// </summary>
        [DataMember]
        public string VoiceroidEditorExe { get; set; }
        public static string DefaultVoiceroidEditorExe = "VoiceroidEditor.exe";

        /// <summary>
        /// インストールディレクトリのパス
        /// </summary>
        [DataMember]
        public string InstallPath { get; set; }
        public static string DefaultInstallPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\AHS\\VOICEROID2";

        /// <summary>
        /// 認証コードのシード値
        /// </summary>
        [DataMember]
        public string AuthCodeSeed { get; set; }

        /// <summary>
        /// 言語名
        /// </summary>
        [DataMember]
        public string LanguageName { get; set; }

        /// <summary>
        /// フレーズ辞書のファイルパス
        /// </summary>
        [DataMember]
        public string PhraseDictionaryPath { get; set; }

        /// <summary>
        /// 単語辞書のファイルパス
        /// </summary>
        [DataMember]
        public string WordDictionaryPath { get; set; }

        /// <summary>
        /// 記号ポーズ辞書のファイルパス
        /// </summary>
        [DataMember]
        public string SymbolDictionaryPath { get; set; }
        
        /// <summary>
        /// 読み仮名変換のタイムアウト[ms]
        /// </summary>
        [DataMember]
        public int KanaTimeout { get; set; }

        /// <summary>
        /// 音声変換のタイムアウト[ms]
        /// </summary>
        [DataMember]
        public int SpeechTimeout { get; set; }

        /// <summary>
        /// 待ち受けアドレス
        /// </summary>
        [DataMember]
        public string ListeningAddress { get; set; }
        public static string DefaultListeningAddress = "http://127.0.0.1:8080/";

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
        public SystemSettingModel()
        {
            LoadInitialValues();
        }

        /// <summary>
        /// メンバーに初期値を代入する
        /// </summary>
        private void LoadInitialValues()
        {
            VoiceroidEditorExe = DefaultVoiceroidEditorExe;
            InstallPath = DefaultInstallPath;
            AuthCodeSeed = "";
            LanguageName = "standard";
            PhraseDictionaryPath = "";
            WordDictionaryPath = "";
            SymbolDictionaryPath = "";
            KanaTimeout = 0;
            SpeechTimeout = 0;
            ListeningAddress = DefaultListeningAddress;
        }

        /// <summary>
        /// クローンを作成する
        /// </summary>
        /// <returns></returns>
        public SystemSettingModel Clone()
        {
            return (SystemSettingModel)MemberwiseClone();
        }
    }
}
