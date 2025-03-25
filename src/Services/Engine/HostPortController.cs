using System.Net;
using System.Net.Sockets;
using Root.Common;

namespace Root.Services.Engine;

/// <summary>
///     Controlling ports of this machine.
/// </summary>
/// <param name="_min"></param>
/// <param name="_max"></param>
public class HostPortController(int _min, int _max) {
    /// <summary>
    ///     The smallest random to give out as a port number.
    /// </summary>
    public int Min => _min;

    /// <summary>
    ///     The biggest number to give out as a port number.
    /// </summary>
    public int Max => _max;

    public Dictionary<int, uint> _ports;

    public PipeOut<Response<uint>> GetFreePort => async () => {
        for (int p = Min; p <= Max; p++) {
            try {
                var listener = new TcpListener(IPAddress.Loopback, p);
                listener.Start();
                listener.Stop();
                return new Response<uint> {
                    status    = (int)HttpStatusCode.OK,
                    errorCode = ErrorCode.OK,
                    value     = (uint)p
                };
            }
            catch {
                continue;
            }
        }

        return new Response<uint> {
            status    = (int)HttpStatusCode.UnprocessableContent,
            errorCode = ErrorCode.ALL_PORT_OCCUPIED,
        };
    };

    public Convert<IPodKey, Response<uint>> OpenPort => async podKey => {
        try {
            if (_ports.ContainsKey(podKey.GetKey())) {
                return new Response<uint> {
                    status    = (int)HttpStatusCode.OK,
                    errorCode = ErrorCode.OK,
                    value     = _ports[podKey.GetKey()]
                };
            }

            var pipe = await GetFreePort.ToTask();
            if (pipe.IsNotOk) return pipe;
            _ports.Add(podKey.GetKey(), pipe.value);
            return pipe;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            return new Response<uint> {
                status    = (int)HttpStatusCode.InternalServerError,
                errorCode = ErrorCode.UNKNOWN_ERROR,
            };
        }
    };
}
