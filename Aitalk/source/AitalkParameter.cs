using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Aitalk
{
    public class AitalkParameter
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voice_db_name">ボイスライブラリ名</param>
        /// <param name="tts_param">パラメータ</param>
        /// <param name="speaker_params">話者のパラメータリスト</param>
        internal AitalkParameter(string voice_db_name, AitalkCore.TtsParam tts_param, AitalkCore.TtsParam.SpeakerParam[] speaker_params)
        {
            VoiceDbName = voice_db_name;
            TtsParam = tts_param;
            SpeakerParameters = speaker_params;
            CurrentVoiceName = SpeakerParameters[0].VoiceName;
        }

        /// <summary>
        /// JSON形式のバイト列に変換する
        /// </summary>
        /// <returns>JSON形式のバイト列</returns>
        public byte[] ToJson()
        {
            // 一時的な構造体にパラメータを格納する
            ParameterJson parameter;
            parameter.VoiceDbName = VoiceDbName;
            parameter.Speakers = SpeakerParameters;

            // JSONにシリアライズする
            using (var stream = new MemoryStream())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  "))
                {
                    var serializer = new DataContractJsonSerializer(typeof(ParameterJson));
                    serializer.WriteObject(writer, parameter);
                    writer.Flush();
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// パラメータが変更されたときにtrueになる
        /// </summary>
        public bool IsParameterChanged { get; internal set; } = true;
        
        /// <summary>
        /// 仮名変換時のバッファサイズ(バイト数)
        /// </summary>
        public int TextBufferCapacityInBytes
        {
            get { return TtsParam.TextBufferCapacityInBytes; }
        }

        /// <summary>
        /// 音声変換時のバッファサイズ(バイト数)
        /// </summary>
        public int RawBufferCapacityInBytes
        {
            get { return TtsParam.RawBufferCapacityInBytes; }
        }
        
        /// <summary>
        /// trueのとき仮名変換結果に文節終了位置を埋め込む
        /// </summary>
        public bool AutoBookmark
        {
            get { return (TtsParam.ExtendFormatFlags & AitalkCore.ExtendFormat.AutoBookmark) != 0; }
            set
            {
                IsParameterChanged |= (value != AutoBookmark);
                if (value == true)
                {
                    TtsParam.ExtendFormatFlags |= AitalkCore.ExtendFormat.AutoBookmark;
                }
                else
                {
                    TtsParam.ExtendFormatFlags &= ~AitalkCore.ExtendFormat.AutoBookmark;
                }
                
            }
        }

        /// <summary>
        /// trueのとき仮名変換結果にJEITA規格のルビを使う
        /// </summary>
        public bool JeitaRuby
        {
            get { return (TtsParam.ExtendFormatFlags & AitalkCore.ExtendFormat.JeitaRuby) != 0; }
            set
            {
                IsParameterChanged |= (value != JeitaRuby);
                if (value == true)
                {
                    TtsParam.ExtendFormatFlags |= AitalkCore.ExtendFormat.JeitaRuby;
                }
                else
                {
                    TtsParam.ExtendFormatFlags &= ~AitalkCore.ExtendFormat.JeitaRuby;
                }
            }
        }

        /// <summary>
        /// マスター音量(0～5)
        /// </summary>
        public double MasterVolume
        {
            get { return TtsParam.Volume; }
            set
            {
                if ((value < MinMasterVolume) || (MaxMasterVolume < value))
                {
                    throw new AitalkException("マスター音量が範囲外です。");
                }
                IsParameterChanged |= (MasterVolume != value);
                TtsParam.Volume = (float)Math.Max(MinMasterVolume, Math.Min(value, MaxMasterVolume));
            }
        }
        public const double MaxMasterVolume = 5.0;
        public const double MinMasterVolume = 0.0;

        /// <summary>
        /// 話者の名前のリスト
        /// </summary>
        public string[] VoiceNames
        {
            get { return SpeakerParameters.Select(x => x.VoiceName).ToArray(); }
        }

        /// <summary>
        /// 選択中の話者
        /// </summary>
        public string CurrentVoiceName
        {
            get { return TtsParam.VoiceName; }
            set
            {
                if (TtsParam.VoiceName == value)
                {
                    return;
                }
                var speaker_parameter = SpeakerParameters.FirstOrDefault(x => x.VoiceName == value);
                if (speaker_parameter == null)
                {
                    throw new AitalkException($"話者'{value}'は存在しません。");
                }
                CurrentSpeakerParameter = speaker_parameter;
                TtsParam.VoiceName = value;
                IsParameterChanged = true;
            }
        }
        
        /// <summary>
        /// 音量(0～2)
        /// </summary>
        public double VoiceVolume
        {
            get { return CurrentSpeakerParameter.Volume; }
            set
            {
                if ((value < MinVoiceVolume) || (MaxVoiceVolume < value))
                {
                    throw new AitalkException("音量が範囲外です。");
                }
                IsParameterChanged |= (VoiceVolume != value);
                CurrentSpeakerParameter.Volume = (float)value;
            }
        }
        public const double MinVoiceVolume = 0.0;
        public const double MaxVoiceVolume = 2.0;

        /// <summary>
        /// 話速(0.5～4)
        /// </summary>
        public double VoiceSpeed
        {
            get { return CurrentSpeakerParameter.Speed; }
            set
            {
                if ((value < MinVoiceSpeed) || (MaxVoiceSpeed < value))
                {
                    throw new AitalkException("話速が範囲外です。");
                }
                IsParameterChanged |= (VoiceSpeed != value);
                CurrentSpeakerParameter.Speed = (float)value;
            }
        }
        public const double MinVoiceSpeed = 0.5;
        public const double MaxVoiceSpeed = 4.0;

        /// <summary>
        /// 高さ(0.5～4)
        /// </summary>
        public double VoicePitch
        {
            get { return CurrentSpeakerParameter.Pitch; }
            set
            {
                if ((value < MinVoicePitch) || (MaxVoicePitch < value))
                {
                    throw new AitalkException("高さが範囲外です。");
                }
                IsParameterChanged |= (VoicePitch != value);
                CurrentSpeakerParameter.Pitch = (float)value;
            }
        }
        public const double MinVoicePitch = 0.5;
        public const double MaxVoicePitch = 4.0;

        /// <summary>
        /// 抑揚(0～2)
        /// </summary>
        public double VoiceEmphasis
        {
            get { return CurrentSpeakerParameter.Range; }
            set
            {
                if ((value < MinVoiceEmphasis) || (MaxVoiceEmphasis < value))
                {
                    throw new AitalkException("抑揚が範囲外です。");
                }
                IsParameterChanged |= (VoiceEmphasis != value);
                CurrentSpeakerParameter.Range = (float)value;
            }
        }
        public const double MinVoiceEmphasis = 0.0;
        public const double MaxVoiceEmphasis = 2.0;

        /// <summary>
        /// 短ポーズ時間[ms] (80～500)
        /// </summary>
        public int PauseMiddle
        {
            get { return CurrentSpeakerParameter.PauseMiddle; }
            set
            {
                if ((value < MinPauseMiddle) || (MaxPauseMiddle < value))
                {
                    throw new AitalkException("短ポーズ時間が範囲外です。");
                }
                IsParameterChanged |= (PauseMiddle != value);
                CurrentSpeakerParameter.PauseMiddle = value;
            }
        }
        public const int MinPauseMiddle = 80;
        public const int MaxPauseMiddle = 500;

        /// <summary>
        /// 長ポーズ時間[ms] (100～2000)
        /// </summary>
        public int PauseLong
        {
            get { return CurrentSpeakerParameter.PauseLong; }
            set
            {
                if ((value < MinPauseLong) || (MaxPauseLong < value))
                {
                    throw new AitalkException("長ポーズ時間が範囲外です。");
                }
                IsParameterChanged |= (PauseLong != value);
                CurrentSpeakerParameter.PauseLong = value;
            }
        }
        public const int MinPauseLong = 100;
        public const int MaxPauseLong = 2000;

        /// <summary>
        /// 文末ポーズ時間[ms] (0～10000)
        /// </summary>
        public int PauseSentence
        {
            get { return CurrentSpeakerParameter.PauseSentence; }
            set
            {
                if ((value < MinPauseSentence) || (MaxPauseSentence < value))
                {
                    throw new AitalkException("文末ポーズ時間が範囲外です。");
                }
                IsParameterChanged |= (PauseSentence != value);
                CurrentSpeakerParameter.PauseSentence = value;
            }
        }
        public const int MinPauseSentence = 0;
        public const int MaxPauseSentence = 10000;

        /// <summary>
        /// ボイスライブラリ名
        /// </summary>
        internal string VoiceDbName;

        /// <summary>
        /// TTSパラメータ
        /// </summary>
        internal AitalkCore.TtsParam TtsParam;

        /// <summary>
        /// 話者パラメータのリスト
        /// </summary>
        internal AitalkCore.TtsParam.SpeakerParam[] SpeakerParameters;

        /// <summary>
        /// 選択されている話者のパラメータ
        /// </summary>
        private AitalkCore.TtsParam.SpeakerParam CurrentSpeakerParameter;

        /// <summary>
        /// JSONに変換するときに一時的に詰める構造体
        /// </summary>
        [DataContract]
        private struct ParameterJson
        {
            [DataMember]
            public string VoiceDbName;

            [DataMember]
            public AitalkCore.TtsParam.SpeakerParam[] Speakers;
        }
    }
}
