using Root.Services.Docker;

namespace Root.Services.Engine;

public readonly struct Service : IPodKey {
    /// <summary>
    ///     The endpoint to call from user to host.
    /// </summary>
    public string HostEndpoint { get; init; }

    /// <summary>
    ///     The endpoint to be transferred into to docker's container.
    /// </summary>
    public string ContainerEndpoint { get; init; }
    
    /// <summary>
    ///     The tag of the image to pull and run with.
    /// </summary>
    public string ImageTag { get; init; }

    /// <summary>
    ///     The minimum value of container to be running. 
    /// </summary>
    public int Replicas { get; init; }
    
    /// <summary>
    ///     <see cref="RunArgs.GpuCount"/> 
    /// </summary>
    public int GpuCount { get; init; }

    /// <summary>
    ///     The port to be mapped to localhost,
    ///     the host port will be chosen automatically.
    /// </summary>
    public uint ContainerPortMap { get; init; }

    public int GetKey() {
        return HashCode.Combine(HostEndpoint, ContainerEndpoint, ImageTag);
    }
}