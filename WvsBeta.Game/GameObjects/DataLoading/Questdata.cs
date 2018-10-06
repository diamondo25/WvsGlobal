using System.Collections.Generic;

public class Questdata
{
    public byte Stage { get; set; }
    public int ReqItem { get; set; }
    public int ItemReward { get; set; }
    public short ItemRewardCount { get; set; }
    public int MesoReward { get; set; }
    public int FameReward { get; set; }
    public int ExpReward { get; set; }
    public List<ItemReward> ReqItems { get; set; }
    public List<ItemReward> ItemRewards { get; set; }
    public List<ItemReward> RandomRewards { get; set; }
    public List<QuestMob> Mobs { get; set; }
}