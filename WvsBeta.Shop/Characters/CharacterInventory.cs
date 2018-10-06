using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game;
using WvsBeta.SharedDataProvider;

namespace WvsBeta.Shop
{
    public class CharacterInventory : BaseCharacterInventory
    {
        private Character Character { get; set; }

        public CharacterInventory(Character character) : base(character.UserID, character.ID)
        {
            Character = character;
        }

        public void SaveInventory()
        {
            base.SaveInventory(null);
        }

        public new void LoadInventory()
        {
            base.LoadInventory();
        }
    }
}
