using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;

namespace GreenerPower
{
    class ITab_Power : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(400f, 280f);
        private float viewHeight = 1000f;
        private Vector2 scrollPosition = new Vector2();
        private static readonly float spacer = 20f;

        public ITab_Power()
        {
            this.size = ITab_Power.WinSize;
            this.labelKey = "Tab Power111";
        }
        public override void TabUpdate()
        {
            base.TabUpdate();
        }

        protected AutoSwitch SelSwitch
        {
            get
            {
                return (AutoSwitch)this.SelThing;
            }
        }


        protected override void FillTab()
        {
            SelSwitch.showSourceGrid();
            Text.Font = GameFont.Small;
            List<CompPower> highlightedThings = new List<CompPower>();
            float top = 30f;
            float left = 30f;
            Widgets.ListSeparator(ref top, 300f, "Set Source");
            DrawSourceSetter(top, left);
            top += 30f;
            Widgets.ListSeparator(ref top, 300f, "Set Stored Power on/off");
            Widgets.Label(new Rect(left, top, 300f, 29f), "Current Source Stored Power");
            top += 30f;
            Widgets.Label(new Rect(left + 300f, top, 150f, 29f), this.SelSwitch.SourceBattery.ToString());
            top += 30f;
            top = DrawTurnOffSetter(top, left);
            top += 30f;
            DrawTurnOnSetter(top, left);
        }

        private void DrawTurnOnSetter(float top, float left)
        {
            Widgets.Label(new Rect(left, top, 150f, 29f), "Turn On at:");
            float onResult;
            if (float.TryParse(Widgets.TextField(new Rect(left + 100f, top, 130, 29f), this.SelSwitch.minSourceBatteryOn.ToString()), out onResult))
            {
                this.SelSwitch.minSourceBatteryOn = onResult;
            }
        }

        private float DrawTurnOffSetter(float top, float left)
        {
            Widgets.Label(new Rect(left, top, 150f, 29f), "Turn Off at:");
            float offResult;
            if (float.TryParse(Widgets.TextField(new Rect(left + 100f, top, 130, 29f), this.SelSwitch.minSourceBatteryOff.ToString()), out offResult))
            {
                this.SelSwitch.minSourceBatteryOff = offResult;
            }
            top += 20f;
            return top;
        }

        private void DrawSourceSetter(float top, float left)
        {
            Widgets.Label(new Rect(left, top, 150f, 29f), "Source");
            Rect SourceBTNRect = new Rect(left + 100f, top, 130f, 29f);
            if (Widgets.TextButton(SourceBTNRect, GreenerConstants.cardnialNames[this.SelSwitch.Source]))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("North", () => { this.SelSwitch.Source = 2; this.SelSwitch.sourceChanged = true; this.SelSwitch.updateGrids(); }));
                options.Add(new FloatMenuOption("East", () => { this.SelSwitch.Source = 3; this.SelSwitch.sourceChanged = true; this.SelSwitch.updateGrids(); }));
                options.Add(new FloatMenuOption("South", () => { this.SelSwitch.Source = 1; this.SelSwitch.sourceChanged = true; this.SelSwitch.updateGrids(); }));
                options.Add(new FloatMenuOption("West", () => { this.SelSwitch.Source = 0; this.SelSwitch.sourceChanged = true; this.SelSwitch.updateGrids(); }));
                Find.LayerStack.Add((Layer)new Layer_FloatMenu(options, false));
            }
        }
    }
}
