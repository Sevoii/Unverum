using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Unverum;

public class UE4SSDownloader
{
    private bool cancelled;

    private ProgressBox progressBox;

    private HttpClient client = new();

    private CancellationTokenSource cancellationToken = new();

    private string UE4SSUrl =
        "https://github.com/Sevoii/Unverum/raw/refs/heads/master/Unverum/Dependencies/ue4ss/UE4SS.dll";

    private string dwmmapiUrl =
        "https://github.com/Sevoii/Unverum/raw/refs/heads/master/Unverum/Dependencies/ue4ss/dwmapi.dll";

    private async Task DownloadFile(string uri, string fileName, Progress<DownloadProgress> progress,
        CancellationTokenSource cancellationToken)
    {
        try
        {
            // Create the downloads folder if necessary
            Directory.CreateDirectory($@"{Global.assemblyLocation}{Global.s}Downloads");
            // Download the file if it doesn't already exist
            if (File.Exists($@"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}ue4ss{Global.s}{fileName}"))
            {
                try
                {
                    File.Delete($@"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}ue4ss{Global.s}{fileName}");
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        $"Couldn't delete the already existing {Global.assemblyLocation}{Global.s}Dependencies{Global.s}ue4ss{Global.s}{fileName} ({e.Message})",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            progressBox = new ProgressBox(cancellationToken);
            progressBox.progressBar.Value = 0;
            progressBox.finished = false;
            progressBox.Title = $"Download Progress";
            progressBox.Show();
            progressBox.Activate();
            // Write and download the file
            using (var fs = new FileStream(
                       $@"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}ue4ss{Global.s}{fileName}", FileMode.Create,
                       FileAccess.Write, FileShare.None))
            {
                await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
            }

            progressBox.Close();
        }
        catch (OperationCanceledException)
        {
            // Remove the file is it will be a partially downloaded one and close up
            File.Delete($@"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}ue4ss{Global.s}{fileName}");
            if (progressBox != null)
            {
                progressBox.finished = true;
                progressBox.Close();
                cancelled = true;
            }

            return;
        }
        catch (Exception e)
        {
            if (progressBox != null)
            {
                progressBox.finished = true;
                progressBox.Close();
            }

            MessageBox.Show($"Error whilst downloading {fileName}. {e.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            cancelled = true;
        }
    }

    private void ReportUpdateProgress(DownloadProgress progress)
    {
        if (progress.Percentage == 1)
        {
            progressBox.finished = true;
        }

        progressBox.progressBar.Value = progress.Percentage * 100;
        progressBox.taskBarItem.ProgressValue = progress.Percentage;
        progressBox.progressTitle.Text = $"Downloading {progress.FileName}...";
        progressBox.progressText.Text = $"{Math.Round(progress.Percentage * 100, 2)}% " +
                                        $"({StringConverters.FormatSize(progress.DownloadedBytes)} of {StringConverters.FormatSize(progress.TotalBytes)})";
    }

    public async Task DownloadUE4SS()
    {
        await DownloadFile(UE4SSUrl, "UE4SS.dll", new Progress<DownloadProgress>(ReportUpdateProgress),
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
    }

    public async Task DownloadDwmapi()
    {
        await DownloadFile(dwmmapiUrl, "dwmapi.dll", new Progress<DownloadProgress>(ReportUpdateProgress),
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
    }

    public async void UpdateFiles()
    {
        await DownloadUE4SS();
        await DownloadDwmapi();
    }
}