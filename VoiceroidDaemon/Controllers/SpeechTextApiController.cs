using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VoiceroidDaemon.Models;
using Aitalk;

namespace VoiceroidDaemon.Controllers
{
    /// <summary>
    /// テキストを音声変換するコントローラ
    /// </summary>
    [Route("api/speechtext")]
    [ApiController]
    public class SpeechTextApiController : ControllerBase
    {
        /// <summary>
        /// リクエストで与えられたテキストを音声変換する
        /// </summary>
        /// <param name="text">読み上げるテキスト</param>
        /// <returns></returns>
        [HttpGet("{text}")]
        public IActionResult SpeectTextFromRequest(string text)
        {
            SpeechModel model = new SpeechModel();
            model.Text = text;
            return SpeectTextFromPost(model);
        }

        /// <summary>
        /// POSTで与えられたテキストを音声変換する
        /// </summary>
        /// <param name="speech_model">読み上げるテキストとパラメータ</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult SpeectTextFromPost([FromBody] SpeechModel speech_model)
        {
            Setting.Lock();
            try
            {
                // 話者パラメータを設定する
                var speaker = speech_model.Speaker ?? new SpeakerModel();
                AitalkWrapper.Parameter.VoiceVolume = (0 <= speaker.Volume) ? speaker.Volume : Setting.DefaultSpeakerParameter.Volume;
                AitalkWrapper.Parameter.VoiceSpeed = (0 <= speaker.Speed) ? speaker.Speed : Setting.DefaultSpeakerParameter.Speed;
                AitalkWrapper.Parameter.VoicePitch = (0 <= speaker.Pitch) ? speaker.Pitch : Setting.DefaultSpeakerParameter.Pitch;
                AitalkWrapper.Parameter.VoiceEmphasis = (0 <= speaker.Emphasis) ? speaker.Emphasis : Setting.DefaultSpeakerParameter.Emphasis;
                AitalkWrapper.Parameter.PauseMiddle = (0 <= speaker.PauseMiddle) ? speaker.PauseMiddle : Setting.DefaultSpeakerParameter.PauseMiddle;
                AitalkWrapper.Parameter.PauseLong = (0 <= speaker.PauseLong) ? speaker.PauseLong : Setting.DefaultSpeakerParameter.PauseLong;
                AitalkWrapper.Parameter.PauseSentence = (0 <= speaker.PauseSentence) ? speaker.PauseSentence : Setting.DefaultSpeakerParameter.PauseSentence;

                // テキストが与えられた場合は仮名に変換する
                string kana = null;
                if ((speech_model.Kana != null) && (0 < speech_model.Kana.Length))
                {
                    kana = speech_model.Kana;
                }
                else if ((speech_model.Text != null) && (0 < speech_model.Text.Length))
                {
                    kana = AitalkWrapper.TextToKana(speech_model.Text, Setting.System.KanaTimeout);
                }
                if ((kana == null) || (kana.Length <= 0))
                {
                    return new NoContentResult();
                }
                
                // 音声変換して結果を返す
                var wave_stream = new MemoryStream();
                AitalkWrapper.KanaToSpeech(kana, wave_stream, Setting.System.SpeechTimeout);
                return new FileContentResult(wave_stream.ToArray(), "audio/wav");
            }
            catch(Exception)
            {
                return new NoContentResult();
            }
            finally
            {
                Setting.Unlock();
            }
        }
    }
}