using System.Collections.Generic;

namespace EasySave.Helpers
{
    public enum Language
    {
        French,
        English
    }

    public class LanguageManager
    {
        private Language _current;

        private readonly Dictionary<string, Dictionary<Language, string>> _texts
            = new Dictionary<string, Dictionary<Language, string>>
        {
            ["menu.title"] = new Dictionary<Language, string>
            {
                [Language.French]  = "=== EasySave — Menu Principal ===",
                [Language.English] = "=== EasySave — Main Menu ==="
            },
            ["menu.list"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Lister les travaux",
                [Language.English] = "List backup jobs"
            },
            ["menu.add"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Ajouter un travail",
                [Language.English] = "Add a backup job"
            },
            ["menu.remove"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Supprimer un travail",
                [Language.English] = "Remove a backup job"
            },
            ["menu.run_one"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Exécuter un travail",
                [Language.English] = "Run one job"
            },
            ["menu.run_all"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Exécuter tous les travaux",
                [Language.English] = "Run all jobs"
            },
            ["menu.language"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Changer la langue",
                [Language.English] = "Change language"
            },
            ["menu.quit"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Quitter",
                [Language.English] = "Quit"
            },
            ["menu.choice"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Votre choix : ",
                [Language.English] = "Your choice: "
            },
            ["job.name"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Nom du travail",
                [Language.English] = "Job name"
            },
            ["job.source"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Dossier source",
                [Language.English] = "Source folder"
            },
            ["job.target"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Dossier cible",
                [Language.English] = "Target folder"
            },
            ["job.type"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Type (complete/differentielle)",
                [Language.English] = "Type (complete/differentielle)"
            },
            ["job.added"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Travail ajouté avec succès",
                [Language.English] = "Job added successfully"
            },
            ["job.removed"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Travail supprimé",
                [Language.English] = "Job removed"
            },
            ["job.none"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Aucun travail configuré.",
                [Language.English] = "No backup jobs configured."
            },
            ["job.select"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Numéro du travail",
                [Language.English] = "Job number"
            },
            ["error.invalid"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Choix invalide, veuillez réessayer.",
                [Language.English] = "Invalid choice, please try again."
            },
            ["error.number"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Veuillez entrer un numéro valide.",
                [Language.English] = "Please enter a valid number."
            },
            ["app.goodbye"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Au revoir !",
                [Language.English] = "Goodbye!"
            },
            ["app.continue"] = new Dictionary<Language, string>
            {
                [Language.French]  = "Appuyez sur une touche pour continuer...",
                [Language.English] = "Press any key to continue..."
            }
        };

        public LanguageManager(Language language = Language.French)
        {
            _current = language;
        }

        public void SetLanguage(Language language)
        {
            _current = language;
        }

        public Language CurrentLanguage => _current;

        public string Get(string key)
        {
            if (_texts.ContainsKey(key) && _texts[key].ContainsKey(_current))
                return _texts[key][_current];

            return $"[{key}]";
        }
    }
}