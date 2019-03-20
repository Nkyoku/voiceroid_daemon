using System;
using System.Runtime.Serialization;

namespace VoiceroidDaemon
{
    /// <summary>
    /// 設定値を管理するモデル
    /// </summary>
    [DataContract]
    public class SettingValues
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
        public string AuthCodeSeed;

        /// <summary>
        /// 言語名
        /// </summary>
        [DataMember]
        public string LanguageName;

        /// <summary>
        /// フレーズ辞書のファイルパス
        /// </summary>
        [DataMember]
        public string PhraseDictionaryPath;

        /// <summary>
        /// 単語辞書のファイルパス
        /// </summary>
        [DataMember]
        public string WordDictionaryPath;

        /// <summary>
        /// 記号ポーズ辞書のファイルパス
        /// </summary>
        [DataMember]
        public string SymbolDictionaryPath;

        /// <summary>
        /// ボイスライブラリ名
        /// </summary>
        [DataMember]
        public string VoiceDbName;

        /// <summary>
        /// 話者名。単一話者のライブラリならボイスライブラリ名と同じだと思われる。
        /// </summary>
        [DataMember]
        public string SpeakerName;

        /// <summary>
        /// 読み仮名変換のタイムアウト[ms]
        /// </summary>
        [DataMember]
        public int KanaTimeout;

        /// <summary>
        /// 音声変換のタイムアウト[ms]
        /// </summary>
        [DataMember]
        public int SpeechTimeout;

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
        public SettingValues()
        {
            LoadInitialValues();
        }
        
        /// <summary>
        /// メンバーに初期値を代入する
        /// </summary>
        private void LoadInitialValues()
        {
            VoiceroidEditorExe = "VoiceroidEditor.exe";
            InstallPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\AHS\\VOICEROID2";
            AuthCodeSeed = "";
            LanguageName = "standard";
            PhraseDictionaryPath = "";
            WordDictionaryPath = "";
            SymbolDictionaryPath = "";
            VoiceDbName = "";
            SpeakerName = "";
            KanaTimeout = 0;
            SpeechTimeout = 0;
            ListeningAddress = "http://127.0.0.1:8080/";
        }
    }
}
