using System.Net;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Drawing.Processing;
using Size = System.Drawing.Size;

namespace VUta.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ImageController : ControllerBase
{
    private static readonly string[] _imageResolutions = { "hq720.jpg", "hqdefault.jpg", "mqdefault.jpg" };
    private readonly HttpClient _http;

    public ImageController(HttpClient http)
    {
        _http = http;
    }

    private async Task<Image?> GetThumbnailAsync(string videoId, CancellationToken cancellation)
    {
        foreach (var file in _imageResolutions)
        {
            cancellation.ThrowIfCancellationRequested();
            var res = null as HttpResponseMessage;

            try
            {
                res = await _http.GetAsync($"https://i.ytimg.com/vi/{videoId}/{file}",
                    HttpCompletionOption.ResponseHeadersRead, cancellation);
                if (res.StatusCode == HttpStatusCode.OK)
                    return await Image.LoadAsync(await res.Content.ReadAsStreamAsync(cancellation), cancellation);
            }
            finally
            {
                if (res != null && res.StatusCode != HttpStatusCode.OK)
                    res.Dispose();
            }
        }

        return null;
    }

    [HttpGet("square/{videoId}.{ext}")]
    public async Task<IActionResult> GetSquareAsync(
        string videoId,
        string ext,
        [FromQuery] bool showFaces = false,
        CancellationToken cancellation = default)
    {
        if (!Configuration.Default.ImageFormatsManager.TryFindFormatByFileExtension(ext, out var format))
            return BadRequest("Unsupported format");

        var getThumbnail = GetThumbnailAsync(videoId, cancellation);

        using var cascadeClassifier = new CascadeClassifier(Path.Combine("Resources", "lbpcascade_animeface.xml"));
        using var img = await getThumbnail;

        if (img == null)
            return NotFound();

        using var imgGray = img.CloneAs<L8>();
        var rawGray = new byte[imgGray.Width * imgGray.Height];
        imgGray.CopyPixelDataTo(rawGray);
        var depthImage = new Image<Gray, byte>(imgGray.Width, imgGray.Height)
        {
            Bytes = rawGray
        };

        var faces = cascadeClassifier.DetectMultiScale(depthImage,
            minNeighbors: 1,
            minSize: new Size(100, 100));

        if (showFaces)
        {
            img.Mutate(ctx =>
            {
                foreach (var face in faces)
                    ctx.Draw(Color.Red, 2, new Rectangle(face.Left, face.Top, face.Width, face.Height));
            });
        }
        else
        {
            var w = img.Width;
            var h = img.Height;
            var size = Math.Min(w, h);
            var face = faces.Any()
                ? size == h ? faces.OrderBy(x => x.X).First() : faces.OrderBy(x => x.Y).First()
                : default;
            var left = (w - size) / 2;
            var top = (h - size) / 2;

            if (face != default)
            {
                if (size == h)
                    left = Math.Max(0, Math.Min(face.X > left ? face.X : face.X - face.Width, (w - size) * 2)) / 2;
                if (size == w)
                    top = Math.Max(0, Math.Min(face.Y > top ? face.Y : face.Y - face.Height, (h - size) * 2)) / 2;
            }

            img.Mutate(ctx => ctx
                .Crop(new Rectangle(left, top, size, size))
                .Resize(512, 512));
        }

        var resizeStream = new MemoryStream();
        await img.SaveAsync(resizeStream, format, cancellation);

        resizeStream.Seek(0, SeekOrigin.Begin);
        return File(resizeStream, format.DefaultMimeType);
    }
}