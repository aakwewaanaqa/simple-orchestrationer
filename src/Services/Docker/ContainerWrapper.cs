using System.Diagnostics;
using System.Net;
using Root.Common;

namespace Root.Services.Docker;

public class ContainerWrapper : IDisposable {
    /// <summary>
    ///     Id for docker to operate with.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    ///     The address of the container in the network of docker.
    /// </summary>
    public string InnerIp { get; init; }

    /// <summary>
    ///     The forwarded url for host to connect at localhost.
    /// </summary>
    public string HostUrl { get; init; }

    /// <summary>
    ///     Checks whether it is connectable from host.
    /// </summary>
    public bool HasHostUrl => !string.IsNullOrEmpty(HostUrl);
    
    /// <summary>
    ///     Stops this container.
    /// </summary>
    /// <returns>Stopped one.</returns>
    public async Task<Response<ContainerWrapper>> Stop() {
        try {
            var startInfo = new ProcessStartInfo {
                FileName               = "docker",
                Arguments              = $"stop {Id}",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };

            var process = Process.Start(startInfo);
            await process.WaitForExitAsync();

            string error   = await process.StandardError.ReadToEndAsync();
            string message = await process.StandardOutput.ReadToEndAsync();

            if (!string.IsNullOrEmpty(error)) throw new Exception(error);

            return new Response<ContainerWrapper> {
                status    = (int)HttpStatusCode.OK,
                errorCode = ErrorCode.OK,
                message   = message,
                value     = this,
            };
        }
        catch (Exception e) {
            return new Response<ContainerWrapper> {
                status    = (int)HttpStatusCode.InternalServerError,
                errorCode = ErrorCode.STOP_CONTAINER_FAIL,
                message   = e.Message,
                value     = this,
            };
        }
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}
