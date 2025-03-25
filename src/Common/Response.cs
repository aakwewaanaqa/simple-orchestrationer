using System.Net;

namespace Root.Common;

/// <summary>
///     資料庫固定回應格式。
///     A fixed class structure to response unity client.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct Response<T> {
    /// <summary>
    ///     檢驗此訊息是否是 Ok 的。
    ///     Checks if this info is ok or not.
    /// </summary>
    public bool IsOk => status == 200 && errorCode == 0;

    /// <summary>
    ///     檢驗此訊息是否是不 Ok 的。
    ///     Checks if this info is not ok or not.
    /// </summary>
    public bool IsNotOk => !IsOk;

    /// <summary>
    ///     欲夾帶之狀態碼
    ///     The HttpStatusCode to go with the response.
    /// </summary>
    public int status { get; set; }

    /// <summary>
    ///     欲夾帶之錯誤碼
    ///     The ErrorCode to go with the response.
    /// </summary>
    public int errorCode { get; set; }

    /// <summary>
    ///     欲夾帶之訊息
    ///     The message to go with the response.
    /// </summary>
    public string message { get; set; }

    /// <summary>
    ///     欲夾帶之值
    ///     The value to go with the response.
    /// </summary>
    public T value { get; set; }

    /// <summary>
    ///     用目前的回應碼、錯誤碼來回覆指定的 value。
    ///     Uses current status and errorCode to response a appointed value.
    /// </summary>
    public Response<U> As<U>(U value = default) {
        return new Response<U> {
            status    = status,
            errorCode = errorCode,
            message   = message,
            value     = value,
        };
    }

    /// <summary>
    ///     用目前的回應碼、錯誤碼來回覆指定的 value。
    ///     Uses current status and errorCode to response a appointed value.
    /// </summary>
    /// <param name="onOk">
    ///     如果目前的回應是 <see cref="IsOk"/> 的話
    ///     Uses onOk as value if current <see cref="IsOk"/>.
    /// </param>
    /// <param name="onNotOk">
    ///     如果目前的回應是 <see cref="IsNotOk"/> 的話
    ///     Uses onNotOk as value if current info <see cref="IsNotOk"/>.
    /// </param>
    public Response<U> As<U>(U onOk, U onNotOk) {
        return IsOk
                   ? new Response<U> {
                       status    = status,
                       errorCode = errorCode,
                       message   = message,
                       value     = onOk,
                   }
                   : new Response<U> {
                       status    = status,
                       errorCode = errorCode,
                       message   = message,
                       value     = onNotOk,
                   };
    }

    /// <summary>
    ///     回應 Ok。
    ///     Responses an ok.
    /// </summary>
    /// <param name="value">想要夾帶的值 The value to response with.</param>
    /// <param name="message">想要夾帶的訊息 The message to response with.</param>
    public static Response<T> Ok(
        T      value   = default,
        string message = "") {
        return new Response<T> {
            status    = 200,
            errorCode = 0,
            message   = message,
            value     = value,
        };
    }

    /// <summary>
    ///     回應 NotOk。
    ///     Responses a not-ok.
    /// </summary>
    public static Response<T> NotOk(
        int    statusCode,
        int    errorCode,
        string message = "") {
        return new Response<T> {
            status    = (int)statusCode,
            errorCode = (int)errorCode,
            message   = message,
            value     = default,
        };
    }

    public static implicit operator Response<T>(T value) {
        return new Response<T> {
            status    = 200,
            errorCode = 0,
            value     = value
        };
    }
}