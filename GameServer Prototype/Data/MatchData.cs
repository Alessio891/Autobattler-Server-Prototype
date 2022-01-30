using LiteNetLib.Utils;
using Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer_Prototype
{
    public class MatchData
    {
        #region Static
        public static int REQUIRED_PLAYERS = 2;
        public static Dictionary<string, MatchData> CurrentMatches = new Dictionary<string, MatchData>();

        public static MatchData GetFirstAvailableMatch(Client client)
        {
            foreach(KeyValuePair<string, MatchData> m in CurrentMatches)
            {
                if (m.Value.State == MatchState.WaitingPlayers)
                {
                    if (m.Value.ConnectedPlayers.Count < MatchData.REQUIRED_PLAYERS)
                    {
                        m.Value.AddPlayer(client.ClientID);
                        return m.Value;
                    }
                }
            }

            MatchData newMatch = new MatchData();

            newMatch.Initialize();
            newMatch.AddPlayer(client.ClientID);

            return newMatch;
        }

        public static MatchData GetClientMatch(int clientId)
        {
            foreach(KeyValuePair<string, MatchData> m in CurrentMatches)
            {
                foreach (PlayerMatchData p in m.Value.ConnectedPlayers)
                    if (p.ClientID == clientId)
                        return m.Value;
            }
            return null;
        }
        #region Match Packet Callbacks
        public static void OnPlayerReady(PlayerReadyRequest packet)
        {
            Client c = Clients.GetClient(packet.ClientID);
            MatchData match = GetClientMatch(packet.ClientID);
            match.GetPlayer(packet.ClientID).Ready = true;
            bool allReady = true;
            foreach(PlayerMatchData m in match.ConnectedPlayers)
            {
                if (!m.Ready)
                {
                    allReady = false;
                    break;
                }
            }
            if (match.State == MatchState.BuyPhase)
                return;

            if (allReady)
            {
                ServerConsole.LogWarning("All players ready");
                
                foreach(PlayerMatchData player in match.ConnectedPlayers)
                {
                    if (match.State == MatchState.Starting)
                    {
                        Server.instance.netProcessor.Send(Clients.GetPeer(player.ClientID),
                            new AllPlayersReadyResponse(),
                            LiteNetLib.DeliveryMethod.ReliableOrdered);
                    }
                    foreach (PlayerMatchData m in match.ConnectedPlayers)
                    {
                        Server.SendPacket<SwitchPhaseResponse>(m.ClientID, match.GetBuyPhasePacket(m.ClientID));
                    }
                }
                match.State = MatchState.BuyPhase;
            }

        }

        public static void BuyMinionRequest(MinionBuyRequest packet)
        {
            MatchData match = GetClientMatch(packet.ClientID);
            PlayerMatchData player = match.GetPlayer(packet.ClientID);
            if (match.State == MatchState.BuyPhase)
            {
                MinionBuyResponse resp = new MinionBuyResponse();
                if (player.Gold < 3)
                {
                    resp.Success = false;
                    resp.UpdatedData = player;
                    Server.SendPacket<MinionBuyResponse>(packet.ClientID, resp);
                    return;
                }

                int minionIndex = packet.MinionIndex;
                List<MinionMatchData> minions = player.CurrentMinionsInShop.ToList();
                MinionMatchData minionToBuy = minions[minionIndex];
                MinionDataStructure minionData = Registry.GetMinion(minionToBuy.MinionID);
                bool bought = false;
                switch(minionData.Type)
                {
                    case "Melee":
                        for(int i = 0; i < player.MeleeMinions.Length; i++)
                        {
                            if (string.IsNullOrEmpty(player.MeleeMinions[i].MinionID))
                            {
                                player.MeleeMinions[i] = minionToBuy;
                                bought = true;
                                break;
                            }
                        }
                        break;
                    case "Flying":
                        if (string.IsNullOrEmpty(player.LeftFlyingMinion.MinionID))
                        {
                            bought = true;
                            player.LeftFlyingMinion = minionToBuy;
                        } else if (string.IsNullOrEmpty(player.RightFlyingMinion.MinionID))
                        {
                            bought = true;
                            player.RightFlyingMinion = minionToBuy;
                        }
                        break;
                    case "Ranged":
                        for(int i = 0; i < player.RangedMinions.Length; i++)
                        {
                            if (string.IsNullOrEmpty(player.RangedMinions[i].MinionID))
                            {
                                bought = true;
                                player.RangedMinions[i] = minionToBuy;
                                break;
                            }
                        }
                        break;
                }

                if (bought)
                {                 
                    minions.RemoveAt(minionIndex);
                    player.CurrentMinionsInShop = minions.ToArray();
                    resp.UpdatedGold = player.Gold;
                    resp.Success = true;
                    player.Gold -= 3;
                    resp.UpdatedData = player;
                } else
                {
                    resp.Success = false;
                    resp.UpdatedData = player;
                }
                Server.SendPacket<MinionBuyResponse>(packet.ClientID, resp);
            } else
            {
                MinionBuyResponse resp = new MinionBuyResponse();
                resp.Success = false;
                resp.UpdatedData = player;
                Server.SendPacket<MinionBuyResponse>(packet.ClientID, resp);
            }
        }
        #endregion
        #endregion

        #region Instance 
        public enum MatchState { WaitingPlayers = 0, Starting, BuyPhase, CombatPhase }

        public string UID;

        public MatchState State = MatchState.WaitingPlayers;

        public List<PlayerMatchData> ConnectedPlayers;

        Random rnd;

        Thread _thread;

        double updateTimer = 0;
        double lastTime = 0;
        long CurrentMS
        {
            get { return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond; }
        }

        double BuyPhaseTimer = 0;
        double BuyPhaseTime = 21000;

        Dictionary<int, int> BattleCouples = new Dictionary<int, int>();

        public void Initialize()
        {
            ConnectedPlayers = new List<PlayerMatchData>();
            UID = Guid.NewGuid().ToString();
            CurrentMatches.Add(UID, this);
            rnd = new Random();
            string log = string.Format("Started match UID {0}. {1} currently active matches.", UID, CurrentMatches.Keys.Count.ToString());
            ServerConsole.Log(log);
            _thread = new Thread(HandleMatch);
            _thread.Start();
        }

        public void StartMatch()
        {
            State = MatchState.Starting;
            foreach (PlayerMatchData client in ConnectedPlayers)
            {
                Client c = Clients.GetClient(client.ClientID);
                Server.instance.netProcessor.Send(c.peer,
                    new JoinLobbyResponsePacket()
                    {
                        LobbyStarted = true,
                        Message = "Match Started",                        
                    }, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
        }

        public void DisposeMatch()
        {
            CurrentMatches.Remove(this.UID);
        }

        #region Player Management
        public PlayerMatchData GetPlayer(int clientId)
        {
            foreach(PlayerMatchData m in ConnectedPlayers)
            {
                if (m.ClientID == clientId)
                    return m;
            }
            return null;
        }
        public void AddPlayer(int clientId)
        {
            if (State != MatchState.WaitingPlayers)
                return;
            foreach (PlayerMatchData player in ConnectedPlayers)
                if (player.ClientID == clientId)
                    return;

            PlayerMatchData p = new PlayerMatchData();
            p.ClientID = clientId;
            p.PlayerName = Clients.GetClient(clientId).Username;            
            ConnectedPlayers.Add(p);
            if (ConnectedPlayers.Count >= REQUIRED_PLAYERS)
                StartMatch();
            
        }
        public void RemovePlayer(int clientId)
        {
            for(int i= 0; i<ConnectedPlayers.Count;i++)
            {
                if (ConnectedPlayers[i].ClientID == clientId)
                {
                    ConnectedPlayers.RemoveAt(i);
                    return;
                }
            }            
        }

        MinionMatchData[] GetRandomMinionsForPlayer(int clientId)
        {            
            PlayerMatchData player = GetPlayer(clientId);

            int minionToShow = player.CurrentTier + 2;
            if (minionToShow > 6)
                minionToShow = 6;
            MinionMatchData[] retVal = new MinionMatchData[minionToShow];

            int minionsPicked = 0;
            List<MinionDataStructure> AvailableMinions = Registry.GetAllMinions().Where((m) => m.Tier <= player.CurrentTier).ToList();
            while(minionsPicked < minionToShow)
            {
                MinionDataStructure minion = AvailableMinions[rnd.Next(AvailableMinions.Count)];
                MinionMatchData data = new MinionMatchData();
                data.MinionID = minion.ID;
                data.MinionAttack = minion.Attack;
                data.MinionHP = minion.MaxHP;
                retVal[minionsPicked] = data;
                minionsPicked++;
            }

            return retVal;
        }
        #endregion

        public SwitchPhaseResponse GetBuyPhasePacket(int clientId)
        {
            SwitchPhaseResponse retVal = new SwitchPhaseResponse();
            PlayerMatchData player = GetPlayer(clientId);
            player.Gold = 6;
            player.CurrentMinionsInShop = GetRandomMinionsForPlayer(clientId);
            retVal.Phase = 0;                       
            retVal.PlayerData = player;
            retVal.Timer = (int)BuyPhaseTime / 1000;
            return retVal;
        }

        public SwitchPhaseResponse GetCombatPacket(int clientId, int enemyId)
        {
            SwitchPhaseResponse retVal = new SwitchPhaseResponse();

            PlayerMatchData player = GetPlayer(clientId);
            PlayerMatchData enemy = GetPlayer(enemyId);

            retVal.PlayerData = player;
            retVal.Enemy = enemy;

            return retVal;
        }

        public void HandleMatch()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            lastTime = 0;
            while(true)
            {
                TimeSpan ts = stopWatch.Elapsed;
                double delta = ts.TotalMilliseconds - lastTime;
                lastTime = ts.TotalMilliseconds;
                updateTimer += delta;
                if (updateTimer > 15)
                {

                    bool atLeastOnePlayer = false;
                    foreach(PlayerMatchData player in ConnectedPlayers)
                    {
                        if (player != null && Clients.GetClient(player.ClientID) != null)
                        {
                            atLeastOnePlayer = true;
                            break;
                        }
                    }

                    if (!atLeastOnePlayer)
                    {
                        ServerConsole.LogWarning("0 players in lobby, disposing lobby");
                        DisposeMatch();
                        return;
                    }

                    switch(State)
                    {
                        case MatchState.BuyPhase:
                            HandleBuyPhase(updateTimer);
                            break;
                        case MatchState.CombatPhase:
                            HandleCombatPhase(updateTimer);
                            break;
                    }
                    updateTimer = 0;
                }
            }
            
        }

        public void HandleBuyPhase(double delta)
        {
            BuyPhaseTimer += delta;
                       
            if (BuyPhaseTimer > BuyPhaseTime)
            {
                ServerConsole.Log("Switching to combat");
                BuyPhaseTimer = 0;
                SwitchToCombat();
                State = MatchState.CombatPhase;
            }
        }

        void SwitchToCombat()
        {
            List<PlayerMatchData> toMatch = new List<PlayerMatchData>(ConnectedPlayers);
            BattleCouples.Clear();
            for(int i = 0; i < toMatch.Count/2; i++)
            {
                PlayerMatchData player = toMatch[i];
                toMatch.Remove(player);
                PlayerMatchData enemy = toMatch[rnd.Next(toMatch.Count)];
                toMatch.Remove(enemy);

                SwitchPhaseResponse p1 = new SwitchPhaseResponse();
                p1.Phase = 1;
                p1.PlayerData = player;
                p1.Enemy = enemy;
                Server.SendPacket<SwitchPhaseResponse>(player.ClientID, p1);

                SwitchPhaseResponse p2 = new SwitchPhaseResponse();
                p2.Phase = 1;
                p2.PlayerData = enemy;
                p2.Enemy = player;
                Server.SendPacket<SwitchPhaseResponse>(enemy.ClientID, p2);
                BattleCouples.Add(p1.ClientID, p2.ClientID);                
            }           
            for(int i = 0; i < ConnectedPlayers.Count; i++)
            {
                ConnectedPlayers[i].Ready = false;
            }
        }

        public void HandleCombatPhase(double delta)
        {

            foreach(KeyValuePair<int,int> pair in BattleCouples)
            {
                PlayerMatchData p1 = GetPlayer(pair.Key);
                PlayerMatchData p2 = GetPlayer(pair.Value);

                // Clone all minions for combat phase (maybe we don't need to if we keep currenthp\maxhp values,
                // just reset it to maxhp when done? TODO
                List<MinionMatchData> Player1Melees = new List<MinionMatchData>();
                List<MinionMatchData> Player2Melees = new List<MinionMatchData>();

                List<MinionMatchData> Player1Ranged = new List<MinionMatchData>();
                List<MinionMatchData> Player2Ranged = new List<MinionMatchData>();

                MinionMatchData Player1LeftFlying = MinionMatchData.GetClone(p1.LeftFlyingMinion);
                MinionMatchData Player1RightFlying = MinionMatchData.GetClone(p1.RightFlyingMinion);
                MinionMatchData Player2LeftFlying = MinionMatchData.GetClone(p2.LeftFlyingMinion);
                MinionMatchData Player2RightFlying = MinionMatchData.GetClone(p2.RightFlyingMinion);

                foreach (MinionMatchData m in p1.MeleeMinions)
                    Player1Melees.Add(MinionMatchData.GetClone(m));
                foreach (MinionMatchData m in p1.RangedMinions)
                    Player1Ranged.Add(MinionMatchData.GetClone(m));
                foreach (MinionMatchData m in p2.MeleeMinions)
                    Player2Melees.Add(MinionMatchData.GetClone(m));
                foreach (MinionMatchData m in p2.RangedMinions)
                    Player2Ranged.Add(MinionMatchData.GetClone(m));

                // 1. melee attack
                // 2. ranged attack
                // 3. Flying attack
                // 4. repeat until someone dead
                while(true)
                {
                    // Melees attack
                    MinionMatchData p1Minion = Player1Melees[0];
                    MinionMatchData p2Minion = Player2Melees[0];

                    p1Minion.MinionHP -= p2Minion.MinionAttack;
                    p2Minion.MinionHP -= p1Minion.MinionAttack;

                    if (p1Minion.MinionHP <= 0)
                        Player1Melees.RemoveAt(0);
                    else
                        Player1Melees[0] = p1Minion;
                    if (p2Minion.MinionHP <= 0)
                        Player2Melees.RemoveAt(0);
                    else
                        Player2Melees[0] = p2Minion;
                    
                    // Ranged
                    for(int i = 0; i < 6; i++)
                    {
                        if (Player2Melees.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(Player1Ranged[i].MinionID))
                            {
                                p1Minion = Player1Ranged[i];
                                p2Minion = Player2Melees[0];
                                p2Minion.MinionHP -= p1Minion.MinionAttack;
                                if (p2Minion.MinionHP <= 0)
                                    Player2Melees.RemoveAt(0);
                                else
                                    Player2Melees[0] = p2Minion;
                            }
                        }

                        if (Player1Melees.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(Player2Ranged[i].MinionID))
                            {
                                p1Minion = Player1Melees[0];
                                p2Minion = Player2Ranged[i];
                                p1Minion.MinionHP -= p2Minion.MinionAttack;
                                if (p1Minion.MinionHP <= 0)
                                    Player1Melees.RemoveAt(0);
                                else
                                    Player1Melees[0] = p1Minion;
                            }
                        }
                    }

                    if (Player1Melees.Count <= 0 || Player2Melees.Count <= 0)
                        break;
                }

                int p1Dmg = Player1Melees.Count + Player1Ranged.Count;
                int p2Dmg = Player2Melees.Count + Player2Ranged.Count;
                GetPlayer(pair.Key).HP -= p2Dmg;
                GetPlayer(pair.Value).HP -= p1Dmg;

                if (GetPlayer(pair.Key).HP < 0)
                {
                    GetPlayer(pair.Key).HP = 0;
                    if (!GetPlayer(pair.Key).Dead)
                    {
                        GetPlayer(pair.Key).Dead = true;
                        Server.SendPacket<DeathResponse>(GetPlayer(pair.Key).ClientID, new DeathResponse());
                    }
                }
                if (GetPlayer(pair.Value).HP < 0)
                {
                    GetPlayer(pair.Value).HP = 0;
                    if (!GetPlayer(pair.Value).Dead)
                    {
                        GetPlayer(pair.Value).Dead = true;
                        Server.SendPacket<DeathResponse>(GetPlayer(pair.Value).ClientID, new DeathResponse());
                    }
                }
            }



            BuyPhaseTimer += delta;
            
            if (BuyPhaseTimer > BuyPhaseTime*2)
            {
                ServerConsole.Log("Switching to buy");
                BuyPhaseTimer = 0;
               
                foreach (PlayerMatchData m in ConnectedPlayers)
                {
                    m.Gold = 6;
                    Server.SendPacket<SwitchPhaseResponse>(m.ClientID, GetBuyPhasePacket(m.ClientID));
                }

                State = MatchState.BuyPhase;
            }
        }        
        #endregion
    }

}
