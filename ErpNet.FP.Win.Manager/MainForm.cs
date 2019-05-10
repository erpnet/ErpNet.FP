using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ErpNet.FP.Win.Manager
{
    public class MainForm : Form
    {
        private readonly NotifyIcon managerNotifyIcon;
        private readonly ContextMenu managerContextMenu;
        private readonly MenuItem menuItemExit;
        private readonly MenuItem menuItemShowConsole;
        private readonly TextBox logBox;
        private readonly IContainer components;

        public MainForm()
        {
            this.Text = "ErpNet.FP.Win Manager";
            this.Icon = new Icon("ErpNet.FP.Win.ico");

            this.components = new Container();

            this.logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Parent = this,
                Multiline = true,
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                WordWrap = false
            };

            this.components.Add(this.logBox);

            this.menuItemShowConsole = new MenuItem
            {
                Text = "Show &console"
            };
            this.menuItemShowConsole.Click += new EventHandler(this.MenuItemShowConsole_Click);

            this.menuItemExit = new MenuItem
            {
                Text = "E&xit"
            };
            this.menuItemExit.Click += new EventHandler(this.MenuItemExit_Click);

            this.managerContextMenu = new ContextMenu();
            this.managerContextMenu.MenuItems.AddRange(
                new MenuItem[] {
                    this.menuItemShowConsole,
                    new MenuItem("-"),
                    this.menuItemExit
                });

            this.managerNotifyIcon = new NotifyIcon(this.components)
            {
                Icon = new Icon("ErpNet.FP.Win.ico"),
                ContextMenu = this.managerContextMenu,
                Text = "ErpNet.FP.Win.Manager",
                Visible = true,
            };
            managerNotifyIcon.DoubleClick += new EventHandler(this.ManagerNotifyIcon_DoubleClick);

            this.managerNotifyIcon.BalloonTipText = "Starting ErpNet.FP.Win service...";
            this.managerNotifyIcon.ShowBalloonTip(3000);
            RunService();
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

        private void MenuItemShowConsole_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            this.Show();
        }

        private void RunService()
        {
            // Creating the service process
            Process process = new Process();
            process.StartInfo.FileName = @"ErpNet.FP.Win.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // Setting output and error (asynchronous) handlers
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);

            // Starting process and handlers
            ThreadStart processThreadStarter = new ThreadStart(() =>
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            });
            Thread processThread = new Thread(processThreadStarter);
            processThread.Start();
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            this.BeginInvoke(new MethodInvoker(() =>
            {
                this.logBox.AppendText(outLine.Data ?? string.Empty);
                this.logBox.AppendText("\n");
            }));
        }
    }
}