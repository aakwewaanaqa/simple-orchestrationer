using Root.Common;
using Root.Services.Docker;

namespace Root.Services;

public class Llama3Controller(DockerWrapper _docker) {
    public async Task<Stream> Generate(string prompt) {
        var runArgs = new Response<RunArgs> {
            value = new RunArgs {
                ImageTag       = "ponito/built-llama3",
                IsRemoveOnStop = true,
                GpuCount       = -1,
                PortMap = new PortMap {
                    ContainerPort = ConventionPorts.LLAMA3,
                    HostPort      = ConventionPorts.LLAMA3,
                }
            }
        };

        object postData = new {
            model  = "llama3",
            prompt = prompt,
        };

        var pipe = await
            Pipe.Start(runArgs)
                .Pass(_docker.RunContainer)
                .Function(ctnRsp => (ctnRsp, postData))
                .Pass(_docker.Post)
                .ToTask();
        
        return null;
    }
}
