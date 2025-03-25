using System.Text;

namespace Root.Services.Docker;

/// <summary>
///     Turns properties into arguments of docker run.
/// </summary>
/// <remarks>
///     https://docs.docker.com/reference/cli/docker/container/run/
/// </remarks>
public readonly struct RunArgs {
    /// <summary>
    ///     to input -d or not
    /// </summary>
    public const bool IS_DETACHED = true;

    /// <summary>
    ///     to input --rm or not.
    ///     The container will be removed when stopped.
    /// </summary>
    public bool IsRemoveOnStop { get; init; }

    /// <summary>
    ///     to input --gpus or not.
    ///     Lets the container use host's gpu.
    /// </summary>
    public int GpuCount { get; init; }

    /// <summary>
    ///     to input --name or not.
    ///     Gives the running container a name for sure.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     to input --expose or not.
    /// </summary>
    public string PortExposing { get; init; }

    /// <summary>
    ///     to input -p or not.
    ///     Maps localhost to the container's port,
    ///     which is inside the docker.
    ///     If we want to connect from the host,
    ///     this value must be specified.
    /// </summary>
    public PortMap PortMap { get; init; }

    /// <summary>
    ///     to input -t or not.
    ///     Tags the running container a new tag to push as an image,
    ///     and it works like a repo's name of github.
    /// </summary>
    public string ImageTag { get; init; }

    public override string ToString() {
        var builder = new StringBuilder();
        if (IS_DETACHED) builder.Append(" -d");
        if (IsRemoveOnStop) builder.Append(" --rm");
        if (GpuCount != 0) {
            if (GpuCount < 0) builder.Append(" --gpus=all");
            else builder.Append($" --gpus={GpuCount}");
        }

        if (Name != null) builder.Append($" --name=\"{Name}\"");
        if (PortMap.HasHost) builder.Append($" -p {PortMap}");
        if (PortExposing != null) builder.Append($" --expose={PortExposing}");
        builder.Append($" \"{ImageTag}\"");
        return builder.ToString();
    }
}
