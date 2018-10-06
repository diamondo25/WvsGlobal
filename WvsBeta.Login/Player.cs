using System.Collections.Generic;

namespace WvsBeta.Login
{

    public class Player
    {
        public enum LoginState
        {
            LoginScreen,
            SetupGender,
            ConfirmEULA,
            PinCheck,
            WorldSelect,
            ChannelSelect,
            CharacterSelect,
            CharacterCreation,
        }

        public string Username { get; set; }
        public int ID { get; set; }
        public byte Gender { get; set; }
        public byte GMLevel { get; set; }
        public bool IsGM { get => GMLevel > 0; }
        public bool IsAdmin { get => GMLevel >= 3; }
        public bool LoggedOn { get; set; } = false;
        public int DateOfBirth { get; set; }
        public LoginState State { get; set; } = LoginState.LoginScreen;
        public byte World { get; set; }
        public byte Channel { get; set; }
        public ClientSocket Socket { get; set; }
        public string SessionHash { get; set; }

        public Dictionary<int, string> Characters { get; } = new Dictionary<int, string>();

        public bool HasCharacterWithID(int id)
        {
            return Characters.ContainsKey(id);
        }
        
    }
}
