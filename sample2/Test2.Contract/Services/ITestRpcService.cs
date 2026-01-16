using BiUM.Test.Contract.Models;
using MagicOnion;

namespace BiUM.Test2.Contract.Services;

public interface ITestRpcService : IService<ITestRpcService>
{
    UnaryResult<GetCurrencyResponse> GetCurrency(GetCurrencyRequest request);
}
