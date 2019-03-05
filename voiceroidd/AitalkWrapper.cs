using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Globalization;
using System.Threading;
using System.IO;

namespace VoiceroidDaemon
{
    public static class AitalkWrapper
    {
        /// <summary>
        /// AITalkを初期化する
        /// </summary>
        /// <param name="install_directory">VOICEROID2のインストールディレクトリ</param>
        /// <param name="authenticate_code">認証コード</param>
        public static void Initialize(string install_directory, string authenticate_code)
        {
            // aitalked.dllをロードするために
            // DLLの探索パスをVOICEROID2のディレクトリに変更する
            SetDllDirectory(install_directory);

            // AITalkを初期化する
            Aitalk.Config config;
            config.VoiceDbSampleRate = VoiceSampleRate;
            config.VoiceDbDirectory = $"{install_directory}\\Voice";
            config.TimeoutMilliseconds = TimeoutMilliseconds;
            config.LicensePath = $"{install_directory}\\aitalk.lic";
            config.AuthenticateCodeSeed = authenticate_code;
            config.ReservedZero = 0;
            var result = Aitalk.Result.Success;
            try
            {
                result = Aitalk.Init(ref config);
            }
            catch (Exception e)
            {
                throw new AitalkException($"AITalkの初期化に失敗しました。", e);
            }
            if (result != Aitalk.Result.Success)
            {
                throw new AitalkException($"AITalkの初期化に失敗しました。", result);
            }
        }

        /// <summary>
        /// AITalkを終了する
        /// </summary>
        public static void Finish()
        {
            Aitalk.End();
        }

        /// <summary>
        /// 言語データを読み込む
        /// </summary>
        /// <param name="install_directory">VOICEROID2のインストールディレクトリ</param>
        /// <param name="language_name">言語名</param>
        public static void LoadLanguage(string install_directory, string language_name)
        {
            // 言語の設定をする際はカレントディレクトリを一時的にVOICEROID2のインストールディレクトリに変更する
            // それ以外ではLangLoad()はエラーを返す
            string current_directory = System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.SetCurrentDirectory(install_directory);
            Aitalk.Result result;
            result = Aitalk.LangClear();
            if ((result == Aitalk.Result.Success) || (result == Aitalk.Result.NotLoaded))
            {
                result = Aitalk.LangLoad($"{install_directory}\\Lang\\{language_name}");
            }
            System.IO.Directory.SetCurrentDirectory(current_directory);
            if (result != Aitalk.Result.Success)
            {
                throw new AitalkException($"言語'{language_name}'の読み込みに失敗しました。", result);
            }
        }

        /// <summary>
        /// フレーズ辞書を読み込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public static void ReloadPhraseDictionary(string path)
        {
            Aitalk.ReloadPhraseDic(null);
            if (path == null)
            {
                return;
            }
            Aitalk.Result result;
            result = Aitalk.ReloadPhraseDic(path);
            if (result == Aitalk.Result.UserDictionaryNoEntry)
            {
                Aitalk.ReloadPhraseDic(null);
            }
            else if (result != Aitalk.Result.Success)
            {
                throw new AitalkException($"フレーズ辞書'{path}'の読み込みに失敗しました。", result);
            }
        }

        /// <summary>
        /// 単語辞書を読み込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public static void ReloadWordDictionary(string path)
        {
            Aitalk.ReloadWordDic(null);
            if (path == null)
            {
                return;
            }
            Aitalk.Result result;
            result = Aitalk.ReloadWordDic(path);
            if (result == Aitalk.Result.UserDictionaryNoEntry)
            {
                Aitalk.ReloadWordDic(null);
            }
            else if (result != Aitalk.Result.Success)
            {
                throw new AitalkException($"単語辞書'{path}'の読み込みに失敗しました。", result);
            }
        }

        /// <summary>
        /// 記号ポーズ辞書を読み込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public static void ReloadSymbolDictionary(string path)
        {
            Aitalk.ReloadSymbolDic(null);
            if (path == null)
            {
                return;
            }
            Aitalk.Result result;
            result = Aitalk.ReloadSymbolDic(path);
            if (result == Aitalk.Result.UserDictionaryNoEntry)
            {
                Aitalk.ReloadSymbolDic(null);
            }
            else if (result != Aitalk.Result.Success)
            {
                throw new AitalkException($"記号ポーズ辞書'{path}'の読み込みに失敗しました。", result);
            }
        }

        /// <summary>
        /// ボイスライブラリを読み込む
        /// </summary>
        /// <param name="voice_name">ボイスライブラリ名</param>
        public static void LoadVoice(string voice_name)
        {
            Aitalk.VoiceClear();
            if (voice_name == null)
            {
                return;
            }
            Aitalk.Result result;
            result = Aitalk.VoiceLoad(voice_name);
            if (result != Aitalk.Result.Success)
            {
                throw new AitalkException($"ボイスライブラリ'{voice_name}'の読み込みに失敗しました。", result);
            }
            
            // パラメータを読み込む
            GetParameters(out var tts_param, out var speaker_params);
            tts_param.TextBufferCallback = TextBufferCallback;
            tts_param.RawBufferCallback = RawBufferCallback;
            tts_param.TtsEventCallback = TtsEventCallback;
            tts_param.PauseBegin = 0;
            tts_param.PauseTerm = 0;
            tts_param.ExtendFormatFlags = Aitalk.ExtendFormat.JeitaRuby | Aitalk.ExtendFormat.AutoBookmark;
            Parameter = new AitalkParameter(tts_param, speaker_params);
        }

        /// <summary>
        /// パラメータを取得する
        /// </summary>
        /// <param name="tts_param">パラメータ(話者パラメータを除く)</param>
        /// <param name="speaker_params">話者パラメータ</param>
        private static void GetParameters(out Aitalk.TtsParam tts_param, out Aitalk.TtsParam.SpeakerParam[] speaker_params)
        {
            // パラメータを格納するのに必要なバッファサイズを取得する
            Aitalk.Result result;
            int size = 0;
            result = Aitalk.GetParam(IntPtr.Zero, ref size);
            if ((result != Aitalk.Result.Insufficient) || (size < Marshal.SizeOf<Aitalk.TtsParam>()))
            {
                throw new AitalkException("動作パラメータの長さの取得に失敗しました。", result);
            }

            IntPtr ptr = Marshal.AllocCoTaskMem(size);
            try
            {
                // パラメータを読み取る
                Marshal.WriteInt32(ptr, (int)Marshal.OffsetOf<Aitalk.TtsParam>("Size"), size);
                result = Aitalk.GetParam(ptr, ref size);
                if (result != Aitalk.Result.Success)
                {
                    throw new AitalkException("動作パラメータの取得に失敗しました。", result);
                }
                tts_param = Marshal.PtrToStructure<Aitalk.TtsParam>(ptr);

                // 話者のパラメータを読み取る
                speaker_params = new Aitalk.TtsParam.SpeakerParam[tts_param.NumberOfSpeakers];
                for (int index = 0; index < speaker_params.Length; index++)
                {
                    IntPtr speaker_ptr = IntPtr.Add(ptr, Marshal.SizeOf<Aitalk.TtsParam>() + Marshal.SizeOf<Aitalk.TtsParam.SpeakerParam>() * index);
                    speaker_params[index] = Marshal.PtrToStructure<Aitalk.TtsParam.SpeakerParam>(speaker_ptr);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        /// <summary>
        /// パラメータを設定する。
        /// param.Sizeおよびparam.NumberOfSpeakersは自動的に設定される。
        /// </summary>
        /// <param name="tts_param">パラメータ(話者パラメータを除く)</param>
        /// <param name="speaker_params">話者パラメータ</param>
        private static void SetParameters(Aitalk.TtsParam tts_param, Aitalk.TtsParam.SpeakerParam[] speaker_params)
        {
            // パラメータを格納するバッファを確保する
            int size = Marshal.SizeOf<Aitalk.TtsParam>() + Marshal.SizeOf<Aitalk.TtsParam.SpeakerParam>() * speaker_params.Length;
            IntPtr ptr = Marshal.AllocCoTaskMem(size);
            try
            {
                // パラメータを設定する
                tts_param.Size = size;
                tts_param.NumberOfSpeakers = speaker_params.Length;
                Marshal.StructureToPtr<Aitalk.TtsParam>(tts_param, ptr, false);
                for (int index = 0; index < speaker_params.Length; index++)
                {
                    IntPtr speaker_ptr = IntPtr.Add(ptr, Marshal.SizeOf<Aitalk.TtsParam>() + Marshal.SizeOf<Aitalk.TtsParam.SpeakerParam>() * index);
                    Marshal.StructureToPtr<Aitalk.TtsParam.SpeakerParam>(speaker_params[index], speaker_ptr, false);
                }
                Aitalk.Result result;
                result = Aitalk.SetParam(ptr);
                if (result != Aitalk.Result.Success)
                {
                    throw new AitalkException("動作パラメータの設定に失敗しました。", result);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        /// <summary>
        /// パラメータが更新されていれば反映する
        /// </summary>
        private static void UpdateParameter()
        {
            if (Parameter.IsParameterChanged == true)
            {
                // パラメータを更新する
                SetParameters(Parameter.TtsParam, Parameter.SpeakerParameters);
                Parameter.IsParameterChanged = false;
            }
        }

        /// <summary>
        /// テキストを読み仮名に変換する
        /// </summary>
        /// <param name="text">テキスト</param>
        /// <param name="Timeout">タイムアウト[ms]。0以下はタイムアウト無しで待ち続ける。</param>
        /// <returns>読み仮名文字列</returns>
        public static string TextToKana(string text, int timeout = 0)
        {
            UpdateParameter();

            // ShiftJISに変換する
            UnicodeToShiftJis(text, out byte[] shiftjis_bytes, out int[] shiftjis_to_unicode);

            // コールバックメソッドとの同期オブジェクトを用意する
            KanaJobData job_data = new KanaJobData();
            job_data.BufferCapacity = 0x1000;
            job_data.Output = new List<byte>();
            job_data.CloseEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            GCHandle gc_handle = GCHandle.Alloc(job_data);
            try
            {
                // 変換を開始する
                Aitalk.JobParam job_param;
                job_param.ModeInOut = Aitalk.JobInOut.PlainToKana;
                job_param.UserData = GCHandle.ToIntPtr(gc_handle);
                Aitalk.Result result;
                result = Aitalk.TextToKana(out int job_id, ref job_param, shiftjis_bytes);
                if (result != Aitalk.Result.Success)
                {
                    throw new AitalkException("仮名変換が開始できませんでした。", result);
                }

                // 変換の終了を待つ
                // timeoutで与えられた時間だけ待つ
                bool respond;
                respond = job_data.CloseEvent.WaitOne((0 < timeout) ? timeout : -1);

                // 変換を終了する
                result = Aitalk.CloseKana(job_id);
                if (respond == false)
                {
                    throw new AitalkException("仮名変換がタイムアウトしました。");
                }
                else if (result != Aitalk.Result.Success)
                {
                    throw new AitalkException("仮名変換が正常に終了しませんでした。", result);
                }
            }
            finally
            {
                gc_handle.Free();
            }
            
            // 変換結果に含まれるIrq MARKのバイト位置をUnicodeの文字の位置へ置き換える
            Encoding encoding = Encoding.GetEncoding(932);
            return ReplaceIrqMark(encoding.GetString(job_data.Output.ToArray()), shiftjis_to_unicode);
        }

        /// <summary>
        /// UTF-16からShiftJISに文字列を変換し、文字位置の変換テーブルを生成する。
        /// 変換後のShiftJIS文字列と変換テーブルにはヌル終端の分の要素も含まれる。
        /// </summary>
        /// <param name="unicode_string">UTF-16文字列</param>
        /// <param name="shiftjis_string">ShiftJIS文字列</param>
        /// <param name="shiftjis_to_unicode">ShiftJISとUTF-16のバイト・ワード位置の変換テーブル</param>
        private static void UnicodeToShiftJis(string unicode_string, out byte[] shiftjis_string, out int[] shiftjis_to_unicode)
        {
            // 文字位置とUTF-16上でのワード位置の変換テーブルを取得し、
            // ShiftJIS上でのバイト位置とUTF-16上でのワード位置の変換テーブルを計算する
            Encoding encoding = Encoding.GetEncoding(932);
            byte[] shiftjis_string_internal = encoding.GetBytes(unicode_string);
            int shiftjis_length = shiftjis_string_internal.Length;
            shiftjis_to_unicode = new int[shiftjis_length + 1];
            char[] unicode_char_array = unicode_string.ToArray();
            int[] unicode_indexes = StringInfo.ParseCombiningCharacters(unicode_string);
            int char_count = unicode_indexes.Length;
            int shiftjis_index = 0;
            for (int char_index = 0; char_index < char_count; char_index++)
            {
                int unicode_index = unicode_indexes[char_index];
                int unicode_count = (((char_index + 1) < char_count) ? unicode_indexes[char_index + 1] : unicode_string.Length) - unicode_index;
                int shiftjis_count = encoding.GetByteCount(unicode_char_array, unicode_index, unicode_count);
                for (int offset = 0; offset < shiftjis_count; offset++)
                {
                    shiftjis_to_unicode[shiftjis_index + offset] = unicode_index;
                }
                shiftjis_index += shiftjis_count;
            }
            shiftjis_to_unicode[shiftjis_length] = unicode_string.Length;
            
            // ヌル終端を付け加える
            shiftjis_string = new byte[shiftjis_length + 1];
            Buffer.BlockCopy(shiftjis_string_internal, 0, shiftjis_string, 0, shiftjis_length);
            shiftjis_string[shiftjis_length] = 0;
        }

        /// <summary>
        /// Irq MARKによる文節位置をUTF-16のワード位置に置き換える
        /// </summary>
        /// <param name="input">文字列</param>
        /// <param name="shiftjis_to_unicode">ShiftJISとUTF-16のバイト・ワード位置の変換テーブル</param>
        /// <returns>変換された文字列</returns>
        private static string ReplaceIrqMark(string input, int[] shiftjis_to_unicode)
        {
            StringBuilder output = new StringBuilder();
            int shiftjis_length = shiftjis_to_unicode.Length;
            int index = 0;
            const string StartOfIrqMark = "(Irq MARK=_AI@";
            const string EndOfIrqMask = ")";
            while (true)
            {
                int start_pos = input.IndexOf(StartOfIrqMark, index);
                if (start_pos < 0)
                {
                    output.Append(input, index, input.Length - index);
                    break;
                }
                start_pos += StartOfIrqMark.Length;
                output.Append(input, index, start_pos - index);
                int end_pos = input.IndexOf(EndOfIrqMask, start_pos);
                if (end_pos < 0)
                {
                    output.Append(input, index, input.Length - start_pos);
                    break;
                }
                if (int.TryParse(input.Substring(start_pos, end_pos - start_pos), out int shiftjis_index) == false)
                {
                    throw new AitalkException("文節位置の取得に失敗しました。");
                }
                if ((shiftjis_index < 0) || (shiftjis_length <= shiftjis_index))
                {
                    throw new AitalkException("文節位置の特定に失敗しました。");
                }
                output.Append(shiftjis_to_unicode[shiftjis_index]);
                output.Append(EndOfIrqMask);
                index = end_pos + EndOfIrqMask.Length;
            }
            return output.ToString();
        }

        /// <summary>
        /// 読み仮名変換時のコールバックメソッド
        /// </summary>
        /// <param name="reason">呼び出し要因</param>
        /// <param name="job_id">ジョブID</param>
        /// <param name="user_data">ユーザーデータ(KanaJobDataへのポインタ)</param>
        /// <returns>ゼロを返す</returns>
        private static int TextBufferCallback(Aitalk.EventReason reason, int job_id, IntPtr user_data)
        {
            GCHandle gc_handle = GCHandle.FromIntPtr(user_data);
            KanaJobData job_data = gc_handle.Target as KanaJobData;
            if (job_data == null)
            {
                return 0;
            }

            // 変換できた分だけGetKana()で読み取ってjob_dataのバッファに格納する
            int buffer_capacity = job_data.BufferCapacity;
            byte[] buffer = new byte[buffer_capacity];
            Aitalk.Result result;
            int read_bytes;
            do
            {
                result = Aitalk.GetKana(job_id, buffer, buffer_capacity, out read_bytes, out _);
                if (result != Aitalk.Result.Success)
                {
                    break;
                }
                job_data.Output.AddRange(new ArraySegment<byte>(buffer, 0, read_bytes));
            }
            while ((buffer_capacity - 1) <= read_bytes);
            if (reason == Aitalk.EventReason.TextBufferClose)
            {
                job_data.CloseEvent.Set();
            }
            return 0;
        }

        /// <summary>
        /// 読み仮名を読み上げてWAVEファイルをストリームに出力する。
        /// なお、ストリームへの書き込みは変換がすべて終わった後に行われる。
        /// </summary>
        /// <param name="kana">読み仮名</param>
        /// <param name="wave_stream">WAVEファイルの出力先ストリーム</param>
        /// <param name="timeout">タイムアウト[ms]。0以下はタイムアウト無しで待ち続ける。</param>
        public static void KanaToSpeech(string kana, Stream wave_stream, int timeout = 0)
        {
            UpdateParameter();
            
            // コールバックメソッドとの同期オブジェクトを用意する
            SpeechJobData job_data = new SpeechJobData();
            job_data.BufferCapacity = 176400;
            job_data.Output = new List<byte>();
            job_data.EventData = new List<TtsEventData>();
            job_data.CloseEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            GCHandle gc_handle = GCHandle.Alloc(job_data);
            try
            {
                // 変換を開始する
                Aitalk.JobParam job_param;
                job_param.ModeInOut = Aitalk.JobInOut.KanaToWave;
                job_param.UserData = GCHandle.ToIntPtr(gc_handle);
                Aitalk.Result result;
                result = Aitalk.TextToSpeech(out int job_id, ref job_param, kana);
                if (result != Aitalk.Result.Success)
                {
                    throw new AitalkException("音声変換が開始できませんでした。", result);
                }

                // 変換の終了を待つ
                // timeoutで与えられた時間だけ待つ
                bool respond;
                respond = job_data.CloseEvent.WaitOne((0 < timeout) ? timeout : -1);

                // 変換を終了する
                result = Aitalk.CloseSpeech(job_id);
                if (respond == false)
                {
                    throw new AitalkException("音声変換がタイムアウトしました。");
                }
                else if (result != Aitalk.Result.Success)
                {
                    throw new AitalkException("音声変換が正常に終了しませんでした。", result);
                }
            }
            finally
            {
                gc_handle.Free();
            }

            // TTSイベントをJSONに変換する
            // 変換後の文字列にヌル終端がてら4の倍数の長さになるようパディングを施す
            MemoryStream event_stream = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(List<TtsEventData>));
            serializer.WriteObject(event_stream, job_data.EventData);
            int padding = 4 - ((int)event_stream.Length % 4);
            for(int cnt = 0; cnt < padding; cnt++)
            {
                event_stream.WriteByte(0x0);
            }
            byte[] event_json = event_stream.ToArray();

            // データをWAVE形式で出力する
            // phonチャンクとしてTTSイベントを埋め込む
            byte[] data = job_data.Output.ToArray();
            var writer = new BinaryWriter(wave_stream);
            writer.Write(new byte[4] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            writer.Write(44 + event_json.Length + data.Length);
            writer.Write(new byte[4] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
            writer.Write(new byte[4] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            writer.Write(16);
            writer.Write((short)0x1);
            writer.Write((short)1);
            writer.Write(VoiceSampleRate);
            writer.Write(2 * VoiceSampleRate);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new byte[4] { (byte)'p', (byte)'h', (byte)'o', (byte)'n' });
            writer.Write(event_json.Length);
            writer.Write(event_json);
            writer.Write(new byte[4] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            writer.Write(data.Length);
            writer.Write(data);
        }

        /// <summary>
        /// 音声変換時のデータコールバックメソッド
        /// </summary>
        /// <param name="reason">呼び出し要因</param>
        /// <param name="job_id">ジョブID</param>
        /// <param name="tick">時刻[ms]</param>
        /// <param name="user_data">ユーザーデータ(SpeechJobDataへのポインタ)</param>
        /// <returns>ゼロを返す</returns>
        private static int RawBufferCallback(Aitalk.EventReason reason, int job_id, long tick, IntPtr user_data)
        {
            GCHandle gc_handle = GCHandle.FromIntPtr(user_data);
            SpeechJobData job_data = gc_handle.Target as SpeechJobData;
            if (job_data == null)
            {
                return 0;
            }

            // 変換できた分だけGetData()で読み取ってjob_dataのバッファに格納する
            int buffer_capacity = job_data.BufferCapacity;
            byte[] buffer = new byte[2 * buffer_capacity];
            Aitalk.Result result;
            int read_samples;
            do
            {
                result = Aitalk.GetData(job_id, buffer, buffer_capacity, out read_samples);
                if (result != Aitalk.Result.Success)
                {
                    break;
                }
                job_data.Output.AddRange(new ArraySegment<byte>(buffer, 0, 2 * read_samples));
            }
            while ((buffer_capacity - 1) <= read_samples);
            if (reason == Aitalk.EventReason.RawBufferClose)
            {
                job_data.CloseEvent.Set();
            }
            return 0;
        }

        /// <summary>
        /// 音声変換時のイベントコールバックメソッド
        /// </summary>
        /// <param name="reason">呼び出し要因</param>
        /// <param name="job_id">ジョブID</param>
        /// <param name="tick">時刻[ms]</param>
        /// <param name="name">イベントの値</param>
        /// <param name="user_data">ユーザーデータ(SpeechJobDataへのポインタ)</param>
        /// <returns>ゼロを返す</returns>
        private static int TtsEventCallback(Aitalk.EventReason reason, int job_id, long tick, string name, IntPtr user_data)
        {
            GCHandle gc_handle = GCHandle.FromIntPtr(user_data);
            SpeechJobData job_data = gc_handle.Target as SpeechJobData;
            if (job_data == null)
            {
                return 0;
            }
            switch (reason)
            {
            case Aitalk.EventReason.PhoneticLabel:
            case Aitalk.EventReason.Bookmark:
            case Aitalk.EventReason.AutoBookmark:
                job_data.EventData.Add(new TtsEventData(tick, name, reason));
                break;
            }
            return 0;
        }
        
        /// <summary>
        /// パラメータ
        /// </summary>
        public static AitalkParameter Parameter { get; private set; }

        /// <summary>
        /// 仮名変換のジョブを管理するクラス
        /// </summary>
        private class KanaJobData
        {
            public int BufferCapacity;
            public List<byte> Output;
            public EventWaitHandle CloseEvent;
        }

        /// <summary>
        /// 音声変換のジョブを管理するクラス
        /// </summary>
        private class SpeechJobData
        {
            public int BufferCapacity;
            public List<byte> Output;
            public List<TtsEventData> EventData;
            public EventWaitHandle CloseEvent;
        }

        /// <summary>
        /// TTSイベントのデータを格納する構造体
        /// </summary>
        [DataContract]
        public struct TtsEventData
        {
            [DataMember]
            public long Tick;

            [DataMember]
            public string Value;

            [DataMember]
            public string Type;

            public TtsEventData(long tick, string value, Aitalk.EventReason reason)
            {
                Tick = tick;
                Value = value;
                switch (reason)
                {
                case Aitalk.EventReason.PhoneticLabel:
                    Type = "Phonetic";
                    break;
                case Aitalk.EventReason.Bookmark:
                    Type = "Bookmark";
                    break;
                case Aitalk.EventReason.AutoBookmark:
                    Type = "AutoBookmark";
                    break;
                default:
                    Type = "";
                    break;
                }
            }
        }
        
        /// <summary>
        /// ボイスライブラリのサンプルレート[Hz]
        /// </summary>
        private const int VoiceSampleRate = 44100;

        /// <summary>
        /// AITalkのタイムアウト[ms]
        /// </summary>
        private const int TimeoutMilliseconds = 1000;

        [DllImport("Kernel32.dll")]
        private static extern bool SetDllDirectory(string lpPathName);
    }

    /// <summary>
    /// AitalkWrapperの例外クラス
    /// </summary>
    [Serializable]
    public class AitalkException : Exception
    {
        public AitalkException() { }

        public AitalkException(string message)
            : base(message) { }

        public AitalkException(string message, Aitalk.Result result)
            : base($"{message}({result})") { }

        public AitalkException(string message, Exception inner)
            : base(message, inner) { }

        protected AitalkException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
