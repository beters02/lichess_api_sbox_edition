#nullable enable annotations

using LichessNET.Internal;

namespace LichessNET.API;

/// <summary>
/// The APIRatelimitController class manages and enforces rate limiting
/// for API requests to prevent abuse and ensure fair usage of system resources.
/// </summary>
public class ApiRatelimitController
{
    private sealed class Bucket
    {
        private readonly int _capacity;
        private readonly int _refillAmount;
        private readonly TimeSpan _interval;
        private int _tokens;
        private DateTime _lastRefill;

        public Bucket(int capacity, int refillAmount, TimeSpan interval)
        {
            _capacity = Math.Max(1, capacity);
            _refillAmount = Math.Max(1, refillAmount);
            _interval = interval;
            _tokens = _capacity;
            _lastRefill = DateTime.UtcNow;
        }

        public async Task Consume(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Refill();
                if (_tokens > 0)
                {
                    _tokens--;
                    return;
                }

                var wait = _interval - (DateTime.UtcNow - _lastRefill);
                await Task.Delay(wait > TimeSpan.Zero ? wait : TimeSpan.FromMilliseconds(100),
                    cancellationToken);
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            if (now - _lastRefill < _interval)
                return;

            var intervals = Math.Max(1, (int)((now - _lastRefill).Ticks / _interval.Ticks));
            _tokens = Math.Min(_capacity, _tokens + intervals * _refillAmount);
            _lastRefill = now;
        }
    }

    private static readonly LichessLog Logger = new("APIRateLimitController");
    private readonly Dictionary<string, Bucket> _buckets = new();
    private readonly Bucket _defaultBucket = new(5, 5, TimeSpan.FromSeconds(15));
    private int _pipedRequests;
    private DateTime _rateLimitedUntil = DateTime.MinValue;

    public int PipedRequests
    {
        get => _pipedRequests;
        internal set
        {
            _pipedRequests = value;
            if (PipedRequests > 5)
                Logger.Warning($"Currently there are {PipedRequests} requests in queue. Either the API is blocking requests, or the client is sending too many requests.");
        }
    }

    public void ReportBlock(int seconds = 60)
    {
        Logger.Warning("API call reported ratelimit block for " + seconds + " seconds");
        _rateLimitedUntil = DateTime.UtcNow.AddSeconds(seconds);
    }

    public void RegisterBucket(string endpointUrl, int capacity, int refillAmount, TimeSpan interval)
    {
        _buckets[endpointUrl] = new Bucket(capacity, refillAmount, interval);
    }

    public Task Consume()
    {
        return Consume(CancellationToken.None);
    }

    public Task Consume(CancellationToken cancellationToken)
    {
        return Consume(string.Empty, true, cancellationToken);
    }

    public Task Consume(string endpointUrl, bool consumeDefaultBucket)
    {
        return Consume(endpointUrl, consumeDefaultBucket, CancellationToken.None);
    }

    public async Task Consume(string endpointUrl, bool consumeDefaultBucket,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PipedRequests++;
        try
        {
            if (_rateLimitedUntil > DateTime.UtcNow)
            {
                var wait = _rateLimitedUntil - DateTime.UtcNow;
                Logger.Warning($"Endpoint call to {endpointUrl} blocked due to ratelimit. Waiting for {wait.TotalMilliseconds:0} ms.");
                await Task.Delay(wait, cancellationToken);
            }

            if (consumeDefaultBucket)
                await _defaultBucket.Consume(cancellationToken);

            if (!string.IsNullOrWhiteSpace(endpointUrl) &&
                _buckets.TryGetValue(endpointUrl.TrimStart('/'), out var bucket))
                await bucket.Consume(cancellationToken);
        }
        finally
        {
            PipedRequests--;
        }
    }
}



