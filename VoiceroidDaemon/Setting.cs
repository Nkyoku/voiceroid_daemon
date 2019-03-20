using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using VoiceroidDaemon.Models;
using Aitalk;

namespace VoiceroidDaemon
{
    internal static class Setting
    {
        /// <summary>
        /// 標準の設定ファイルパス
        /// </summary>
        public const string DefaultPath = "setting.json";

        /// <summary>
        /// 設定ファイルパス
        /// </summary>
        public static string Path = DefaultPath;

        /// <summary>
        /// システム設定値
        /// </summary>
        public static SystemSettingModel System;

        /// <summary>
        /// 話者設定値
        /// </summary>
        public static SpeakerSettingModel Speaker;

        /// <summary>
        /// 話者パラメータの初期値
        /// </summary>
        public static SpeakerModel DefaultSpeakerParameter;
        
        /// <summary>
        /// バイナリセマフォ
        /// </summary>
        private static SemaphoreSlim Semaphore = new SemaphoreSlim(1);

        /// <summary>
        /// セマフォを取得する
        /// </summary>
        public static void Lock()
        {
            Semaphore.Wait();
        }

        /// <summary>
        /// セマフォを解放する
        /// </summary>
        public static void Unlock()
        {
            Semaphore.Release();
        }

        /// <summary>
        /// VOICEROID2エディタのアイコンファイルのバイト配列
        /// </summary>
        public static byte[] IconByteArray;

        /// <summary>
        /// 設定ファイルを読み込む
        /// </summary>
        /// <returns>読み込まれたらtrueを返す</returns>
        public static bool Load()
        {
            bool result;
            SystemSettingModel system_setting;
            SpeakerSettingModel speaker_setting;
            try
            {
                using (Stream stream = new FileStream(Path, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(SettingJson));
                    var setting = (SettingJson)serializer.ReadObject(stream);
                    system_setting = setting.System;
                    speaker_setting = setting.Speaker;
                }
                result = true;
            }
            catch (Exception)
            {
                // 初期値を返す
                system_setting = new SystemSettingModel();
                speaker_setting = new SpeakerSettingModel();
                result = false;
            }
            ApplySystemSetting(system_setting);
            ApplySpeakerSetting(speaker_setting);
            return result;
        }

        /// <summary>
        /// 設定ファイルを保存する
        /// </summary>
        /// <returns>保存できたらtrueを返す</returns>
        public static bool Save()
        {
            try
            {
                using (Stream stream = new FileStream(Path, FileMode.Create, FileAccess.Write))
                {
                    // 人が読みやすい形でシリアライズするためにインデントと改行を有効にしてCreateJsonWriterを作成する
                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  "))
                    {
                        SettingJson setting;
                        setting.System = System;
                        setting.Speaker = Speaker;
                        var serializer = new DataContractJsonSerializer(typeof(SettingJson));
                        serializer.WriteObject(writer, setting);
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

        /// <summary>
        /// 新しいシステム設定値を適用する。
        /// </summary>
        /// <param name="setting">新しいシステム設定値</param>
        /// <returns>エラーメッセージ、もしくはnull</returns>
        public static string ApplySystemSetting(SystemSettingModel setting)
        {
            System = setting;
            IconByteArray = null;
            try
            {
                // インストールディレクトリと実行ファイルの存在を確認する
                if (Directory.Exists(System.InstallPath) == false)
                {
                    return "インストールディレクトリが存在しません。";
                }
                string exe_path = $"{System.InstallPath}\\{System.VoiceroidEditorExe}";
                if (File.Exists(exe_path) == false)
                {
                    return "VOICEROID2エディタの実行ファイルが存在しません。";
                }

                // アイコンを取得する
                try
                {
                    using (var icon = Icon.ExtractAssociatedIcon(exe_path))
                    using (var bitmap = icon.ToBitmap())
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, ImageFormat.Png);
                        IconByteArray = stream.ToArray();
                    }
                }
                catch (Exception) { }
                
                // AITalkを初期化する
                AitalkWrapper.Initialize(System.InstallPath, System.AuthCodeSeed);

                // 言語ライブラリを読み込む
                if ((System.LanguageName != null) && (0 < System.LanguageName.Length))
                {
                    // 指定された言語ライブラリを読み込む
                    AitalkWrapper.LoadLanguage(System.LanguageName);
                }
                else
                {
                    // 未指定の場合、初めに見つけたものを読み込む
                    string language_name = AitalkWrapper.LanguageList.FirstOrDefault() ?? "";
                    AitalkWrapper.LoadLanguage(language_name);
                }

                // フレーズ辞書が指定されていれば読み込む
                if (File.Exists(System.PhraseDictionaryPath))
                {
                    AitalkWrapper.ReloadPhraseDictionary(System.PhraseDictionaryPath);
                }

                // 単語辞書が指定されていれば読み込む
                if (File.Exists(System.WordDictionaryPath))
                {
                    AitalkWrapper.ReloadWordDictionary(System.WordDictionaryPath);
                }

                // 記号ポーズ辞書が指定されていれば読み込む
                if (File.Exists(System.SymbolDictionaryPath))
                {
                    AitalkWrapper.ReloadSymbolDictionary(System.SymbolDictionaryPath);
                }

                return null;
            }
            catch (AitalkException ex)
            {
                return ex.Message;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// 新しい話者設定値を適用する。
        /// </summary>
        /// <param name="setting">新しい話者設定値</param>
        /// <returns>エラーメッセージ、もしくはnull</returns>
        public static string ApplySpeakerSetting(SpeakerSettingModel setting)
        {
            Speaker = setting;
            try
            {
                // ボイスライブラリを読み込む
                if (0 < Speaker.VoiceDbName.Length)
                {
                    // 指定されたボイスライブラリを読み込む
                    string voice_db_name = Speaker.VoiceDbName;
                    AitalkWrapper.LoadVoice(voice_db_name);

                    // 話者が指定されているときはその話者を選択する
                    if (0 < Speaker.SpeakerName.Length)
                    {
                        AitalkWrapper.Parameter.CurrentSpeakerName = Speaker.SpeakerName;
                    }
                }
                else
                {
                    // 未指定の場合、初めに見つけたものを読み込む
                    string voice_db_name = AitalkWrapper.VoiceDbList.FirstOrDefault() ?? "";
                    AitalkWrapper.LoadVoice(voice_db_name);
                }

                // 話者パラメータの初期値を記憶する
                DefaultSpeakerParameter = new SpeakerModel
                {
                    Volume = AitalkWrapper.Parameter.VoiceVolume,
                    Speed = AitalkWrapper.Parameter.VoiceSpeed,
                    Pitch = AitalkWrapper.Parameter.VoicePitch,
                    Emphasis = AitalkWrapper.Parameter.VoiceEmphasis,
                    PauseMiddle = AitalkWrapper.Parameter.PauseMiddle,
                    PauseLong = AitalkWrapper.Parameter.PauseLong,
                    PauseSentence = AitalkWrapper.Parameter.PauseSentence
                };
                
                return null;
            }
            catch (AitalkException ex)
            {
                return ex.Message;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

    }
}
