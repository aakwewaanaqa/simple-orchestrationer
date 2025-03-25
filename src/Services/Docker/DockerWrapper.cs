using System.Diagnostics;
using System.Net;
using Docker.DotNet;
using Root.Common;

namespace Root.Services.Docker;

/// <summary>
///     Docker services wrapped inside a wrapper.
/// </summary>
public class DockerWrapper(
HttpClient   _http,
DockerClient _client) : IDisposable {
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
                            : null,
                        InnerIp =
                            (await _client
                                  .Containers
                                  .InspectContainerAsync(message.Trim())
                                  .Guard()
                            ).NetworkSettings.IPAddress,
                    }
            };
        };

    public Convert<(Response<ContainerWrapper>, object), Response<HttpResponseMessage>> Post
        => async tuple => {
            (var ctnRsp, object rawObj) = tuple;
            if (ctnRsp.IsNotOk) return ctnRsp.As<HttpResponseMessage>();
            string url          = ctnRsp.value.HostUrl;
            var    httpResponse = await _http.PostAsJsonAsync(url, rawObj);
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
