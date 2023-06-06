namespace VUta.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    using SixLabors.ImageSharp.Formats.Jpeg;

    using System.Net;
    using System.Text.Json;

    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly HttpClient _http;
        private readonly IOptionsSnapshot<BoonOptions> _boonOptions;
        private readonly static string[] _imageResolutions = new[] { "hq720.jpg", "hqdefault.jpg", "mqdefault.jpg" };

        public ImageController(HttpClient http,
            IOptionsSnapshot<BoonOptions> boonOptions)
        {
            _http = http;
            _boonOptions = boonOptions;
        }

        private async Task<Image?> GetThumbnailAsync(string videoId, CancellationToken cancellation)
        {
            foreach (var file in _imageResolutions)
            {
                cancellation.ThrowIfCancellationRequested();
                var res = null as HttpResponseMessage;

                try
                {
                    res = await _http.GetAsync($"https://i.ytimg.com/vi/{videoId}/{file}", HttpCompletionOption.ResponseHeadersRead, cancellation);
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

        private record BoonRect(int X, int Y, int W, int H);

        private async Task<BoonRect[]?> GetFacesAsync(
            string videoId,
            CancellationToken cancellation)
        {
            if (_boonOptions.Value.FaceDetectionEndpoint == null)
                return null;

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{_boonOptions.Value.FaceDetectionEndpoint}/anime/{videoId}");
                if (!string.IsNullOrEmpty(_boonOptions.Value.FaceDetectionKey))
                    req.Headers.TryAddWithoutValidation("this-is-boon4681", _boonOptions.Value.FaceDetectionKey);

                using var res = await _http.SendAsync(req, cancellation);
                return await res.Content.ReadFromJsonAsync<BoonRect[]>(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }, cancellation);
            }
            catch
            {
                return null;
            }
        }

        [HttpGet("square/{videoId}.{ext}")]
        public async Task<IActionResult> GetSquareAsync(
            string videoId,
            string ext,
            CancellationToken cancellation)
        {
            if (!Configuration.Default.ImageFormatsManager.TryFindFormatByFileExtension(ext, out var format))
                return BadRequest("Unsupported format");

            var getThumbnail = GetThumbnailAsync(videoId, cancellation);
            var getFaces = GetFacesAsync(videoId, cancellation);

            using var img = await getThumbnail;
            if (img == null)
                return NotFound();

            var faces = await getFaces;
            var w = img.Width;
            var h = img.Height;
            var size = Math.Min(w, h);
            var face = (faces?.Any() ?? false) ? (size == h ? faces.OrderBy(x => x.X).First() : faces.OrderBy(x => x.Y).First()) : null;
            var left = (w - size) / 2;
            var top = (h - size) / 2;

            if (face != null)
            {
                if (size == h) left = Math.Max(0, Math.Min((face.X > left) ? face.X : face.X - face.W, (w - size) * 2)) / 2;
                if (size == w) top = Math.Max(0, Math.Min((face.Y > top) ? face.Y : face.Y - face.H, (h - size) * 2)) / 2;
            }

            img.Mutate(ctx => ctx
                .Crop(new(left, top, size, size))
                .Resize(512, 512));

            var resizeStream = new MemoryStream();
            await img.SaveAsync(resizeStream, format, cancellation);

            resizeStream.Seek(0, SeekOrigin.Begin);
            return File(resizeStream, format.DefaultMimeType);
        }
    }
}
