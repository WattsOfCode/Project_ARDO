using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ARDO.Interop
{
    public static class AsciiArtLoader
    {
        public static string LoadArt(string category, string fileName)
        {
            string FileName = fileName.Trim();
            if (!FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                FileName += ".txt";
            }

            // 1. Generate the standard path
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Content",
                "ASCII",
                category,
                FileName
            );

            // 2. FALLBACK CHECK: If Capitalized file isn't found, try lowercase!
            if (!File.Exists(path))
            {
                string lowercasePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Content",
                    "ASCII",
                    category,
                    FileName.ToLower() // Try looking for 'warrior.txt' instead of 'Warrior.txt'
                );

                if (File.Exists(lowercasePath))
                {
                    path = lowercasePath; // Use the lowercase version instead!
                }
            }

            // 3. Final verification
            if (!File.Exists(path))
            {
                MessageBox.Show($"ASCII file missing:\n{path}\n\nPlease check that your file name capitalization matches exactly on disk.");
                return "[Missing ASCII Art]";
            }

            try
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "[ASCII ART LOAD FAILURE]";
            }
        }
    }
}