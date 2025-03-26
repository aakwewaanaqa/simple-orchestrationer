using Docker.DotNet;
using Root.Services.Docker;

namespace Root;

public static class Program {
    private static WebApplicationBuilder Create(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
        builder.Services
               .AddTransient<DockerWrapper>();

        builder.Services.AddRazorComponents()
               .AddInteractiveServerComponents();

        return builder;
    }

    private static async Task<int> Run(WebApplicationBuilder builder) {
        var app = builder.Build();

// Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();


        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();

        {
            var docker = app.Services.GetService<DockerWrapper>();
            var runArgs = new RunArgs {
                Image          = "ponito/built-llama3",
                Name           = "built-llama3",
                UseGpu         = true,
                IsRemoveOnStop = true,
                PortMap        = new PortMap { HostPort = 11434, ContainerPort = 11434 }
            };
            var pipe = await Pipe.Start(Response<RunArgs>.Ok(runArgs))
                                 .Pass(docker.RunContainer)
                                 .ToTask();

            if (pipe.IsNotOk) throw new InvalidOperationException(pipe.message);
        }

        {
            AppDomain.CurrentDomain.ProcessExit += (_, __) => {
                using var docker = new DockerWrapper();
                var getArgs = new GetArgs {
                    Image = "ponito/built-llama3",
                };

                Pipe.Start(Response<GetArgs>.Ok(getArgs))
                    .Pass(docker.GetContainer)
                    .FunctionAsync(async ctnRsp => {
                         await ctnRsp.value.Stop();
                         return ctnRsp;
                     })
                    .ToTask()
                    .Wait();
            };
        }

        await app.RunAsync();

        return 0;
    }

    public static async Task<int> Main(string[] args) {
        return await Run(Create(args));
    }
}
