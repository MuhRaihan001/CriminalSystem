using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace CriminalSystem
{
    public class criminal : Script
    {
        private int bounty = 0;
        private int lastBounty;
        private Ped BountyHunter;
        private DateTime lastBountyHunterSpawnTime;
        private TimeSpan bountyHunterSpawnInterval = TimeSpan.FromMinutes(5);
        private bool isWanted = false;
        private bool wantedLevelSet = false;
        private bool MostWanted = false;
        private DateTime LastWanted;
        private TimeSpan DeleteMostWanted = TimeSpan.FromMinutes(5);
        private bool IsDisguise = false;

        public criminal()
        {
            GTA.Native.Function.Call(Hash.WAIT, 0);
            Tick += OnTick;
            KeyUp += OnKeyUp;
            KeyDown += OnKeyDown;

            lastBounty = 0;
            LastWanted = DateTime.Now;
            lastBountyHunterSpawnTime = DateTime.Now;
        }

        private void OnTick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - lastBountyHunterSpawnTime;
            TimeSpan countdown = DateTime.Now - LastWanted;

            KillNpc();
            StoleVehicleHandle();
            NpcReaction();
            IsPlayerLooseCops();
            DisguiseHandle();

            if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player.Handle, true))
            {
                lastBounty = bounty;
                bounty = 0;
            }
            else if (Game.Player.IsDead)
            {
                lastBounty = bounty;
                Game.Player.Money -= bounty;
                bounty = 0;
            }

            if (elapsed >= bountyHunterSpawnInterval)
            {
                SpawnBountyHunter();
                lastBountyHunterSpawnTime = DateTime.Now;
            }
            if (countdown >= DeleteMostWanted)
            {
                MostWanted = false;
                LastWanted = DateTime.Now;
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
            if(IsDisguise)
                IsDisguise = false;

            foreach (Ped npc in nearestNpc)
            {
                int npcId = npc.Handle;

                if (npc.Killer == player && !countedNpcs.Contains(npcId) && npc != BountyHunter && !npc.IsInCombatAgainst(player) && player.Weapons.Current != WeaponHash.Unarmed)
                {
                    IncreaseBounty(20, "Killing Innocent People");
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
                        if (!npc.IsPlayer && npc != BountyHunter && !npc.IsInCombatAgainst(player) && !player.IsInVehicle() && IsDisguise)
                        {
                            if (npc.IsHuman)
                            {
                                npc.Task.ClearAll();
                                Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, npc.Handle, "WORLD_HUMAN_MOBILE_FILM_SHOCKING", 0, true);
                                npc.Task.ReactAndFlee(player);
                                wantedLevelSet = true;
                            }
                        }
                    }
                }
                if (!wantedLevelSet)
                {
                    Game.Player.WantedLevel = 3;
                    wantedLevelSet = true;
                }
            }
            else if (Game.Player.WantedLevel == 0 && wantedLevelSet)
            {
                wantedLevelSet = false;
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
            if (bounty < 200)
                return;
            if (IsDisguise)
                return;

            if(MostWanted)
            {
                int amount = new Random().Next(2, 5);
                for(int i = 0; i < amount; i++)
                {
                    BountyHunter = World.CreatePed(PedHash.Bankman, BountyHunterPos);
                }
                NotificationIcon icon = new NotificationIcon();
                icon = NotificationIcon.Blocked;
                GTA.UI.Notification.Show(icon, "Bounty Hunter Group", $"Hello {Game.Player.Name}", $"Hello The {Rank}. We Heard The {Rank} Have A high Bounty. So We Team Up To Catch You And Get The Money From Your Head. It Will Be {amount} People Will Against You", false, false);
            }
            else
            {
                BountyHunter = World.CreatePed(PedHash.Bankman, BountyHunterPos);
                Console.WriteLine("Bounty Hunter Created To Chase You");
                string bountyHunterName = GetRandomString();
                NotificationIcon icon = new NotificationIcon();
                icon = NotificationIcon.Blocked;
                GTA.UI.Notification.Show(icon, bountyHunterName, $"Hello {Game.Player.Name}", $"Hello The {Rank}. don't try to run from me. lest have some fun before i take your head to police", false, false);
            }
            GiveBountyHunterWeapon();
            BountyHunter.Health = 500;
            BountyHunter.Task.FightAgainst(Game.Player.Character);
            
        }

        private void GiveBountyHunterWeapon()
        {
            if (bounty <= 5000)
            {
                if (!BountyHunter.Weapons.HasWeapon(WeaponHash.Knife))
                {
                    BountyHunter.Weapons.Give(WeaponHash.Knife, 100, true, true);
                }
            }
            else if (bounty >= 5000 && bounty < 10000)
            {
                if (!BountyHunter.Weapons.HasWeapon(WeaponHash.Pistol))
                {
                    BountyHunter.Weapons.Give(WeaponHash.Pistol, 100, true, true);
                }
            }
            else if (bounty >= 10000)
            {
                if (!BountyHunter.Weapons.HasWeapon(WeaponHash.HeavyShotgun))
                {
                    BountyHunter.Weapons.Give(WeaponHash.HeavyShotgun, 100, true, true);
                }
            }
        }

        public string GetRank(decimal bounty)
        {
            if (bounty < 1500)
            {
                return "Punk";
            }
            else if (bounty < 5000)
            {
                return "Gangster Class";
            }
            else if (bounty < 10000)
            {
                return "Mafia Class";
            }
            else if (bounty < 200000)
            {
                return "Hitman Class";
            }
            else if (bounty < 250000)
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

        private void IsPlayerLooseCops()
        {
            if(Game.Player.WantedLevel > 0)
            {
                isWanted = true;
            }
            else if(Game.Player.WantedLevel < 1 && isWanted)
            {
                isWanted = false;
                IncreaseBounty(5000, "Most Wanted Person");
                MostWanted = true;
            }

            if(Game.Player.IsDead || Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player.Handle, true))
            {
                isWanted = false;
            }
        }

        private void DisguiseHandle()
        {
            Ped player = Game.Player.Character;
            if(player.IsWearingHelmet)
            {
                IsDisguise = true;
            }
            else
            {
                IsDisguise = false;
            }
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