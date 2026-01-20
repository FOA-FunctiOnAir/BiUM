using BiApp.Test.Contract.Models.Rpc;
using MagicOnion;

namespace BiApp.Test.Contract.Services.Rpc;

public interface ITestRpcService : IService<ITestRpcService>
{
    UnaryResult<GetCurrencyResponse> GetCurrency(GetCurrencyRequest request);
}
