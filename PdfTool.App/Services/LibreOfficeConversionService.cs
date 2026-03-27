using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PdfTool.App.Services;

/// <summary>
/// Invokes a portable LibreOffice placed under Tools/LibreOffice (relative to the app base directory).
/// </summary>
internal sealed class LibreOfficeConversionService
{
    private readonly string _baseDirectory;

    public LibreOfficeConversionService()
    {
        _baseDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public string? FindSofficeExecutable()
    {
        foreach (var candidate in GetCandidatePaths())
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Converts a Word document to PDF using headless LibreOffice. Output PDF name matches the input file base name.
    /// </summary>
    public ConversionResult ConvertWordToPdf(string inputDocxPath, string outputDirectory)
    {
        var soffice = FindSofficeExecutable();
        if (soffice == null)
        {
            return ConversionResult.Fail(
                "未找到 LibreOffice。请将便携版解压到应用程序目录下的 Tools\\LibreOffice（详见该文件夹内说明）。");
        }

        Directory.CreateDirectory(outputDirectory);
        var fullInput = Path.GetFullPath(inputDocxPath);
        var outDir = Path.GetFullPath(outputDirectory);

        var args =
            $"--headless --nologo --nofirststartwizard --norestore --convert-to pdf --outdir \"{outDir}\" \"{fullInput}\"";

        var psi = new ProcessStartInfo
        {
            FileName = soffice,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(soffice) ?? _baseDirectory
        };

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null)
                return ConversionResult.Fail("无法启动 LibreOffice 进程。");

            proc.WaitForExit(TimeSpan.FromMinutes(5));
            var exit = proc.ExitCode;
            if (exit != 0)
            {
                var err = proc.StandardError.ReadToEnd();
                return ConversionResult.Fail($"LibreOffice 退出码 {exit}。{err}");
            }
        }
        catch (Exception ex)
        {
            return ConversionResult.Fail(ex.Message);
        }

        var expectedPdf = Path.Combine(outDir, Path.GetFileNameWithoutExtension(fullInput) + ".pdf");
        if (!File.Exists(expectedPdf))
            return ConversionResult.Fail("转换完成但未找到输出的 PDF 文件。");

        return ConversionResult.Ok(expectedPdf);
    }

    private IEnumerable<string> GetCandidatePaths()
    {
        var tools = Path.Combine(_baseDirectory, "Tools", "LibreOffice");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return Path.Combine(tools, "program", "soffice.com");
            yield return Path.Combine(tools, "program", "soffice.exe");
            yield return Path.Combine(tools, "App", "libreoffice", "program", "soffice.com");
            yield return Path.Combine(tools, "App", "libreoffice", "program", "soffice.exe");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return Path.Combine(tools, "LibreOffice.app", "Contents", "MacOS", "soffice");
            yield return Path.Combine(tools, "Contents", "MacOS", "soffice");
        }
        else
        {
            yield return Path.Combine(tools, "program", "soffice");
        }
    }

    internal readonly record struct ConversionResult(bool Success, string? Message, string? OutputPdfPath)
    {
        public static ConversionResult Ok(string pdfPath) => new(true, null, pdfPath);
        public static ConversionResult Fail(string message) => new(false, message, null);
    }
}
