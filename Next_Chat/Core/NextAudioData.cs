using Hazel;

namespace Next_Chat.Core;

public class NextAudioData : IRpcInfo, IActive
{
    
    public int dataId { get; set; }
    public INextPlayer Player { get; set; } = null!;
    
    public int pcmLength { get; set; }
    public int EncodedLength { get; set; }
    public byte[] EncodeBytes => GetEncodedBytes();
    public byte[] DataBytes { get; set; } = [];

    public byte[] DecodeBytes => GetDecodedBytes();

    public int DecodeLength
    {
        get;
        set;
    }

    private static WaveTool? Tool => NextVoiceManager.Instance._WaveTool;

    private byte[] GetDecodedBytes()
    {
        if (DataBytes.Length == 0) return []; 
        var pcmBuffer = new byte[pcmLength];
        DecodeLength = Tool!.Decoder!.Decode(DataBytes, DataBytes.Length, pcmBuffer, pcmLength);
        return pcmBuffer;
    }

    private byte[] GetEncodedBytes()
    {
        if (DataBytes.Length == 0) return [];
        var opusBuffer = new byte[2048];
        var length = Tool!.Encoder!.Encode(DataBytes, pcmLength, opusBuffer, 2048);
        var array = opusBuffer.Take(length).ToArray();
        EncodedLength = length;
        return array;
    }
    
    public void Dispose()
    {
    }

    public void RpcWrite(MessageWriter writer)
    {
        writer.Write(dataId);
        writer.Write(Player.player.PlayerId);
        writer.Write(pcmLength);
        writer.Write(EncodedLength);
        writer.Write(EncodeBytes);
    } 

    public void RpcRead(MessageReader reader)
    {
        dataId = reader.ReadInt32();
        Player = NextVoiceManager.Instance.GetPlayer(reader.ReadByte());
        pcmLength = reader.ReadInt32();
        EncodedLength = reader.ReadInt32();
        DataBytes = reader.ReadBytes(EncodedLength);
    }

    public bool _active { get; set; }
    public int _Id { get; set; }
}