
namespace SharedLibrary.Operation
{
    public enum ServerOperationCode : byte
    {
        //Server
        RegisterToMaster = 0,
        UpdateGameServerInfo,

        CreateRoom,

        UpdateRoomList,
    }
}
