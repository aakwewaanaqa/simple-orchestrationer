using System.Diagnostics;
using Root.Common;
using Docker.DotNet;
using Root.Services.Docker;
using Newtonsoft.Json;

namespace Tests;

[TestFixture]
public class PressureTests {
    private Stopwatch     _stopWatch;
    private DockerWrapper _docker;
    private HttpClient    _http;

    [SetUp]
    public void Setup() {
        _stopWatch = new Stopwatch();
        _stopWatch.Start();

        _docker = new DockerWrapper(
        _http,
        new DockerClientConfiguration().CreateClient());

        _http = new HttpClient {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    [Test]
    public async Task TestRunAndRemove() {
        var runArgs = Response<RunArgs>.Ok(new RunArgs {
            ImageTag       = "ponito/built-llama3",
            IsRemoveOnStop = true,
            PortExposing   = "11434",
        });
        var response = await
            Pipe.Start(runArgs)
                .Pass(_docker.RunContainer)
                .Delay(TimeSpan.FromSeconds(3))
                .FunctionAsync(async it => await it.value.Stop())
                .ToTask();
        Console.WriteLine(response.message);
        That(response.errorCode, EqualTo(0));
        That(response.IsOk,      True);
        That(response.value.Id,  Not.Null);
    }

    private async Task TestRunAndExecute() {
        var runArgs = Response<RunArgs>.Ok(new RunArgs {
            ImageTag       = "ponito/built-llama3",
            IsRemoveOnStop = true,
            GpuCount       = -1,
            PortMap = new PortMap {
                HostPort      = NamedPorts.LLAMA3,
                ContainerPort = NamedPorts.LLAMA3,
            }
        });
        var response = await
            Pipe.Start(runArgs)
                .Pass(_docker.RunContainer)
                .Delay(TimeSpan.FromSeconds(3))
                .FunctionAsync(async it => {
                     var    get = await _http.GetAsync(it.value.HostUrl);
                     string str = await get.Content.ReadAsStringAsync();
                     Console.WriteLine(str);
                     That(str, Not.Null);

                     string endpoint = $"{it.value.HostUrl}/api/generate";
                     string json = JsonConvert.SerializeObject(new {
                         model  = "llama3",
                         prompt = "Read me a story~",
                         stream = false,
                     });
                     var post = await _http.PostAsync(endpoint, new StringContent(json));
                     str = await post.Content.ReadAsStringAsync();
                     Console.WriteLine(str);
                     That(str, Not.Null);

                     return it;
                 })
                .FunctionAsync(async it => await it.value.Stop())
                .ToTask();

        Console.WriteLine(response.message);
        That(response.errorCode, EqualTo(0));
        That(response.IsOk,      True);
        That(response.value.Id,  Not.Null);
    }

    [Test]
    public async Task RunOne() {
        await Task.WhenAll(
        TestRunAndExecute());
    }

    [Test]
    public async Task RunTwo() {
        await Task.WhenAll(
        TestRunAndExecute(),
        TestRunAndExecute());
    }

    [Test]
    public async Task RunThree() {
        await Task.WhenAll(
        TestRunAndExecute(),
        TestRunAndExecute(),
        TestRunAndExecute());
    }

    [Test]
    public async Task RunFour() {
        await Task.WhenAll(
        TestRunAndExecute(),
        TestRunAndExecute(),
        TestRunAndExecute(),
        TestRunAndExecute());
    }

    [TearDown]
    public void TearDown() {
        _stopWatch.Stop();
        Console.WriteLine(_stopWatch.Elapsed);

        _docker.Dispose();
        _http.Dispose();
    }
}
