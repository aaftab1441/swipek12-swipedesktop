using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SqlServer.Server;

namespace SwipeDesktop
{
    [RunInstaller(false)]
    public class InstallerAction : System.Configuration.Install.Installer
    {
        private static string SqlName = @"localhost\SQLEXPRESS";

        public InstallerAction()
        {
            //InitializeComponent();
        }

        private bool GrantAccess(string fullPath)
        {
            var fi = new FileInfo(fullPath);
            var fSecurity = fi.GetAccessControl();

            var rule = new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), FileSystemRights.FullControl, AccessControlType.Allow);

            fSecurity.AddAccessRule(rule);
            fi.SetAccessControl(fSecurity);

            return true;

        }

        static void RunScript(string installPath)
        {
            string sqlConnectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=master;Data Source=" + SqlName;


            SqlConnection Connection = new SqlConnection(sqlConnectionString);

            string script = File.ReadAllText(installPath);

            // split script on GO command
            IEnumerable<string> commandStrings = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            Connection.Open();
            foreach (string commandString in commandStrings)
            {
                if (commandString.Trim() != "")
                {
                    using (var command = new SqlCommand(commandString, Connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            Connection.Close();

        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
        {
            var path = new FileInfo(this.Context.Parameters["assemblypath"]);

            var app_data = path.Directory.FullName + @"\App_Data";

            //MessageBox.Show("Click 'OK' to continue installing database.", "Database Installer");

            var dbPath = app_data + @"\SwipeDesktop.mdf";
            var logPath = app_data + @"\SwipeDesktop_log.ldf";
            var installDb = app_data + @"\Install-Db.sql";

            try
            {
                GrantAccess(dbPath);
                GrantAccess(logPath);
            }
            catch (Exception ex)
            {
                /*no-op */
                MessageBox.Show("Could not grant access to the database: " + ex.Message);
            }
            try
            {

                RunScript(installDb);
            }
            catch (Exception ex)
            {
                 MessageBox.Show("Could not complete the database installation.  Please run Install-Db.bat with an Administrator account: " + ex.Message);
            }


            base.Install(stateSaver);

        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Commit(IDictionary savedState)
        {
           
            base.Commit(savedState);
           
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Uninstall(IDictionary savedState)
        {

            //MessageBox.Show("Click 'OK' to continue un-installing database.", "Database Installer");

            var path = new FileInfo(this.Context.Parameters["assemblypath"]);

            var app_data = path.Directory.FullName + @"\App_Data";

            var unInstallDb = app_data + @"\UnInstall-Db.sql";

            try
            {
                //RunScript(unInstallDb);
            }
            catch { }

            base.Uninstall(savedState);

        }

    }
}
