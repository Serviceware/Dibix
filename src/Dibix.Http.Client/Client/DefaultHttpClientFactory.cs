using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace Dibix.Http.Client
{
    /// <summary>
    /// Roughly inspired by https://github.com/dotnet/runtime/blob/0712aebead42b6605fda32687b341de08f530a3c/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs
    /// </summary>
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        #region Fields
        private static readonly TraceSource TraceSource = new TraceSource(typeof(DefaultHttpClientFactory).FullName);
        private static readonly TimerCallback CleanupCallback = s => ((DefaultHttpClientFactory)s).CleanupTimer_Tick();
        private readonly Uri _baseAddress;
        private readonly HttpRequestTracer _httpRequestTracer;

        // Default time of 10s for cleanup seems reasonable.
        // Quick math:
        // 10 distinct named clients * expiry time >= 1s = approximate cleanup queue of 100 items
        //
        // This seems frequent enough. We also rely on GC occurring to actually trigger disposal.
        private static readonly TimeSpan DefaultCleanupInterval = TimeSpan.FromSeconds(10);

        // We use a new timer for each regular cleanup cycle, protected with a lock. Note that this scheme
        // doesn't give us anything to dispose, as the timer is started/stopped as needed.
        //
        // There's no need for the factory itself to be disposable. If you stop using it, eventually everything will
        // get reclaimed.
        private Timer _cleanupTimer;
        private readonly object _cleanupTimerLock;
        private readonly object _cleanupActiveLock;

        // Collection of 'active' handlers.
        //
        // Using lazy for synchronization to ensure that only one instance of HttpMessageHandler is created
        // for each name.
        private readonly ConcurrentDictionary<string, Lazy<ActiveHandlerTrackingEntry>> _activeHandlers;

        // Collection of 'expired' but not yet disposed handlers.
        //
        // Used when we're rotating handlers so that we can dispose HttpMessageHandler instances once they
        // are eligible for garbage collection.
        private readonly ConcurrentQueue<ExpiredHandlerTrackingEntry> _expiredHandlers;
        private readonly TimerCallback _expiryCallback;
        #endregion

        #region Properties
        public bool FollowRedirectsForGetRequests { get; set; } = true;
        #endregion

        #region Constructor
        public DefaultHttpClientFactory(Uri baseAddress = null, HttpRequestTracer httpRequestTracer = null)
        {
            this._baseAddress = baseAddress;
            this._httpRequestTracer = httpRequestTracer;

            this._activeHandlers = new ConcurrentDictionary<string, Lazy<ActiveHandlerTrackingEntry>>(StringComparer.Ordinal);

            this._expiredHandlers = new ConcurrentQueue<ExpiredHandlerTrackingEntry>();
            this._expiryCallback = ExpiryTimer_Tick;

            this._cleanupTimerLock = new object();
            this._cleanupActiveLock = new object();
        }
        #endregion

        #region IHttpClientFactory Members
        public HttpClient CreateClient() => this.CreateClient(this._baseAddress);
        public HttpClient CreateClient(Uri baseAddress)
        {
            const string name = "";
            HttpClientBuilder builder = new HttpClientBuilder();
            ICollection<Action<HttpClient>> postActions = new Collection<Action<HttpClient>>();
            this.ConfigureClientCore(builder, postActions);
            this.ConfigureClient(builder);

            // Wrap the handler so we can ensure the inner handler outlives the outer handler.
            HttpMessageHandler handler = this.CreateHandler(builder, name);
            HttpClient client = new HttpClient(handler, disposeHandler: false);
            client.BaseAddress = baseAddress;
            builder.ConfigureClient(client);

            foreach (Action<HttpClient> postAction in postActions)
                postAction(client);

            return client;
        }
        #endregion

        #region Protected Methods
        protected virtual void ConfigureClient(IHttpClientBuilder builder) { }
        #endregion

        #region Private Methods
        private void ConfigureClientCore(IHttpClientBuilder builder, ICollection<Action<HttpClient>> postActions)
        {
            builder.AddHttpMessageHandler<EnsureSuccessStatusCodeHttpMessageHandler>();
            
            if (this.FollowRedirectsForGetRequests)
                builder.AddHttpMessageHandler<FollowRedirectHttpMessageHandler>();
            
            builder.AddHttpMessageHandler<TraceProxyHttpMessageHandler>();

            if (this._httpRequestTracer != null)
                builder.AddHttpMessageHandler(new TracingHttpMessageHandler(this._httpRequestTracer));

            TimeoutHttpMessageHandler timeoutHandler = new TimeoutHttpMessageHandler();
            builder.AddHttpMessageHandler(timeoutHandler);
            postActions.Add(x =>
            {
                timeoutHandler.Timeout = x.Timeout;
                x.Timeout = Timeout.InfiniteTimeSpan;
            });
        }

        private HttpMessageHandler CreateHandler(HttpClientBuilder builder, string name)
        {
            Lazy<ActiveHandlerTrackingEntry> CreateEntry(string key) => new Lazy<ActiveHandlerTrackingEntry>(() => CreateHandlerEntry(builder, key), LazyThreadSafetyMode.ExecutionAndPublication);

            ActiveHandlerTrackingEntry entry = _activeHandlers.GetOrAdd(name, CreateEntry).Value;

            StartHandlerEntryTimer(entry);

            return entry.Handler;
        }

        private static ActiveHandlerTrackingEntry CreateHandlerEntry(HttpClientBuilder builder, string name)
        {
            // Wrap the handler so we can ensure the inner handler outlives the outer handler.
            var handler = new LifetimeTrackingHttpMessageHandler(CreateHandlerPipeline(builder));

            // Note that we can't start the timer here. That would introduce a very very subtle race condition
            // with very short expiry times. We need to wait until we've actually handed out the handler once
            // to start the timer.
            //
            // Otherwise it would be possible that we start the timer here, immediately expire it (very short
            // timer) and then dispose it without ever creating a client. That would be bad. It's unlikely
            // this would happen, but we want to be sure.
            return new ActiveHandlerTrackingEntry(name, handler);
        }

        private void ExpiryTimer_Tick(object state)
        {
            var active = (ActiveHandlerTrackingEntry)state;

            // The timer callback should be the only one removing from the active collection. If we can't find
            // our entry in the collection, then this is a bug.
            bool removed = _activeHandlers.TryRemove(active.Name, out Lazy<ActiveHandlerTrackingEntry> found);
            Debug.Assert(removed, "Entry not found. We should always be able to remove the entry");
            Debug.Assert(object.ReferenceEquals(active, found.Value), "Different entry found. The entry should not have been replaced");

            // At this point the handler is no longer 'active' and will not be handed out to any new clients.
            // However we haven't dropped our strong reference to the handler, so we can't yet determine if
            // there are still any other outstanding references (we know there is at least one).
            //
            // We use a different state object to track expired handlers. This allows any other thread that acquired
            // the 'active' entry to use it without safety problems.
            var expired = new ExpiredHandlerTrackingEntry(active);
            _expiredHandlers.Enqueue(expired);

            TraceSource.TraceInformation($"HttpMessageHandler expired after {active.Lifetime}ms for client '{active.Name}'");

            StartCleanupTimer();
        }

        private void StartHandlerEntryTimer(ActiveHandlerTrackingEntry entry) => entry.StartExpiryTimer(_expiryCallback);

        private void StartCleanupTimer()
        {
            lock (_cleanupTimerLock)
            {
                if (_cleanupTimer == null)
                {
                    _cleanupTimer = NonCapturingTimer.Create(CleanupCallback, this, DefaultCleanupInterval, Timeout.InfiniteTimeSpan);
                }
            }
        }

        private void StopCleanupTimer()
        {
            lock (_cleanupTimerLock)
            {
                _cleanupTimer.Dispose();
                _cleanupTimer = null;
            }
        }

        private void CleanupTimer_Tick()
        {
            // Stop any pending timers, we'll restart the timer if there's anything left to process after cleanup.
            //
            // With the scheme we're using it's possible we could end up with some redundant cleanup operations.
            // This is expected and fine.
            //
            // An alternative would be to take a lock during the whole cleanup process. This isn't ideal because it
            // would result in threads executing ExpiryTimer_Tick as they would need to block on cleanup to figure out
            // whether we need to start the timer.
            StopCleanupTimer();

            if (!Monitor.TryEnter(_cleanupActiveLock))
            {
                // We don't want to run a concurrent cleanup cycle. This can happen if the cleanup cycle takes
                // a long time for some reason. Since we're running user code inside Dispose, it's definitely
                // possible.
                //
                // If we end up in that position, just make sure the timer gets started again. It should be cheap
                // to run a 'no-op' cleanup.
                StartCleanupTimer();
                return;
            }

            try
            {
                int initialCount = _expiredHandlers.Count;
                TraceSource.TraceInformation($"Starting HttpMessageHandler cleanup cycle with {initialCount} items");

                var stopwatch = ValueStopwatch.StartNew();

                int disposedCount = 0;
                for (int i = 0; i < initialCount; i++)
                {
                    // Since we're the only one removing from _expired, TryDequeue must always succeed.
                    _expiredHandlers.TryDequeue(out ExpiredHandlerTrackingEntry entry);
                    Debug.Assert(entry != null, "Entry was null, we should always get an entry back from TryDequeue");

                    if (entry.CanDispose)
                    {
                        try
                        {
                            entry.InnerHandler.Dispose();
                            disposedCount++;
                        }
                        catch (Exception ex)
                        {
                            TraceSource.TraceInformation($@"HttpMessageHandler.Dispose() threw an unhandled exception for client: '{entry.Name}'
{ex}");
                        }
                    }
                    else
                    {
                        // If the entry is still live, put it back in the queue so we can process it
                        // during the next cleanup cycle.
                        _expiredHandlers.Enqueue(entry);
                    }
                }

                TraceSource.TraceInformation($"Ending HttpMessageHandler cleanup cycle after {stopwatch.GetElapsedTime()}ms - processed: {disposedCount} items - remaining: {_expiredHandlers.Count} items");
            }
            finally
            {
                Monitor.Exit(_cleanupActiveLock);
            }

            // We didn't totally empty the cleanup queue, try again later.
            if (!_expiredHandlers.IsEmpty)
            {
                StartCleanupTimer();
            }
        }

        private static HttpMessageHandler CreateHandlerPipeline(HttpClientBuilder clientBuilder)
        {
            // Subsequent handlers, that make their own requests with the same tracer, might overwrite the last request trace
            // Therefore we move the tracing handler before user handlers
            for (int i = 0; i < clientBuilder.AdditionalHandlers.Count; i++)
            {
                DelegatingHandler handler = clientBuilder.AdditionalHandlers[i];
                if (!(handler is TracingHttpMessageHandler))
                    continue;

                clientBuilder.AdditionalHandlers.RemoveAt(i);
                clientBuilder.AdditionalHandlers.Add(handler);
                break;
            }

            HttpMessageHandler next = clientBuilder.PrimaryHttpMessageHandler;
            for (int i = clientBuilder.AdditionalHandlers.Count - 1; i >= 0; i--)
            {
                DelegatingHandler handler = clientBuilder.AdditionalHandlers[i];

                // Checking for this allows us to catch cases where someone has tried to re-use a handler. That really won't
                // work the way you want and it can be tricky for callers to figure out.
                if (handler.InnerHandler != null)
                {
                    string message = $@"The '{nameof(DelegatingHandler.InnerHandler)}' property must be null. '{nameof(DelegatingHandler)}' instances provided to '{nameof(IHttpClientBuilder)}' must not be reused or cached.
Handler: '{handler}'";
                    throw new InvalidOperationException(message);
                }

                handler.InnerHandler = next;
                next = handler;
            }

            return next;
        }
        #endregion

        #region Nested Types
        private sealed class HttpClientBuilder : IHttpClientBuilder
        {
            private readonly ICollection<Action<HttpClient>> _clientActions;

            public HttpMessageHandler PrimaryHttpMessageHandler { get; set; } = new HttpClientHandler();
            public IList<DelegatingHandler> AdditionalHandlers { get; }

            public HttpClientBuilder()
            {
                this._clientActions = new Collection<Action<HttpClient>>();
                this.AdditionalHandlers = new Collection<DelegatingHandler>();
            }

            public IHttpClientBuilder ConfigureClient(Action<HttpClient> configure)
            {
                Guard.IsNotNull(configure, nameof(configure));
                this._clientActions.Add(configure);
                return this;
            }

            public IHttpClientBuilder ConfigureClient(HttpClient client)
            {
                foreach (Action<HttpClient> configure in this._clientActions) 
                    configure(client);

                return this;
            }

            public IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>() where THandler : HttpMessageHandler, new()
            {
                this.ConfigurePrimaryHttpMessageHandler(new THandler());
                return this;
            }
            public IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>(THandler handler) where THandler : HttpMessageHandler
            {
                this.PrimaryHttpMessageHandler = handler;
                return this;
            }

            public IHttpClientBuilder AddHttpMessageHandler<THandler>() where THandler : DelegatingHandler, new() => this.AddHttpMessageHandler(new THandler());
            public IHttpClientBuilder AddHttpMessageHandler(DelegatingHandler handler)
            {
                Guard.IsNotNull(handler, nameof(handler));
                this.AdditionalHandlers.Add(handler);
                return this;
            }

            public IHttpClientBuilder RemoveHttpMessageHandler<THandler>() where THandler : DelegatingHandler
            {
                foreach (THandler handler in this.AdditionalHandlers.OfType<THandler>().ToArray()) 
                    this.AdditionalHandlers.Remove(handler);

                return this;
            }
            public IHttpClientBuilder RemoveHttpMessageHandler(DelegatingHandler handler)
            {
                this.AdditionalHandlers.Remove(handler);
                return this;
            }

            public IHttpClientBuilder ClearHttpMessageHandlers()
            {
                this.AdditionalHandlers.Clear();
                return this;
            }
        }

        // Thread-safety: We treat this class as immutable except for the timer. Creating a new object
        // for the 'expiry' pool simplifies the threading requirements significantly.
        private sealed class ActiveHandlerTrackingEntry
        {
            private static readonly TimerCallback TimerCallback = s => ((ActiveHandlerTrackingEntry)s).Timer_Tick();
            private readonly object _lock;
            private bool _timerInitialized;
            private Timer _timer;
            private TimerCallback _callback;

            public LifetimeTrackingHttpMessageHandler Handler { get; }
            public TimeSpan Lifetime { get; }
            public string Name { get; }

            public ActiveHandlerTrackingEntry(string name, LifetimeTrackingHttpMessageHandler handler) : this(name, handler, lifetime: TimeSpan.FromMinutes(2)) { }
            public ActiveHandlerTrackingEntry(string name, LifetimeTrackingHttpMessageHandler handler, TimeSpan lifetime)
            {
                Name = name;
                Handler = handler;
                Lifetime = lifetime;
                _lock = new object();
            }

            public void StartExpiryTimer(TimerCallback callback)
            {
                if (Lifetime == Timeout.InfiniteTimeSpan)
                {
                    return; // never expires.
                }

                if (Volatile.Read(ref _timerInitialized))
                {
                    return;
                }

                StartExpiryTimerSlow(callback);
            }

            private void StartExpiryTimerSlow(TimerCallback callback)
            {
                Debug.Assert(Lifetime != Timeout.InfiniteTimeSpan);

                lock (_lock)
                {
                    if (Volatile.Read(ref _timerInitialized))
                    {
                        return;
                    }

                    _callback = callback;
                    _timer = NonCapturingTimer.Create(TimerCallback, this, Lifetime, Timeout.InfiniteTimeSpan);
                    _timerInitialized = true;
                }
            }

            private void Timer_Tick()
            {
                Debug.Assert(_callback != null);
                Debug.Assert(_timer != null);

                lock (_lock)
                {
                    if (_timer != null)
                    {
                        _timer.Dispose();
                        _timer = null;

                        _callback(this);
                    }
                }
            }
        }
        
        // Thread-safety: This class is immutable
        private sealed class ExpiredHandlerTrackingEntry
        {
            private readonly WeakReference _livenessTracker;

            public bool CanDispose => !_livenessTracker.IsAlive;
            public HttpMessageHandler InnerHandler { get; }
            public string Name { get; }

            // IMPORTANT: don't cache a reference to `other` or `other.Handler` here.
            // We need to allow it to be GC'ed.
            public ExpiredHandlerTrackingEntry(ActiveHandlerTrackingEntry other)
            {
                Name = other.Name;

                _livenessTracker = new WeakReference(other.Handler);
                InnerHandler = other.Handler.InnerHandler;
            }
        }

        // A convenience API for interacting with System.Threading.Timer in a way
        // that doesn't capture the ExecutionContext. We should be using this (or equivalent)
        // everywhere we use timers to avoid rooting any values stored in asynclocals.
        private static class NonCapturingTimer
        {
            public static Timer Create(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                if (callback == null)
                {
                    throw new ArgumentNullException(nameof(callback));
                }

                // Don't capture the current ExecutionContext and its AsyncLocals onto the timer
                bool restoreFlow = false;
                try
                {
                    if (!ExecutionContext.IsFlowSuppressed())
                    {
                        ExecutionContext.SuppressFlow();
                        restoreFlow = true;
                    }

                    return new Timer(callback, state, dueTime, period);
                }
                finally
                {
                    // Restore the current ExecutionContext
                    if (restoreFlow)
                    {
                        ExecutionContext.RestoreFlow();
                    }
                }
            }
        }

        private readonly struct ValueStopwatch
        {
            private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
            private readonly long _startTimestamp;

            public bool IsActive => _startTimestamp != 0;

            private ValueStopwatch(long startTimestamp)
            {
                _startTimestamp = startTimestamp;
            }

            public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());

            public TimeSpan GetElapsedTime()
            {
                // Start timestamp can't be zero in an initialized ValueStopwatch. It would have to be literally the first thing executed when the machine boots to be 0.
                // So it being 0 is a clear indication of default(ValueStopwatch)
                if (!IsActive)
                {
                    throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");
                }

                long end = Stopwatch.GetTimestamp();
                long timestampDelta = end - _startTimestamp;
                long ticks = (long)(TimestampToTicks * timestampDelta);
                return new TimeSpan(ticks);
            }
        }
        #endregion
    }
}