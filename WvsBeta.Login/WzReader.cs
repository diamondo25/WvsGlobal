using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using reNX;
using reNX.NXProperties;

namespace WvsBeta.Login
{
    class WzReader
    {
        public static IList<string> ForbiddenName { get; private set; }
        
        public static void Load()
        {
            using (var pFile = new NXFile(Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "Data.nx")))
            {
                ForbiddenName = pFile.BaseNode["Etc"]["ForbiddenName.img"].Select(x => x.ValueString()).ToList();
                Program.MainForm.LogAppend("Loaded {0} forbidden names.", ForbiddenName.Count);

                CreateCharacterInfo.Init(pFile.BaseNode["Etc"]["MakeCharInfo.img"]["Info"]);
            }
        }
    }

    class CreateCharacterInfo
    {
        public readonly int[] Face;
        public readonly int[] Hair;
        public readonly int[] HairColor;
        public readonly int[] Skin;
        public readonly int[] Pants;
        public readonly int[] Coat;
        public readonly int[] Shoes;
        public readonly int[] Weapon;

        public static CreateCharacterInfo Female { get; private set; }
        public static CreateCharacterInfo Male { get; private set; }

        public CreateCharacterInfo(NXNode node)
        {
            int[] getIds(NXNode subNode)
            {
                return subNode.Select(x => x.ValueInt32()).ToArray();
            }

            Face = getIds(node["0"]);
            Hair = getIds(node["1"]);
            HairColor = getIds(node["2"]);
            Skin = getIds(node["3"]);
            Coat = getIds(node["4"]);
            Pants = getIds(node["5"]);
            Shoes = getIds(node["6"]);
            Weapon = getIds(node["7"]);
        }

        public static void Init(NXNode mainNode)
        {
            Female = new CreateCharacterInfo(mainNode["CharFemale"]);
            Male = new CreateCharacterInfo(mainNode["CharMale"]);
        }
    }
}
