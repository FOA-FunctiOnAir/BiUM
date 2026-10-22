using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.Events;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.MessageBroker;

public sealed class AfterCommitEventScopeTests
{
    [Fact]
    public void BeginOwned_ShouldReturnNull_WhenScopeIsAlreadyActive()
    {
        var owner = AfterCommitEventScope.BeginOwned();

        try
        {
            owner.Should().NotBeNull();
            AfterCommitEventScope.Current.Should().BeSameAs(owner);
            AfterCommitEventScope.BeginOwned().Should().BeNull();
        }
        finally
        {
            owner!.End();
        }

        AfterCommitEventScope.Current.Should().BeNull();
    }

    [Fact]
    public void Drain_ShouldReturnEnqueuedEventsInOrder_AndResetState()
    {
        var scope = AfterCommitEventScope.BeginOwned()!;

        try
        {
            var first = new EntityChangesCommittedEvent { Domain = "first" };
            var second = new EntityChangesCommittedEvent { Domain = "second" };

            scope.Enqueue(first);
            scope.Enqueue(second);

            var state = scope.GetOrAddState(() => new object());

            scope.GetOrAddState(() => new object()).Should().BeSameAs(state);
            scope.Drain().Should().Equal(first, second);
            scope.Drain().Should().BeEmpty();
            scope.GetOrAddState(() => new object()).Should().NotBeSameAs(state);
        }
        finally
        {
            scope.End();
        }
    }

    [Fact]
    public void Discard_ShouldDropEnqueuedEvents()
    {
        var scope = AfterCommitEventScope.BeginOwned()!;

        try
        {
            scope.Enqueue(new EntityChangesCommittedEvent());
            scope.Discard();

            scope.Drain().Should().BeEmpty();
        }
        finally
        {
            scope.End();
        }
    }

    [Fact]
    public async Task Current_ShouldFlowIntoAwaitedCalls()
    {
        var scope = AfterCommitEventScope.BeginOwned()!;

        try
        {
            var observed = await Task.Run(() => AfterCommitEventScope.Current);

            observed.Should().BeSameAs(scope);
        }
        finally
        {
            scope.End();
        }
    }
}