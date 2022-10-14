using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanTint.MessageCenterCore.NettyCommon
{
    public static class MessagePackHelper
    {
        public static byte[] SerializeToBinary<T>(T obj) => MessagePackSerializer.Serialize(obj);

        public static T DeserializeWithBinary<T>(byte[] data) => MessagePackSerializer.Deserialize<T>(data);

        public static string DeserializeJson(byte[] data) => MessagePackSerializer.ConvertToJson(data);

        public static string DeserializeJson<T>(T data) => MessagePackSerializer.SerializeToJson(data);

        public static byte[] SerializeJson(string data) => MessagePackSerializer.ConvertFromJson(data);

        public static T DeserializeToJson<T>(string data) => MessagePackSerializer.Deserialize<T>(SerializeJson(data));

    }
}
