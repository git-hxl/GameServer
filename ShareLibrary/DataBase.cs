using MessagePack;
namespace ShareLibrary
{
    public class DataBase
    {
        public static T Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }

        public byte[] Serialize<T>() where T : DataBase
        {
            return MessagePackSerializer.Serialize(this as T);
        }
    }
}
