using HarmonyLib;
using Model.AI;
using Model;
using System.Reflection;
using UI.Builder;
using UI.CarInspector;
using Game.Messages;
using UI.EngineControls;
using JetBrains.Annotations;
using Network;
using System.Linq;
using Model.OpsNew;
using static Model.Car;
using System.Collections.Generic;
using System;

namespace FlyShuntUI.HarmonyPatches
{
    [UsedImplicitly]
    [HarmonyPatch(typeof(CarInspector), "BuildContextualOrders")]
    public static class CarInspectorBuildContextualOrdersPatch
    {
        static void Prefix(UIPanelBuilder builder, AutoEngineerPersistence persistence, CarInspector __instance)
        {
            if (!FlyShuntUIPlugin.Shared.IsEnabled)
            {
                return;
            }

            BaseLocomotive _car = (BaseLocomotive)(typeof(CarInspector).GetField("_car", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance));
            AutoEngineerOrdersHelper helper = new AutoEngineerOrdersHelper(_car as BaseLocomotive, persistence);
            AutoEngineerMode mode2 = helper.Mode();

            if (mode2 != AutoEngineerMode.Road)
            {
                return;
            }

            void SetOrdersValue(AutoEngineerMode? mode = null, bool? forward = null, int? maxSpeedMph = null, float? distance = null)
            {
                helper.SetOrdersValue(mode, forward, maxSpeedMph, distance);
            }

            builder.AddField("Disconnect Groups", builder.ButtonStrip(delegate (UIPanelBuilder bldr)
            {
                bldr.AddButton("All", delegate
                {
                    DisconnectCars(_car, -999, persistence);
                }).Tooltip("Disconnect all cars with waybills from the back", "Disconnect all cars with waybills from the back");

                bldr.AddButton("-3", delegate
                {
                    DisconnectCars(_car, -3, persistence);
                }).Tooltip("Disconnect 3 Car Groups From Back", "Disconnect 3 groups of cars from the back that are headed to 3 different locations");

                bldr.AddButton("-2", delegate
                {
                    DisconnectCars(_car, -2, persistence);
                }).Tooltip("Disconnect 2 Car Groups From Back", "Disconnect 2 groups of cars from the back that are headed to 2 different locations");

                bldr.AddButton("-1", delegate
                {
                    DisconnectCars(_car, -1, persistence);
                }).Tooltip("Disconnect 1 Car Group From Back", "Disconnect all cars from the back of the train headed to the same location");

                bldr.AddButton("1", delegate
                {
                    DisconnectCars(_car, 1, persistence);
                }).Tooltip("Disconnect 1 Car Group From Front", "Disconnect all cars from the front of the train headed to the same location");

                bldr.AddButton("2", delegate
                {
                    DisconnectCars(_car, 2, persistence);
                }).Tooltip("Disconnect 2 Car Groups From Front", "Disconnect 2 groups of cars from the front that are headed to 2 different locations");

                bldr.AddButton("3", delegate
                {
                    DisconnectCars(_car, 3, persistence);
                }).Tooltip("Disconnect 3 Car Groups From Front", "Disconnect 3 groups of cars from the front that are headed to 3 different locations");

                bldr.AddButton("All", delegate
                {
                    DisconnectCars(_car, 999, persistence);
                }).Tooltip("Disconnect all cars with waybills from the front", "Disconnect all cars with waybills from the front");

            }, 4)).Tooltip("Disconnect Car Groups", "Disconnect groups of cars headed for the same location from the front (positive numbers) or the back (negative numbers) in the direction of travel");
        }

        static void DisconnectCars(BaseLocomotive locomotive, int numGroups, AutoEngineerPersistence persistence)
        {
            var end = numGroups > 0 ? "front" : "back";
            numGroups = Math.Abs(numGroups);

            var orders = persistence.Orders;

            List<Car> cars;

            if (end == "front")
            {
                if (orders.Forward)
                {
                    cars = locomotive.EnumerateCoupled(Car.End.R).Reverse().ToList();
                }
                else
                {
                    cars = locomotive.EnumerateCoupled(Car.End.F).Reverse().ToList();
                }
            }
            else
            {
                if (orders.Forward)
                {
                    cars = locomotive.EnumerateCoupled(Car.End.F).Reverse().ToList();
                }
                else
                {
                    cars = locomotive.EnumerateCoupled(Car.End.R).Reverse().ToList();

                }
            }

            OpsController opsController = OpsController.Shared;

            if (cars.Count < 2)
            {
                DebugLog("ERROR: not enough cars");
                return;
            }

            Car firstCar = cars[0];

            var maybeFirstCarWaybill = firstCar.GetWaybill(opsController);
            if (maybeFirstCarWaybill == null)
            {
                return;
            }

            OpsCarPosition destination = maybeFirstCarWaybill.Value.Destination;
            Car? carToDisconnect = null;

            int carsToDisconnectCount = 0;
            int groupsFound = 1;

            foreach (Car car in cars)
            {
                var maybeWaybill = car.GetWaybill(opsController);
                if (maybeWaybill == null)
                {

                    DebugLog($"Car {car.DisplayName}, has no waybill, stopping search");
                    break;
                }

                OpsCarPosition thisCarDestination = maybeWaybill.Value.Destination;
                if (destination.Identifier == thisCarDestination.Identifier)
                {
                    DebugLog($"Car {car.DisplayName} is part of group {groupsFound}");
                    carToDisconnect = car;
                    carsToDisconnectCount++;
                }
                else
                {
                    if (groupsFound < numGroups)
                    {
                        destination = thisCarDestination;
                        carToDisconnect = car;
                        carsToDisconnectCount++;
                        groupsFound++;
                        DebugLog($"Car {car.DisplayName} is part of new group {groupsFound}");
                    } else
                    {
                        DebugLog($"{groupsFound} groups found, stopping search");
                        break;
                    }
                }
            }

            if (carsToDisconnectCount == 0)
            {
                DebugLog($"No cars found to disconnect");
                return;
            }

            Car newEndCar = cars[carsToDisconnectCount];

            var groupsMaybePlural = groupsFound > 1 ? "groups of cars" : "group of cars";

            var groupsString = numGroups == 999 ? "all cars with waybills" : $"{groupsFound} {groupsMaybePlural}";

            var carsMaybePlural = carsToDisconnectCount > 1 ? "cars" : "car";
            Multiplayer.Broadcast($"Disconnecting {groupsString} totalling {carsToDisconnectCount} {carsMaybePlural} from the {end} of the train");
            DebugLog($"Disconnecting coupler between {newEndCar.DisplayName} and {carToDisconnect.DisplayName}");

            var newEndCarEndToDisconnect = (newEndCar.CoupledTo(LogicalEnd.A) == carToDisconnect) ? LogicalEnd.A : LogicalEnd.B;
            var carToDisconnectEndToDisconnect = (carToDisconnect.CoupledTo(LogicalEnd.A) == newEndCar) ? LogicalEnd.A : LogicalEnd.B;

            newEndCar.ApplyEndGearChange(newEndCarEndToDisconnect, EndGearStateKey.CutLever, 1f);
            newEndCar.ApplyEndGearChange(newEndCarEndToDisconnect, EndGearStateKey.Anglecock, 0f);
            carToDisconnect.ApplyEndGearChange(carToDisconnectEndToDisconnect, EndGearStateKey.Anglecock, 0f);
        }

        private static void DebugLog(string message)
        {
            if (!FlyShuntUIPlugin.Settings.EnableDebug)
            {
                return;
            }

            Multiplayer.Broadcast(message);
        }
    }
}