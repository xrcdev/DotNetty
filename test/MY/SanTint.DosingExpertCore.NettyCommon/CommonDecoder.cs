using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;

namespace SanTint.DosingExpertCore.NettyCommon
{
    public class CommonDecoder : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            try
            {
                if (input.ReadableBytes > 0)
                {
                    var length = input.ReadInt();
                    var sType = input.ReadByte();
                    input.ReadByte();
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
                System.Diagnostics.Debug.WriteLine($"CommonEncoder:{ex.Message}");
            }
        }
    }

    public class CommonEncoder<T> : MessageToByteEncoder<T>
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

                byte[] bytes = new byte[messageBytes.Length + 2];
                var lenBites = BitConverter.GetBytes(messageBytes.Length);
                //lenBites.CopyTo(bytes, 0);
                bytes[0] = 0x01;
                bytes[1] = 0xff;
                messageBytes.CopyTo(bytes, 2);
                output.WriteBytes(bytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CommonEncoder:{ex.Message}");
            }
        }
    }
}