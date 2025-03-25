namespace Root.Common;

/// <summary>
///     一個無接收任何參數產生<see cref="Task{T}"/>的函式類型
///     A function only produces <see cref="Task{T}"/>
/// </summary>
public delegate Task<TResult> PipeOut<TResult>();

/// <summary>
///     一個函式類型收<see cref="TArg"/>產<see cref="Task{TResult}"/>
///     A function receiving <see cref="TArg"/> is used to produce <see cref="Task{TResult}"/>
/// </summary>
public delegate Task<TResult> Convert<in TArg, TResult>(TArg t);

/// <summary>
///     一個函式類型收<see cref="TArg1"/>、<see cref="TArg2"/>產<see cref="Task{TResult}"/>
///     A function receiving <see cref="TArg1"/>, <see cref="TArg2"/> is used to produce <see cref="Task{TResult}"/>
/// </summary>
public delegate Task<TResult> Convert<in TArg1, in TArg2, TResult>(TArg1 t, TArg2 u);

/// <summary>
///     一個函式類型收<see cref="TResult"/>產<see cref="Task{TResult}"/>
///     A function receiving <see cref="TResult"/> is used to produce <see cref="Task{TResult}"/>
/// </summary>
public delegate Task<TResult> Pipe<TResult>(TResult t);

/// <summary>
///     一個函式類型收<see cref="TResult"/>、<see cref="TArg"/>產<see cref="Task{TResult}"/>
///     A function receiving <see cref="TResult"/>, <see cref="TArg"/> is used to produce <see cref="Task{TResult}"/>
/// </summary>
public delegate Task<TResult> Pipe<TResult, in TArg>(TResult t, TArg u);

public static class Pipe {
    public static PipeOut<T> Start<T>(T arg) {
        return async () => arg;
    }
}