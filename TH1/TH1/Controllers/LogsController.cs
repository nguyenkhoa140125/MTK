using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TH1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private static readonly Regex LogLineRegex = new Regex(
            @"^\[(?<ts>\d{2}-\d{2}-\d{4}\s+\d{2}:\d{2}:\d{2})\]\s*(?<msg>.*)$",
            RegexOptions.Compiled);

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int take = 100)
        {
            take = Math.Clamp(take, 1, 200);

            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "log.txt");
            if (!System.IO.File.Exists(logPath))
            {
                return Ok(Array.Empty<object>());
            }

            var lines = await System.IO.File.ReadAllLinesAsync(logPath);
            var last = lines.Skip(Math.Max(0, lines.Length - take)).ToArray();

            var result = last
                .Select(line =>
                {
                    var m = LogLineRegex.Match(line);
                    if (!m.Success)
                    {
                        return new
                        {
                            Timestamp = (DateTime?)null,
                            Message = line,
                            Raw = line
                        };
                    }

                    DateTime? ts = null;
                    if (DateTime.TryParseExact(
                        m.Groups["ts"].Value,
                        "dd-MM-yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsed))
                    {
                        ts = parsed;
                    }

                    return new
                    {
                        Timestamp = ts,
                        Message = m.Groups["msg"].Value,
                        Raw = line
                    };
                })
                .ToArray();

            return Ok(result);
        }
    }
}

