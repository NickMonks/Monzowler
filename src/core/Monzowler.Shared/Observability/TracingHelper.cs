using System.Diagnostics;
using System.Reflection;

namespace Monzowler.Shared.Observability;

public static class TracingHelper
{
    private const string SourceName = "Monzowler";
    public static readonly ActivitySource Source = new ActivitySource(SourceName);

    /// <summary>
    /// Utility method that uses reflection to take the property name and values and sets in the
    /// tag of the current span. It also needs to return disposable, so if the activity is null
    /// make sure we return a type disposable 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="tagSource"></param>
    /// <returns></returns>
    public static Activity? StartSpanWithActivity(string name, object? tagSource = null)
    {
        var activity = Source.StartActivity(name);
        if (activity != null && tagSource != null)
        {
            var type = tagSource.GetType();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(tagSource);
                if (value != null)
                {
                    activity.SetTag(prop.Name, value.ToString());
                }
            }
        }

        return activity;
    }

    public static IDisposable StartSpan(string name, object? tagSource = null)
    {
        var activity = TracingHelper.Source.StartActivity(name);
        if (activity != null && tagSource != null)
        {
            var type = tagSource.GetType();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(tagSource);
                if (value != null)
                {
                    activity.SetTag(prop.Name, value.ToString());
                }
            }
        }

        return (IDisposable?)activity ?? new Disposable();
    }

    public static IDisposable StartSpan(string name, Dictionary<string, object>? tags = null)
    {
        var activity = TracingHelper.Source.StartActivity(name);
        if (activity != null && tags != null)
        {
            foreach (var tag in tags)
                activity.SetTag(tag.Key, tag.Value);
        }

        return (IDisposable?)activity ?? new Disposable();

    }

    private class Disposable : IDisposable
    {
        public void Dispose() { }
    }
}



