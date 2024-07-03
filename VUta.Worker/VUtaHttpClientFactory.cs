using DynamicExpresso;
using Google.Apis.Http;
using Microsoft.Extensions.Options;

namespace VUta.Worker;

public class VUtaHttpClientFactory
    : HttpClientFactory, IHttpExecuteInterceptor, IHttpClientFactory
{
    private readonly IOptions<WorkerOptions> _options;
    private readonly Action<HttpRequestMessage>[] _customInterceptActions;

    public VUtaHttpClientFactory(IOptions<WorkerOptions> options)
    {
        _options = options;

        var inp = new Interpreter();
        _customInterceptActions =
            options.Value.YoutubeInterceptActions
                .Select(code => inp.ParseAsDelegate<Action<HttpRequestMessage>>(code, "req"))
                .ToArray();
    }

    public new ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
    {
        var client = base.CreateHttpClient(args);
        client.MessageHandler.AddExecuteInterceptor(this);
        return client;
    }

    public Task InterceptAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        foreach (var interceptAction in _customInterceptActions)
            interceptAction(request);
        
        return Task.CompletedTask;
    }
}