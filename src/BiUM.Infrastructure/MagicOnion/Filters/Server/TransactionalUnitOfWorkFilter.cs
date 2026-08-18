using BiUM.Core.Database;
using MagicOnion.Server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.MagicOnion.Filters.Server;

public class TransactionalUnitOfWorkFilter : MagicOnionFilterAttribute
{
    public TransactionalUnitOfWorkFilter()
    {
        Order = 100;
    }

    public override async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        var runner = context.ServiceProvider.GetService<ITransactionalUnitOfWorkRunner>();

        if (runner is null)
        {
            await next.Invoke(context);

            return;
        }

        await runner.RunAsync(() => next.Invoke(context).AsTask());
    }
}