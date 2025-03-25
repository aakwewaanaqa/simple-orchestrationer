using System.Diagnostics;
using System.Net;
using Docker.DotNet;
using Root.Common;

namespace Root.Services.Docker;

/// <summary>
///     Docker services wrapped inside a wrapper.
/// </summary>
public class DockerWrapper(
IHttpClientFactory _httpFactory,
DockerClient       _client) : IDisposable {
    /// <summary>
    ///     Creates a container.
    /// </summary>
    /// <param name="args"><see cref="RunArgs"/></param>
    public Convert<Response<RunArgs>, Response<ContainerWrapper>> RunContainer =>
        async argsResponse => {
            if (argsResponse.IsNotOk) return argsResponse.As<ContainerWrapper>();
            var args = argsResponse.value;

            var startInfo = new ProcessStartInfo {
                FileName               = "docker",
                Arguments              = $"container run {args}",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };
            var process = Process.Start(startInfo);

            if (RunArgs.IS_DETACHED) await process.WaitForExitAsync();
            string error   = await process.StandardError.ReadToEndAsync();
            string message = await process.StandardOutput.ReadToEndAsync();

            if (!string.IsNullOrEmpty(error)) {
                return new Response<ContainerWrapper> {
                    status    = (int)HttpStatusCode.InternalServerError,
                    errorCode = ErrorCode.CREATE_CONTAINER_FAIL,
                    message   = error,
                };
            }

            return new Response<ContainerWrapper> {
                status    = (int)HttpStatusCode.OK,
                errorCode = ErrorCode.OK,
                message   = message,
                value =
                    new ContainerWrapper {
                        Id = message.Trim(),
                        HostUrl = args.PortMap.HasHost
                            ? $"http://localhost:{args.PortMap.HostPort}"
                            : null
                    }
            };
        };
    
    public Convert<(Response<ContainerWrapper>, string, object), Response<HttpResponseMessage>> Post
        => async tuple => {
            (var ctnRsp, string endpoint, object rawObj) = tuple;
            if (ctnRsp.IsNotOk) return ctnRsp.As<HttpResponseMessage>();
            string url          = ctnRsp.value.HostUrl + endpoint;
            var http = _httpFactory.CreateClient();
            var    httpResponse = await http.PostAsJsonAsync(url, rawObj);
            bool   httpIsOk     = httpResponse.IsSuccessStatusCode;
            return new Response<HttpResponseMessage> {
                status = (int)httpResponse.StatusCode,
                errorCode = httpIsOk
                    ? ErrorCode.OK
                    : ErrorCode.POST_CONTAINER_FAIL,
                message = httpResponse.ReasonPhrase,
                value   = httpResponse,
            };
        };

    public void Dispose() {
        // Do not do this, because it is handled by Di
        // _http?.Dispose();
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
