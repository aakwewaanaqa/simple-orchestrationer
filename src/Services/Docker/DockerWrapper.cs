using System.Diagnostics;
using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using Root.Common;

namespace Root.Services.Docker;

/// <summary>
///     Docker services wrapped inside a wrapper.
/// </summary>
public class DockerWrapper : IDisposable {

    private readonly DockerClient _client = new DockerClientConfiguration().CreateClient();
    
    /// <summary>
    ///     Gets a container by <see cref="GetArgs"/>.
    /// </summary>
    public Convert<Response<GetArgs>, Response<ContainerWrapper>> GetContainer =>
        async argRsp => {
            if (argRsp.IsNotOk) return argRsp.As<ContainerWrapper>();
            var args = argRsp.value;

            try {
                var containers = await
                    _client.Containers
                           .ListContainersAsync(
                            new ContainersListParameters { All = true });

                var matchedContainer = containers.FirstOrDefault(c => c.Image == args.Image);
                if (matchedContainer == null) {
                    return new Response<ContainerWrapper> {
                        status    = (int)HttpStatusCode.NotFound,
                        errorCode = ErrorCode.GET_CONTAINER_FAIL,
                        message   = "No matching container found."
                    };
                }

                string innerIp = matchedContainer.NetworkSettings?.Networks?.Values.FirstOrDefault()?.IPAddress ?? "";

                return new Response<ContainerWrapper> {
                    status    = (int)HttpStatusCode.OK,
                    errorCode = ErrorCode.OK,
                    message   = "Container found.",
                    value = new ContainerWrapper {
                        Id      = matchedContainer.ID,
                        InnerIp = innerIp,
                        HostUrl = matchedContainer.Ports.Any()
                            ? $"http://localhost:{matchedContainer.Ports.First().PublicPort}"
                            : null
                    }
                };
            }
            catch (Exception ex) {
                return new Response<ContainerWrapper> {
                    status    = (int)HttpStatusCode.InternalServerError,
                    errorCode = ErrorCode.GET_CONTAINER_FAIL,
                    message   = ex.Message,
                };
            }
        };

    /// <summary>
    ///     Creates a container using Docker.DotNet.
    /// </summary>
    /// <param name="args"><see cref="RunArgs"/></param>
    public Convert<Response<RunArgs>, Response<ContainerWrapper>> RunContainer =>
        async argRsp => {
            if (argRsp.IsNotOk) return argRsp.As<ContainerWrapper>();
            var args = argRsp.value;

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
                        AutoRemove      = args.IsRemoveOnStop,
                        Runtime         = args.UseGpu ? "nvidia" : null,
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

    /// <summary>
    ///     Posts a raw object, converted automatically as json, to endpoint.
    /// </summary>
    /// <param name="endpoint"><see cref="ContainerWrapper.HostUrl"/> + <see cref="endpoint"/></param>
    public Convert<
        (
        Response<ContainerWrapper> ctnRsp,
        HttpClient http,
        string endpoint,
        object rawObj
        ),
        Response<HttpResponseMessage>>
        Post =>
        async tuple => {
            (var ctnRsp, HttpClient http, string endpoint, object rawObj) = tuple;
            if (ctnRsp.IsNotOk) return ctnRsp.As<HttpResponseMessage>();
            string url          = ctnRsp.value.HostUrl + endpoint;
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
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
