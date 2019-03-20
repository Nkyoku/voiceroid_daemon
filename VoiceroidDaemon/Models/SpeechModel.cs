using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceroidDaemon.Models
{
    /// <summary>
    /// 読み上げパラメータを格納するモデル
    /// </summary>
    public class SpeechModel
    {
        /// <summary>
        /// 読み上げるテキスト
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 読み上げる読み仮名
        /// </summary>
        public string Kana { get; set; }

        /// <summary>
        /// 話者のパラメータ
        /// </summary>
        public SpeakerModel Speaker { get; set; }
    }
}
