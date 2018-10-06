using System;

public struct DropData
{
    public int ItemID { get; set; }
    public int Mesos { get; set; }
    public short Min { get; set; }
    public short Max { get; set; }
    public bool Premium { get; set; }
    public int Chance { get; set; }
    // Expires after X days
    public ushort Period { get; set; }

    public DateTime DateExpire { get; set; }
}