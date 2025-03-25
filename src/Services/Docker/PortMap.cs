namespace Root.Services.Docker;

public readonly struct PortMap {
    /// <summary>
    ///     Checks if this struct is set or not.
    /// </summary>
    public bool HasHost => HostPort > 0;

    /// <summary>
    ///     The port to connect from localhost.
    /// </summary>
    public uint HostPort { get; init; }

    /// <summary>
    ///     The port for container to listen to.
    /// </summary>
    public uint ContainerPort { get; init; }

    public override string ToString() {
        return $"{HostPort}:{ContainerPort}";
    }
}
