using LiteNetLib;
using MasterServer;
using Serilog;

namespace GameServer
{
    internal class GameClientPeer
    {
        private NetPeer? peer;

        private bool isMaster;

        public GameClientPeer(NetPeer peer, bool isMaster)
        {
            this.peer = peer;
            this.isMaster = isMaster;

            if (isMaster)
            {
                RegisterToMaster();
                UpdateSystemInfo();
            }
        }

        public void RegisterToMaster()
        {
            RegisterServerRequest request = new RegisterServerRequest();

            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);

            OperationRequest operationRequest = new OperationRequest(OperationCode.RegisterGameServer, data, DeliveryMethod.ReliableOrdered);

            operationRequest.SendTo(peer);
        }

        public void OnRegisterToMasterResponse(OperationResponse operationResponse)
        {
            if (operationResponse.ReturnCode != ReturnCode.Success)
                return;
            RegisterServerResponse response = MessagePack.MessagePackSerializer.Deserialize<RegisterServerResponse>(operationResponse.Data);
        }

        public OperationResponse OnCreateRoomRequest(OperationRequest operationRequest)
        {
            CreateRoomRequest request = MessagePack.MessagePackSerializer.Deserialize<CreateRoomRequest>(operationRequest.Data);

            string roomID = Guid.NewGuid().ToString();
            RoomState? roomState = RoomCache.Instance.AddRoom(roomID, request);
            CreateRoomResponse response = new CreateRoomResponse();
            if (roomState != null)
            {
                response.RoomID = roomID;
                response.UserID = request.UserID;
                response.RequestData = operationRequest.Data;
                byte[] data = MessagePack.MessagePackSerializer.Serialize(response);
                return OperationResponse.CreateResponse(operationRequest, ReturnCode.Success, data);
            }
            else
            {
                response.UserID = request.UserID;
                byte[] data = MessagePack.MessagePackSerializer.Serialize(response);
                return OperationResponse.CreateResponse(operationRequest, ReturnCode.Failed, data);
            }
        }


        private void UpdateSystemInfo()
        {
            Task.Run(async () =>
            {
                SystemInfo systemInfo = new SystemInfo();
                while (true)
                {
                    await Task.Delay(5000);
                    int cpu = (int)systemInfo.GetCPUPercent();
                    int ram = (int)systemInfo.GetMemoryPercent();
                    Log.Information("CPU：{0}% RAM：{1}%", cpu, ram);

                    UpdateServerInfoRequest request = new UpdateServerInfoRequest();
                    request.CPUPercent = cpu;
                    request.MemoryPercent = ram;
                    byte[] data = MessagePack.MessagePackSerializer.Serialize(request);
                    OperationRequest operationRequest = new OperationRequest(OperationCode.UpdateGameServer, data, DeliveryMethod.ReliableOrdered);
                }
            });
        }

    }
}
