using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using System.Collections.Generic;
using GDC = Godot.Collections;

namespace NakamaWebRTCDemo
{
    public partial class Screen : Control
    {
        [OnReadyGet(OrNull = true)]
        public Screen ParentScreen { get; protected set; }

        protected UILayer uiLayer;

        public virtual void Construct(UILayer uilayer)
        {
            this.uiLayer = uilayer;
        }

        public virtual void ShowScreen(object args)
        {
            Visible = true;
        }

        public virtual void HideScreen()
        {
            Visible = false;
        }
    }
}
