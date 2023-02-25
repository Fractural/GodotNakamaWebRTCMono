using Godot;
using GDC = Godot.Collections;

namespace NakamaWebRTCDemo
{
    public class Screen : Control
    {
        protected UILayer uilayer;

        public virtual void Construct(UILayer uilayer)
        {
            this.uilayer = uilayer;
        }

        public virtual void ShowScreen(GDC.Dictionary args = null)
        {
            Visible = true;
        }

        public virtual void HideScreen()
        {
            Visible = false;
        }
    }
}
