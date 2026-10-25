using System.Diagnostics;

namespace AuditService.Infrastructure.IntegrationTests.TestDoubles;

public sealed class ActivitySourceListener : IDisposable
{
    private readonly ActivityListener _listener;

    public ActivitySourceListener(params string[] sourceNames)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => sourceNames.Contains(source.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();
}
