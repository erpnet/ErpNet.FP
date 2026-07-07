namespace ErpNet.FP.Core.Transports
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Generic transport for devices that communicate over HTTP with a simple request/response
    /// exchange - one HTTP POST per command.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Transport" />
    public class HttpTransport : Transport
    {
        public override string TransportName => "http";

        // HttpClient is intended to be instantiated once and reused; see
        // https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private readonly string _defaultPath;
        private readonly string _contentType;

        private readonly IDictionary<string, Channel> _openedChannels =
            new Dictionary<string, Channel>();

        /// <param name="defaultPath">
        /// Optional endpoint path appended to a configured address. Empty by default.
        /// </param>
        /// <param name="contentType">
        /// The Content-Type header sent with each POST. Defaults to "application/octet-stream";
        /// </param>
        public HttpTransport(string defaultPath = "", string contentType = "application/octet-stream")
        {
            _defaultPath = defaultPath;
            _contentType = contentType;
        }

        public override IChannel OpenChannel(string address)
        {
            if (_openedChannels.TryGetValue(address, out Channel? channel))
            {
                return channel;
            }

            channel = new Channel(address, _defaultPath, _contentType);
            _openedChannels.Add(address, channel);
            return channel;
        }

        public override void Drop(IChannel channel)
        {
            // HTTP is connectionless; nothing to close.
        }

        public class Channel : IChannel
        {
            private readonly string _url;
            private readonly MediaTypeHeaderValue _contentType;
            private byte[] _lastResponse = [];

            public string Descriptor { get; }

            public Channel(string address, string defaultPath = "", string contentType = "application/octet-stream")
            {
                Descriptor = address;
                _contentType = MediaTypeHeaderValue.Parse(contentType);

                var full = address.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                           || address.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? address
                    : $"http://{address}";

                // Append the transport's default path only if the address has no path of its own.
                var afterScheme = full.Substring(full.IndexOf("://", StringComparison.Ordinal) + 3);
                if (!string.IsNullOrEmpty(defaultPath) && !afterScheme.Contains('/'))
                {
                    full += defaultPath.StartsWith('/') ? defaultPath : $"/{defaultPath}";
                }
                _url = full;
            }

            /// <summary>
            /// Returns the body of the response to the most recent <see cref="Write"/>.
            /// </summary>
            public byte[] Read()
            {
                return _lastResponse;
            }

            /// <summary>
            /// Posts the data as the request body and buffers the response for the following Read.
            /// </summary>
            public void Write(byte[] data)
            {
                using var content = new ByteArrayContent(data);
                content.Headers.ContentType = _contentType;

                try
                {
                    var task = _httpClient.PostAsync(_url, content);
                    var response = task.GetAwaiter().GetResult();
                    _lastResponse = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                }
                catch (TaskCanceledException ex)
                {
                    var errorMessage = $"Timeout occured while posting to {_url}";
                    Log.Error(errorMessage);
                    throw new TimeoutException(errorMessage, ex);
                }
                catch (HttpRequestException ex)
                {
                    var errorMessage = $"HTTP error while posting to {_url}: {ex.Message}";
                    Log.Error(errorMessage);
                    throw new TimeoutException(errorMessage, ex);
                }
            }
        }
    }
}
