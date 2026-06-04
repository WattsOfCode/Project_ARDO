/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `AsciiArtLoader.cs` is a concrete utility file that loads ASCII art files 
    for the character and enemy panels. It exposes helper methods with 
    file-system access.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

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
                Debug.WriteLine($"[ARDO][ASCII] Missing art file: {path}");
                return "[Missing ASCII Art]";
            }

            try
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ARDO][ASCII] Failed to read art file '{path}': {ex}");
                return "[ASCII ART LOAD FAILURE]";
            }
        }
    }
}
