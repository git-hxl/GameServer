namespace ShareLibrary
{
    public enum MessageType : byte
    {
        ClientRequest = 0,
        GameRequest,
        GameResponse,
        MasterRequest,
        MasterResponse,
    }
}
