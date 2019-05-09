using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ErpNet.FP.Win.Manager
{
    public class MainForm : System.Windows.Forms.Form
    {
        private NotifyIcon managerNotifyIcon;
        private ContextMenu managerContextMenu;
        private MenuItem menuItemExit;
        private IContainer components;

        public MainForm()
        {
            this.Text = "ErpNet.FP.Win Manager";

            this.components = new Container();

            this.menuItemExit = new MenuItem
            {
                Index = 0,
                Text = "E&xit"
            };
            this.menuItemExit.Click += new EventHandler(this.MenuItemExit_Click);

            this.managerContextMenu = new ContextMenu();
            this.managerContextMenu.MenuItems.AddRange(
                new MenuItem[] { this.menuItemExit });

            this.managerNotifyIcon = new NotifyIcon(this.components)
            {
                Icon = new Icon("ErpNet.Win.FP.ico"),
                ContextMenu = this.managerContextMenu,
                Text = "ErpNet.FP.Win.Manager",
                Visible = true,
            };
            managerNotifyIcon.DoubleClick += new EventHandler(this.ManagerNotifyIcon_DoubleClick);
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!this.IsHandleCreated)
            {
                // Starting form invisible, show notify icon only in tray area
                CreateHandle();
                value = false;
            }
            base.SetVisibleCore(value);
        }

        protected override void Dispose(bool disposing)
        {
            // Clean up any components being used.
            if (disposing)
                if (components != null)
                    components.Dispose();

            base.Dispose(disposing);
        }

        private void ManagerNotifyIcon_DoubleClick(object Sender, EventArgs e)
        {
            // Show the form when the user double clicks on the notify icon.

            // Set the WindowState to normal if the form is minimized.
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            // Activate the form.
            this.Activate();
        }

        private void MenuItemExit_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            this.Close();
        }
    }
}