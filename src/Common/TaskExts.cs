namespace Root.Common;

/// <summary>
///     To convert every delegate to a single <see cref="PipeOut{T}"/>,
///     also chains them together.
/// </summary>
/// <remarks>
///     If you find out that it is hard to add breakpoint,
///     be free to test your chained functionality in unit-test.
/// </remarks>
public static class TaskExts {
    /// <summary>
    ///     Starts chaining delegates from <see cref="tuple"/>.
    ///     On the inside, it passes <see cref="tArg"/> to <see cref="tFunc"/>.
    /// </summary>
    /// <returns>
    ///     <see cref="PipeOut{TResult}"/> for chaining.
    /// </returns>
    public static PipeOut<TResult> Start<TArg, TResult>(this (Convert<TArg, TResult> tFunc, TArg tArg) tuple) {
        return async () => await tuple.tFunc(tuple.tArg);
    }

    /// <summary>
    ///     Starts chaining delegates from <see cref="tuple"/>.
    ///     On the inside, it passes <see cref="tArg1"/>, <see cref="tArg2"/> to <see cref="tFunc"/>.
    /// </summary>
    /// <returns>
    ///     <see cref="PipeOut{TResult}"/> for chaining.
    /// </returns>
    public static PipeOut<TResult> Start<TArg1, TArg2, TResult>(
    this (Convert<TArg1, TArg2, TResult> tFunc, TArg1 tArg1, TArg2 tArg2) tuple) {
        return async () => await tuple.tFunc(tuple.tArg1, tuple.tArg2);
    }

    /// <summary>
    ///     Passes <see cref="previous"/>'s <see cref="TResult"/> to <see cref="nextToCall"/>.
    /// </summary>
    /// <returns>
    ///     <see cref="PipeOut{TResult}"/> for chaining.
    /// </returns>
    public static PipeOut<TResult> Pass<TResult>(
    this PipeOut<TResult> previous,
    Pipe<TResult>         nextToCall) {
        return async () => {
            var resultToPass = await previous();
            return await nextToCall(resultToPass);
        };
    }

    /// <summary>
    ///     Passes <see cref="previous"/>'s <see cref="TArg"/> to <see cref="nextToCall"/>.
    /// </summary>
    /// <returns>
    ///     <see cref="PipeOut{TResult}"/> for chaining.
    /// </returns>
    public static PipeOut<TResult> Pass<TArg, TResult>(
    this PipeOut<TArg>     previous,
    Convert<TArg, TResult> nextToCall) {
        return async () => {
            var argToPass = await previous();
            return await nextToCall(argToPass);
        };
    }

    /// <summary>
    ///     Passes <see cref="previous"/>'s <see cref="TArg"/> to <see cref="nextToCall"/>'s <see cref="tFunc"/>.
    ///     Also, <see cref="TArg"/> is passed to <see cref="tFunc"/> in the same time.
    /// </summary>
    /// <returns>
    ///     <see cref="PipeOut{TResult}"/> for chaining.
    /// </returns>
    public static PipeOut<TResult> Pass<TResult, TArg>(
    this PipeOut<TResult>                  previous,
    (Pipe<TResult, TArg> tFunc, TArg tArg) nextToCall) {
        return async () => await nextToCall.tFunc(await previous(), nextToCall.tArg);
    }

    /// <summary>
    ///     Calls <see cref="converter"/> to change <see cref="TArg"/> to <see cref="TResult"/>.
    /// </summary>
    /// <param name="converter">
    ///     A lambda to change type.
    /// </param>
    /// <returns>
    ///     <see cref="PipeOut{TResult}"/> for chaining.
    /// </returns>
    /// <remarks>
    ///     Use this when next chaining-target has different input types.
    /// </remarks>
    public static PipeOut<TResult> Function<TArg, TResult>(
    this PipeOut<TArg>  previous,
    Func<TArg, TResult> converter) => async () =>
        converter(await previous());

    public static PipeOut<TResult> FunctionAsync<TArg, TResult>(
    this PipeOut<TArg>        previous,
    Func<TArg, Task<TResult>> converter) => async () =>
        await converter(await previous());

    /// <summary>
    ///     Calls <see cref="converter"/> to change <see cref="TArg"/> to <see cref="TResult"/>.
    /// </summary>
    /// <param name="converter">
    ///     A lambda to change type.
    /// </param>
    /// <returns>
    ///     <see cref="PipeOut{TResult}"/> for chaining.
    /// </returns>
    /// <remarks>
    ///     Use this when next chaining-target has different input types.
    /// </remarks>
    public static PipeOut<TResult> Function<TArg, TResult>(
    this PipeOut<TArg> previous,
    Func<TResult>      converter) {
        return async () => {
            await previous(); // 這個必須保留喔。
            return converter();
        };
    }

    /// <summary>
    ///     Calls <see cref="final"/> to produce <see cref="Task{TResult}"/>
    /// </summary>
    /// <param name="final">final <see cref="PipeOut{TResult}"/></param>
    /// <typeparam name="TResult">final result</typeparam>
    /// <returns>async <see cref="Task{TResult}"/></returns>
    public static Task<TResult> ToTask<TResult>(this PipeOut<TResult> final) {
        return final();
    }

    public static PipeOut<TResult> Delay<TResult>(this PipeOut<TResult> previous, TimeSpan delay) {
        return async () => {
            await Task.Delay(delay);
            return await previous();
        };
    }

    public static PipeOut<TResult> Log<TResult>(this PipeOut<TResult> previous, string log) {
        return async () => {
            Console.WriteLine(log);
            return await previous();
        };
    }
    
    public static async Task<TResult> Guard<TResult>(this Task<TResult> task) {
        try {
            var result = await task;
            return result;
        }
        catch {
            return default;
        }
    }
}
