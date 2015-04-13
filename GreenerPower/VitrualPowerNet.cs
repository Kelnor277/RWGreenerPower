using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace GreenerPower
{
    public class VirtualPowerNet
    {
        private static List<CompPowerTrader> partsWantingPowerOn = new List<CompPowerTrader>();
        private static List<CompPowerTrader> potentialShutdownParts = new List<CompPowerTrader>();
        public List<CompPower> connectors = new List<CompPower>();
        public List<CompPower> transmitters = new List<CompPower>();
        public List<CompPowerTrader> powerComps = new List<CompPowerTrader>();
        public List<CompPowerBattery> batteryComps = new List<CompPowerBattery>();
        private List<CompPowerBattery> givingBats = new List<CompPowerBattery>();
        private List<CompPowerBattery> batteryCompsShuffled = new List<CompPowerBattery>();
        private const int MaxRestartTryInterval = 200;
        private const int MinRestartTryInterval = 30;
        private const int ShutdownInterval = 20;
        private const float MinStoredEnergyToTurnOn = 5f;
        public bool hasPowerSource;
        private float debugLastCreatedEnergy;
        private float debugLastRawStoredEnergy;
        private float debugLastApparentStoredEnergy;

        private static HashSet<Building> closedSet = new HashSet<Building>();
        private static HashSet<Building> openSet = new HashSet<Building>();
        private static HashSet<Building> currentSet = new HashSet<Building>();

        public static IEnumerable<CompPower> ContiguousPowerBuildings(Building root)
        {
            closedSet.Clear();
            currentSet.Clear();
            openSet.Add(root);
            do
            {
                using (HashSet<Building>.Enumerator enumerator = openSet.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Building current = enumerator.Current;
                        closedSet.Add(current);
                    }
                }
                HashSet<Building> hashSet = currentSet;
                currentSet = openSet;
                openSet = hashSet;
                openSet.Clear();
                using (HashSet<Building>.Enumerator enumerator = currentSet.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        foreach (IntVec3 c in GenAdj.CellsAdjacentCardinal((Thing)enumerator.Current))
                        {
                            List<Thing> thingList = GridsUtility.GetThingList(c);
                            for (int index = 0; index < thingList.Count; ++index)
                            {
                                Building building = thingList[index] as Building;
                                if (building != null && building.TransmitsPowerNow && (!openSet.Contains(building) && !currentSet.Contains(building)) && !closedSet.Contains(building))
                                {
                                    openSet.Add(building);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            while (openSet.Count > 0);
            return Enumerable.Select<Building, CompPower>((IEnumerable<Building>)closedSet, (Func<Building, CompPower>)(b => b.PowerComp));
        }

        public VirtualPowerNet(System.Collections.Generic.IEnumerable<CompPower> newTransmitters)
        {
            foreach (CompPower compPower in newTransmitters)
            {
                this.transmitters.Add(compPower);
                this.RegisterAllComponentsOf(compPower.parent);
                if (compPower.connectChildren != null)
                {
                    List<CompPower> list = compPower.connectChildren;
                    for (int index = 0; index < list.Count; ++index)
                        this.RegisterConnector(list[index]);
                }
            }
            this.hasPowerSource = false;
            for (int index = 0; index < this.transmitters.Count; ++index)
            {
                if (this.IsPowerSource(this.transmitters[index]))
                {
                    this.hasPowerSource = true;
                    break;
                }
            }
        }

        private void RegisterAllComponentsOf(ThingWithComponents parentThing)
        {
            CompPowerTrader comp1 = parentThing.GetComp<CompPowerTrader>();
            if (comp1 != null)
                this.powerComps.Add(comp1);
            CompPowerBattery comp2 = parentThing.GetComp<CompPowerBattery>();
            if (comp2 == null)
                return;
            this.batteryComps.Add(comp2);
        }

        private void DeregisterAllComponentsOf(ThingWithComponents parentThing)
        {
            CompPowerTrader comp1 = parentThing.GetComp<CompPowerTrader>();
            if (comp1 != null)
                this.powerComps.Remove(comp1);
            CompPowerBattery comp2 = parentThing.GetComp<CompPowerBattery>();
            if (comp2 == null)
                return;
            this.batteryComps.Remove(comp2);
        }

        public float CurrentEnergyGainRate()
        {
            if (DebugSettings.unlimitedPower)
                return 100000f;
            float num = 0.0f;
            for (int index = 0; index < this.powerComps.Count; ++index)
            {
                if (this.powerComps[index].PowerOn)
                    num += this.powerComps[index].EnergyOutputPerTick;
            }
            return num;
        }

        public float CurrentStoredEnergy()
        {
            float num = 0.0f;
            for (int index = 0; index < this.batteryComps.Count; ++index)
                num += this.batteryComps[index].StoredEnergy;
            return num;
        }

        private bool IsPowerSource(CompPower cp)
        {
            return cp is CompPowerBattery || cp is CompPowerTrader && (double)cp.props.basePowerConsumption < 0.0;
        }

        public void RegisterConnector(CompPower b)
        {
            this.connectors.Add(b);
            this.RegisterAllComponentsOf(b.parent);
        }

        public void DeregisterConnector(CompPower b)
        {
            this.connectors.Remove(b);
            this.DeregisterAllComponentsOf(b.parent);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("POWERNET:");
            stringBuilder.AppendLine("  Created energy: " + (object)this.debugLastCreatedEnergy);
            stringBuilder.AppendLine("  Raw stored energy: " + (object)this.debugLastRawStoredEnergy);
            stringBuilder.AppendLine("  Apparent stored energy: " + (object)this.debugLastApparentStoredEnergy);
            stringBuilder.AppendLine("  Transmitters: ");
            using (List<CompPower>.Enumerator enumerator = this.transmitters.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    CompPower current = enumerator.Current;
                    stringBuilder.AppendLine("      " + (object)current.parent);
                }
            }
            stringBuilder.AppendLine("  Connectors: ");
            using (List<CompPower>.Enumerator enumerator = this.connectors.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    CompPower current = enumerator.Current;
                    stringBuilder.AppendLine("      " + (object)current.parent);
                }
            }
            return stringBuilder.ToString();
        }
    }
}
