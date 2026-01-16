using BiUM.Test.Contract.Models;
using MagicOnion;

namespace BiUM.Test.Contract.Services;

public interface ITestRpcService : IService<ITestRpcService>
{
    UnaryResult<GetCurrencyResponse> GetCurrency(GetCurrencyRequest request);
}
