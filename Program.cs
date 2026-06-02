using System;
using System.Windows.Forms;
using ARDO.Data.SQLite;
using ARDO.UI.Forms;

namespace ARDO
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            DatabaseConfig databaseConfig = new DatabaseConfig();
            databaseConfig.InitializeDatabase();

            PlayerRepository playerRepository = new PlayerRepository(databaseConfig);

            Application.Run(new StartMenuForm(playerRepository));
        }
    }
}
