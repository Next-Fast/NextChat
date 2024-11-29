using Hazel;
using OpusDotNet;

namespace NextChat.Core;

public class NextAudioData : IRpcInfo
{
    public int dataId { get; set; }
    public INextPlayer Player { get; set; } = null!;

    public int Length { get; set; }
    private byte[] DataBytes { get; set; } = [];

    public NextAudioData AddFormInfo(EncodeInfo info)
    {
        var encoder = info.Encoder;
        var data = info.Data;
        var length = info.buffedLength;
        var opusLength = length / 4;
        var opusBytes = new byte[opusLength];

        var encodeLength = encoder.Encode(data, length, opusBytes, opusLength);
        DataBytes = opusBytes.Take(encodeLength).ToArray();
        Length = encodeLength;
        return this;
    }
    
    public void Dispose()
    {
    }

    public void RpcWrite(MessageWriter writer)
    {
        writer.Write(dataId);
        writer.Write(Player.player.PlayerId);
        writer.Write(Length);
        writer.Write(DataBytes);
    } 

    public void RpcRead(MessageReader reader)
    {
        dataId = reader.ReadInt32();
        Player = NextVoiceManager.Instance.GetPlayer(reader.ReadByte());
        Length = reader.ReadInt32();
        DataBytes = reader.ReadBytes(Length);
    }

    public void GetDecodeByte(OpusDecoder decoder, int buffedLength, out int length, out byte[] Data)
    {
        var buffed = new byte[buffedLength];
        var decodeLength = decoder.Decode(DataBytes, Length, buffed, buffedLength);
        length = decodeLength;
        Data = buffed.Take(decodeLength).ToArray();
    }
}

public record EncodeInfo(int buffedLength, OpusEncoder Encoder,  byte[] Data);

public record DecodeInfo(int buffedLength, byte[] Data);
