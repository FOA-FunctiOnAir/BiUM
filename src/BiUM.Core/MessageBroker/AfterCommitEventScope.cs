using System;
using System.Collections.Generic;
using System.Threading;

namespace BiUM.Core.MessageBroker;

public sealed class AfterCommitEventScope
{
    private static readonly AsyncLocal<AfterCommitEventScope?> Ambient = new();

    private readonly List<IBaseEvent> _events = [];
    private readonly Dictionary<Type, object> _states = [];

    private AfterCommitEventScope()
    {
    }

    public static AfterCommitEventScope? Current => Ambient.Value;

    public static AfterCommitEventScope? BeginOwned()
    {
        if (Ambient.Value is not null)
        {
            return null;
        }

        var scope = new AfterCommitEventScope();

        Ambient.Value = scope;

        return scope;
    }

    public void Enqueue(IBaseEvent message)
    {
        lock (_events)
        {
            _events.Add(message);
        }
    }

    public T GetOrAddState<T>(Func<T> factory)
        where T : class
    {
        lock (_events)
        {
            if (_states.TryGetValue(typeof(T), out var state))
            {
                return (T)state;
            }

            var created = factory();

            _states[typeof(T)] = created;

            return created;
        }
    }

    public IReadOnlyList<IBaseEvent> Drain()
    {
        lock (_events)
        {
            var events = _events.ToArray();

            _events.Clear();
            _states.Clear();

            return events;
        }
    }

    public void Discard()
    {
        lock (_events)
        {
            _events.Clear();
            _states.Clear();
        }
    }

    public void End()
    {
        if (ReferenceEquals(Ambient.Value, this))
        {
            Ambient.Value = null;
        }
    }
}