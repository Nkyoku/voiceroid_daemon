using System;
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
    /// テキストを読み仮名変換するコントローラ
    /// </summary>
    [Route("api/converttext")]
    [ApiController]
    public class ConvertTextApiController : ControllerBase
    {
        /// <summary>
        /// リクエストで与えられたテキストを仮名変換する
        /// </summary>
        /// <param name="text">仮名変換するテキスト</param>
        /// <returns></returns>
        [HttpGet("{text}")]
        public string ConvertTextFromRequest(string text)
        {
            SpeechModel model = new SpeechModel();
            model.Text = text;
            return ConvertTextFromPost(model);
        }

        /// <summary>
        /// POSTで与えられたテキストを仮名変換する
        /// </summary>
        /// <param name="speech_model">仮名変換するテキストが含まれたパラメータ</param>
        /// <returns></returns>
        [HttpPost]
        public string ConvertTextFromPost([FromBody] SpeechModel speech_model)
        {
            Setting.Lock();
            try
            {
                if ((speech_model.Text == null) || (speech_model.Text.Length <= 0))
                {
                    return null;
                }
                return AitalkWrapper.TextToKana(speech_model.Text, Setting.System.KanaTimeout);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                Setting.Unlock();
            }
        }
    }
}