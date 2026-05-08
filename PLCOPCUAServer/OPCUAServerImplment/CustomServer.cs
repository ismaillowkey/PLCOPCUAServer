using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace PLCOPCUAServer.WorkerServices;

public class CustomServer : StandardServer
{
    private readonly Action<GenericNodeManager> _onNodeManagerCreated;

    public CustomServer(Action<GenericNodeManager> onCreated)
    {
        _onNodeManagerCreated = onCreated;
    }

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration config)
    {
        var nm = new GenericNodeManager(server, config);
        _onNodeManagerCreated(nm);
        return new MasterNodeManager(server, config, null, nm);
    }
}