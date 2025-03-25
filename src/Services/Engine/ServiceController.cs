using System.Net;
using Microsoft.AspNetCore.Mvc;
using Root.Common;

namespace Root.Services.Engine;

[Controller]
[Route("ctl/services")]
public class ServiceController(PodController _podController) : Controller {
    /// <summary>
    ///     The current stored services to be run.
    /// </summary>
    private Dictionary<string, Service> _services;

    [Route("set")]
    public async Task<Response<bool>> SetService([FromBody] Service service) {
        var key = service.HostEndpoint;
        if (!_services.TryAdd(key, service)) {
            // TODO: Updates all serving pods.
            _services[key] = service;
            Console.WriteLine($"Service endpoint {service.HostEndpoint} updated.");
            return new Response<bool> {
                status    = (int)HttpStatusCode.OK,
                errorCode = ErrorCode.OK,
                value     = true,
            };
        } else {
            return (await
                    (_podController.Deploy, service)
                   .Start()
                   .Log($"Service added as {service.HostEndpoint} endpoint.")
                   .ToTask())
               .As(true, false);
        }
    }

    public async Task RemoveService(string hostEndpoint) {
        string key = hostEndpoint;
        if (_services.Remove(key, out var service)) {
            // TODO: Stops all pods
            await _podController.Stop(service);
            Console.WriteLine($"Service removed from endpoint {hostEndpoint}.");
        } else {
            Console.WriteLine($"Service endpoint {hostEndpoint} not found.");
        }
    }
}
