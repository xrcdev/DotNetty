using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;

namespace SanTint.MessageCenterCore.NettyCommon
{
    
    public class MessageCodecs : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            try
            {
                if (input.ReadableBytes > 0)
                {
                    var sType = input.ReadByte();
                    var length = input.ReadInt();
                    //input.ReadByte();
                    //if (length < 0)
                    //    input.SetReaderIndex(input.ReaderIndex - 4);
                    byte[] array = new byte[input.ReadableBytes];
                    input.GetBytes(input.ReaderIndex, array, 0, input.ReadableBytes);
                    input.Clear();
                    var temp = MessagePackHelper.DeserializeWithBinary<Message>(array);
                    //string ss = System.Text.Encoding.UTF8.GetString(array);
                    //var temp = JsonConvert.DeserializeObject<Message>(ss);
                    output.Add(temp);
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CommonCodecs:{ex.Message}");
            }
        }
    }

    public class CommonCodecs<T> : MessageToByteEncoder<T>
    {
        protected override void Encode(IChannelHandlerContext context, T message, IByteBuffer output)
        {
            //序列化类
            try
            {
                byte[] messageBytes = MessagePackHelper.SerializeToBinary(message);
                //context.Allocator.Buffer
                //IByteBuffer initialMessage = Unpooled.Buffer(messageBytes.Length);
                //initialMessage.WriteBytes(messageBytes);
                //output.WriteInt(messageBytes.Length);

                byte[] bytes = new byte[messageBytes.Length + 1 + 4];
                bytes[0] = 0x01;
                var lenBites = BitConverter.GetBytes(messageBytes.Length).Reverse().ToArray();
                lenBites.CopyTo(bytes, 1);

                messageBytes.CopyTo(bytes, lenBites.Length + 1);
                output.WriteBytes(bytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CommonCodecs:{ex.Message}");
            }
        }
    }
}