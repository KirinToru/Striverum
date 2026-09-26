using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Striverum.UI;

namespace Striverum
{
    public class Mod : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name;
        private bool _enabled;
        private bool _isExpanded = true;
        private bool _isGroupHeader;
        private bool _isChild;
        private string _displayName;
        private string _displaySubtext;
        private string _childCountText;
        private string _group;
        private string _filetitle;
        private string _filedescription;

        public string name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(name)); }
        }
        public bool enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(nameof(enabled)); }
        }
        public Dictionary<string, bool> paks { get; set; }
        
        // Cached metadata properties for UI
        public List<string> tags { get; set; }
        public string cat { get; set; }
        public string subcategory { get; set; }
        public Uri caticon { get; set; }
        public DateTime? lastupdate { get; set; }
        public string cachedIconPath { get; set; }
        public Uri homepage { get; set; }
        
        public string group
        {
            get => _group;
            set { _group = value; OnPropertyChanged(nameof(group)); }
        }
        public string filetitle
        {
            get => _filetitle;
            set { _filetitle = value; OnPropertyChanged(nameof(filetitle)); }
        }
        public string filedescription
        {
            get => _filedescription;
            set { _filedescription = value; OnPropertyChanged(nameof(filedescription)); }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool isGroupHeader
        {
            get => _isGroupHeader;
            set { _isGroupHeader = value; OnPropertyChanged(nameof(isGroupHeader)); }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool isChild
        {
            get => _isChild;
            set { _isChild = value; OnPropertyChanged(nameof(isChild)); }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool isExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(isExpanded)); }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public Mod parentGroup { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<Mod> children { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string displayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : name;
            set { _displayName = value; OnPropertyChanged(nameof(displayName)); }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string displaySubtext
        {
            get => _displaySubtext ?? subcategory;
            set { _displaySubtext = value; OnPropertyChanged(nameof(displaySubtext)); }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string childCountText
        {
            get => _childCountText;
            set { _childCountText = value; OnPropertyChanged(nameof(childCountText)); }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public int activeChildCount => children != null ? System.Linq.Enumerable.Count(children, c => c.enabled) : 0;

        [System.Text.Json.Serialization.JsonIgnore]
        public ObservableCollection<ModTag> TagItems { get; set; } = new ObservableCollection<ModTag>();

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
    }
    public class ModTag : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isActive;
        private string _iconPath;
        private FontAwesome5.EFontAwesomeIcon _faIcon;

        public string Name { get; set; }
        public string IconPath
        {
            get => _iconPath;
            set
            {
                _iconPath = value;
                OnPropertyChanged(nameof(IconPath));
                OnPropertyChanged(nameof(HasImage));
            }
        }
        public FontAwesome5.EFontAwesomeIcon FaIcon
        {
            get => _faIcon;
            set
            {
                _faIcon = value;
                OnPropertyChanged(nameof(FaIcon));
            }
        }
        public bool HasImage => !string.IsNullOrEmpty(IconPath);
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
    }
    public class Metadata
    {
        public string name { get; set; }
        public string group { get; set; }
        public string filetitle { get; set; }
        public Uri preview { get; set; }
        public string submitter { get; set; }
        public Uri avi { get; set; }
        public Uri upic { get; set; }
        public Uri caticon { get; set; }
        public string cat { get; set; }
        public string subcategory { get; set; }
        public List<string> tags { get; set; }
        public string description { get; set; }
        public string filedescription { get; set; }
        public Uri homepage { get; set; }
        public DateTime? lastupdate { get; set; }
    }
    public class Config
    {
        public string CurrentGame
        {
            get => "Guilty Gear -Strive-";
            set { }
        }
        public Dictionary<string, GameConfig> Configs { get; set; }
        public double? LeftGridWidth { get; set; }
        public double? RightGridWidth { get; set; }
        public double? TopGridHeight { get; set; }
        public double? BottomGridHeight { get; set; }
        public double? Height { get; set; }
        public double? Width { get; set; }
        public bool Maximized { get; set; }
    }
    public class GameConfig
    {
        public string Launcher { get; set; }
        public string GamePath { get; set; }
        public bool LauncherOption { get; set; }
        public int LauncherOptionIndex { get; set; }
        public bool LauncherOptionConverted { get; set; }
        public bool FirstOpen { get; set; }
        public string ModsFolder { get; set; }
        public string CustomModsFolder { get; set; }
        public string PatchesFolder { get; set; }
        public long? PakLength { get; set; }
        public ObservableCollection<Mod> ModList { get; set; }
        public string CurrentLoadout { get; set; }
        public Dictionary<string, ObservableCollection<Mod>> Loadouts { get; set; }
        public void DisableAllMods(string loadout)
        {
            foreach (var mod in Loadouts[loadout])
            {
                mod.enabled = false;
            }
        }
    }
    public class Choice
    {
        public string OptionText { get; set; }
        public string OptionSubText { get; set; }
        public int Index { get; set; }
        public FontAwesome5.EFontAwesomeIcon FaIcon { get; set; } = FontAwesome5.EFontAwesomeIcon.None;
        public bool HasIcon => FaIcon != FontAwesome5.EFontAwesomeIcon.None;
    }

    public class ModGroupInfo
    {
        public string title { get; set; }
        public List<Mod> members { get; set; } = new List<Mod>();
        public Uri homepage => members?.FirstOrDefault(m => m.homepage != null)?.homepage;
    }
}
