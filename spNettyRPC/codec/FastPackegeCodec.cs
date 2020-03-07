using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using NettyRPC.Fast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NettyRPC.codec
{
    public class FastPacketDecode : LengthFieldBasedFrameDecoder
    {




        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxFrameLength">解码时，处理每个帧数据的最大长度</param>
        /// <param name="lengthFieldOffset">该帧数据中，存放该帧数据的长度的数据的起始位置</param>
        /// <param name="lengthFieldLength">记录该帧数据长度的字段本身的长度</param>
        /// <param name="lengthAdjustment">修改帧数据长度字段中定义的值，可以为负数</param>
        /// <param name="initialBytesToStrip">解析的时候需要跳过的字节数</param>
        /// <param name="failFast">为true，当frame长度超过maxFrameLength时立即报TooLongFrameException异常，为false，读取完整个帧再报异常</param>
        public FastPacketDecode(int maxFrameLength, int lengthFieldOffset, int lengthFieldLength,
                int lengthAdjustment, int initialBytesToStrip, bool failFast) : base(maxFrameLength, lengthFieldOffset, lengthFieldLength,
                     lengthAdjustment, initialBytesToStrip, failFast)
        {

        }


        protected override Object Decode(IChannelHandlerContext ctx, IByteBuffer input)
        {
            if (input == null)
            {
                return null;
            }

            FastPacket customMsg=null;
            if (!FastPacket.Parse(input, out customMsg))
            {
                throw new Exception("数据包格式错误");
            }
            return customMsg;
        }
        


    }
    public class FastPacketEncoder : MessageToByteEncoder<FastPacket>
    {
        protected override void Encode(IChannelHandlerContext context, FastPacket message, IByteBuffer output)
        {
            //序列化类
            IByteBuffer bb = null;
            try
            {
                bb = message.ToByteBuffer();
                output.WriteBytes(bb);
            }
            finally
            {
                if (bb != null)
                    bb.Release();
            }
        }
    }
}
