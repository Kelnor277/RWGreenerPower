using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;

namespace GreenerPower
{
    public class AutoSwitch : Building_PowerSwitch
    {
        //Dictionary<string, PowerNet> grids = new Dictionary<string, PowerNet>();
        private float sourceBattery;
        public float minSourceBatteryOff = 0f;
        public float minSourceBatteryOn = 1000f;
        private bool autoOff = false;
        private VirtualPowerNet sourceNet = null;
        public bool drawSourceNet = false;
        public bool sourceChanged = false;
        public bool gridChanged = false;
        private bool pretendOff = false;
        private int source = 2;

        public VirtualPowerNet SourceNet
        {
            get { return sourceNet; }
        }

        public float SourceBattery
        {
            get { return sourceBattery; }
            set { sourceBattery = value; }
        }


        public int Source
        {
            get { return source; }
            set 
            { 
                if(source < 4 && source >= 0)
                {
                    this.source = value;
                }
            }
        }

        public override void Tick()
        {
            updateGrids();
            //PowerNet sourceGrid = this.grids[GreenerConstants.cardnialNames[this.source]];
            if (this.sourceNet != null)
            {
                this.sourceBattery = this.sourceNet.CurrentStoredEnergy();
                if (!this.SwitchOn && this.autoOff)
                {
                    //Check if turn back on
                    if (this.sourceBattery > this.minSourceBatteryOn)
                    {
                        Log.Message("Turning on at: " + this.sourceBattery.ToString());
                        this.autoOff = false;
                        this.SwitchOn = true;
                    }
                }
                if (this.SwitchOn && !this.autoOff)
                {
                    //Check if turn off
                    if (this.sourceBattery < this.minSourceBatteryOff)
                    {
                        Log.Message("Turning off at: " + this.sourceBattery.ToString());
                        this.autoOff = true;
                        this.SwitchOn = false;
                    }
                }
            }
            base.Tick();
        }

        public void updateGrids()
        {
            if (!this.gridChanged)
                return;
            this.gridChanged = false;
            Log.Message("Updating grids for " + this.ToString() + " Source Grid is: " + GreenerConstants.cardnialNames[this.source]);
            this.pretendOff = true;
            updateSourceGrid();
            this.pretendOff = false;
        }

        public void showSourceGrid()
        {
            if (this.sourceChanged)
            {
                this.gridChanged = true;
                this.updateGrids();
                this.ClearSourceGrid();
                this.sourceChanged = false;
            }
            if (this.sourceNet != null)
            {
                List<SectionLayer_SourceGrid> layers = new List<SectionLayer_SourceGrid>();
                foreach (CompPower p in this.sourceNet.transmitters)
                {
                    Section s = Find.MapDrawer.SectionAt(p.parent.Position);
                    SectionLayer_SourceGrid gridLayer = (SectionLayer_SourceGrid)s.GetLayer(typeof(SectionLayer_SourceGrid));
                    gridLayer.TakePrintFrom((Thing)p.parent, Time.frameCount);
                    if(!layers.Contains(gridLayer))
                    {
                        layers.Add(gridLayer);
                    }
                }
                foreach(SectionLayer_SourceGrid layer in layers)
                {
                    layer.finalMeshes();
                }
            }
        }

        public void ClearSourceGrid()
        {
            if (this.sourceNet != null)
            {
                List<SectionLayer_SourceGrid> layers = new List<SectionLayer_SourceGrid>();
                foreach (CompPower p in this.sourceNet.transmitters)
                {
                    Section s = Find.MapDrawer.SectionAt(p.parent.Position);
                    SectionLayer_SourceGrid gridLayer = (SectionLayer_SourceGrid)s.GetLayer(typeof(SectionLayer_SourceGrid));
                    if (!layers.Contains(gridLayer))
                    {
                        layers.Add(gridLayer);
                    }
                }
                foreach (SectionLayer_SourceGrid layer in layers)
                {
                    layer.clear();
                }
            }
        }

        private void updateSourceGrid()
        {
            this.sourceNet = null;
            IntVec3[] adjacent = GenAdj.CellsAdjacentCardinal((Thing)this).ToArray<IntVec3>();
            IntVec3 c = adjacent[this.source];
            if (GenGrid.InBounds(c))
            {
                Building transmitter = GridsUtility.GetTransmitter(c);
                if (transmitter != null && transmitter.TransmitsPowerNow)
                {
                    this.sourceNet = new VirtualPowerNet(VirtualPowerNet.ContiguousPowerBuildings(transmitter));
                }
            }
        }

        public override bool TransmitsPowerNow
        {
            get
            {
                if (!pretendOff)
                {
                    return base.TransmitsPowerNow;
                }
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<bool>(ref this.autoOff, "autoOff", false, false);
            Scribe_Values.LookValue<float>(ref this.minSourceBatteryOff, "minSourceBatterOff", 0f, false);
            Scribe_Values.LookValue<float>(ref this.minSourceBatteryOn, "minSourceBatteryOn", 1000f, false);
        }
    }
}
