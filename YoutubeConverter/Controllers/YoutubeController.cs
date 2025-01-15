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

        string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "temp");
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputFileName = $"video_{timestamp}.mp4";
        string outputPath = Path.Combine(tempPath, outputFileName);

        if (Directory.Exists(tempPath))
        {
            try
            {
                string[] oldFiles = Directory.GetFiles(tempPath);
                foreach (var file in oldFiles)
                {
                    System.IO.File.Delete(file);
                    Console.WriteLine($"Arquivo antigo removido: {file}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao limpar arquivos antigos na pasta temp: {ex.Message}");
            }
        }
        else
        {
            Directory.CreateDirectory(tempPath);
        }

        try
        {
            string ytDlpPath;
            string ffmpegLocation;

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                ytDlpPath = Path.Combine(AppContext.BaseDirectory, "Resources", "yt-dlp.exe");
                ffmpegLocation = @"C:\Users\Rafae\Downloads\ffmpeg-master-latest-win64-gpl\bin";
                if (!System.IO.File.Exists(ytDlpPath))
                {
                    throw new FileNotFoundException($"O executável yt-dlp não foi encontrado no caminho: {ytDlpPath}");
                }

            }
            else
            {
                ytDlpPath = "/venv/bin/yt-dlp";
                ffmpegLocation = "/usr/bin";
            }

            Console.WriteLine($"Executável yt-dlp: {ytDlpPath}");

            var processInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = $"-f \"bestvideo+bestaudio\" --ffmpeg-location \"{ffmpegLocation}\"  --merge-output-format mp4  chromium --browser-profile \"/root/.config/chromium/Default\"  --progress --print-json \"{video.VideoUrl}\" -o \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Console.WriteLine($"Comando yt-dlp: {processInfo.FileName} {processInfo.Arguments}");

            var process = new Process { StartInfo = processInfo };
            process.Start();

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    Console.WriteLine(args.Data);

                    if (args.Data.Contains("[download]"))
                    {
                        var match = Regex.Match(args.Data, @"\d+\.\d+%");
                        if (match.Success)
                        {
                            CurrentProgress = match.Value;
                        }
                    }
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    Console.WriteLine($"[ERROR] {args.Data}");
                }
            };


            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Erro ao converter o vídeo. Código de saída: {process.ExitCode}");
            }

            CurrentProgress = "100%";

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Erro ao converter o vídeo. Código de saída: {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na conversão: {ex.Message}");
            ViewBag.Error = $"Erro: {ex.Message}";
            return View("Index");
        }

        Console.WriteLine($"Arquivo convertido: {outputPath}");

        try
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
            var fileName = Path.GetFileName(outputPath);

            return File(fileBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao ler o arquivo convertido: {ex.Message}");
            return View("Index", $"Erro ao acessar o arquivo convertido: {ex.Message}");
        }
    }

    [HttpGet]
    public IActionResult Download()
    {
        string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "temp");

        if (!Directory.Exists(tempPath))
        {
            Console.WriteLine("Diretório temporário não encontrado.");
            return NotFound("Diretório de arquivos temporários não encontrado.");
        }

        string[] files = Directory.GetFiles(tempPath, "video_*.mp4");

        if (files.Length == 0)
        {
            Console.WriteLine("Nenhum arquivo encontrado no diretório temporário.");
            return NotFound("Nenhum arquivo de vídeo encontrado.");
        }

        string latestFile = files.OrderByDescending(f => new FileInfo(f).CreationTime).First();

        Console.WriteLine($"Arquivo mais recente encontrado: {latestFile}");

        try
        {
            var fileStream = new FileStream(latestFile, FileMode.Open, FileAccess.Read, FileShare.None);
            var fileName = Path.GetFileName(latestFile);

            Response.OnCompleted(() =>
            {
                LimparPastaTemp(tempPath);
                return Task.CompletedTask;
            });

            return File(fileStream, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao ler o arquivo para download: {ex.Message}");
            return NotFound($"Erro ao acessar o arquivo para download: {ex.Message}");
        }
    }

    private void LimparPastaTemp(string tempPath)
    {
        try
        {
            string[] allFiles = Directory.GetFiles(tempPath);
            foreach (var file in allFiles)
            {
                System.IO.File.Delete(file);
                Console.WriteLine($"Arquivo removido: {file}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao limpar a pasta temp: {ex.Message}");
        }
    }




    [HttpGet]
    public IActionResult Progress()
    {
        return Json(new { progress = CurrentProgress });
    }


}
