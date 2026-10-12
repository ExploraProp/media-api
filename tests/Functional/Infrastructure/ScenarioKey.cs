using Reqnroll;

namespace Media.Functional.Infrastructure;

/// <summary>
/// Typed ScenarioContext key: route-shaped (or ProblemDetails) string plus the expected payload type.
/// </summary>
public readonly record struct ScenarioKey<T>(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// Type-safe ScenarioContext helpers for <see cref="ScenarioKey{T}"/>.
/// Named <c>Put</c>/<c>Has</c> (not <c>Set</c>/<c>ContainsKey</c>) so they do not lose
/// overload resolution to Reqnroll's instance <c>Set&lt;T&gt;(T data, string id)</c> when
/// <typeparamref name="T"/> is <see cref="string"/>.
/// </summary>
public static class ScenarioKeyExtensions
{
    public static void Put<T>(this ScenarioContext context, ScenarioKey<T> key, T value) =>
        context[key.Value] = value!;

    public static T Get<T>(this ScenarioContext context, ScenarioKey<T> key) =>
        context.TryGetValue(key.Value, out var boxed) && boxed is T typed
            ? typed
            : throw new KeyNotFoundException(
                $"The given key '{key.Value}' was not present in the dictionary.");

    public static bool TryGetValue<T>(this ScenarioContext context, ScenarioKey<T> key, out T value)
    {
        if (context.TryGetValue(key.Value, out var boxed) && boxed is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    public static bool Has<T>(this ScenarioContext context, ScenarioKey<T> key) =>
        context.ContainsKey(key.Value);

    public static bool Remove<T>(this ScenarioContext context, ScenarioKey<T> key) =>
        context.Remove(key.Value);
}
