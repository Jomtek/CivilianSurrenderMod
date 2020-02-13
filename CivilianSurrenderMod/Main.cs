using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;
using System.Windows.Forms;
using System.Drawing;

namespace CivilianSurrenderMod
{
    public class Main : Script
    {
        private List<Ped> rebelPeds = new List<Ped>();
        private List<Ped> surrenderedVehicleOccupants = new List<Ped>();
        private List<Ped> surrenderedPeds = new List<Ped>();
        private string[] fullNames = new string[] { "Sid Alderson", "Timeli Zidan", "Jess Kotcharian",
                                                    "Samy Baniata", "Cameric Jefferson", "Aimery Mendelssohn",
                                                    "Yang LeBron", "Leslie Clever", "Hilary Clonton",
                                                    "Don El Trump", "Jenny Zboubman", "Jeffrey Glacon"};
        public Main()
        {
            UI.Notify("Civilian Surrender Mod by Jomtek");

            // this.Tick += onTick;
            // this.KeyUp += onKeyUp;
            this.KeyDown += onKeyDown;
        }

/*        private void onTick(object sender, EventArgs e)
        {
            


        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {

        }*/

        private void onKeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.X)
            {
                Entity targetedEntity = Game.Player.GetTargetedEntity();

                if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, targetedEntity))
                {
                    Ped targetedPed = World.GetClosestPed(targetedEntity.Position, 1);
                    bool pedHasWeapon = targetedPed.Weapons.Current.AmmoInClip != 1;

                    if (targetedPed.IsAttachedTo(targetedPed.LastVehicle) && !(targetedPed.IsInPoliceVehicle) && !(rebelPeds.Contains(targetedPed)))
                    {

                        /* This starts when Ped is:
                         * In any vehicle (except for police vehicles)
                         * Not a rebel
                         */

                        if ((new Random()).Next(0, 4) == 3 || targetedPed.Weapons.Current.AmmoInClip != 1)
                        {
                            // Targeted pedestrian has a weapon OR
                            // Random pick. Determinates if the ped should be a rebel or not.


                            UI.Notify("~r~Radio info~s~: Rebel civilian detected. Be careful, he might have a weapon on him.");
                            targetedPed.MovementAnimationSet = "middle_finger";
                            return;
                        }
                        else
                        {
                            // Generate a false ID card for the arrested Ped.
                            UI.Notify("~g~Radio info~s~: Driver/passenger arrested successfully.");
                            UI.ShowHelpMessage(GenerateFalseIdentity(targetedPed), 7000, true);
                        }

                        //foreach (Ped passenger in targetedPed.LastVehicle.Occupants)
                        for (int i = 0; i < targetedPed.LastVehicle.Occupants.Length; i++)
                        {
                            Ped passenger = targetedPed.LastVehicle.Occupants[i];

                            surrenderedVehicleOccupants.Add(passenger);
                            Function.Call(Hash.SET_PED_ALERTNESS, passenger, 0); // Make the occupants dumb


                            // TODO: random pick to know if whether or not they wanna get out of vehicle

                            if (!passenger.IsAimingFromCover)
                            {
                                passenger.Task.LeaveVehicle(); // Let each one of them get out of the vehicle
                                Wait(1200);
                                ArrestPed(ref passenger, pedHasWeapon);
                            }
                        }

                    }

                    else if (!targetedPed.IsAttachedTo(targetedPed.LastVehicle) && surrenderedVehicleOccupants.Contains(targetedPed))
                    {
                        // Starts if ped is out of his vehicle and has already been arrested while he was in a vehicle
                        ReleasePed(ref targetedPed, ref targetedEntity);
                        UI.Notify("~g~Radio info~s~: Driver/passenger released successfully.");                        
                        surrenderedVehicleOccupants.Remove(targetedPed);
                    }

                    else if (pedHasWeapon)
                    {
                        if (targetedPed.Health < 190)
                        {
                            if (targetedPed.IsInVehicle())
                            {
                                targetedPed.Task.LeaveVehicle();
                                Wait(1200);
                            }

                            ArrestPed(ref targetedPed, true);
                            UI.Notify("~g~Radio info~s~: This gangster surrendered himself because he was injured.");
                            // targetedPed.MovementAnimationSet = "sit";
                        }
                        else
                            UI.Notify("~r~Radio info~s~: Rebel civilian detected. We know that he has weapons on him.");
                    }

                    // Walking pedestrian arrestation part
                    else if (!surrenderedPeds.Contains(targetedPed))
                    {
                        if ((new Random()).Next(0, 4) == 3)
                            UI.Notify("~r~Radio info~s~: Rebel civilian detected. Be careful, he might have a weapon on him.");
                        else {
                            ArrestPed(ref targetedPed, pedHasWeapon);
                            UI.Notify("~g~Radio info~s~: Pedestrian arrested successfully.");
                            surrenderedPeds.Add(targetedPed);
                        }
                    }
                    
                    else
                    {
                        ReleasePed(ref targetedPed, ref targetedEntity);
                        UI.Notify("~g~Radio info~s~: Pedestrian released successfully.");
                        surrenderedPeds.Remove(targetedPed);
                    }
                    
                }

            }
        }

        private void ArrestPed(ref Ped ped, bool hasWeapon = false)
        {
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, ped, true, true);
            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, ped, 0, 0);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 17, 1);
            Function.Call(Hash.SET_PED_SEEING_RANGE, ped, 0.0f);
            Function.Call(Hash.SET_PED_HEARING_RANGE, ped, 0.0f);
            Function.Call(Hash.SET_PED_ALERTNESS, 0);

            // TODO: Personal reactions (on the knees, hands up...) using random num generation
            if (hasWeapon)
            {
                ped.Weapons.Drop();
                ped.Task.PlayAnimation("random@arrests@busted", "idle_c", 1, -1, true, 1);
                UI.Notify("~r~Radio info~s~: This civilian has a ~r~weapon~s~. Be careful!");
            }
            else
                ped.Task.HandsUp(-1);

            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, ped, 1);
        }

        private void ReleasePed(ref Ped ped, ref Entity entity)
        {
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, entity, false, false);
            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, ped, 0);
            Function.Call(Hash.SET_PED_SEEING_RANGE, ped, 100.0f);
            Function.Call(Hash.SET_PED_HEARING_RANGE, ped, 100.0f);
            ped.Task.FleeFrom(Game.Player.Character);
        }

        private string GenerateFalseIdentity(Ped suspect)
        {
            Random selector = new Random();
            String full_name = fullNames[selector.Next(0, fullNames.Count() - 1)];
            return "Full name: " + full_name;
        }

    }
}   
    