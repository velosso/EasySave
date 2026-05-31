using System;
using System.Collections.Generic;
using EasySave.Helpers;
using EasySave.Models;
using EasySave.Services;
using EasyLog;

namespace EasySave
{
    class Program
    {
        private static LanguageManager _lang;
        private static BackupEngine    _engine;
        private static List<WorkSave>  _jobs;

        static void Main(string[] args)
        {
            try
            {
                RunApplication(args);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n=== FATAL ERROR ===");
                Console.WriteLine(ex.Message);
                Console.ResetColor();

                System.IO.File.AppendAllText("crash.log",
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");

                Console.WriteLine("Error saved in crash.log");
                Console.ReadKey();
            }
        }
        
        private static void RunApplication(string[] args)
        {
            _lang = new LanguageManager(Language.French);

            Ilogger logger = new JsonLogger();
            StateWriter  stateWriter = new StateWriter();
            _engine = new BackupEngine(logger, stateWriter);

            _jobs = ConfigManager.LoadJobs();

            List<int> cmdIndices = CommandLineParser.Parse(args);
            if (cmdIndices.Count > 0)
            {
                Console.WriteLine("EasySave — Command line mode");
                Console.WriteLine(new string('─', 40));
                _engine.ExecuteSelection(_jobs, cmdIndices);
                Console.WriteLine("\nDone. Press any key to exit...");
                Console.ReadKey();
                return;
            }

            RunMenu();
        }
        
        private static void RunMenu()
        {
            bool running = true;

            while (running)
            {
                Console.Clear();
                DisplayMenu();

                string choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1": HandleListJobs();   break;
                    case "2": HandleAddJob();     break;
                    case "3": HandleRemoveJob();  break;
                    case "4": HandleRunOne();     break;
                    case "5": HandleRunAll();     break;
                    case "6": HandleLanguage();   break;
                    case "7": running = false;    break;
                    default:
                        PrintWarning(_lang.Get("error.invalid"));
                        Pause();
                        break;
                }
            }

            ConfigManager.SaveJobs(_jobs);
            Console.WriteLine(_lang.Get("app.goodbye"));
        }

        private static void DisplayMenu()
        {
            Console.WriteLine(_lang.Get("menu.title"));
            Console.WriteLine();
            Console.WriteLine($"  1.  {_lang.Get("menu.list")}  ({_jobs.Count}/5)");
            Console.WriteLine($"  2.  {_lang.Get("menu.add")}");
            Console.WriteLine($"  3.  {_lang.Get("menu.remove")}");
            Console.WriteLine($"  4.  {_lang.Get("menu.run_one")}");
            Console.WriteLine($"  5.  {_lang.Get("menu.run_all")}");
            Console.WriteLine($"  6.  {_lang.Get("menu.language")}");
            Console.WriteLine($"  7.  {_lang.Get("menu.quit")}");
            Console.WriteLine();
            Console.Write(_lang.Get("menu.choice"));
        }
        
        private static void HandleListJobs()
        {
            Console.Clear();
            Console.WriteLine(_lang.Get("menu.title"));
            Console.WriteLine();

            if (_jobs.Count == 0)
            {
                Console.WriteLine($"  {_lang.Get("job.none")}");
                Pause();
                return;
            }

            for (int i = 0; i < _jobs.Count; i++)
            {
                WorkSave job = _jobs[i];
                Console.WriteLine($"  {i + 1}. {job.Name}");
                Console.WriteLine($"       Source : {job.SourcePath}");
                Console.WriteLine($"       Target : {job.TargetPath}");
                Console.WriteLine($"       Type   : {job.BackupType}");
                Console.WriteLine();
            }

            Pause();
        }
        
        private static void HandleAddJob()
        {
            Console.Clear();
            Console.WriteLine(_lang.Get("menu.add"));
            Console.WriteLine();

            try
            {
                Console.Write($"  {_lang.Get("job.name")}     : ");
                string name = Console.ReadLine()?.Trim();

                Console.Write($"  {_lang.Get("job.source")}   : ");
                string source = Console.ReadLine()?.Trim();

                Console.Write($"  {_lang.Get("job.target")}    : ");
                string target = Console.ReadLine()?.Trim();

                Console.Write($"  {_lang.Get("job.type")} : ");
                string type = Console.ReadLine()?.Trim();

                WorkSave newJob = new WorkSave(name, source, target, type);
                ConfigManager.AddJob(_jobs, newJob);

                PrintSuccess($"{_lang.Get("job.added")}: '{name}' ({_jobs.Count}/5)");
            }
            catch (InvalidOperationException ex)
            {
                PrintWarning(ex.Message);
            }
            catch (ArgumentException ex)
            {
                PrintWarning(ex.Message);
            }

            Pause();
        }
        
        private static void HandleRemoveJob()
        {
            Console.Clear();

            if (_jobs.Count == 0)
            {
                Console.WriteLine($"  {_lang.Get("job.none")}");
                Pause();
                return;
            }

            HandleListJobs();

            Console.Write($"  {_lang.Get("job.select")} (1-{_jobs.Count}): ");
            string input = Console.ReadLine()?.Trim();

            if (!int.TryParse(input, out int idx))
            {
                PrintWarning(_lang.Get("error.number"));
                Pause();
                return;
            }

            try
            {
                string name = _jobs[idx - 1].Name;
                ConfigManager.RemoveJob(_jobs, idx - 1);
                PrintSuccess($"{_lang.Get("job.removed")}: '{name}'");
            }
            catch (IndexOutOfRangeException ex)
            {
                PrintWarning(ex.Message);
            }

            Pause();
        }
        
        private static void HandleRunOne()
        {
            Console.Clear();

            if (_jobs.Count == 0)
            {
                Console.WriteLine($"  {_lang.Get("job.none")}");
                Pause();
                return;
            }

            HandleListJobs();

            Console.Write($"  {_lang.Get("job.select")} (1-{_jobs.Count}): ");
            string input = Console.ReadLine()?.Trim();

            if (!int.TryParse(input, out int idx)
                || idx < 1 || idx > _jobs.Count)
            {
                PrintWarning(_lang.Get("error.number"));
                Pause();
                return;
            }

            WorkSave job = _jobs[idx - 1];

            try
            {
                _engine.Execute(job);
            }
            catch (Exception ex)
            {
                PrintWarning(ex.Message);
            }

            Pause();
        }

        private static void HandleRunAll()
        {
            Console.Clear();

            if (_jobs.Count == 0)
            {
                Console.WriteLine($"  {_lang.Get("job.none")}");
                Pause();
                return;
            }

            try
            {
                _engine.ExecuteAll(_jobs);
            }
            catch (Exception ex)
            {
                PrintWarning(ex.Message);
            }

            Pause();
        }

        private static void HandleLanguage()
        {
            Console.Clear();
            Console.WriteLine("  1. Français");
            Console.WriteLine("  2. English");
            Console.WriteLine();
            Console.Write("  Choice / Choix: ");

            string choice = Console.ReadLine()?.Trim();

            if (choice == "2")
            {
                _lang.SetLanguage(Language.English);
                PrintSuccess("Language set to English.");
            }
            else
            {
                _lang.SetLanguage(Language.French);
                PrintSuccess("Langue définie en Français.");
            }

            Pause();
        }
        
        private static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n  ✓ {message}");
            Console.ResetColor();
        }

        private static void PrintWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  ⚠ {message}");
            Console.ResetColor();
        }

        private static void Pause()
        {
            Console.WriteLine();
            Console.Write($"  {_lang.Get("app.continue")}");
            Console.ReadKey();
        }
        
        
        
        
    }
}