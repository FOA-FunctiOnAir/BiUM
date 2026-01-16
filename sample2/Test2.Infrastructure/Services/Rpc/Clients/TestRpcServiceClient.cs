using BiUM.Specialized.MagicOnion;
using BiUM.Test2.Contract.Services;
using MagicOnion.Client;

namespace BiApp.Infrastructure.Services.Rpc.Clients;

[MagicOnionClientGeneration(typeof(ITestRpcService), Serializer = MagicOnionClientGenerationAttribute.GenerateSerializerType.MemoryPack)]
public sealed partial class TestRpcServiceClient : IMagicOnionRpcClient<ITestRpcService>;
