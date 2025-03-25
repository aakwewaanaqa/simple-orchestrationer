namespace Root.Common;

public struct ErrorCode {
    public const int UNKNOWN_ERROR = -1;

    public const int OK                    = 0;
    public const int RUN_CONTAINER_FAIL = 1;
    public const int STOP_CONTAINER_FAIL   = 2;
    public const int POST_CONTAINER_FAIL   = 3;
    public const int GET_CONTAINER_FAIL    = 4;

    public const int UNIMPLEMENTED_IPODKEY_ERROR = 102;

    public const int ALL_PORT_OCCUPIED = 200;
}
