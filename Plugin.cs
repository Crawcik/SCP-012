using System.Collections.Generic;
using System.Threading.Tasks;

using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;

namespace SCP_012
{
    [PluginDetails(author = "Crawcik",
        configPrefix = "scp012",
        description = "What can I saaaaaaay",
        id = "crawcik.scp012",
        langFile = "scp012",
        name = "SCP-012",
        SmodMajor = 3,
        SmodMinor = 9,
        SmodRevision = 7,
        version = "1.4")]
    public class PluginHandler : Plugin, IEventHandlerRoundStart, IEventHandlerRoundEnd
    {
        Vector scp_vector;
        bool interrupt = false;
        #region Settings
        public override void OnDisable()
        {
            this.EventManager.RemoveEventHandlers(this);
        }

        public override void OnEnable()
        {
            this.AddEventHandlers(this);
        }

        public override void Register() { }
        #endregion

        #region Events
        public void OnRoundStart(RoundStartEvent ev)
        {
            Room room = ev.Server.Map.GetRooms().Find(x => x.RoomType == RoomType.LCZ_012);
            scp_vector = room.Position;
            this.Info(scp_vector.ToString());
            float rotX = 0f;
            float rotZ = 0f;
            float scp_rotation = new ServerMod2.API.SmodVector(((UnityEngine.GameObject)room.GetGameObject()).transform.eulerAngles).y;
            this.Info(scp_rotation.ToString());
            switch(scp_rotation)
            {
                case 0f:
                    rotX = 5f;
                    rotZ = 6f;
                    break;
                case 90f:
                    rotX = 3.5f;
                    rotZ = -4.5f;
                    break;
                case 180f:
                    rotX = -5f;
                    rotZ = -6f;
                    break;
                case 270f:
                    rotX = -3f;
                    rotZ = 5f;
                    break;
            }
            scp_vector = new Vector(scp_vector.x + rotX, scp_vector.y - 6.5215f, scp_vector.z + rotZ);
            this.Info(scp_vector.ToString());
            interrupt = false;
            CheckPlayers().GetAwaiter();
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if (ev.Status == ROUND_END_STATUS.ON_GOING)
                return;
            interrupt = true;
        }
        #endregion

        public async Task CheckPlayers()
        {
            Dictionary<int, int> player_lure_points = new Dictionary<int, int>();
            while (!interrupt)
            {
                foreach (Player player in this.Server.GetPlayers(x => !(x.TeamRole.Team == TeamType.SPECTATOR || x.TeamRole.Team == TeamType.SCP || x.TeamRole.Team == TeamType.TUTORIAL)))
                {
                    Vector ply_vector = player.GetPosition();
                    if (Vector.Distance(new Vector(0f,ply_vector.y,0f), new Vector(0f,scp_vector.y,0f)) < 2f && Vector.Distance(scp_vector, ply_vector) <= 8.5f)
                    {
                        if (player_lure_points.ContainsKey(player.PlayerId))
                        {
                            if (player_lure_points[player.PlayerId] > 0)
                            {
                                player_lure_points[player.PlayerId]++;
                                if (player_lure_points[player.PlayerId] == 3)
                                {
                                    player_lure_points[player.PlayerId] = 0;
                                    player.GetPlayerEffect(StatusEffect.INVIGORATED).Enable(10f);
                                    player.GetPlayerEffect(StatusEffect.BURNED).Enable(10f);
                                    player.GetPlayerEffect(StatusEffect.HEMORRHAGE).Enable(10f);
                                    LurePlayer(player).GetAwaiter();
                                }
                            }
                        }
                        else
                        {
                            player_lure_points.Add(player.PlayerId, 1);
                        }
                    }
                }
                await Task.Delay(1000);
            }
            interrupt = false;
        }
        public async Task LurePlayer(Player player)
        {
            Vector vector = player.GetPosition();
            decimal now = 0;
            decimal time = 14;
            PunchPlayer(player, time).GetAwaiter();
            while (time * 30 >= now)
            {
                if (player.TeamRole.Team == TeamType.SPECTATOR || player.TeamRole.Team == TeamType.NONE)
                {
                    return;
                }
                float timestep = (float)(now / (time * 30));
                float x = UnityEngine.Mathf.SmoothStep(vector.x, scp_vector.x, timestep);
                float y = UnityEngine.Mathf.SmoothStep(vector.y, scp_vector.y, timestep);
                float z = UnityEngine.Mathf.SmoothStep(vector.z, scp_vector.z, timestep);
                player.Teleport(new Vector(x, y, z));
                await Task.Delay(1000 / 30);
                now++;
            }
        }
        public async Task PunchPlayer(Player player, decimal time)
        {
            float d = (float)time * 1.7f;
            while (player.TeamRole.Role != Smod2.API.RoleType.SPECTATOR && d > 0)
            {
                if (player.TeamRole.Team == TeamType.SPECTATOR || player.TeamRole.Team == TeamType.NONE)
                {
                    player.PersonalBroadcast(5, "Zostałeś zabity przez SCP-012", false);
                    return;
                }
                player.Damage(6, DamageType.LURE);
                await Task.Delay(720);
                d--;
            }
        }
    }
}
