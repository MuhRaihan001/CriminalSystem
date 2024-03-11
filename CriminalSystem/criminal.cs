using System;
using System.IO;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace CriminalSystem
{

    public class criminal : Script
    {
        int bounty;
        int lastBounty;
        public criminal()
        {
            GTA.Native.Function.Call(Hash.WAIT, 0);
            Tick += OnTick;
            KeyUp += OnKeyUp;
            KeyDown += OnKeyDown;

            bounty = 0;
            lastBounty = 0;
            NotificationIcon icon = new NotificationIcon();
            icon = NotificationIcon.Call911;
            GTA.UI.Notification.Show(icon, "Government", "TsumuX", "Improvement Criminal Loaded", false, false);
        }

        private void OnTick(object sender, EventArgs e)
        {
            
            KillNpc();
            StoleVehicleHandle();
            NpcReaction();

            if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player.Handle, true))
            {
                lastBounty = bounty;
                bounty = 0;
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
            if(player.IsInVehicle())
            {
                Vehicle car = player.CurrentVehicle;

                if(!car.Equals(Game.Player.LastVehicle))
                {

                    int increasedBounty = 1000;
                    IncreaseBounty(increasedBounty, "Stole Vehicle");
                }
            }
        }

        private void KillNpc()
        {
            Ped player = Game.Player.Character;

            if (player.IsAiming && Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING_AT_ENTITY, player, -1))
            {
                Entity targetEntity = Function.Call<Entity>(Hash.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT, player);

                if (targetEntity != null && targetEntity.Exists() && targetEntity.GetType() == typeof(Ped))
                {
                    Ped targetPed = (Ped)targetEntity;

                    if (targetPed.IsDead)
                    {
                        Console.WriteLine("Npc Died");
                        int increasedBounty = 5000;
                        IncreaseBounty(increasedBounty, "Killing People");

                    }
                }
            }
        }

        private void NpcReaction()
        {
            Ped player = Game.Player.Character;
            bool bountyHunterSpawnned = false;
            Vector3 Pos = player.Position;
            Ped[] nearbyNPCs = World.GetNearbyPeds(Pos, 30.0f);
            if(bounty >= 100000)
            {
                foreach (Ped npc in nearbyNPCs)
                {
                    float distance = World.GetDistance(Pos, npc.Position);
                    if (distance < 30.0f)
                    {
                        if (!npc.IsInVehicle() && !npc.IsPlayer)
                        {
                            npc.Task.ClearAll();
                            npc.Task.ReactAndFlee(player);
                            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player.Handle, 4);
                        }
                    }
                }
                if(!bountyHunterSpawnned)
                {
                    SpawnBountyHunter();
                    bountyHunterSpawnned= true;
                }
                
            }

        }

        private void SpawnBountyHunter()
        {
            Vector3 playerPos = Game.Player.Character.Position;
            Vector3 BountyHunterPos = playerPos.Around(1.0f);

            Ped BountyHunter = World.CreatePed(PedHash.Bankman, BountyHunterPos);
            Console.WriteLine("Bounty Hunter Created To Chase You");
            if(bounty <= 5000)
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

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V)
            {
                IncreaseBounty(5000, "Testing");
            }
            if(e.KeyCode == Keys.X)
            {
                SpawnBountyHunter();
            }
        }
    }
}