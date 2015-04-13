using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace GreenerPower
{
    class SectionLayer_SourceGrid : SectionLayer
    {
        private int ShowFrame;
        private static readonly int numFrames = 20;
        private List<Thing> taken = new List<Thing>();
        public SectionLayer_SourceGrid(Section section) : base(section)
        {
            relevantChangeTypes.Add(MapChangeType.PowerGrid);
        }
        public override void Regenerate()
        {
            this.ClearSubMeshes(MeshParts.All);
            foreach (IntVec3 c in Find.Map.AllCells)
            {
                List<Thing> list = Find.ThingGrid.ThingsListAt(c);
                foreach(Thing t in list)
                {
                    if(t is AutoSwitch)
                    {
                        AutoSwitch A = (AutoSwitch)t;
                        A.gridChanged = true;
                    }
                }
            }
        }

        public override void DrawLayer()
        {
            if (Time.frameCount > this.ShowFrame + numFrames)
                return;
            base.DrawLayer();
        }

        public void TakePrintFrom(Thing t, int frameCount)
        {
            if(this.ShowFrame + numFrames < frameCount)
            {
                this.clear();
            }
            if(!this.taken.Contains(t))
            {
                Building building = t as Building;
                if (building == null)
                    return;
                this.taken.Add(t);
                //Log.Message("Taking print from " + building.ToString() + " At FC " + frameCount.ToString());
                building.PrintForPowerGrid((SectionLayer)this);
            }
            this.ShowFrame = frameCount;
        }

        public void finalMeshes()
        {
            this.FinalizeMesh(MeshParts.All);
        }

        public void clear()
        {
            taken.Clear();
            this.ClearSubMeshes(MeshParts.All);
        }
    }
}
