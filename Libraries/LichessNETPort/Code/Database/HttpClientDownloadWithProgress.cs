#nullable enable annotations

namespace LichessNET.Database;

internal class HttpClientDownloadWithProgress : IDisposable
{
    public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded,
        double? progressPercentage);

    public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
    {
    }

    public event ProgressChangedHandler ProgressChanged;

    public Task StartDownload()
    {
        ProgressChanged?.Invoke(0, 0, 0);
        throw new NotSupportedException("Lichess database file downloads are disabled in the whitelist-safe s&box port.");
    }

    public void Dispose()
    {
    }
}
