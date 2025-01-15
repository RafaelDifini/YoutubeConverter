using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;

public class YoutubeController : Controller
{
    private static string CurrentProgress = "0%";

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Convert(YoutubeVideo video)
    {
        if (string.IsNullOrEmpty(video.VideoUrl))
        {
            ViewBag.Error = "Insira um link válido do YouTube.";
            return View("Index");
        }

        string tempPath = Path.GetTempPath();
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputFileName = $"video_{timestamp}.mp4";
        string outputPath = Path.Combine(tempPath, outputFileName);

        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        try
        {
            var ytDlpPath = "/venv/bin/yt-dlp";

            if (!System.IO.File.Exists(ytDlpPath))
            {
                throw new FileNotFoundException($"O executável yt-dlp não foi encontrado em: {ytDlpPath}");
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = $"-f bestvideo+bestaudio --merge-output-format mp4 --progress \"{video.VideoUrl}\" -o \"{outputPath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = processInfo };

            process.Start();

            using (var reader = process.StandardError)
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.Contains("[download]"))
                    {
                        var match = Regex.Match(line, @"\d+\.\d+%");
                        if (match.Success)
                        {
                            CurrentProgress = match.Value;
                        }
                    }
                }
            }

            CurrentProgress = "100%";

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception("Erro ao converter o vídeo.");
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Erro: {ex.Message}";
            return View("Index");
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
        var fileName = Path.GetFileName(outputPath);

        return File(fileBytes, "application/octet-stream", fileName);
    }

    [HttpGet]
    public IActionResult Progress()
    {
        return Json(new { progress = CurrentProgress });
    }

    [HttpGet]
    public IActionResult Download()
    {
        string tempPath = Path.GetTempPath();
        string[] files = Directory.GetFiles(tempPath, "video_*.mp4");
        string latestFile = files.OrderByDescending(f => new FileInfo(f).CreationTime).FirstOrDefault();

        if (latestFile == null)
        {
            return NotFound("Arquivo não encontrado.");
        }

        var fileBytes = System.IO.File.ReadAllBytes(latestFile);
        var fileName = Path.GetFileName(latestFile);

        return File(fileBytes, "application/octet-stream", fileName);
    }
}
