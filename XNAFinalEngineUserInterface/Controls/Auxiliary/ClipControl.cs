
#region License
/*

 Based in the project Neoforce Controls (http://neoforce.codeplex.com/)
 GNU Library General Public License (LGPL)

-----------------------------------------------------------------------------------------------------------------------------------------------
Modified by: Schneider, Jos� Ignacio (jis@cs.uns.edu.ar)
-----------------------------------------------------------------------------------------------------------------------------------------------

*/
#endregion

namespace XNAFinalEngine.UserInterface
{

    public class ClipControl : Control
    {

        #region Properties

        /// <summary>
        /// Client Area.
        /// </summary>
        public virtual ClipBox ClientArea { get; set; }

        /// <summary>
        /// Get and set the control's client margins.
        /// </summary>
        public override Margins ClientMargins
        {
            get { return base.ClientMargins; }
            set
            {
                base.ClientMargins = value;
                if (ClientArea != null)
                {
                    ClientArea.Left   = ClientLeft;
                    ClientArea.Top    = ClientTop;
                    ClientArea.Width  = ClientWidth;
                    ClientArea.Height = ClientHeight;
                }
            }
        } // ClientMargins

        #endregion

        #region Constructor

        public ClipControl()
        {
            ClientArea = new ClipBox
            {
                MinimumWidth = 0,
                MinimumHeight = 0,
                Left = ClientLeft,
                Top = ClientTop,
                Width = ClientWidth,
                Height = ClientHeight
            };

            base.Add(ClientArea);
        } // ClipControl

        #endregion

        #region Add and Remove
        
        public virtual void Add(Control control, bool client)
        {
            if (client)
            {
                ClientArea.Add(control);
            }
            else
            {
                base.Add(control);
            }
        } // Add

        public override void Add(Control control)
        {
            Add(control, true);
        } // Add
      
        public override void Remove(Control control)
        {
            base.Remove(control);
            ClientArea.Remove(control);
        } // Remove

        #endregion

        #region On Resize

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            if (ClientArea != null)
            {
                ClientArea.Left = ClientLeft;
                ClientArea.Top = ClientTop;
                ClientArea.Width = ClientWidth;
                ClientArea.Height = ClientHeight;
            }
        } // OnResize

        #endregion

        #region Adjust Margins

        protected virtual void AdjustMargins()
        {
            // Overrite it!!
        } // AdjustMargins

        #endregion

    } // ClipControl
} // XNAFinalEngine.UI