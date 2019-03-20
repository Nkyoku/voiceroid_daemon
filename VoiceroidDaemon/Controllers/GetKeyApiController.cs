using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VoiceroidDaemon.Models;

namespace VoiceroidDaemon.Controllers
{
    /// <summary>
    /// テキストを読み仮名変換するコントローラ
    /// </summary>
    [Route("api/getkey")]
    [ApiController]
    public class GetKeyApiController : ControllerBase
    {
        /// <summary>
        /// 起動中のVOICEROID2エディタから認証コードを取得する
        /// </summary>
        /// <returns></returns>
        [HttpGet("{exe}")]
        public string ConvertTextFromRequest(string exe)
        {
            try
            {
                ProcessStartInfo start_info = new ProcessStartInfo();
                start_info.FileName = "Injecter/Injecter.exe";
                start_info.Arguments = exe ?? Setting.System.VoiceroidEditorExe;
                start_info.CreateNoWindow = true;
                start_info.RedirectStandardOutput = true;
                using (Process process = Process.Start(start_info))
                {
                    process.WaitForExit(10000);
                    if (process.HasExited == true)
                    {
                        return process.StandardOutput.ReadToEnd();
                    }
                    else
                    {
                        process.Kill();
                    }
                }
            }
            catch (Exception) { }
            return null;
        }
    }
}