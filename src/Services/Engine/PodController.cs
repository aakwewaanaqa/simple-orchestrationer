using System.Net;
using Root.Common;
using Root.Services.Docker;

namespace Root.Services.Engine;

public class PodController(
DockerWrapper      _docker,
HostPortController _hostPortController) {
    /// <summary>
    ///     All running pods.
    /// </summary>
    private readonly List<Pod> _pods = [];

    public Convert<IPodKey, Response<int>> Deploy => async args => {
        var podKey = args;
        switch (podKey) {
            case Service s:
                int deployedCount = 0;
                for (int i = 0; i < s.Replicas; i++) {
                    var pipe = await
                        (_hostPortController.OpenPort, podKey)
                       .Start()
                       .Function(openPortResponse => openPortResponse
                           .As(new RunArgs {
                                IsRemoveOnStop = true,
                                ImageTag = s.ImageTag,
                                GpuCount = s.GpuCount,
                                PortMap = new PortMap {
                                    ContainerPort = s.ContainerPortMap,
                                    HostPort      = openPortResponse.value,
                                },
                            }, default))
                       .Pass(_docker.RunContainer)
                       .ToTask();

                    if (pipe.IsNotOk) continue;

                    deployedCount++;

                    _pods.Add(new Pod {
                        PodKey    = podKey,
                        Container = pipe.value,
                    });
                }

                if (deployedCount == 0)
                    return new Response<int> {
                        status    = (int)HttpStatusCode.ExpectationFailed,
                        errorCode = ErrorCode.CREATE_CONTAINER_FAIL,
                    };

                return new Response<int> {
                    status    = (int)HttpStatusCode.OK,
                    errorCode = ErrorCode.OK,
                    value     = deployedCount,
                };
            
            default:
                return new Response<int> {
                    status    = (int)HttpStatusCode.InternalServerError,
                    errorCode = ErrorCode.UNIMPLEMENTED_IPODKEY_ERROR,
                };
        }
    };

    public async Task Stop(IPodKey key, CancellationToken ct = default) {
        var where = _pods.Where(p => p.PodKey.GetKey() == key.GetKey());
        foreach (var pod in where) {
            await pod.Container.Stop();
        }
    }
}
