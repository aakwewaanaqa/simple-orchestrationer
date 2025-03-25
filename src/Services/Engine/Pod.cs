using Root.Services.Docker;

namespace Root.Services.Engine;

public struct Pod {
    public IPodKey          PodKey    { get; init; }
    public ContainerWrapper Container { get; init; }
}
