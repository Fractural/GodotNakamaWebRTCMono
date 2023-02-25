using Godot;
using GDC = Godot.Collections;

namespace NakamaWebRTCDemo
{
    public class Screen : Control
    {
        protected UILayer uiLayer;

        public virtual void Construct(UILayer uilayer)
        {
            this.uiLayer = uilayer;
        }

        public virtual void ShowScreen(GDC.Dictionary args)
        {
            Visible = true;
        }

        public virtual void HideScreen()
        {
            Visible = false;
        }
    }
}
