using BiUM.Core.Compensation;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Services.Compensation;
using BiUM.Specialized.Services.Crud;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using System.Reflection;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class CompensatableApiActionFilterTests
{
    [Fact]
    public async Task Non_controller_action_descriptor_calls_next_without_touching_compensation()
    {
        var accessor = new TestCorrelationContextAccessor();
        var crud = new Mock<ICrudService>(MockBehavior.Strict);
        var compensation = new Mock<ICompensationService>(MockBehavior.Strict);
        var publisher = new Mock<ICompensationSessionFinalizedPublisher>(MockBehavior.Strict);

        var filter = new CompensatableApiActionFilter(accessor, crud.Object, compensation.Object, publisher.Object);

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var controllerStub = new object();
        var executing = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controllerStub);

        var called = false;
        await filter.OnActionExecutionAsync(executing, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], controllerStub));
        });

        called.Should().BeTrue();
    }

    [Fact]
    public async Task Compensatable_on_non_CrudController_with_incoming_session_does_not_finalize()
    {
        // BPMN orchestrator sends a session → this API is a participant, not the finalizer.
        var incomingSession = Guid.NewGuid();
        var accessor = new TestCorrelationContextAccessor
        {
            CorrelationContext = CorrelationTestHelper.CreateBpmnLike(
                Guid.NewGuid(), Guid.NewGuid(), compensationSessionId: incomingSession)
        };

        var crud = new Mock<ICrudService>(MockBehavior.Strict);
        var compensation = new Mock<ICompensationService>(MockBehavior.Strict);
        var publisher = new Mock<ICompensationSessionFinalizedPublisher>(MockBehavior.Strict);

        var filter = new CompensatableApiActionFilter(accessor, crud.Object, compensation.Object, publisher.Object);

        var t = typeof(MarkedCompensatableController);
        var cad = new ControllerActionDescriptor
        {
            ControllerTypeInfo = t.GetTypeInfo(),
            MethodInfo = t.GetMethod(nameof(MarkedCompensatableController.Post))!,
            ActionName = nameof(MarkedCompensatableController.Post),
            ControllerName = nameof(MarkedCompensatableController)
        };

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), cad);
        var controllerStub = new object();
        var executing = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controllerStub);

        await filter.OnActionExecutionAsync(executing, () =>
            Task.FromResult(new ActionExecutedContext(actionContext, [], controllerStub)));

        // Strict mocks: neither CommitSessionAsync nor RollbackSessionAsync should be called.
    }

    [Fact]
    public async Task Compensatable_on_non_CrudController_without_incoming_session_self_orchestrates()
    {
        // No incoming session → API self-orchestrates: creates session, commits on success.
        var accessor = new TestCorrelationContextAccessor
        {
            CorrelationContext = CorrelationTestHelper.CreateBpmnLike(Guid.NewGuid(), Guid.NewGuid())
        };

        var crud = new Mock<ICrudService>(MockBehavior.Strict);
        var compensation = new Mock<ICompensationService>(MockBehavior.Strict);
        var publisher = new Mock<ICompensationSessionFinalizedPublisher>(MockBehavior.Strict);

        compensation
            .Setup(c => c.CommitSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        publisher
            .Setup(p => p.PublishAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var filter = new CompensatableApiActionFilter(accessor, crud.Object, compensation.Object, publisher.Object);

        var t = typeof(MarkedCompensatableController);
        var cad = new ControllerActionDescriptor
        {
            ControllerTypeInfo = t.GetTypeInfo(),
            MethodInfo = t.GetMethod(nameof(MarkedCompensatableController.Post))!,
            ActionName = nameof(MarkedCompensatableController.Post),
            ControllerName = nameof(MarkedCompensatableController)
        };

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), cad);
        var controllerStub = new object();
        var executing = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controllerStub);

        await filter.OnActionExecutionAsync(executing, () =>
            Task.FromResult(new ActionExecutedContext(actionContext, [], controllerStub)));

        compensation.Verify(c => c.CommitSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(p => p.PublishAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [CompensatableApi]
    public sealed class MarkedCompensatableController : ControllerBase
    {
        public IActionResult Post() => Ok();
    }
}