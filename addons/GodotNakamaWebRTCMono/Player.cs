using Godot;
using GDC = Godot.Collections;
using Nakama;

namespace NakamaWebRTC
{
    public class Player : Godot.Object, IBufferSerializable
    {
        public string SessionID { get; set; }
        public string Username { get; set; }
        public int PeerID { get; set; }

        public Player() { }

        public Player(string sessionID, string username, int peerID)
        {
            SessionID = sessionID;
            Username = username;
            PeerID = peerID;
        }

        public static Player FromPresence(IUserPresence presence, int peerID)
        {
            return new Player(presence.SessionId, presence.Username, peerID);
        }

        public static Player FromLocal(string username, int peerID)
        {
            // We don't use sessionID if the player is local
            return new Player("", username, peerID);
        }

        public void Serialize(StreamPeerBuffer buffer)
        {
            buffer.PutString(SessionID);
            buffer.PutString(Username);
            buffer.Put32(PeerID);
        }

        public void Deserialize(StreamPeerBuffer buffer)
        {
            SessionID = buffer.GetString();
            Username = buffer.GetString();
            PeerID = buffer.Get32();
        }

        // TODO: REmove if we can use serialiez instead
        //public GDC.Dictionary ToGDDict()
        //{
        //    return new GDC.Dictionary()
        //    {
        //        [nameof(Username)] = Username,
        //        [nameof(SessionID)] = SessionID,
        //        [nameof(PeerID)] = PeerID,
        //    };
        //}

        //public void FromGDDict(GDC.Dictionary dict)
        //{
        //    Username = dict.Get<string>(nameof(Username));
        //    SessionID = dict.Get<string>(nameof(SessionID));
        //    PeerID = dict.Get<int>(nameof(PeerID));
        //}
    }
}