using System.Diagnostics;
using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using Root.Common;

namespace Root.Services.Docker;

/// <summary>
///     Docker services wrapped inside a wrapper.
/// </summary>
public class DockerWrapper(
HttpClient   _http,
DockerClient _client) : IDisposable {
    /// <summary>
    ///     Gets a container by <see cref="SearchArgs"/>.
    /// </summary>
    public Convert<Response<SearchArgs>, Response<ContainerListResponse>> GetContainer =>
        async argRsp => {
            try {
                var containers =
                    await _client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                var matchedContainers = containers.Where(c => c.Image == argRsp.value.Image).ToList();

                if (!matchedContainers.Any()) {
                    return new Response<ContainerListResponse> {
                        status    = (int)HttpStatusCode.NotFound,
                        errorCode = ErrorCode.GET_CONTAINER_FAIL,
                        message   = "No matching container found."
                    };
                }

                return new Response<ContainerListResponse> {
                    status    = (int)HttpStatusCode.OK,
                    errorCode = ErrorCode.OK,
                    value     = matchedContainers.First()
                };
            }
            catch (Exception ex) {
                return new Response<ContainerListResponse> {
                    status    = (int)HttpStatusCode.InternalServerError,
                    errorCode = ErrorCode.UNKNOWN_ERROR,
                    message   = ex.Message,
                };
            }
        };

    /// <summary>
    ///     Creates a container using Docker.DotNet.
    /// </summary>
    /// <param name="args"><see cref="RunArgs"/></param>
    public Convert<Response<RunArgs>, Response<ContainerWrapper>> RunContainer =>
        async argsResponse => {
            if (argsResponse.IsNotOk) return argsResponse.As<ContainerWrapper>();
            var args = argsResponse.value;

            try {
                var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters {
                    Image = args.Image,
                    Name  = args.Name,
                    HostConfig = new HostConfig {
                        PortBindings = args.PortMap.HasHost
                            ? new Dictionary<string, IList<PortBinding>> {
                                [$"{args.PortMap.ContainerPort}/tcp"] = new List<PortBinding> {
                                    new() { HostPort = args.PortMap.HostPort.ToString() }
                                }
                            }
                            : null,
                        PublishAllPorts = args.PortMap.HasHost,
                        AutoRemove      = true,
                        Runtime         = "nvidia",
                    },
                    ExposedPorts = args.PortMap.HasHost
                        ? new Dictionary<string, EmptyStruct> {
                            [$"{args.PortMap.ContainerPort}/tcp"] = new()
                        }
                        : null,
                });

                await _client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

                return new Response<ContainerWrapper> {
                    status    = (int)HttpStatusCode.OK,
                    errorCode = ErrorCode.OK,
                    message   = "Container created and started successfully.",
                    value = new ContainerWrapper {
                        Id      = response.ID,
                        HostUrl = args.PortMap.HasHost ? $"http://localhost:{args.PortMap.HostPort}" : null
                    }
                };
            }
            catch (Exception ex) {
                return new Response<ContainerWrapper> {
                    status    = (int)HttpStatusCode.InternalServerError,
                    errorCode = ErrorCode.RUN_CONTAINER_FAIL,
                    message   = ex.Message,
                };
            }
        };

    public Convert<(Response<ContainerWrapper>, string, object), Response<HttpResponseMessage>> Post
        => async tuple => {
            (var ctnRsp, string endpoint, object rawObj) = tuple;
            if (ctnRsp.IsNotOk) return ctnRsp.As<HttpResponseMessage>();
            string url          = ctnRsp.value.HostUrl + endpoint;
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
        _http?.Dispose();
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
