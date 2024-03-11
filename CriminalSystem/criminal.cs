using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.NaturalMotion;
using GTA.UI;

namespace CriminalSystem
{
    public class criminal : Script
    {
        int bounty;
        int lastBounty;
        Ped BountyHunter;
        private DateTime lastBountyHunterSpawnTime;
        private TimeSpan bountyHunterSpawnInterval = TimeSpan.FromMinutes(5);
        public criminal()
        {
            GTA.Native.Function.Call(Hash.WAIT, 0);
            Tick += OnTick;
            KeyUp += OnKeyUp;
            KeyDown += OnKeyDown;
            bounty = 0;
            lastBounty = 0;
            lastBountyHunterSpawnTime = DateTime.Now;
        }

        private void OnTick(object sender, EventArgs e)
        {
            KillNpc();
            StoleVehicleHandle();
            NpcReaction();

            if (Function.Call<int>(Hash.GET_PLAYER_WANTED_LEVEL, Game.Player.Handle) == 0 && lastBounty > 0)
            {
                IncreaseBounty(1000, "The Most Wanted");
            }

                if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player.Handle, true))
            {
                lastBounty = bounty;
                bounty = 0;
            }
            else if (Function.Call<bool>(Hash.IS_PLAYER_DEAD, Game.Player.Handle, true))
            {
                lastBounty = bounty;
                Game.Player.Money -= bounty;
                bounty = 0;
            }

            TimeSpan elapsed = DateTime.Now - lastBountyHunterSpawnTime;
            if (elapsed >= bountyHunterSpawnInterval)
            {
                SpawnBountyHunter();
                lastBountyHunterSpawnTime = DateTime.Now;
            }
        }
        private void IncreaseBounty(int value, string reason)
        {
            string message;
            if (reason == null)
            {
                message = $"We Increase Your Bounty Because Of your action and now your bounty is {bounty}";
            }
            else
            {
                message = $"We Increase Your Bounty Because Of {reason} and now your bounty is {bounty}";
            }

            bounty += value;
            NotificationIcon icon = new NotificationIcon();
            icon = NotificationIcon.Call911;
            GTA.UI.Notification.Show(icon, "Government", "Impact Of Your Action", message, false, false);
            
        }

        private void StoleVehicleHandle()
        {
            Ped player = Game.Player.Character;
            if (player.IsInVehicle())
            {
                Vehicle car = player.CurrentVehicle;

                if (!car.Equals(Game.Player.LastVehicle))
                {
                    int increasedBounty = 50;
                    IncreaseBounty(increasedBounty, "Stole Vehicle");
                }
            }
        }

        private HashSet<int> countedNpcs = new HashSet<int>();

        private void KillNpc()
        {
            Ped player = Game.Player.Character;
            Vector3 playerPos = player.Position;

            Ped[] nearestNpc = World.GetNearbyPeds(playerPos, 100.0f);
            foreach (Ped npc in nearestNpc)
            {
                int npcId = npc.Handle;

                if (npc.Killer == player && !countedNpcs.Contains(npcId) && npc != BountyHunter && !npc.IsInCombatAgainst(player))
                {
                    IncreaseBounty(50, "Killing Innocent People");
                    countedNpcs.Add(npcId);
                    break;
                }
            }
        }

        private void NpcReaction()
        {
            Ped player = Game.Player.Character;
            Vector3 Pos = player.Position;

            Ped[] nearbyNPCs = World.GetNearbyPeds(Pos, 30.0f);
            if (bounty >= 100000)
            {
                foreach (Ped npc in nearbyNPCs)
                {
                    float distance = World.GetDistance(Pos, npc.Position);
                    if (distance < 10.0f)
                    {
                        if (!npc.IsPlayer && npc != BountyHunter && !npc.IsInCombatAgainst(player) && !player.IsInVehicle())
                        {
                            npc.Task.ClearAll();
                            Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, npc.Handle, "WORLD_HUMAN_MOBILE_FILM_SHOCKING", 0, true);
                            npc.Task.ReactAndFlee(player);
                            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player.Handle, 4);
                        }
                    }
                }
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, player.Handle, 3);
            }
        }

        private List<string> bountyHunterName = new List<string>
        {
            "Jack Turner",
            "Marcus Reynolds",
            "Dylan Granger",
            "Nathan Foster",
            "Caleb Mitchell",
            "Jordan Fleming",
            "Ethan Caldwell",
            "Noah Anderson",
            "Owen Garrett",
            "Logan Parker"
        };

        private string GetRandomString()
        {
            Random random = new Random();
            int indeksAcak = random.Next(bountyHunterName.Count);
            return bountyHunterName[indeksAcak];
        }

        private void SpawnBountyHunter()
        {
            Vector3 playerPos = Game.Player.Character.Position;
            Vector3 BountyHunterPos = playerPos.Around(50.0f);

            string Rank = GetRank(bounty);

            BountyHunter = World.CreatePed(PedHash.Bankman, BountyHunterPos);
            Console.WriteLine("Bounty Hunter Created To Chase You");
            string bountyHunterName = GetRandomString();
            NotificationIcon icon = new NotificationIcon();
            icon = NotificationIcon.Blocked;
            GTA.UI.Notification.Show(icon, bountyHunterName, $"Hello The {Rank} {Game.Player.Name}", $"Hello {Game.Player.Name}. don't try to run from me. lest have some fun before i take your head to police", false, false);
            if (bounty <= 5000)
            {
                if(!BountyHunter.Weapons.HasWeapon(WeaponHash.Knife))
                {
                    BountyHunter.Weapons.Give(WeaponHash.Knife, 100, true, true);
                }
            }
            else if(bounty >= 5000 && bounty < 10000)
            {
                if(!BountyHunter.Weapons.HasWeapon(WeaponHash.Pistol))
                {
                    BountyHunter.Weapons.Give(WeaponHash.Pistol, 100, true, true);
                }
            }
            else if(bounty >= 10000)
            {
                if(!BountyHunter.Weapons.HasWeapon(WeaponHash.HeavyShotgun))
                {
                    BountyHunter.Weapons.Give(WeaponHash.HeavyShotgun, 100, true, true);
                }
            }

            BountyHunter.Armor = 100;
            BountyHunter.Health = 500;
            BountyHunter.Task.FightAgainst(Game.Player.Character);
            
        }

        public string GetRank(decimal bounty)
        {
            if (bounty < 1000)
            {
                return "Punk";
            }
            else if (bounty < 2000)
            {
                return "Gangster Class";
            }
            else if (bounty < 5000)
            {
                return "Mafia Class";
            }
            else if (bounty < 100000)
            {
                return "Hitman Class";
            }
            else if (bounty < 150000)
            {
                return "Underground Mob";
            }
            else if (bounty < 300000)
            {
                return "Underground Mob Leader";
            }
            else if (bounty < 500000)
            {
                return "Underground Elite";
            }
            else if (bounty < 1000000)
            {
                return "Underground Hitman";
            }
            else if (bounty < 1000000000000)
            {
                return "Underground Emperor";
            }
            return "Unknown Rank";
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.N)
            {
                IncreaseBounty(5000, "Testing");
            }
            if (e.KeyCode == Keys.X)
            {
                SpawnBountyHunter();
            }
        }
    }
}