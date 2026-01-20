using BiApp.Test2.Contract.Models.Rpc;
using MagicOnion;

namespace BiApp.Test2.Contract.Services.Rpc;

public interface ITestRpcService : IService<ITestRpcService>
{
    UnaryResult<GetCurrencyResponse> GetCurrency(GetCurrencyRequest request);
}
