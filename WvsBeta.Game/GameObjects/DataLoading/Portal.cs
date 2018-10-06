using reNX.NXProperties;

namespace WvsBeta.Game
{
    public class Portal
    {
        public Portal(NXNode node, byte idx)
        {
            ID = idx;
            Type = node["pt"].ValueByte();
            X = node["x"].ValueInt16();
            Y = node["y"].ValueInt16();
            Name = node["pn"].ValueString();
            ToMapID = node["tm"].ValueInt32();
            ToName = node["tn"].ValueString();


            string script = null;

            if (node.ContainsChild("script"))
                script = node["script"].ValueString();

            if (script == "") script = null;

            Script = script;

            if (Type != 0)
            {
                Y -= 40;
            }
            Enabled = true;
        }

        public readonly byte ID;
        public readonly byte Type;
        public readonly short X;
        public readonly short Y;
        public readonly string Name;
        public readonly int ToMapID;
        public readonly string ToName;
        public readonly string Script;
        public bool Enabled;
    }
}