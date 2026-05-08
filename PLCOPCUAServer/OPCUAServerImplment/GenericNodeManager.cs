using Opc.Ua;
using Opc.Ua.Server;
using PLCOPCUAServer.Models;
using PLCOPCUAServer.Shared;
using static PLCOPCUAServer.Shared.GlobalData;

namespace PLCOPCUAServer.WorkerServices;

public class GenericNodeManager : CustomNodeManager2
{
    // KUNCINYA: Pake string (NodeId) biar gak tabrakan antar mesin
    private readonly Dictionary<string, BaseDataVariableState> _nodeLookup = new();
    private readonly Dictionary<string, BaseDataVariableState> _connectionNodes = new();
    private const string NamespaceUri = "http://ismail-automation.com/UA/IndustrialServer";

    public GenericNodeManager(IServerInternal s, ApplicationConfiguration c)
        : base(s, c, NamespaceUri)
    {
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        lock (Lock)
        {
            base.CreateAddressSpace(externalReferences);
            ushort nsIdx = (ushort)Server.NamespaceUris.GetIndexOrAppend(NamespaceUri);

            if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out IList<IReference> objRefs))
            {
                externalReferences[ObjectIds.ObjectsFolder] = objRefs = new List<IReference>();
            }

            foreach (var machine in GlobalData.MachineStates)
            {
                int dcId = machine.Key;
                var tags = machine.Value;
                var first = tags.FirstOrDefault();
                if (first == null) continue;

                string gName = $"Line{first.LineNo}_DC{dcId}";

                // Create Folder Mesin
                var folder = new FolderState(null)
                {
                    SymbolicName = gName,
                    NodeId = new NodeId(gName, nsIdx),
                    BrowseName = new QualifiedName(gName, nsIdx),
                    DisplayName = new LocalizedText(gName),
                    TypeDefinitionId = ObjectTypeIds.FolderType
                };

                folder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                objRefs.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, folder.NodeId));
                AddPredefinedNode(SystemContext, folder);

                // 2. _isConnected (Pake NodeId string biar unik per mesin)
                var isConnectedVar = new BaseDataVariableState(folder)
                {
                    SymbolicName = "_isConnected",
                    NodeId = new NodeId($"{gName}._isConnected", nsIdx),
                    BrowseName = new QualifiedName("_isConnected", nsIdx),
                    DisplayName = new LocalizedText("_isConnected"),
                    TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                    DataType = DataTypeIds.Boolean,
                    ValueRank = ValueRanks.Scalar,
                    AccessLevel = AccessLevels.CurrentRead,
                    UserAccessLevel = AccessLevels.CurrentRead,
                    Value = false,
                    StatusCode = StatusCodes.Good,
                    Timestamp = DateTime.UtcNow
                };

                folder.AddChild(isConnectedVar);
                AddPredefinedNode(SystemContext, isConnectedVar);

                // SIMPEN PAKE NODE ID STRING
                _connectionNodes[isConnectedVar.NodeId.ToString()] = isConnectedVar;

                // 3. Create Tags
                foreach (var tag in tags)
                {
                    var v = new BaseDataVariableState(folder)
                    {
                        SymbolicName = tag.TagName,
                        NodeId = new NodeId($"{gName}.{tag.TagName}", nsIdx),
                        BrowseName = new QualifiedName(tag.TagName, nsIdx),
                        DisplayName = new LocalizedText(tag.TagName),
                        TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                        DataType = GetDataTypeId(tag.DataTypeId),
                        ValueRank = ValueRanks.Scalar,
                        AccessLevel = AccessLevels.CurrentRead,
                        UserAccessLevel = AccessLevels.CurrentRead,
                        Value = tag.Value ?? GetDefaultValue(tag.DataTypeId),
                        StatusCode = StatusCodes.Good,
                        Timestamp = DateTime.UtcNow
                    };

                    folder.AddChild(v);
                    AddPredefinedNode(SystemContext, v);

                    // MAPPING PAKE NODE ID STRING (BIAR GAK KETUKER SAMA MESIN LAIN)
                    _nodeLookup[v.NodeId.ToString()] = v;
                }
            }
        }
    }

    public void SyncValues()
    {
        lock (Lock)
        {
            ushort nsIdx = (ushort)Server.NamespaceUris.GetIndexOrAppend(NamespaceUri);

            foreach (var machine in GlobalData.MachineStates)
            {
                int dcId = machine.Key;
                var tagList = machine.Value;
                string gName = $"Line{tagList.FirstOrDefault()?.LineNo}_DC{dcId}";

                // A. Update Tag PLC
                foreach (var dto in tagList)
                {
                    // Cari pake key yang sama persis pas Create
                    string nodeIdKey = new NodeId($"{gName}.{dto.TagName}", nsIdx).ToString();

                    if (_nodeLookup.TryGetValue(nodeIdKey, out var n))
                    {
                        var typedValue = CastToOpcType(dto.Value, dto.DataTypeId);

                        if (!Object.Equals(n.Value, typedValue) || n.Timestamp != dto.TimestampRead)
                        {
                            n.Value = typedValue;
                            n.Timestamp = dto.TimestampRead;
                            n.ClearChangeMasks(SystemContext, false);
                        }
                    }
                }

                // B. Update _isConnected
                string connNodeIdKey = new NodeId($"{gName}._isConnected", nsIdx).ToString();
                if (_connectionNodes.TryGetValue(connNodeIdKey, out var connNode))
                {
                    bool statusKonek = GlobalData.PlcFirstConnectFlags.TryGetValue(dcId, out var flag) && flag;

                    if (!Object.Equals(connNode.Value, statusKonek))
                    {
                        connNode.Value = statusKonek;
                        connNode.Timestamp = DateTime.UtcNow;
                        connNode.ClearChangeMasks(SystemContext, false);
                    }
                }
            }
        }
    }

    private object CastToOpcType(object value, int dataTypeId)
    {
        if (value == null) return GetDefaultValue(dataTypeId);
        var type = (PlcDataType)dataTypeId;
        return type switch
        {
            PlcDataType.Int16 => Convert.ToInt16(value),
            PlcDataType.UInt16 => Convert.ToUInt16(value),
            PlcDataType.Int32 => Convert.ToInt32(value),
            PlcDataType.UInt32 => Convert.ToUInt32(value),
            PlcDataType.Float32 => Convert.ToSingle(value),
            PlcDataType.Boolean => Convert.ToBoolean(value),
            _ => value
        };
    }

    private NodeId GetDataTypeId(int dataTypeId)
    {
        var type = (PlcDataType)dataTypeId;
        return type switch
        {
            PlcDataType.Boolean => DataTypeIds.Boolean,
            PlcDataType.Int16 => DataTypeIds.Int16,
            PlcDataType.UInt16 => DataTypeIds.UInt16,
            PlcDataType.Int32 => DataTypeIds.Int32,
            PlcDataType.UInt32 => DataTypeIds.UInt32,
            PlcDataType.Int64 => DataTypeIds.Int64,
            PlcDataType.UInt64 => DataTypeIds.UInt64,
            PlcDataType.Float32 => DataTypeIds.Float,
            PlcDataType.String => DataTypeIds.String,
            _ => DataTypeIds.Double
        };
    }

    private object GetDefaultValue(int dataTypeId)
    {
        var type = (PlcDataType)dataTypeId;
        return type switch
        {
            PlcDataType.Boolean => false,
            PlcDataType.Int16 => (short)0,
            PlcDataType.UInt16 => (ushort)0,
            PlcDataType.Int32 => 0,
            PlcDataType.UInt32 => (uint)0,
            PlcDataType.Int64 => (long)0,
            PlcDataType.UInt64 => (ulong)0,
            PlcDataType.Float32 => 0.0f,
            PlcDataType.String => "",
            _ => 0.0
        };
    }
}