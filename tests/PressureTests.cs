using System.Diagnostics;
using Docker.DotNet;
using Newtonsoft.Json;
using Root.Common;
using Root.Services.Docker;

namespace Tests;

[TestFixture]
public class PressureTests {
    private DockerWrapper _docker;

    [SetUp]
    public void Setup() {
        _docker = new DockerWrapper();
    }

    [Test]
    public async Task Run_Post() {
        var runArgs = new RunArgs {
            Image   = "ponito/built-llama3",
            Name    = "llama3_test",
            UseGpu  = true,
            PortMap = new PortMap { HostPort = 11434, ContainerPort = 11434 }
        };
        using var http = new HttpClient();
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
                             .Function(ctnRsp => (ctnRsp, http, api, jsonData))
                             .Pass(_docker.Post)
                             .ToTask();

        That(pipe.IsOk);
    }

    [Test]
    public async Task Run_Get_Post() {
        var runArgs = new RunArgs {
            Image   = "ponito/built-llama3",
            Name    = "llama3_test",
            UseGpu  = true,
            PortMap = new PortMap { HostPort = 11434, ContainerPort = 11434 }
        };
        var getArgs = new GetArgs {
            Image = "ponito/built-llama3",
        };
        using var http = new HttpClient();
        string    api  = "/api/generate";
        object rawObj = new {
            model  = "llama3",
            prompt = "Read me a story.",
            stream = false,
        };

        var pipe = await Pipe.Start(Response<RunArgs>.Ok(runArgs))
                             .Pass(_docker.RunContainer)
                             .Function(_ => Response<GetArgs>.Ok(getArgs))
                             .Pass(_docker.GetContainer)
                             .Function(ctnRsp => (ctnRsp, http, api, jsonData: rawObj))
                             .Pass(_docker.Post)
                             .ToTask();
        
        That(pipe.IsOk);
    }

    [TearDown]
    public void TearDown() {
        _docker.Dispose();
    }
}
