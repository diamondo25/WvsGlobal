using System.Collections.Generic;

public class NPCData
{
    public int ID { get; set; }
    public string Quest { get; set; }
    public int Trunk { get; set; }
    public short Speed { get; set; }
    public byte SpeakLineCount { get; set; }
    public List<ShopItemData> Shop { get; set; }
}