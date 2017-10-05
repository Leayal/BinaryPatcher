using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;

namespace BinaryPatcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ResolveEventHandler ev = new ResolveEventHandler(AssemblyLoader.AssemblyResolve);
            AppDomain.CurrentDomain.AssemblyResolve += ev;

            Controller controller = new Controller();
            controller.Run(System.Environment.GetCommandLineArgs());

            AppDomain.CurrentDomain.AssemblyResolve -= ev;
        }

        private class Controller : WindowsFormsApplicationBase
        {
            public Controller() : base(AuthenticationMode.Windows)
            {
                this.IsSingleInstance = false;
                this.EnableVisualStyles = true;
            }

            protected override void OnCreateMainForm()
            {
                this.MainForm = new MyMainMenu();
            }
        }
    }
}
