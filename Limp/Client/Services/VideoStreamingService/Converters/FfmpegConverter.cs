using System.Text;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using FFmpegBlazor;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService.Converters;

public class FfmpegConverter
{
    private Func<Task<string>> PostConvertationTask { get; set; }
    private string videoId { get; set; }
    private FFMPEG ff { get; set; }
    private readonly IJSRuntime _jsRuntime;
    private readonly ICallbackExecutor _callbackExecutor;

    public FfmpegConverter(IJSRuntime jsRuntime, ICallbackExecutor callbackExecutor)
    {
        videoId = Guid.NewGuid().ToString();
        _jsRuntime = jsRuntime;
        _callbackExecutor = callbackExecutor;
    }

    private async Task<FFMPEG> InitializeFfAsync()
    {
        ff = FFmpegFactory.CreateFFmpeg(new FFmpegConfig() { Log = true });
        await ff.Load();
        if (!ff.IsLoaded)
        {
            throw new ApplicationException($"Could not load {nameof(ff)}");
        }

        return ff;
    }

    public async Task ConvertMp4ToM3U8(byte[] mp4)
    {
        videoId = Guid.NewGuid().ToString();
        await FFmpegFactory.Init(_jsRuntime);
        ff = await InitializeFfAsync();
        FFmpegFactory.Progress += (e) => ProgressUpdate(e);

        var keyFile = "{\n    \"method\": \"AES-128\",\n    \"key\": \"56307ae25300fdba6013597b128c84e2\"\n}\n";
        var keyFileBytes = Encoding.UTF8.GetBytes(keyFile);
        var keyFileURI = await _jsRuntime.InvokeAsync<string>("createBlobUrl", keyFileBytes, "application/octet-stream");
        ff.WriteFile("enc.key", keyFileBytes);

        var keyInfoFile = $"{keyFileURI}\nenc.key";
        var keyInfoFileBytes = Encoding.UTF8.GetBytes(keyInfoFile);
        var keyInfoFileURI = await _jsRuntime.InvokeAsync<string>("createBlobUrl", keyInfoFileBytes, "application/octet-stream");
        ff.WriteFile("enc.keyinfo", keyInfoFileBytes);

        //write to in-memory emscripten files
        ff.WriteFile($"{videoId}.mp4", mp4);
        await ff.Run(
            "-i", $"{videoId}.mp4",
            "-codec", "copy",
            "-hls_time", "10",
            "-hls_key_info_file", "enc.keyinfo",
             $"{videoId}.m3u8");
    }

    private async Task ProgressUpdate(Progress m)
    {
        if (m.Ratio >= 1) //ratio >= 1 means that convert job is done
        {
            var m3U8Url = await GetM3U8Url();
            Console.WriteLine($"m3u8 url: {m3U8Url}");
            await _jsRuntime.InvokeVoidAsync("startStream", m3U8Url);
        }
    }

    private async Task<string> GetM3U8Url()
    {
        var playlistName = $"{videoId}.m3u8";
        var playlistContent = await ff.ReadFile(playlistName);
        StringBuilder sb = new();

        using (var streamReader = new StreamReader(new MemoryStream(playlistContent)))
        {
            var content = await streamReader.ReadToEndAsync();
            var lines = content.Split("\n");
            foreach (var line in lines)
            {
                if (line.StartsWith(videoId))
                {
                    var tsBytes = await ff.ReadFile(line);
                    ff.UnlinkFile(line);
                    var tsUrl = FFmpegFactory
                        .CreateURLFromBuffer(tsBytes, Guid.NewGuid().ToString(), "video/mp2t");
                    sb.AppendLine(tsUrl);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            var modifiedPlaylist = sb.ToString();
            playlistContent = Encoding.UTF8.GetBytes(modifiedPlaylist);
        }

        return FFmpegFactory.CreateURLFromBuffer(playlistContent, $"{videoId}.m3u8", "application/vnd.apple.mpegurl");
    }
}