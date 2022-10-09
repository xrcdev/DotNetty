using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Echo.Common
{

    public class CommonEncoder<T> : MessageToByteEncoder<T>
    {
        protected override void Encode(IChannelHandlerContext context, T message, IByteBuffer output)
        {
            //序列化类
            //byte[] messageBytes = MessagePackHelper.SerializeToBinary(message);
            //IByteBuffer initialMessage = Unpooled.Buffer(messageBytes.Length);
            //initialMessage.WriteBytes(messageBytes);

            //output.WriteBytes(initialMessage);
            //序列化类
            try
            {
                string ss = JsonConvert.SerializeObject(message);
                byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(ss);//MessagePackHelper.SerializeToBinary(message);
                IByteBuffer initialMessage = Unpooled.Buffer(messageBytes.Length);
                initialMessage.WriteBytes(messageBytes);

                output.WriteBytes(initialMessage);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"D:\Log\Netty.txt", ex.Message);
                //Logger.Write(ex, CategoryLog.Error);
            }
        }
    }
    public class CommonDecoder : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            //byte[] array = new byte[input.ReadableBytes];
            //input.GetBytes(input.ReaderIndex, array, 0, input.ReadableBytes);
            //input.Clear();
            //var temp = MessagePackHelper.DeserializeWithBinary<Message>(array);
            //output.Add(temp);
            try
            {
                byte[] array = new byte[input.ReadableBytes];
                input.GetBytes(input.ReaderIndex, array, 0, input.ReadableBytes);
                input.Clear();
                //var temp = MessagePackHelper.DeserializeWithBinary<Message>(array);
                string ss = System.Text.Encoding.UTF8.GetString(array);
                var temp = JsonConvert.DeserializeObject<Song>(ss);
                output.Add(temp);
            }
            catch (Exception ex)
            {
                //Logger.Write(ex, CategoryLog.Error);
                System.IO.File.AppendAllText(@"D:\Log\Netty.txt", ex.Message);
            }
        }
    }
}
