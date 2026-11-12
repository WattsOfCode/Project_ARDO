/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `Program.cs` is the application entry point and a concrete bootstrap file for the 
    WinForms app. It contains the static `Main` method.
 */
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

            var playerRepository = new PlayerRepository(databaseConfig);
            var questRepository = new QuestRepository(databaseConfig);

            Application.Run(new StartMenuForm(playerRepository, questRepository));
        }
    }
}
