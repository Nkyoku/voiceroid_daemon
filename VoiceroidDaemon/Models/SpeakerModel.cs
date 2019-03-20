using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aitalk;

namespace VoiceroidDaemon.Models
{
    public class SpeakerModel
    {
        /// <summary>
        /// 音量(0～2)
        /// </summary>
        public double Volume { get; set; } = double.NaN;

        /// <summary>
        /// 話速(0.5～4)
        /// </summary>
        public double Speed { get; set; } = double.NaN;

        /// <summary>
        /// 高さ(0.5～4)
        /// </summary>
        public double Pitch { get; set; } = double.NaN;

        /// <summary>
        /// 抑揚(0～2)
        /// </summary>
        public double Emphasis { get; set; } = double.NaN;

        /// <summary>
        /// 短ポーズ時間[ms] (80～500)。PauseLong以下。
        /// </summary>
        public int PauseMiddle { get; set; } = -1;

        /// <summary>
        /// 長ポーズ時間[ms] (100～2000)。PauseMiddle以上。
        /// </summary>
        public int PauseLong { get; set; } = -1;

        /// <summary>
        /// 文末ポーズ時間[ms] (0～10000)
        /// </summary>
        public int PauseSentence { get; set; } = -1;
    }
}
