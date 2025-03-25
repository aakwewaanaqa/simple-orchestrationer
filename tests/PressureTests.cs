using System.Diagnostics;
using Root.Common;
using Docker.DotNet;
using Root.Services.Docker;
using Newtonsoft.Json;

namespace Tests;

[TestFixture]
public class PressureTests {
    private DockerWrapper _docker;

    [SetUp]
    public void Setup() {
        _docker = new DockerWrapper(
        new HttpClient(),
        new DockerClientConfiguration().CreateClient());
    }

    [Test]
    public async Task RunContainer_And_CallGenerateAPI() {
        var runArgs = new RunArgs {
            Image    = "ponito/built-llama3",
            Name     = "llama3_test",
            GpuCount = -1,
            PortMap  = new PortMap { HostPort = 11434, ContainerPort = 11434 }
        };
        string api = "/api/generate";
        object jsonData = new {
            model  = "llama3",
            prompt = "Read me a story.",
            stream = false,
        };

        var pipe = await Pipe.Start(runArgs)
                             .Function(a => new Response<RunArgs> {
                                  status = 200,
                                  value  = a,
                              })
                             .Pass(_docker.RunContainer)
                             .Function(ctnRsp => (ctnRsp, api, jsonData))
                             .Pass(_docker.Post)
                             .ToTask();

        That(pipe.IsOk);
    }

    [TearDown]
    public void TearDown() {
        _docker.Dispose();
    }
}
