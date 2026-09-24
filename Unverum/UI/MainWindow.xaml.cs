using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Net.Http;
using System.Windows.Media;
using Striverum.UI;
using System.Windows.Controls.Primitives;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Runtime.CompilerServices;

namespace Striverum
{
    public class CategoryItem : INotifyPropertyChanged
    {
        private bool _isActive;
        private int _modCount;
        public string Name { get; set; }
        public string IconPath { get; set; }
        public FontAwesome5.EFontAwesomeIcon FaIcon { get; set; } = FontAwesome5.EFontAwesomeIcon.Solid_Tag;
        public bool HasImage => !string.IsNullOrEmpty(IconPath);
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }
        public int ModCount
        {
            get => _modCount;
            set
            {
                _modCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMods));
                OnPropertyChanged(nameof(DisplayName));
            }
        }
        public bool HasMods => _modCount > 0;
        public string DisplayName => HasMods ? $"{Name} - {ModCount}" : Name;
        public ObservableCollection<CategoryItem> Subcategories { get; set; } = new ObservableCollection<CategoryItem>();
        public bool HasSubcategories => Subcategories != null && Subcategories.Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class SectionItem : INotifyPropertyChanged
    {
        private bool _isActive;
        private int _modCount;
        public string Name { get; set; }
        public string IconPath { get; set; }
        public FontAwesome5.EFontAwesomeIcon FaIcon { get; set; } = FontAwesome5.EFontAwesomeIcon.Solid_Folder;
        public bool HasImage => !string.IsNullOrEmpty(IconPath);
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }
        public int ModCount
        {
            get => _modCount;
            set
            {
                _modCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMods));
                OnPropertyChanged(nameof(DisplayName));
            }
        }
        public bool HasMods => _modCount > 0;
        public string DisplayName => HasMods ? $"{Name} - {ModCount}" : Name;
        public ObservableCollection<CategoryItem> Categories { get; set; } = new ObservableCollection<CategoryItem>();
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class CoverFlowItem : INotifyPropertyChanged
    {
        public int Index { get; set; }
        public string ImageUrl { get; set; }
        public string Title { get; set; }
        public string Caption { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class BrowseSectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private int _modCount;
        public string Name { get; set; }
        public string IconPath { get; set; }
        public FontAwesome5.EFontAwesomeIcon FaIcon { get; set; }
        public bool HasImage => !string.IsNullOrEmpty(IconPath);
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
        public int ModCount
        {
            get => _modCount;
            set
            {
                _modCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMods));
            }
        }
        public bool HasMods => _modCount > 0;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class BrowseCategoryItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private int _modCount;
        public string Name { get; set; }
        public string IconPath { get; set; }
        public FontAwesome5.EFontAwesomeIcon FaIcon { get; set; }
        public bool HasImage => !string.IsNullOrEmpty(IconPath);
        public GameBananaCategory GbCategory { get; set; }
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
        public int ModCount
        {
            get => _modCount;
            set
            {
                _modCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMods));
            }
        }
        public bool HasMods => _modCount > 0;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class GbCategoryRecord
    {
        public int _idRow { get; set; }
        public string _sName { get; set; }
        public string _sIconUrl { get; set; }
        public string _sProfileUrl { get; set; }
        public string _idParentCategoryRow { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Filter state
        public HashSet<string> ActiveSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ActiveCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private SectionItem _currentSection = null;
        private CategoryItem _parentCategory = null;
        private ObservableCollection<SectionItem> _sections = new ObservableCollection<SectionItem>();
        public ObservableCollection<BrowseSectionItem> BrowseSections { get; set; } = new ObservableCollection<BrowseSectionItem>();
        public ObservableCollection<BrowseCategoryItem> BrowseCategoryPills { get; set; } = new ObservableCollection<BrowseCategoryItem>();
        public ObservableCollection<CoverFlowItem> GalleryItems { get; set; } = new ObservableCollection<CoverFlowItem>();

        public static bool CategoryMatches(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            string normA = a.Replace("'", "").Replace("-", "").Replace("♯", "#").Replace("?", "").Trim();
            string normB = b.Replace("'", "").Replace("-", "").Replace("♯", "#").Replace("?", "").Trim();
            return string.Equals(normA, normB, StringComparison.OrdinalIgnoreCase);
        }

        private string GetCategoryIconPath(string iconUrl)
        {
            if (string.IsNullOrEmpty(iconUrl)) return null;
            if (iconUrl.StartsWith("pack://") || iconUrl.StartsWith("/")) return iconUrl;
            try
            {
                string fileName = Path.GetFileName(new Uri(iconUrl).LocalPath);
                string localPath = $@"{Global.assemblyLocation}{Global.s}Cache{Global.s}Icons{Global.s}{fileName}";
                if (File.Exists(localPath)) return localPath;
            }
            catch { }
            return iconUrl;
        }

        private static FontAwesome5.EFontAwesomeIcon GetSectionFaIcon(string section)
        {
            if (string.IsNullOrEmpty(section)) return FontAwesome5.EFontAwesomeIcon.Solid_Folder;
            var s = section.ToLowerInvariant();
            if (s.Contains("sound") || s.Contains("audio") || s.Contains("voice") || s.Contains("music")) return FontAwesome5.EFontAwesomeIcon.Solid_VolumeUp;
            if (s.Contains("wip") || s.Contains("work in progress")) return FontAwesome5.EFontAwesomeIcon.Solid_Wrench;
            if (s.Contains("skin") || s.Contains("char") || s.Contains("costume")) return FontAwesome5.EFontAwesomeIcon.Solid_Tshirt;
            if (s.Contains("misc") || s.Contains("other")) return FontAwesome5.EFontAwesomeIcon.Solid_QuestionCircle;
            if (s.Contains("gui") || s.Contains("ui") || s.Contains("hud")) return FontAwesome5.EFontAwesomeIcon.Solid_Palette;
            if (s.Contains("gameplay") || s.Contains("move") || s.Contains("script")) return FontAwesome5.EFontAwesomeIcon.Solid_Cog;
            if (s.Contains("stage")) return FontAwesome5.EFontAwesomeIcon.Solid_Tree;
            return FontAwesome5.EFontAwesomeIcon.Solid_Folder;
        }

        private static FontAwesome5.EFontAwesomeIcon GetCategoryFaIcon(string category)
        {
            if (string.IsNullOrEmpty(category)) return FontAwesome5.EFontAwesomeIcon.Solid_Cog;
            var c = category.ToLowerInvariant();
            if (c.Contains("translat")) return FontAwesome5.EFontAwesomeIcon.Solid_Language;
            if (c.Contains("hud") || c.Contains("portrait") || c.Contains("font") || c.Contains("select")) return FontAwesome5.EFontAwesomeIcon.Solid_Palette;
            if (c.Contains("stage")) return FontAwesome5.EFontAwesomeIcon.Solid_Mountain;
            return FontAwesome5.EFontAwesomeIcon.Solid_Cog;
        }

        private void ResolveModTagIcon(ModTag tagItem, Mod mod)
        {
            if (tagItem == null) return;

            // 1. Check if tag matches a section in _sections (e.g. Skins, Other/Misc, GUIs, Gameplay, Stages)
            var matchingSec = _sections.FirstOrDefault(s => CategoryMatches(s.Name, tagItem.Name));
            if (matchingSec != null)
            {
                tagItem.IconPath = matchingSec.IconPath;
                tagItem.FaIcon = matchingSec.FaIcon;
                return;
            }

            // 2. Check if tag matches a category in any section (e.g. character name, Translation)
            foreach (var sec in _sections)
            {
                var matchingCat = sec.Categories.FirstOrDefault(c => CategoryMatches(c.Name, tagItem.Name));
                if (matchingCat != null && !string.IsNullOrEmpty(matchingCat.IconPath))
                {
                    tagItem.IconPath = matchingCat.IconPath;
                    tagItem.FaIcon = matchingCat.FaIcon;
                    return;
                }
                foreach (var cat in sec.Categories)
                {
                    var matchingSub = cat.Subcategories?.FirstOrDefault(s => CategoryMatches(s.Name, tagItem.Name));
                    if (matchingSub != null && !string.IsNullOrEmpty(matchingSub.IconPath))
                    {
                        tagItem.IconPath = matchingSub.IconPath;
                        tagItem.FaIcon = matchingSub.FaIcon;
                        return;
                    }
                }
            }

            // 3. Fallback to mod's own cached icon if tag matches mod subcategory
            if (mod != null && !string.IsNullOrEmpty(mod.subcategory) && CategoryMatches(tagItem.Name, mod.subcategory))
            {
                tagItem.IconPath = mod.cachedIconPath ?? mod.caticon?.ToString();
                tagItem.FaIcon = GetCategoryFaIcon(tagItem.Name);
                return;
            }

            // 4. Default fallback
            tagItem.FaIcon = GetCategoryFaIcon(tagItem.Name);
        }

        private void InitDefaultSections()
        {
            if (_sections.Count > 0) return;

            // 1. Skins (5 main GameBanana mod categories for Guilty Gear -Strive-)
            var skins = new SectionItem
            {
                Name = "Skins",
                IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/60ce8d5f438ee.png"),
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Tshirt
            };
            var striveSkinCats = new (string Name, string Icon)[]
            {
                ("Robo-Ky", "https://images.gamebanana.com/img/ico/ModCategory/6a4661f5c42fb.png"),
                ("Jam Kuradoberi", "https://images.gamebanana.com/img/ico/ModCategory/69c02cde0f13e.png"),
                ("Lucy", "https://images.gamebanana.com/img/ico/ModCategory/6892a26b62af1.png"),
                ("Unika", "https://images.gamebanana.com/img/ico/ModCategory/683484e320e85.png"),
                ("Venom", "https://images.gamebanana.com/img/ico/ModCategory/67e9e9dd5b1a7.png"),
                ("Queen Dizzy", "https://images.gamebanana.com/img/ico/ModCategory/671a967d8aaf0.png"),
                ("Slayer", "https://images.gamebanana.com/img/ico/ModCategory/665696e58f26d.png"),
                ("A.B.A", "https://images.gamebanana.com/img/ico/ModCategory/6601bfa7852a8.png"),
                ("Elphelt Valentine", "https://images.gamebanana.com/img/ico/ModCategory/65729b7692a78.png"),
                ("Johnny", "https://images.gamebanana.com/img/ico/ModCategory/64e7e423a999d.png"),
                ("Bedman?", "https://images.gamebanana.com/img/ico/ModCategory/64e7e51206831.png"),
                ("Sin Kiske", "https://images.gamebanana.com/img/ico/ModCategory/637f1e29eaf46.png"),
                ("Bridget", "https://images.gamebanana.com/img/ico/ModCategory/6572b2b833fae.png"),
                ("Testament", "https://images.gamebanana.com/img/ico/ModCategory/623cf5a74ab8d.png"),
                ("Baiken", "https://images.gamebanana.com/img/ico/ModCategory/61f3aacebcb3b.png"),
                ("Potemkin", "https://images.gamebanana.com/img/ico/ModCategory/60cfc7cf41135.png"),
                ("Leo Whitefang", "https://images.gamebanana.com/img/ico/ModCategory/60cfca12de46b.png"),
                ("Zato-1", "https://images.gamebanana.com/img/ico/ModCategory/60cfcab6b9bd5.png"),
                ("Anji Mito", "https://images.gamebanana.com/img/ico/ModCategory/60cfc93fe449f.png"),
                ("Giovanna", "https://images.gamebanana.com/img/ico/ModCategory/60cfc9800be32.png"),
                ("I-No", "https://images.gamebanana.com/img/ico/ModCategory/60cfc9cb2b71e.png"),
                ("Millia Rage", "https://images.gamebanana.com/img/ico/ModCategory/60cfca5d301a5.png"),
                ("Jack'O", "https://images.gamebanana.com/img/ico/ModCategory/613257b01099d.png"),
                ("Goldlewis Dickinson", "https://images.gamebanana.com/img/ico/ModCategory/61325e24e2c3c.png"),
                ("Happy Chaos", "https://images.gamebanana.com/img/ico/ModCategory/61e334a4a8e69.png"),
                ("Asuka R♯", "https://images.gamebanana.com/img/ico/ModCategory/657ba66ddb03d.png"),
                ("Nagoriyuki", "https://images.gamebanana.com/img/ico/ModCategory/61325d241b84c.png"),
                ("Axl Low", "https://images.gamebanana.com/img/ico/ModCategory/61325db7a5368.png"),
                ("Chipp Zanuff", "https://images.gamebanana.com/img/ico/ModCategory/61325ccb7e398.png"),
                ("Faust", "https://images.gamebanana.com/img/ico/ModCategory/613258ab7293f.png"),
                ("Sol Badguy", "https://images.gamebanana.com/img/ico/ModCategory/613256a677273.png"),
                ("Ky Kiske", "https://images.gamebanana.com/img/ico/ModCategory/61325a8fe708f.png"),
                ("Ramlethal Valentine", "https://images.gamebanana.com/img/ico/ModCategory/60ce909a99027.png"),
                ("Several Characters", "https://images.gamebanana.com/img/ico/ModCategory/631633f5138f8.png"),
                ("May", "https://images.gamebanana.com/img/ico/ModCategory/60ce9048b155e.png")
            };
            foreach (var item in striveSkinCats)
            {
                skins.Categories.Add(new CategoryItem
                {
                    Name = item.Name,
                    IconPath = GetCategoryIconPath(item.Icon),
                    FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_User
                });
            }
            _sections.Add(skins);

            // 2. Other/Misc
            var misc = new SectionItem
            {
                Name = "Other/Misc",
                IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/62829c5f9e5f8.png"),
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_QuestionCircle
            };
            misc.Categories.Add(new CategoryItem
            {
                Name = "Translation",
                IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/667490b249fb9.png"),
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Language
            });
            _sections.Add(misc);

            // 3. GUIs
            var guis = new SectionItem
            {
                Name = "GUIs",
                IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/6101d57ac2be9.png"),
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Palette
            };
            _sections.Add(guis);

            // 4. Gameplay
            var gameplay = new SectionItem
            {
                Name = "Gameplay",
                IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/616169f346a22.png"),
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Cog
            };
            _sections.Add(gameplay);

            // 5. Stages
            var stages = new SectionItem
            {
                Name = "Stages",
                IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/6168e12ead8c9.png"),
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Tree
            };
            _sections.Add(stages);

            // 6. Sounds
            var sounds = new SectionItem
            {
                Name = "Sounds",
                IconPath = GetCategoryIconPath("pack://application:,,,/Assets/Icons/sounds.png"),
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_VolumeUp
            };

            var charVoiceCat = new CategoryItem
            {
                Name = "Character Voice",
                IconPath = "",
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Cog
            };
            var charVoiceSubs = new (string Name, string Icon)[]
            {
                ("Ky Kiske", "https://images.gamebanana.com/img/ico/SoundCategory/6a67a57d415c9.png"),
                ("Robo-Ky", ""),
                ("Jam Kuradoberi", ""),
                ("Lucy", ""),
                ("Unika", ""),
                ("May", ""),
                ("Happy Chaos", ""),
                ("Dizzy", ""),
                ("Baiken", ""),
                ("Slayer", ""),
                ("Chipp", ""),
                ("Elphelt", ""),
                ("Potemkin", ""),
                ("Asuka R#", ""),
                ("Faust", ""),
                ("Axl", ""),
                ("Bridget", ""),
                ("Jack", ""),
                ("Nagoriyuki", ""),
                ("I-No", "")
            };
            foreach (var cv in charVoiceSubs)
            {
                charVoiceCat.Subcategories.Add(new CategoryItem
                {
                    Name = cv.Name,
                    IconPath = string.IsNullOrEmpty(cv.Icon) ? "" : GetCategoryIconPath(cv.Icon),
                    FaIcon = string.IsNullOrEmpty(cv.Icon) ? FontAwesome5.EFontAwesomeIcon.Solid_Cog : FontAwesome5.EFontAwesomeIcon.Solid_User
                });
            }
            sounds.Categories.Add(charVoiceCat);

            var soundOtherCats = new string[]
            {
                "BGM",
                "Sound Effects",
                "Announcer",
                "Counter",
                "Wall Break",
                "Round Intro",
                "Heavy Mob Cemetery",
                "Other/Misc"
            };
            foreach (var name in soundOtherCats)
            {
                sounds.Categories.Add(new CategoryItem
                {
                    Name = name,
                    IconPath = "",
                    FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Cog
                });
            }
            _sections.Add(sounds);

            // 7. WiPs
            var wips = new SectionItem
            {
                Name = "WiPs",
                IconPath = GetCategoryIconPath("pack://application:,,,/Assets/Icons/wips.png"),
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Wrench
            };
            var wipSubCats = new string[] { "Audio", "Skins", "Other/Misc" };
            foreach (var name in wipSubCats)
            {
                wips.Categories.Add(new CategoryItem
                {
                    Name = name,
                    IconPath = "",
                    FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Cog
                });
            }
            _sections.Add(wips);

            // Kick off background GameBanana category synchronization
            _ = Task.Run(SyncCategoriesWithGameBananaAsync);
        }

        private async Task SyncCategoriesWithGameBananaAsync()
        {
            try
            {
                string cacheDir = $@"{Global.assemblyLocation}{Global.s}Cache";
                string iconsDir = $@"{cacheDir}{Global.s}Icons";
                Directory.CreateDirectory(iconsDir);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Striverum");

                async Task<List<GbCategoryRecord>> FetchCategoryEndpoint(string endpoint)
                {
                    var list = new List<GbCategoryRecord>();
                    int page = 1;
                    while (true)
                    {
                        string url = $"https://gamebanana.com/apiv4/{endpoint}/ByGame?_aGameRowIds[]=11534&_sRecordSchema=Custom&_csvProperties=_idRow,_sName,_sProfileUrl,_sIconUrl,_idParentCategoryRow&_nPerpage=50&_nPage={page}";
                        var response = await client.GetAsync(url);
                        if (!response.IsSuccessStatusCode) break;
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.ValueKind != JsonValueKind.Array) break;
                        int countThisPage = 0;
                        foreach (var el in doc.RootElement.EnumerateArray())
                        {
                            countThisPage++;
                            var rec = new GbCategoryRecord();
                            if (el.TryGetProperty("_idRow", out var idProp))
                            {
                                if (idProp.ValueKind == JsonValueKind.Number) rec._idRow = idProp.GetInt32();
                                else if (int.TryParse(idProp.GetString(), out var id)) rec._idRow = id;
                            }
                            if (el.TryGetProperty("_sName", out var nameProp))
                                rec._sName = nameProp.GetString();
                            if (el.TryGetProperty("_sIconUrl", out var iconProp))
                                rec._sIconUrl = iconProp.GetString();
                            if (el.TryGetProperty("_sProfileUrl", out var profProp))
                                rec._sProfileUrl = profProp.GetString();
                            if (el.TryGetProperty("_idParentCategoryRow", out var parProp))
                            {
                                if (parProp.ValueKind == JsonValueKind.Number)
                                    rec._idParentCategoryRow = parProp.GetInt32().ToString();
                                else if (parProp.ValueKind == JsonValueKind.String)
                                    rec._idParentCategoryRow = parProp.GetString();
                            }
                            list.Add(rec);
                        }
                        if (countThisPage < 50) break;
                        page++;
                    }
                    return list;
                }

                var modItems = await FetchCategoryEndpoint("ModCategory");
                var soundItems = await FetchCategoryEndpoint("SoundCategory");
                var wipItems = await FetchCategoryEndpoint("WipCategory");

                var totalList = new List<GbCategoryRecord>();
                totalList.AddRange(modItems);
                totalList.AddRange(soundItems);
                totalList.AddRange(wipItems);

                if (totalList.Count > 0)
                {
                    // Cache to disk
                    string cacheFile = $@"{cacheDir}{Global.s}Categories_GGS.json";
                    File.WriteAllText(cacheFile, JsonSerializer.Serialize(totalList));

                    // Download missing icons in background
                    foreach (var item in totalList)
                    {
                        if (!string.IsNullOrEmpty(item._sIconUrl))
                        {
                            try
                            {
                                string iconFileName = Path.GetFileName(new Uri(item._sIconUrl).LocalPath);
                                string localPath = $@"{iconsDir}{Global.s}{iconFileName}";
                                if (!File.Exists(localPath))
                                {
                                    var bytes = await client.GetByteArrayAsync(item._sIconUrl);
                                    await File.WriteAllBytesAsync(localPath, bytes);
                                }
                            }
                            catch { }
                        }
                    }

                    // Apply to UI
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        ApplyGbCategories(modItems, soundItems, wipItems);
                        UpdateModCounts();
                    });
                }
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"GameBanana category sync: {ex.Message}", LoggerType.Warning);
            }
        }

        private void ApplyGbCategories(List<GbCategoryRecord> allItems, List<GbCategoryRecord> soundItems = null, List<GbCategoryRecord> wipItems = null)
        {
            var roots = allItems.Where(x => x._idParentCategoryRow == null || x._idParentCategoryRow.ToString() == "0" || string.IsNullOrEmpty(x._idParentCategoryRow.ToString())).ToList();

            foreach (var r in roots)
            {
                var sec = _sections.FirstOrDefault(s => CategoryMatches(s.Name, r._sName));
                if (sec == null)
                {
                    sec = new SectionItem
                    {
                        Name = r._sName,
                        IconPath = GetCategoryIconPath(r._sIconUrl),
                        FaIcon = GetSectionFaIcon(r._sName)
                    };
                    _sections.Add(sec);
                }
                else
                {
                    sec.IconPath = GetCategoryIconPath(r._sIconUrl);
                }

                var children = allItems.Where(x => x._idParentCategoryRow != null && x._idParentCategoryRow.ToString() == r._idRow.ToString()).ToList();
                foreach (var child in children)
                {
                    var cat = sec.Categories.FirstOrDefault(c => CategoryMatches(c.Name, child._sName));
                    if (cat == null)
                    {
                        sec.Categories.Add(new CategoryItem
                        {
                            Name = child._sName,
                            IconPath = GetCategoryIconPath(child._sIconUrl),
                            FaIcon = GetCategoryFaIcon(child._sName)
                        });
                    }
                    else
                    {
                        cat.IconPath = GetCategoryIconPath(child._sIconUrl);
                    }
                }
            }

            // Sync Sounds
            if (soundItems != null && soundItems.Count > 0)
            {
                var soundSec = _sections.FirstOrDefault(s => CategoryMatches(s.Name, "Sounds"));
                if (soundSec == null)
                {
                    soundSec = new SectionItem
                    {
                        Name = "Sounds",
                        IconPath = GetCategoryIconPath("pack://application:,,,/Assets/Icons/sounds.png"),
                        FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_VolumeUp
                    };
                    _sections.Add(soundSec);
                }
                else
                {
                    soundSec.IconPath = GetCategoryIconPath("pack://application:,,,/Assets/Icons/sounds.png");
                }

                var charVoiceCat = soundSec.Categories.FirstOrDefault(c => CategoryMatches(c.Name, "Character Voice"));
                if (charVoiceCat == null)
                {
                    charVoiceCat = new CategoryItem
                    {
                        Name = "Character Voice",
                        IconPath = "",
                        FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Cog
                    };
                    soundSec.Categories.Insert(0, charVoiceCat);
                }

                foreach (var sItem in soundItems)
                {
                    bool isSubOfVoice = sItem._idParentCategoryRow == "4433";
                    if (isSubOfVoice)
                    {
                        var existingSub = charVoiceCat.Subcategories.FirstOrDefault(c => CategoryMatches(c.Name, sItem._sName));
                        if (existingSub == null)
                        {
                            charVoiceCat.Subcategories.Add(new CategoryItem
                            {
                                Name = sItem._sName,
                                IconPath = GetCategoryIconPath(sItem._sIconUrl),
                                FaIcon = string.IsNullOrEmpty(sItem._sIconUrl) ? FontAwesome5.EFontAwesomeIcon.Solid_Cog : FontAwesome5.EFontAwesomeIcon.Solid_User
                            });
                        }
                        else if (!string.IsNullOrEmpty(sItem._sIconUrl))
                        {
                            existingSub.IconPath = GetCategoryIconPath(sItem._sIconUrl);
                            existingSub.FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_User;
                        }
                    }
                    else
                    {
                        var existing = soundSec.Categories.FirstOrDefault(c => CategoryMatches(c.Name, sItem._sName));
                        if (existing == null)
                        {
                            soundSec.Categories.Add(new CategoryItem
                            {
                                Name = sItem._sName,
                                IconPath = GetCategoryIconPath(sItem._sIconUrl),
                                FaIcon = string.IsNullOrEmpty(sItem._sIconUrl) ? FontAwesome5.EFontAwesomeIcon.Solid_Cog : FontAwesome5.EFontAwesomeIcon.Solid_Music
                            });
                        }
                        else if (!string.IsNullOrEmpty(sItem._sIconUrl))
                        {
                            existing.IconPath = GetCategoryIconPath(sItem._sIconUrl);
                        }
                    }
                }
            }

            // Sync WiPs
            if (wipItems != null && wipItems.Count > 0)
            {
                var wipSec = _sections.FirstOrDefault(s => CategoryMatches(s.Name, "WiPs"));
                if (wipSec == null)
                {
                    wipSec = new SectionItem
                    {
                        Name = "WiPs",
                        IconPath = GetCategoryIconPath("pack://application:,,,/Assets/Icons/wips.png"),
                        FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Wrench
                    };
                    _sections.Add(wipSec);
                }
                else
                {
                    wipSec.IconPath = GetCategoryIconPath("pack://application:,,,/Assets/Icons/wips.png");
                }

                foreach (var wItem in wipItems)
                {
                    var existing = wipSec.Categories.FirstOrDefault(c => CategoryMatches(c.Name, wItem._sName));
                    if (existing == null)
                    {
                        wipSec.Categories.Add(new CategoryItem
                        {
                            Name = wItem._sName,
                            IconPath = GetCategoryIconPath(wItem._sIconUrl),
                            FaIcon = string.IsNullOrEmpty(wItem._sIconUrl) ? FontAwesome5.EFontAwesomeIcon.Solid_Cog : FontAwesome5.EFontAwesomeIcon.Solid_Cogs
                        });
                    }
                    else if (!string.IsNullOrEmpty(wItem._sIconUrl))
                    {
                        existing.IconPath = GetCategoryIconPath(wItem._sIconUrl);
                    }
                }
            }

            if (Global.ModList != null)
            {
                foreach (var mod in Global.ModList)
                {
                    if (mod.TagItems != null)
                    {
                        foreach (var tagItem in mod.TagItems)
                        {
                            ResolveModTagIcon(tagItem, mod);
                        }
                    }
                }
            }
        }

        public void UpdateModCounts()
        {
            if (Global.ModList == null || _sections == null) return;

            foreach (var sec in _sections)
            {
                // Update categories count
                foreach (var cat in sec.Categories)
                {
                    if (cat.HasSubcategories)
                    {
                        foreach (var sub in cat.Subcategories)
                        {
                            sub.ModCount = Global.ModList.Count(m =>
                                (!string.IsNullOrEmpty(m.subcategory) && CategoryMatches(m.subcategory, sub.Name)) ||
                                (m.tags != null && m.tags.Any(t => CategoryMatches(t, sub.Name)))
                            );
                        }
                        cat.ModCount = Global.ModList.Count(m =>
                            (!string.IsNullOrEmpty(m.subcategory) && (CategoryMatches(m.subcategory, cat.Name) || cat.Subcategories.Any(sub => CategoryMatches(sub.Name, m.subcategory)))) ||
                            (m.tags != null && m.tags.Any(t => CategoryMatches(t, cat.Name) || cat.Subcategories.Any(sub => CategoryMatches(sub.Name, t))))
                        );
                    }
                    else
                    {
                        cat.ModCount = Global.ModList.Count(m =>
                            (!string.IsNullOrEmpty(m.subcategory) && CategoryMatches(m.subcategory, cat.Name)) ||
                            (m.tags != null && m.tags.Any(t => CategoryMatches(t, cat.Name)))
                        );
                    }
                }

                // Update section count (directly or through contained categories and subcategories)
                sec.ModCount = Global.ModList.Count(m =>
                    (!string.IsNullOrEmpty(m.cat) && CategoryMatches(m.cat, sec.Name)) ||
                    (m.tags != null && m.tags.Any(t => CategoryMatches(t, sec.Name))) ||
                    sec.Categories.Any(c =>
                        (!string.IsNullOrEmpty(m.subcategory) && (CategoryMatches(m.subcategory, c.Name) || (c.HasSubcategories && c.Subcategories.Any(sub => CategoryMatches(sub.Name, m.subcategory))))) ||
                        (m.tags != null && m.tags.Any(t => CategoryMatches(t, c.Name) || (c.HasSubcategories && c.Subcategories.Any(sub => CategoryMatches(sub.Name, t)))))
                    )
                );
            }

            UpdateBrowseSectionCounts();
            UpdateBrowseCategoryPillCounts();
        }

        private bool ModFilter(object item)
        {
            Mod mod = item as Mod;
            if (mod == null) return false;

            // Nothing selected = show all
            if (ActiveSections.Count == 0 && ActiveCategories.Count == 0)
                return true;

            // 1. If categories are selected, check if mod matches any active category
            if (ActiveCategories.Count > 0)
            {
                if (!string.IsNullOrEmpty(mod.subcategory) && ActiveCategories.Any(ac => CategoryMatches(ac, mod.subcategory)))
                    return true;
                if (mod.tags != null && mod.tags.Any(t => ActiveCategories.Any(ac => CategoryMatches(ac, t))))
                    return true;
            }

            // 2. If sections are selected, check if mod matches an active section with no specific categories active
            if (ActiveSections.Count > 0)
            {
                var sectionsWithActiveCats = _sections
                    .Where(s => s.Categories.Any(c => ActiveCategories.Any(ac => CategoryMatches(ac, c.Name))))
                    .Select(s => s.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                string modSection = mod.cat;
                if (string.IsNullOrEmpty(modSection) && mod.tags != null)
                {
                    modSection = _sections.FirstOrDefault(s => mod.tags.Any(t => CategoryMatches(t, s.Name)))?.Name;
                }

                if (!string.IsNullOrEmpty(modSection) && ActiveSections.Any(asSec => CategoryMatches(asSec, modSection)))
                {
                    if (!sectionsWithActiveCats.Contains(modSection))
                        return true;
                }
            }

            return false;
        }

        private void TagBubble_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var tagName = btn?.Tag?.ToString();
            if (string.IsNullOrEmpty(tagName)) return;

            InitDefaultSections();

            bool isActiveCategory = ActiveCategories.Any(c => CategoryMatches(c, tagName));
            bool isActiveSection = ActiveSections.Any(s => CategoryMatches(s, tagName));
            bool isCurrentlyActive = isActiveCategory || isActiveSection;

            if (isCurrentlyActive)
            {
                // Toggle OFF
                if (isActiveCategory)
                {
                    ActiveCategories.RemoveWhere(c => CategoryMatches(c, tagName));
                    foreach (var s in _sections)
                    {
                        foreach (var c in s.Categories.Where(cat => CategoryMatches(cat.Name, tagName)))
                        {
                            c.IsActive = false;
                        }
                    }

                    // Check if parent section has any remaining active categories
                    var parentSec = _sections.FirstOrDefault(s => s.Categories.Any(c => CategoryMatches(c.Name, tagName)));
                    if (parentSec != null && !parentSec.Categories.Any(c => c.IsActive))
                    {
                        parentSec.IsActive = false;
                        ActiveSections.RemoveWhere(s => CategoryMatches(s, parentSec.Name));
                    }
                }

                if (isActiveSection)
                {
                    ActiveSections.RemoveWhere(s => CategoryMatches(s, tagName));
                    var sec = _sections.FirstOrDefault(s => CategoryMatches(s.Name, tagName));
                    if (sec != null)
                    {
                        sec.IsActive = false;
                        foreach (var c in sec.Categories)
                        {
                            c.IsActive = false;
                            ActiveCategories.RemoveWhere(ac => CategoryMatches(ac, c.Name));
                        }
                    }
                }
            }
            else
            {
                // Toggle ON
                var parentSection = _sections.FirstOrDefault(s => s.Categories.Any(c => CategoryMatches(c.Name, tagName)));
                if (parentSection != null)
                {
                    var cat = parentSection.Categories.FirstOrDefault(c => CategoryMatches(c.Name, tagName));
                    if (cat != null) cat.IsActive = true;
                    parentSection.IsActive = true;
                    ActiveCategories.Add(cat != null ? cat.Name : tagName);
                    ActiveSections.Add(parentSection.Name);

                    // Switch sidebar to category view for this section
                    if (CategoryPanel.Visibility != Visibility.Visible || _currentSection != parentSection)
                    {
                        AnimateToCategories(parentSection);
                    }
                }
                else
                {
                    // Tag is a section name
                    var sec = _sections.FirstOrDefault(s => CategoryMatches(s.Name, tagName));
                    if (sec != null)
                    {
                        sec.IsActive = true;
                        ActiveSections.Add(sec.Name);
                        if (sec.Categories.Count > 0 && (CategoryPanel.Visibility != Visibility.Visible || _currentSection != sec))
                        {
                            AnimateToCategories(sec);
                        }
                    }
                    else
                    {
                        ActiveCategories.Add(tagName);
                    }
                }
            }

            UpdateModTagActiveStates();
            RefreshModList();
        }

        private void SectionSidebar_ItemClicked(object sender, MouseButtonEventArgs e)
        {
            var item = (e.OriginalSource as FrameworkElement)?.DataContext as SectionItem;
            if (item == null)
            {
                item = SectionSidebar.SelectedItem as SectionItem;
                if (item == null) return;
            }

            // User requirement:
            // "Jesli sekcja juz jest aktywna to jak nacisniemy jeszcze raz to to poprostu wylaczy ten tag sekcji z filtru"
            if (item.IsActive)
            {
                item.IsActive = false;
                ActiveSections.Remove(item.Name);
                foreach (var c in item.Categories)
                {
                    c.IsActive = false;
                    ActiveCategories.Remove(c.Name);
                }
                UpdateModTagActiveStates();
                RefreshModList();
            }
            else
            {
                item.IsActive = true;
                ActiveSections.Add(item.Name);
                _currentSection = item;
                if (item.Categories.Count > 0)
                {
                    AnimateToCategories(item);
                }
                UpdateModTagActiveStates();
                RefreshModList();
            }
        }

        private void CategorySidebar_ItemClicked(object sender, MouseButtonEventArgs e)
        {
            var cat = (e.OriginalSource as FrameworkElement)?.DataContext as CategoryItem;
            if (cat == null)
            {
                cat = CategorySidebar.SelectedItem as CategoryItem;
                if (cat == null) return;
            }

            if (cat.HasSubcategories)
            {
                // Drill down into subcategories (e.g. Character Voice -> characters)
                _parentCategory = cat;
                CategoryHeader.Text = cat.Name;
                if (cat.HasImage && !string.IsNullOrEmpty(cat.IconPath))
                {
                    try
                    {
                        CategoryHeaderImage.Source = new BitmapImage(new Uri(cat.IconPath, UriKind.RelativeOrAbsolute));
                        CategoryHeaderImage.Visibility = Visibility.Visible;
                        CategoryHeaderIcon.Visibility = Visibility.Collapsed;
                    }
                    catch
                    {
                        CategoryHeaderIcon.Icon = cat.FaIcon;
                        CategoryHeaderIcon.Visibility = Visibility.Visible;
                        CategoryHeaderImage.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    CategoryHeaderIcon.Icon = cat.FaIcon;
                    CategoryHeaderIcon.Visibility = Visibility.Visible;
                    CategoryHeaderImage.Visibility = Visibility.Collapsed;
                }
                CategorySidebar.ItemsSource = cat.Subcategories;
                return;
            }

            cat.IsActive = !cat.IsActive;
            if (cat.IsActive)
            {
                ActiveCategories.Add(cat.Name);
                if (_parentCategory != null)
                {
                    ActiveCategories.Add(_parentCategory.Name);
                }
                if (_currentSection != null)
                {
                    _currentSection.IsActive = true;
                    ActiveSections.Add(_currentSection.Name);
                }
            }
            else
            {
                ActiveCategories.Remove(cat.Name);
            }

            UpdateModTagActiveStates();
            RefreshModList();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parentCategory != null && _currentSection != null)
            {
                // Return from subcategories back to section's main categories
                _parentCategory = null;
                CategoryHeader.Text = _currentSection.Name;
                if (_currentSection.HasImage && !string.IsNullOrEmpty(_currentSection.IconPath))
                {
                    try
                    {
                        CategoryHeaderImage.Source = new BitmapImage(new Uri(_currentSection.IconPath, UriKind.RelativeOrAbsolute));
                        CategoryHeaderImage.Visibility = Visibility.Visible;
                        CategoryHeaderIcon.Visibility = Visibility.Collapsed;
                    }
                    catch
                    {
                        CategoryHeaderIcon.Icon = _currentSection.FaIcon;
                        CategoryHeaderIcon.Visibility = Visibility.Visible;
                        CategoryHeaderImage.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    CategoryHeaderIcon.Icon = _currentSection.FaIcon;
                    CategoryHeaderIcon.Visibility = Visibility.Visible;
                    CategoryHeaderImage.Visibility = Visibility.Collapsed;
                }
                CategorySidebar.ItemsSource = _currentSection.Categories;
            }
            else
            {
                _parentCategory = null;
                AnimateToSections();
            }
        }

        private void AnimateToCategories(SectionItem section)
        {
            if (section == null) return;
            _parentCategory = null;
            _currentSection = section;
            CategoryHeader.Text = section.Name;
            if (section.HasImage && !string.IsNullOrEmpty(section.IconPath))
            {
                try
                {
                    CategoryHeaderImage.Source = new BitmapImage(new Uri(section.IconPath, UriKind.RelativeOrAbsolute));
                    CategoryHeaderImage.Visibility = Visibility.Visible;
                    CategoryHeaderIcon.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    CategoryHeaderIcon.Icon = section.FaIcon;
                    CategoryHeaderIcon.Visibility = Visibility.Visible;
                    CategoryHeaderImage.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                CategoryHeaderIcon.Icon = section.FaIcon;
                CategoryHeaderIcon.Visibility = Visibility.Visible;
                CategoryHeaderImage.Visibility = Visibility.Collapsed;
            }
            CategorySidebar.ItemsSource = section.Categories;

            CategoryPanel.Visibility = Visibility.Visible;
            var duration = TimeSpan.FromMilliseconds(200);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            var widthAnim = new DoubleAnimation(64, 190, duration) { EasingFunction = ease };
            SidebarContainer.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);

            var secOpacity = new DoubleAnimation(1, 0, duration) { EasingFunction = ease };
            var secTranslate = new DoubleAnimation(0, -30, duration) { EasingFunction = ease };
            secOpacity.Completed += (s, e) => SectionPanel.Visibility = Visibility.Collapsed;
            SectionPanel.BeginAnimation(UIElement.OpacityProperty, secOpacity);
            SectionPanelTranslate.BeginAnimation(TranslateTransform.XProperty, secTranslate);

            CategoryPanel.Opacity = 0;
            CategoryPanelTranslate.X = 30;
            var catOpacity = new DoubleAnimation(0, 1, duration) { EasingFunction = ease };
            var catTranslate = new DoubleAnimation(30, 0, duration) { EasingFunction = ease };
            CategoryPanel.BeginAnimation(UIElement.OpacityProperty, catOpacity);
            CategoryPanelTranslate.BeginAnimation(TranslateTransform.XProperty, catTranslate);
        }

        private void AnimateToSections()
        {
            _parentCategory = null;
            SectionPanel.Visibility = Visibility.Visible;
            var duration = TimeSpan.FromMilliseconds(200);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            var widthAnim = new DoubleAnimation(190, 64, duration) { EasingFunction = ease };
            SidebarContainer.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);

            var catOpacity = new DoubleAnimation(1, 0, duration) { EasingFunction = ease };
            var catTranslate = new DoubleAnimation(0, 30, duration) { EasingFunction = ease };
            catOpacity.Completed += (s, e) => CategoryPanel.Visibility = Visibility.Collapsed;
            CategoryPanel.BeginAnimation(UIElement.OpacityProperty, catOpacity);
            CategoryPanelTranslate.BeginAnimation(TranslateTransform.XProperty, catTranslate);

            SectionPanel.Opacity = 0;
            SectionPanelTranslate.X = -30;
            var secOpacity = new DoubleAnimation(0, 1, duration) { EasingFunction = ease };
            var secTranslate = new DoubleAnimation(-30, 0, duration) { EasingFunction = ease };
            SectionPanel.BeginAnimation(UIElement.OpacityProperty, secOpacity);
            SectionPanelTranslate.BeginAnimation(TranslateTransform.XProperty, secTranslate);
        }

        private void UpdateModTagActiveStates()
        {
            if (Global.ModList == null) return;
            foreach (var mod in Global.ModList)
            {
                if (mod.TagItems == null) continue;
                foreach (var tag in mod.TagItems)
                {
                    tag.IsActive = ActiveCategories.Contains(tag.Name) || ActiveSections.Contains(tag.Name);
                }
            }
        }

        private void SectionSidebar_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void CategorySidebar_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void RefreshModList()
        {
            if (ModListView != null && ModListView.ItemsSource != null)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(ModListView.ItemsSource);
                if (view != null)
                {
                    view.Filter = ModFilter;
                    view.Refresh();
                }
            }
        }
        public string version;
        // Separated from Global.config so that order is updated when datagrid is modified
        public List<string> exes;
        private FileSystemWatcher ModsWatcher;
        private FlowDocument defaultFlow = new FlowDocument();
        private string defaultText = "Striverum Mod Manager is here to help out with all your UE4 Mods!\n\n" +
            "(Right Click Row > Fetch Metadata and confirm the GameBanana URL of the mod to fetch metadata to show here.)";
        private ObservableCollection<String> LauncherOptions = new ObservableCollection<String>(new string[] { "Executable", "Steam" });
        public MainWindow()
        {
            InitializeComponent();
            Global.logger = new Logger(ConsoleWindow);
            Global.config = new();

            // Get Version Number
            try
            {
                var StriverumVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
                if (!string.IsNullOrEmpty(StriverumVersion) && StriverumVersion.Contains('.'))
                    version = StriverumVersion.Substring(0, StriverumVersion.LastIndexOf('.'));
                else
                    version = "1.0.0";
            }
            catch
            {
                version = "1.0.0";
            }

            Global.logger.WriteLine($"Launched Striverum Mod Manager v{version}!", LoggerType.Info);
            // Get Global.config if it exists
            if (File.Exists($@"{Global.assemblyLocation}{Global.s}Config.json"))
            {
                try
                {
                    var configString = File.ReadAllText($@"{Global.assemblyLocation}{Global.s}Config.json");
                    Global.config = JsonSerializer.Deserialize<Config>(configString);
                    foreach (var game in Global.config.Configs.Keys)
                    {
                        if (Global.config.Configs[game].FirstOpen && !Global.config.Configs[game].LauncherOptionConverted)
                        {
                            Global.config.Configs[game].LauncherOptionIndex = Convert.ToInt32(Global.config.Configs[game].LauncherOption);
                            Global.config.Configs[game].LauncherOptionConverted = true;
                            Global.UpdateConfig();
                        }
                    }
                }
                catch (Exception e)
                {
                    Global.logger.WriteLine(e.Message, LoggerType.Error);
                }
            }

            // Last saved windows settings
            if (Global.config.Height != null && Global.config.Height >= MinHeight)
                Height = (double)Global.config.Height;
            if (Global.config.Width != null && Global.config.Width >= MinWidth)
                Width = (double)Global.config.Width;
            if (Global.config.Maximized)
                WindowState = WindowState.Maximized;
            if (Global.config.TopGridHeight != null)
                MainGrid.RowDefinitions[1].Height = new GridLength((double)Global.config.TopGridHeight, GridUnitType.Star);
            if (Global.config.BottomGridHeight != null)
                MainGrid.RowDefinitions[3].Height = new GridLength((double)Global.config.BottomGridHeight, GridUnitType.Star);
            if (Global.config.LeftGridWidth != null)
                MiddleGrid.ColumnDefinitions[1].Width = new GridLength((double)Global.config.LeftGridWidth, GridUnitType.Star);
            if (Global.config.RightGridWidth != null)
                MiddleGrid.ColumnDefinitions[3].Width = new GridLength((double)Global.config.RightGridWidth, GridUnitType.Star);

            Global.games = new List<string>();
            foreach (var item in GameBox.Items)
            {
                var game = (((item as ComboBoxItem).Content as StackPanel).Children[1] as TextBlock).Text.Trim().Replace(":", String.Empty);
                Global.games.Add(game);
            }

            if (Global.config.Configs == null)
            {
                Global.config.CurrentGame = "Guilty Gear -Strive-";
                Global.config.Configs = new()
                {
                    {
                        Global.config.CurrentGame, new()
                        {
                            CurrentLoadout = "Default",
                            Loadouts = new() { { "Default", new() } },
                            FirstOpen = true
                        }
                    }
                };
            }

            if (Global.config.CurrentGame == "Dragon Ball FighterZ" || string.IsNullOrEmpty(Global.config.CurrentGame))
            {
                if (Global.config.Configs.ContainsKey("Dragon Ball FighterZ"))
                {
                    if (!Global.config.Configs.ContainsKey("Guilty Gear -Strive-"))
                    {
                        Global.config.Configs["Guilty Gear -Strive-"] = Global.config.Configs["Dragon Ball FighterZ"];
                    }
                    Global.config.Configs.Remove("Dragon Ball FighterZ");
                }
                Global.config.CurrentGame = "Guilty Gear -Strive-";
            }

            if (!Global.config.Configs.ContainsKey(Global.config.CurrentGame))
            {
                Global.config.Configs[Global.config.CurrentGame] = new()
                {
                    CurrentLoadout = "Default",
                    Loadouts = new() { { "Default", new() } },
                    FirstOpen = true
                };
            }

            int ggsIndex = Global.games.IndexOf(Global.config.CurrentGame);
            GameBox.SelectedIndex = ggsIndex >= 0 ? ggsIndex : Global.games.IndexOf("Guilty Gear -Strive-");

            if (GameBox.SelectedIndex == 7)
                DiscordButton.Visibility = Visibility.Collapsed;

            if (String.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout))
                Global.config.Configs[Global.config.CurrentGame].CurrentLoadout = "Default";
            if (Global.config.Configs[Global.config.CurrentGame].Loadouts == null)
                Global.config.Configs[Global.config.CurrentGame].Loadouts = new();
            if (!Global.config.Configs[Global.config.CurrentGame].Loadouts.ContainsKey(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout))
                if (Global.config.Configs[Global.config.CurrentGame].ModList != null && Global.config.Configs[Global.config.CurrentGame].CurrentLoadout == "Default")
                {
                    Global.config.Configs[Global.config.CurrentGame].Loadouts.Add(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout, Global.config.Configs[Global.config.CurrentGame].ModList);
                    Global.config.Configs[Global.config.CurrentGame].ModList = null;
                }
                else
                    Global.config.Configs[Global.config.CurrentGame].Loadouts.Add(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout, new());
            else if (Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout] == null)
                if (Global.config.Configs[Global.config.CurrentGame].ModList != null && Global.config.Configs[Global.config.CurrentGame].CurrentLoadout == "Default")
                {
                    Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout] = Global.config.Configs[Global.config.CurrentGame].ModList;
                    Global.config.Configs[Global.config.CurrentGame].ModList = null;
                }
                else
                    Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout] = new();
            Global.ModList = Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout];

            Global.LoadoutItems = new ObservableCollection<String>(Global.config.Configs[Global.config.CurrentGame].Loadouts.Keys);

            LoadoutsBox.ItemsSource = Global.LoadoutItems;
            LoadoutsBox.SelectedItem = Global.config.Configs[Global.config.CurrentGame].CurrentLoadout;

            if ((String.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].ModsFolder)
                && Global.config.CurrentGame.Equals("Shin Megami Tensei V", StringComparison.InvariantCultureIgnoreCase)
                && Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex == 1) ||
                (!Global.config.CurrentGame.Equals("Shin Megami Tensei V", StringComparison.InvariantCultureIgnoreCase)
                && Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex == 0 &&
                (String.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].ModsFolder)
                || String.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].Launcher)
                || !File.Exists(Global.config.Configs[Global.config.CurrentGame].Launcher))))
            {
                LaunchButton.IsEnabled = false;
                Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
            }

            if (Global.config.CurrentGame.Equals("Shin Megami Tensei V", StringComparison.InvariantCultureIgnoreCase))
            {
                LauncherOptions[0] = "Emulator";
                LauncherOptions[1] = "Hardware";
            }
            else if (Global.config.CurrentGame.Equals("Kingdom Hearts III", StringComparison.InvariantCultureIgnoreCase))
                LauncherOptions[1] = "Epic Games";
            else if (Global.config.CurrentGame.Equals("The King of Fighters XV", StringComparison.InvariantCultureIgnoreCase)
                || Global.config.CurrentGame.Equals("MultiVersus", StringComparison.InvariantCultureIgnoreCase))
                LauncherOptions.Add("Epic Games");

            Directory.CreateDirectory($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}");

            // Watch mods folder to detect
            ModsWatcher = new FileSystemWatcher($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}");
            ModsWatcher.Created += OnModified;
            ModsWatcher.Deleted += OnModified;
            ModsWatcher.Renamed += OnModified;

            RefreshAll();
            Refresh();
            ModsWatcher.EnableRaisingEvents = true;

            defaultFlow.Blocks.Add(ConvertToFlowParagraph(defaultText));
            DescriptionWindow.Document = defaultFlow;
            var bitmap = new BitmapImage(new Uri("pack://application:,,,/Striverum;component/Assets/Striverumpreview.png"));
            Preview.Source = bitmap;
            PreviewBG.Source = null;

            BrowseSectionBar.ItemsSource = BrowseSections;
            BrowseCategoryPillsBar.ItemsSource = BrowseCategoryPills;
            GalleryItemsControl.ItemsSource = GalleryItems;
            GalleryScroll.SizeChanged += (s, e) =>
            {
                if (DescPanel != null && DescPanel.Visibility == Visibility.Visible)
                {
                    UpdateGallerySelection(imageCounter);
                }
            };
            InitBrowseSectionBar();

            Global.logger.WriteLine("Checking for updates...", LoggerType.Info);
            GameBox.IsEnabled = false;
            ModListView.IsEnabled = false;
            ConfigButton.IsEnabled = false;
            LaunchButton.IsEnabled = false;
            OpenModsButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;
            EditLoadoutsButton.IsEnabled = false;
            LoadoutsBox.IsEnabled = false;
            LauncherOptionsBox.IsEnabled = false;
            App.Current.Dispatcher.Invoke(() =>
            {
                ModUpdater.CheckForUpdates($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}", this);
            });
        }
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            OnFirstOpen();

            if (Global.config.CurrentGame.Equals("Dragon Ball FighterZ", StringComparison.InvariantCultureIgnoreCase))
                LauncherOptionsBox.IsEnabled = false;
            else
                LauncherOptionsBox.IsEnabled = true;

            LauncherOptionsBox.ItemsSource = LauncherOptions;
            LauncherOptionsBox.SelectedIndex = Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex;
        }
        private void OnModified(object sender, FileSystemEventArgs e)
        {
            Refresh();
            Global.UpdateConfig();
            // Bring window to front after download is done
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                Activate();
            });
        }

        private async void Refresh()
        {
            var currentModDirectory = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}";
            // Add new folders found in Mods to the ModList
            foreach (var mod in Directory.GetDirectories(currentModDirectory))
            {
                if (Global.ModList.ToList().Where(x => x.name == Path.GetFileName(mod)).Count() == 0)
                {
                    Mod m = new Mod();
                    m.name = Path.GetFileName(mod);
                    m.enabled = false;
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Global.ModList.Add(m);
                    });
                    Global.logger.WriteLine($"Added {Path.GetFileName(mod)}", LoggerType.Info);
                }
            }
            // Remove deleted folders that are still in the ModList
            foreach (var mod in Global.ModList.ToList())
            {
                if (!Directory.GetDirectories(currentModDirectory).ToList().Select(x => Path.GetFileName(x)).Contains(mod.name))
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Global.ModList.Remove(mod);
                    });
                    Global.logger.WriteLine($"Deleted {mod.name}", LoggerType.Info);
                    continue;
                }
                // Update all paks found
                if (mod.paks == null)
                    mod.paks = new();
                foreach (var file in Directory.GetFiles($"{currentModDirectory}{Global.s}{mod.name}", "*.*", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(file).Equals(".pak", StringComparison.InvariantCultureIgnoreCase)
                        && !mod.paks.ContainsKey(file))
                        mod.paks.Add(file, true); // Enable all paks when first added
                }
                // Remove all paks that no longer exist
                foreach (var pak in mod.paks.Keys)
                {
                    if (!File.Exists(pak))
                        mod.paks.Remove(pak);
                }
            }

            InitDefaultSections();

            // Load metadata for each mod and build TagItems
            foreach (var mod in Global.ModList)
            {
                string modJsonPath = $@"{currentModDirectory}{Global.s}{mod.name}{Global.s}mod.json";
                if (File.Exists(modJsonPath))
                {
                    try
                    {
                        var meta = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(modJsonPath));
                        if (meta != null)
                        {
                            mod.cat = meta.cat;
                            mod.subcategory = meta.subcategory;
                            mod.caticon = meta.caticon;
                            mod.tags = meta.tags ?? new List<string>();
                            if (!string.IsNullOrEmpty(mod.cat) && !mod.tags.Contains(mod.cat))
                                mod.tags.Insert(0, mod.cat);
                            if (!string.IsNullOrEmpty(mod.subcategory) && !mod.tags.Contains(mod.subcategory))
                                mod.tags.Add(mod.subcategory);

                            if (meta.caticon != null)
                            {
                                string iconFileName = Path.GetFileName(meta.caticon.LocalPath);
                                string iconCachePath = $@"{Global.assemblyLocation}{Global.s}Cache{Global.s}Icons{Global.s}{iconFileName}";
                                if (File.Exists(iconCachePath))
                                    mod.cachedIconPath = iconCachePath;
                            }
                        }
                    }
                    catch { }
                }

                // If subcategory has an icon, attach it to the CategoryItem in _sections
                if (!string.IsNullOrEmpty(mod.subcategory) && (mod.cachedIconPath != null || mod.caticon != null))
                {
                    foreach (var sec in _sections)
                    {
                        var cat = sec.Categories.FirstOrDefault(c => CategoryMatches(c.Name, mod.subcategory));
                        if (cat != null && string.IsNullOrEmpty(cat.IconPath))
                        {
                            cat.IconPath = mod.cachedIconPath ?? mod.caticon?.ToString();
                        }
                    }
                }

                // Ensure custom section or category from mod exists in _sections
                if (!string.IsNullOrEmpty(mod.cat))
                {
                    var existingSec = _sections.FirstOrDefault(s => CategoryMatches(s.Name, mod.cat));
                    if (existingSec == null)
                    {
                        existingSec = new SectionItem { Name = mod.cat, FaIcon = GetSectionFaIcon(mod.cat) };
                        _sections.Add(existingSec);
                    }
                    if (!string.IsNullOrEmpty(mod.subcategory))
                    {
                        var existingCat = existingSec.Categories.FirstOrDefault(c => CategoryMatches(c.Name, mod.subcategory));
                        if (existingCat == null)
                        {
                            existingSec.Categories.Add(new CategoryItem
                            {
                                Name = mod.subcategory,
                                IconPath = mod.cachedIconPath ?? mod.caticon?.ToString(),
                                FaIcon = GetCategoryFaIcon(mod.subcategory)
                            });
                        }
                    }
                }

                // Populate TagItems with resolved icons
                mod.TagItems.Clear();
                if (mod.tags != null)
                {
                    foreach (var tag in mod.tags)
                    {
                        var tagItem = new ModTag { Name = tag };
                        ResolveModTagIcon(tagItem, mod);
                        tagItem.IsActive = ActiveCategories.Contains(tag) || ActiveSections.Contains(tag);
                        mod.TagItems.Add(tagItem);
                    }
                }
            }

            UpdateModCounts();

            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    SectionSidebar.ItemsSource = _sections;
                    ModListView.ItemsSource = Global.ModList;
                    RefreshModList();
                    Stats.Text = $"{Global.ModList.Count} mods • {Directory.GetFiles($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}", "*", SearchOption.AllDirectories).Length.ToString("N0")} files • " +
                    $"{StringConverters.FormatSize(new DirectoryInfo($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}").GetDirectorySize())} • v{version}";
                });
            });
            Global.config.Configs[Global.config.CurrentGame].ModList = Global.ModList;
            Global.logger.WriteLine("Refreshed!", LoggerType.Info);
        }
        private void RefreshAll()
        {
            var currentModDirectory = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}";
            var currlist = Global.config.Configs[Global.config.CurrentGame].CurrentLoadout;
            foreach (var list in Global.config.Configs[Global.config.CurrentGame].Loadouts)
            {
                // Add new folders found in Mods to the ModList
                foreach (var mod in Directory.GetDirectories(currentModDirectory))
                {
                    if (list.Value.ToList().Where(x => x.name == Path.GetFileName(mod)).Count() == 0)
                    {
                        Mod m = new Mod();
                        m.name = Path.GetFileName(mod);
                        m.enabled = false;
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            list.Value.Add(m);
                        });
                    }
                }
                // Remove deleted folders that are still in the ModList
                foreach (var mod in list.Value.ToList())
                {
                    if (!Directory.GetDirectories(currentModDirectory).ToList().Select(x => Path.GetFileName(x)).Contains(mod.name))
                    {
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            list.Value.Remove(mod);
                        });
                        continue;
                    }
                    // Update all paks found
                    if (mod.paks == null)
                        mod.paks = new();
                    foreach (var file in Directory.GetFiles($"{currentModDirectory}{Global.s}{mod.name}", "*.*", SearchOption.AllDirectories))
                    {
                        if (Path.GetExtension(file).Equals(".pak", StringComparison.InvariantCultureIgnoreCase)
                            && !mod.paks.ContainsKey(file))
                            mod.paks.Add(file, true); // Enable all paks when first added
                    }
                    // Remove all paks that no longer exist
                    foreach (var pak in mod.paks.Keys)
                    {
                        if (!File.Exists(pak))
                            mod.paks.Remove(pak);
                    }
                }

                Global.logger.WriteLine($"Total {list.Value.Count} mods in {list.Key}", LoggerType.Info);
            }

            Global.config.Configs[Global.config.CurrentGame].CurrentLoadout = currlist;
            Global.config.Configs[Global.config.CurrentGame].ModList = Global.ModList;
        }

        // Events for Enabled checkboxes
        private void OnChecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            Mod mod = checkBox?.DataContext as Mod;

            if (mod != null)
            {
                mod.enabled = true;
                List<Mod> temp = Global.config.Configs[Global.config.CurrentGame].ModList.ToList();
                foreach (var m in temp)
                {
                    if (m.name == mod.name)
                        m.enabled = true;
                }
                Global.config.Configs[Global.config.CurrentGame].ModList = new ObservableCollection<Mod>(temp);
                Global.UpdateConfig();
            }
        }
        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            Mod mod = checkBox?.DataContext as Mod;

            if (mod != null)
            {
                mod.enabled = false;
                List<Mod> temp = Global.config.Configs[Global.config.CurrentGame].ModList.ToList();
                foreach (var m in temp)
                {
                    if (m.name == mod.name)
                        m.enabled = false;
                }
                Global.config.Configs[Global.config.CurrentGame].ModList = new ObservableCollection<Mod>(temp);
                Global.UpdateConfig();
            }
        }
        // Triggered when priority is switched on drag and dropped
        private void ModListView_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            Global.UpdateConfig();
        }

        private bool SetupGame()
        {
            var index = 0;
            bool emu = true;
            bool epic = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                index = GameBox.SelectedIndex;
                emu = LauncherOptionsBox.SelectedIndex == 0;
                epic = LauncherOptionsBox.SelectedIndex == 2;
            });
            return Setup.Generic("GGST.exe", "RED", @"C:\Program Files (x86)\Steam\steamapps\common\GUILTY GEAR -STRIVE-\GGST.exe", steamId: "1384160");
        }

        private async void Setup_Click(object sender, RoutedEventArgs e)
        {
            GameBox.IsEnabled = false;
            await Task.Run(() =>
            {
                var index = 0;
                Dispatcher.Invoke(() =>
                {
                    index = GameBox.SelectedIndex;
                });
                if (!String.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].ModsFolder)
                    || !String.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].Launcher) && File.Exists(Global.config.Configs[Global.config.CurrentGame].Launcher))
                {
                    var dialogResult = MessageBox.Show($@"Setup again?", $@"Notification", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (dialogResult == MessageBoxResult.No)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            GameBox.IsEnabled = true;
                        });
                        return;
                    }
                }
                if (SetupGame())
                {
                    Dispatcher.Invoke(() =>
                    {
                        LaunchButton.IsEnabled = true;
                    });
                }
            });
            GameBox.IsEnabled = true;
        }
        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Ensure the destination directory exists
            Directory.CreateDirectory(destinationDir);

            // Copy all files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Recursively copy all subdirectories
            foreach (var subdir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subdir));
                CopyDirectory(subdir, destSubDir);
            }
        }
        private async void Launch_Click(object sender, RoutedEventArgs e)
        {
            // Build Mod Loadout
            if (Global.config.Configs[Global.config.CurrentGame].ModsFolder != null)
            {
                GameBox.IsEnabled = false;
                ModListView.IsEnabled = false;
                ConfigButton.IsEnabled = false;
                LaunchButton.IsEnabled = false;
                OpenModsButton.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                EditLoadoutsButton.IsEnabled = false;
                LoadoutsBox.IsEnabled = false;
                LauncherOptionsBox.IsEnabled = false;
                Refresh();
                // Check if mods from before Striverum install existed
                Regex regex = new Regex(@"(^~*[a-z]$|^--Base--$)");
                var manuallyInstalledMods = Directory.GetDirectories(Global.config.Configs[Global.config.CurrentGame].ModsFolder)
                .Where(folder => !regex.IsMatch(Path.GetFileName(folder)));
                if (manuallyInstalledMods.Count() > 0)
                {
                    var dialogResult = MessageBox.Show($@"Striverum detected manually installed mods in {Global.config.Configs[Global.config.CurrentGame].ModsFolder}. " +
                        $@"Would you like to copy over these mods to Striverum before it DELETES them?", $@"Notification", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        foreach (var manuallyInstalledMod in manuallyInstalledMods)
                            CopyDirectory(manuallyInstalledMod,
                                Path.Combine(Global.assemblyLocation, "Mods", Global.config.CurrentGame, Path.GetFileName(manuallyInstalledMod)));
                    }
                }
                Directory.CreateDirectory(Global.config.Configs[Global.config.CurrentGame].ModsFolder);
                Global.logger.WriteLine($"Building loadout for {Global.config.CurrentGame}", LoggerType.Info);
                if (!await Build(Global.config.Configs[Global.config.CurrentGame].ModsFolder))
                {
                    Global.logger.WriteLine($"Failed to build loadout, not building and launching", LoggerType.Error);
                    ModListView.IsEnabled = true;
                    ConfigButton.IsEnabled = true;
                    LaunchButton.IsEnabled = true;
                    OpenModsButton.IsEnabled = true;
                    UpdateButton.IsEnabled = true;
                    GameBox.IsEnabled = true;
                    EditLoadoutsButton.IsEnabled = true;
                    LoadoutsBox.IsEnabled = true;
                    if (!Global.config.CurrentGame.Equals("Dragon Ball FighterZ", StringComparison.InvariantCultureIgnoreCase))
                        LauncherOptionsBox.IsEnabled = true;
                    return;
                }
                ModListView.IsEnabled = true;
                ConfigButton.IsEnabled = true;
                LaunchButton.IsEnabled = true;
                OpenModsButton.IsEnabled = true;
                UpdateButton.IsEnabled = true;
                GameBox.IsEnabled = true;
                EditLoadoutsButton.IsEnabled = true;
                LoadoutsBox.IsEnabled = true;
                if (!Global.config.CurrentGame.Equals("Dragon Ball FighterZ", StringComparison.InvariantCultureIgnoreCase))
                    LauncherOptionsBox.IsEnabled = true;
            }
            else
            {
                Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
                return;
            }
            // Launch game
            if (Global.config.CurrentGame.Equals("Shin Megami Tensei V", StringComparison.InvariantCultureIgnoreCase))
            {
                if (LauncherOptionsBox.SelectedIndex == 1)
                    return;
                else if (Global.config.Configs[Global.config.CurrentGame].Launcher == null || !File.Exists(Global.config.Configs[Global.config.CurrentGame].Launcher)
                    && Global.config.Configs[Global.config.CurrentGame].GamePath == null || !File.Exists(Global.config.Configs[Global.config.CurrentGame].GamePath))
                {
                    Global.logger.WriteLine($"Please click Setup to configure launching from emulator!", LoggerType.Warning);
                    return;
                }
                else
                {
                    try
                    {
                        Global.logger.WriteLine($"Launching {Global.config.Configs[Global.config.CurrentGame].GamePath} with {Global.config.Configs[Global.config.CurrentGame].Launcher}", LoggerType.Info);
                        var ps = new ProcessStartInfo(Global.config.Configs[Global.config.CurrentGame].Launcher)
                        {
                            WorkingDirectory = Path.GetDirectoryName(Global.config.Configs[Global.config.CurrentGame].Launcher),
                            UseShellExecute = true,
                            Verb = "open",
                            Arguments = $"\"{Global.config.Configs[Global.config.CurrentGame].GamePath}\""
                        };
                        Process.Start(ps);
                    }
                    catch (Exception ex)
                    {
                        Global.logger.WriteLine($"Couldn't launch {Global.config.Configs[Global.config.CurrentGame].GamePath} with {Global.config.Configs[Global.config.CurrentGame].Launcher} ({ex.Message})", LoggerType.Error);
                    }

                }
            }
            else if (Global.config.Configs[Global.config.CurrentGame].Launcher != null && File.Exists(Global.config.Configs[Global.config.CurrentGame].Launcher))
            {
                var path = Global.config.Configs[Global.config.CurrentGame].Launcher;
                try
                {
                    Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex = LauncherOptionsBox.SelectedIndex;
                    Global.UpdateConfig();
                    if (Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex > 0)
                    {
                        var id = "1384160";
                        var epic = false;
                        path = epic ? $"com.epicgames.launcher://apps/{id}?action=launch&silent=true" : $"steam://rungameid/{id}";
                    }
                    Global.logger.WriteLine($"Launching {path}", LoggerType.Info);
                    var ps = new ProcessStartInfo(path)
                    {
                        WorkingDirectory = Path.GetDirectoryName(Global.config.Configs[Global.config.CurrentGame].Launcher),
                        UseShellExecute = true,
                        Verb = "open"
                    };

                    Process.Start(ps);
                }
                catch (Exception ex)
                {
                    Global.logger.WriteLine($"Couldn't launch {path} ({ex.Message})", LoggerType.Error);
                }
            }
            else
                Global.logger.WriteLine($"Please click Setup before starting!", LoggerType.Warning);
        }
        private void GameBanana_Click(object sender, RoutedEventArgs e)
        {
            var id = "11534";
            try
            {
                var ps = new ProcessStartInfo($"https://gamebanana.com/games/{id}")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"Couldn't open up GameBanana ({ex.Message})", LoggerType.Error);
            }
        }
        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string discordLink;
                var index = managerSelected ? GameBox.SelectedIndex : GameFilterBox.SelectedIndex;
                switch (index)
                {
                    case 9:
                        discordLink = "https://discord.gg/Se2XTnA";
                        break;
                    default:
                        discordLink = "https://discord.gg/tgFrebr";
                        break;
                }
                var ps = new ProcessStartInfo(discordLink)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine(ex.Message, LoggerType.Error);
            }
        }
        private void ScrollToBottom(object sender, TextChangedEventArgs args)
        {
            ConsoleWindow.ScrollToEnd();
        }

        private void ModListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }

            if (ModListView.SelectedItem == null)
                element.ContextMenu.Visibility = Visibility.Collapsed;
            else
                element.ContextMenu.Visibility = Visibility.Visible;
        }

        private async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModListView.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            foreach (var row in temp)
                if (row != null)
                {
                    var dialogResult = MessageBox.Show($@"Are you sure you want to delete {row.name}?" + Environment.NewLine + "This cannot be undone.", $@"Deleting {row.name}: Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        try
                        {
                            await Task.Run(() => Directory.Delete($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{row.name}", true));
                            Global.logger.WriteLine($@"Deleting {row.name}.", LoggerType.Info);
                            ShowMetadata(null);
                        }
                        catch (Exception ex)
                        {
                            Global.logger.WriteLine($@"Couldn't delete {row.name} ({ex.Message})", LoggerType.Error);
                        }
                    }
                }
        }

        private async Task<bool> Build(string path)
        {
            return await Task.Run(() =>
            {
                // Get other folders using the mods folder
                string SplashFolder = null;
                string MoviesFolder = null;
                string SoundsFolder = null;
                var ContentFolder = new DirectoryInfo(Global.config.Configs[Global.config.CurrentGame].ModsFolder).Parent.Parent.FullName;
                if (Directory.Exists($"{ContentFolder}{Global.s}Splash"))
                    SplashFolder = $"{ContentFolder}{Global.s}Splash";
                if (Directory.Exists($"{ContentFolder}{Global.s}Movies"))
                    MoviesFolder = $"{ContentFolder}{Global.s}Movies";
                else if (Directory.Exists($"{ContentFolder}{Global.s}Binaries{Global.s}Movie"))
                    MoviesFolder = $"{ContentFolder}{Global.s}Binaries{Global.s}Movie";
                else if (Directory.Exists($"{ContentFolder}{Global.s}Movies"))
                    MoviesFolder = $"{ContentFolder}{Global.s}Movie";
                if (Directory.Exists($"{ContentFolder}{Global.s}Sound") && !Global.config.CurrentGame.Equals("Shin Megami Tensei V", StringComparison.InvariantCultureIgnoreCase))
                    SoundsFolder = $"{ContentFolder}{Global.s}Sound";
                else if (Directory.Exists($"{ContentFolder}{Global.s}CriWareData"))
                    SoundsFolder = $"{ContentFolder}{Global.s}CriWareData";
                bool? Patched = null;
                if (!ModLoader.Restart(path, MoviesFolder, SplashFolder, SoundsFolder))
                    return false;
                var mods = Global.config.Configs[Global.config.CurrentGame].ModList.Where(x => x.enabled).ToList();
                mods.Reverse();



                ModLoader.Build(path, mods, Patched, MoviesFolder, SplashFolder, SoundsFolder);
                return true;
            });
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                Global.config.Height = RestoreBounds.Height;
                Global.config.Width = RestoreBounds.Width;
                Global.config.Maximized = true;
            }
            else
            {
                Global.config.Height = Height;
                Global.config.Width = Width;
                Global.config.Maximized = false;
            }
            Global.config.TopGridHeight = MainGrid.RowDefinitions[1].Height.Value;
            Global.config.BottomGridHeight = MainGrid.RowDefinitions[3].Height.Value;
            Global.config.LeftGridWidth = MiddleGrid.ColumnDefinitions[1].Width.Value;
            Global.config.RightGridWidth = MiddleGrid.ColumnDefinitions[3].Width.Value;
            Global.UpdateConfig();
            Application.Current.Shutdown();
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModListView.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            foreach (var row in temp)
                if (row != null)
                {
                    var folderName = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{row.name}";
                    if (Directory.Exists(folderName))
                    {
                        try
                        {
                            Process process = Process.Start("explorer.exe", folderName);
                            Global.logger.WriteLine($@"Opened {folderName}.", LoggerType.Info);
                        }
                        catch (Exception ex)
                        {
                            Global.logger.WriteLine($@"Couldn't open {folderName}. ({ex.Message})", LoggerType.Error);
                        }
                    }
                }
        }
        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModListView.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);

            // Stop refreshing while renaming folders
            ModsWatcher.EnableRaisingEvents = false;
            foreach (var row in temp)
                if (row != null)
                {
                    EditWindow ew = new EditWindow(row.name, true);
                    ew.ShowDialog();
                }
            ModsWatcher.EnableRaisingEvents = true;
            Global.UpdateConfig();
            ModListView.Items.Refresh();
        }
        private void ConfigurePaksItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModListView.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            foreach (var row in temp)
                if (row != null)
                {
                    ConfigurePaksWindow cpw = new ConfigurePaksWindow(row);
                    cpw.ShowDialog();
                    var index = Global.ModList.IndexOf(row);
                    Global.ModList[index] = cpw._mod;
                    Global.UpdateConfig();
                }
        }
        private void FetchItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedMods = ModListView.SelectedItems;
            var temp = new Mod[selectedMods.Count];
            selectedMods.CopyTo(temp, 0);
            foreach (var row in temp)
                if (row != null)
                {
                    FetchWindow fw = new FetchWindow(row);
                    fw.ShowDialog();
                    if (fw.success)
                        ShowMetadata(row.name);
                }
        }
        private void Add_Enter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Handled = true;
                e.Effects = DragDropEffects.Move;
                
            }
        }
        private void Add_Leave(object sender, DragEventArgs e)
        {
            e.Handled = true;

        }
        private void Add_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                CreateMod(fileList);
            }

        }
        private void CreateMod(string[] files)
        {
            var nameWindow = new EditWindow(null, true);
            nameWindow.ShowDialog();
            if (nameWindow.directory != null)
            {
                Directory.CreateDirectory(nameWindow.directory);
                string defaultSig = null;
                if (Directory.Exists(Global.config.Configs[Global.config.CurrentGame].ModsFolder))
                {
                    var sigs = Directory.GetFiles(Path.GetDirectoryName(Global.config.Configs[Global.config.CurrentGame].ModsFolder), "*.sig", SearchOption.TopDirectoryOnly);
                    if (sigs.Length > 0)
                        defaultSig = sigs[0];
                }
                foreach (var file in files)
                {
                    // Get the file attributes for file or directory
                    FileAttributes attr = File.GetAttributes(file);

                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        foreach (string path in Directory.GetFiles(file, "*.*", SearchOption.AllDirectories))
                        {
                            var newPath = path.Replace(file, $"{nameWindow.directory}{Global.s}{Path.GetFileName(file)}");
                            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                            File.Copy(path, newPath, true);
                            if (Path.GetExtension(path).Equals(".pak", StringComparison.InvariantCultureIgnoreCase) && defaultSig != null)
                            {
                                var sig = Path.ChangeExtension(path, ".sig");
                                var newPathSig = Path.ChangeExtension(newPath, ".sig");
                                // Check if mod folder has corresponding .sig
                                if (File.Exists(sig))
                                    File.Copy(sig, newPathSig, true);
                                // Otherwise copy over original game's .sig
                                else if (File.Exists(defaultSig))
                                    File.Copy(defaultSig, newPathSig, true);
                            }
                        }
                    }
                    else
                    {
                        var newPath = $"{nameWindow.directory}{Global.s}{Path.GetFileName(file)}";
                        File.Copy(file, newPath, true);
                        if (Path.GetExtension(file).Equals(".pak", StringComparison.InvariantCultureIgnoreCase) && defaultSig != null)
                        {
                            var sig = Path.ChangeExtension(file, ".sig");
                            var newPathSig = Path.ChangeExtension(newPath, ".sig");
                            // Check if mod folder has corresponding .sig
                            if (File.Exists(sig))
                                File.Copy(sig, newPathSig, true);
                            // Otherwise copy over original game's .sig
                            else if (File.Exists(defaultSig))
                                File.Copy(defaultSig, newPathSig, true);
                        }
                    }
                }
            }
        }
        private void ModsFolder_Click(object sender, RoutedEventArgs e)
        {
            var choices = new List<Choice>();
            choices.Add(new Choice()
            {
                OptionText = "Create New Mod",
                OptionSubText = "Name a mod and choose the .pak file for it to use",
                Index = 0,
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Plus
            });
            choices.Add(new Choice()
            {
                OptionText = "Open Mods Folder",
                OptionSubText = "Drag or extract mod folders into this directory",
                Index = 1,
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_FolderOpen
            });
            var choice = new ChoiceWindow(choices, "Add Mods");
            choice.ShowDialog();
            if (choice.choice != null && (int)choice.choice == 0)
            {
                var nameWindow = new EditWindow(null, true);
                nameWindow.ShowDialog();
                if (nameWindow.directory != null)
                {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.DefaultExt = ".pak";
                    dialog.Filter = "UE4 Package Files (*.pak)|*.pak";
                    dialog.Title = $"Select .pak to add in {Path.GetFileName(nameWindow.directory)}";
                    dialog.Multiselect = false;
                    dialog.ShowDialog();
                    if (!String.IsNullOrEmpty(dialog.FileName))
                    {
                        Directory.CreateDirectory(nameWindow.directory);
                        File.Copy(dialog.FileName, $"{nameWindow.directory}{Global.s}{Path.GetFileName(dialog.FileName)}", true);
                        if (Directory.Exists(Global.config.Configs[Global.config.CurrentGame].ModsFolder))
                        {
                            // Copy over sig if it exists
                            var sigs = Directory.GetFiles(Path.GetDirectoryName(Global.config.Configs[Global.config.CurrentGame].ModsFolder), "*.sig", SearchOption.TopDirectoryOnly);
                            if (sigs.Length > 0)
                                File.Copy(sigs[0], Path.ChangeExtension($"{nameWindow.directory}{Global.s}{Path.GetFileName(dialog.FileName)}", ".sig"), true);
                        }
                    }
                }
            }
            else if (choice.choice != null && (int)choice.choice == 1)
            {
                var folderName = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}";
                if (Directory.Exists(folderName))
                {
                    try
                    {
                        Process process = Process.Start("explorer.exe", folderName);
                        Global.logger.WriteLine($@"Opened {folderName}.", LoggerType.Info);
                    }
                    catch (Exception ex)
                    {
                        Global.logger.WriteLine($@"Couldn't open {folderName}. ({ex.Message})", LoggerType.Error);
                    }
                }
            }
        }
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Global.logger.WriteLine("Checking for updates...", LoggerType.Info);
            GameBox.IsEnabled = false;
            ModListView.IsEnabled = false;
            ConfigButton.IsEnabled = false;
            LaunchButton.IsEnabled = false;
            OpenModsButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;
            EditLoadoutsButton.IsEnabled = false;
            LoadoutsBox.IsEnabled = false;
            LauncherOptionsBox.IsEnabled = false;
            App.Current.Dispatcher.Invoke(() =>
            {
                ModUpdater.CheckForUpdates($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}", this);
            });
        }
        private Paragraph ConvertToFlowParagraph(string text)
        {
            var flowDocument = new FlowDocument();

            var regex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Matches(text).Cast<Match>().Select(m => m.Value).ToList();

            var paragraph = new Paragraph();
            flowDocument.Blocks.Add(paragraph);

            foreach (var segment in regex.Split(text))
            {
                if (matches.Contains(segment))
                {
                    var hyperlink = new Hyperlink(new Run(segment))
                    {
                        NavigateUri = new Uri(segment),
                        Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)),
                        Cursor = Cursors.Hand
                    };

                    void OpenUrl(object s, RoutedEventArgs a)
                    {
                        try
                        {
                            var ps = new ProcessStartInfo(segment)
                            {
                                UseShellExecute = true,
                                Verb = "open"
                            };
                            Process.Start(ps);
                        }
                        catch (Exception ex)
                        {
                            Global.logger.WriteLine($"Couldn't open up {segment} ({ex.Message})", LoggerType.Error);
                        }
                        a.Handled = true;
                    }

                    hyperlink.RequestNavigate += (s, a) => OpenUrl(s, a);
                    hyperlink.Click += (s, a) => OpenUrl(s, a);

                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(segment));
                }
            }

            return paragraph;
        }

        private void ShowMetadata(string mod)
        {
            if (mod == null || !File.Exists($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{mod}{Global.s}mod.json"))
            {
                DescriptionWindow.Document = defaultFlow;
                var bitmap = new BitmapImage(new Uri("pack://application:,,,/Striverum;component/Assets/Striverumpreview.png"));
                Preview.Source = bitmap;
                PreviewBG.Source = null;
            }
            else
            {
                FlowDocument descFlow = new FlowDocument();
                var metadataString = File.ReadAllText($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{mod}{Global.s}mod.json");
                Metadata metadata = JsonSerializer.Deserialize<Metadata>(metadataString);

                var para = new Paragraph();
                if (metadata.submitter != null)
                {
                    para.Inlines.Add($"Submitter: ");
                    if (metadata.avi != null && metadata.avi.ToString().Length > 0)
                    {
                        BitmapImage bm = new BitmapImage(metadata.avi);
                        Image image = new Image();
                        image.Source = bm;
                        image.Height = 35;
                        para.Inlines.Add(image);
                        para.Inlines.Add(" ");
                    }
                    if (metadata.upic != null && metadata.upic.ToString().Length > 0)
                    {
                        BitmapImage bm = new BitmapImage(metadata.upic);
                        Image image = new Image();
                        image.Source = bm;
                        image.Height = 25;
                        para.Inlines.Add(image);
                    }
                    else
                        para.Inlines.Add(metadata.submitter);
                    descFlow.Blocks.Add(para);
                }
                if (metadata.preview != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = metadata.preview;
                    bitmap.EndInit();
                    Preview.Source = bitmap;
                    PreviewBG.Source = bitmap;
                }
                else
                {
                    var bitmap = new BitmapImage(new Uri("pack://application:,,,/Striverum;component/Assets/Striverumpreview.png"));
                    Preview.Source = bitmap;
                    PreviewBG.Source = null;
                }
                para = new Paragraph();
                para.Inlines.Add("Category: ");
                if (metadata.caticon != null && metadata.caticon.ToString().Length > 0)
                {
                    BitmapImage bm = new BitmapImage(metadata.caticon);
                    Image image = new Image();
                    image.Source = bm;
                    image.Width = 20;
                    para.Inlines.Add(image);
                }
                para.Inlines.Add($" {metadata.cat}");
                descFlow.Blocks.Add(para);
                var text = "";
                if (!String.IsNullOrEmpty(metadata.description))
                    text += $"Description: {metadata.description}\n\n";
                if (!String.IsNullOrEmpty(metadata.filedescription))
                    text += $"File Description: {metadata.filedescription}\n\n";
                if (metadata.homepage != null && metadata.homepage.ToString().Length > 0)
                    text += $"Home Page: {metadata.homepage}";
                var init = ConvertToFlowParagraph(text);
                descFlow.Blocks.Add(init);
                DescriptionWindow.Document = descFlow;
                var descriptionText = new TextRange(DescriptionWindow.Document.ContentStart, DescriptionWindow.Document.ContentEnd);
                descriptionText.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Center);
            }
        }
        private void ModListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mod row = (Mod)ModListView.SelectedItem;
            if (row != null)
                ShowMetadata(row.name);
        }
        private void ModGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModListView_SelectionChanged(sender, e);
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (e != null) e.Handled = true;
            Button button = sender as Button;
            var item = button?.DataContext as GameBananaRecord;
            if (item != null)
                new ModDownloader().BrowserDownload(Global.games[0], item);
        }
        private void AltDownload_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            new AltLinkWindow(item.AlternateFileSources, item.Title,
                (((GameFilterBox.SelectedValue as ComboBoxItem).Content as StackPanel).Children[1] as TextBlock).Text.Trim().Replace(":", String.Empty),
                item.Link.AbsoluteUri).ShowDialog();
        }
        private void Homepage_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            try
            {
                var ps = new ProcessStartInfo(item.Link.ToString())
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"Couldn't open up {item.Link} ({ex.Message})", LoggerType.Error);
            }
        }
        private int imageCounter;
        private int imageCount;
        private FlowDocument ConvertToFlowDocument(string text)
        {
            var flowDocument = new FlowDocument();

            var regex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Matches(text).Cast<Match>().Select(m => m.Value).ToList();

            var paragraph = new Paragraph();
            flowDocument.Blocks.Add(paragraph);

            foreach (var segment in regex.Split(text))
            {
                if (matches.Contains(segment))
                {
                    var hyperlink = new Hyperlink(new Run(segment))
                    {
                        NavigateUri = new Uri(segment),
                        Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)),
                        Cursor = Cursors.Hand
                    };

                    void OpenUrl(object s, RoutedEventArgs a)
                    {
                        try
                        {
                            var ps = new ProcessStartInfo(segment)
                            {
                                UseShellExecute = true,
                                Verb = "open"
                            };
                            Process.Start(ps);
                        }
                        catch (Exception ex)
                        {
                            Global.logger.WriteLine($"Couldn't open up {segment} ({ex.Message})", LoggerType.Error);
                        }
                        a.Handled = true;
                    }

                    hyperlink.RequestNavigate += (s, a) => OpenUrl(s, a);
                    hyperlink.Click += (s, a) => OpenUrl(s, a);

                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(segment));
                }
            }

            return flowDocument;
        }

        private void ModTile_Click(object sender, MouseButtonEventArgs e)
        {
            // If the user clicked on a button inside the tile (e.g. Download), let the button handle it
            DependencyObject source = e.OriginalSource as DependencyObject;
            while (source != null && source != sender)
            {
                if (source is System.Windows.Controls.Button || source is System.Windows.Controls.Primitives.ButtonBase)
                    return;
                source = VisualTreeHelper.GetParent(source);
            }

            FrameworkElement elem = sender as FrameworkElement;
            if (elem?.DataContext is GameBananaRecord record)
            {
                OpenModDetails(record);
            }
        }

        private void MoreInfo_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button?.DataContext as GameBananaRecord;
            if (item != null)
                OpenModDetails(item);
        }

        private void OpenModDetails(GameBananaRecord item)
        {
            if (item == null) return;
            HomepageButton.Content = $"{(TypeBox.SelectedValue as ComboBoxItem)?.Content.ToString().Trim().TrimEnd('s')} Page";
            if (item.Compatible)
                DownloadButton.Visibility = Visibility.Visible;
            else
                DownloadButton.Visibility = Visibility.Collapsed;
            if (item.HasAltLinks)
                AltButton.Visibility = Visibility.Visible;
            else
                AltButton.Visibility = Visibility.Collapsed;
            DescPanel.DataContext = item;
            MediaPanel.DataContext = item;
            DescText.ScrollToHome();
            var text = "";
            text += item.ConvertedText;
            DescText.Document = ConvertToFlowDocument(text);
            ImageLeft.IsEnabled = true;
            ImageRight.IsEnabled = true;
            BigImageLeft.IsEnabled = true;
            BigImageRight.IsEnabled = true;
            GalleryItems.Clear();
            var images = item.Media?.Where(x => x.Type == "image").ToList();
            if (images != null && images.Count > 0)
            {
                imageCount = images.Count;
                for (int i = 0; i < images.Count; i++)
                {
                    string fileName = "";
                    try
                    {
                        fileName = System.IO.Path.GetFileName(images[i].File?.OriginalString ?? images[i].File?.ToString() ?? "");
                    }
                    catch { }

                    string title = !string.IsNullOrEmpty(fileName) && !fileName.All(char.IsDigit)
                        ? $"{fileName} ({i + 1}/{images.Count})"
                        : $"Image {i + 1} of {images.Count}";

                    GalleryItems.Add(new CoverFlowItem
                    {
                        Index = i,
                        ImageUrl = $"{images[i].Base}/{images[i].File}",
                        Title = title,
                        Caption = images[i].Caption,
                        IsSelected = (i == 0)
                    });
                }
                ImagePanel.Visibility = Visibility.Visible;
                UpdateGallerySelection(0);
            }
            else
            {
                imageCount = 0;
                imageCounter = 0;
                ImagePanel.Visibility = Visibility.Collapsed;
            }

            DescPanel.Visibility = Visibility.Visible;
        }
        private void CloseDesc_Click(object sender, RoutedEventArgs e)
        {
            MediaPanel.Visibility = Visibility.Collapsed;
            DescPanel.Visibility = Visibility.Collapsed;
        }
        private void CloseMedia_Click(object sender, RoutedEventArgs e)
        {
            MediaPanel.Visibility = Visibility.Collapsed;
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            MediaPanel.Visibility = Visibility.Visible;
        }

        private void UpdateGallerySelection(int newIndex)
        {
            if (GalleryItems == null || GalleryItems.Count == 0) return;
            if (newIndex < 0) newIndex = GalleryItems.Count - 1;
            if (newIndex >= GalleryItems.Count) newIndex = 0;
            imageCounter = newIndex;

            for (int i = 0; i < GalleryItems.Count; i++)
            {
                GalleryItems[i].IsSelected = (i == newIndex);
            }

            var selectedItem = GalleryItems[newIndex];
            CaptionText.Text = selectedItem.Caption;
            CaptionText.Visibility = string.IsNullOrEmpty(selectedItem.Caption) ? Visibility.Collapsed : Visibility.Visible;

            try
            {
                var image = new BitmapImage(new Uri(selectedItem.ImageUrl));
                Screenshot.Source = image;
                BigScreenshot.Source = image;
                BigImageTitle.Text = selectedItem.Title ?? $"Image {newIndex + 1} of {GalleryItems.Count}";
                BigCaptionText.Text = selectedItem.Caption;
                BigCaptionText.Visibility = string.IsNullOrEmpty(selectedItem.Caption) ? Visibility.Collapsed : Visibility.Visible;
            }
            catch { }

            if (GalleryItems.Count <= 1)
            {
                ImageLeft.IsEnabled = false;
                ImageRight.IsEnabled = false;
                BigImageLeft.IsEnabled = false;
                BigImageRight.IsEnabled = false;
            }
            else
            {
                ImageLeft.IsEnabled = true;
                ImageRight.IsEnabled = true;
                BigImageLeft.IsEnabled = true;
                BigImageRight.IsEnabled = true;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (GalleryScroll != null && GalleryItemsControl != null)
                    {
                        double viewport = GalleryScroll.ActualWidth;
                        if (viewport <= 0) viewport = GalleryScroll.ViewportWidth;
                        if (viewport <= 0 && ImagePanel != null) viewport = ImagePanel.ActualWidth;
                        if (viewport <= 0) viewport = 800;

                        double sidePad = Math.Max(0, (viewport - 302.0) / 2.0);
                        GalleryItemsControl.Margin = new Thickness(sidePad, 0, sidePad, 0);

                        double targetOffset = newIndex * 222.0;
                        GalleryScroll.ScrollToHorizontalOffset(targetOffset);
                    }
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void GalleryItem_Click(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as CoverFlowItem;
            if (item != null)
            {
                UpdateGallerySelection(item.Index);
                MediaPanel.Visibility = Visibility.Visible;
            }
        }

        private void ImageLeft_Click(object sender, RoutedEventArgs e)
        {
            if (GalleryItems != null && GalleryItems.Count > 0)
            {
                UpdateGallerySelection(imageCounter - 1);
            }
        }

        private void ImageRight_Click(object sender, RoutedEventArgs e)
        {
            if (GalleryItems != null && GalleryItems.Count > 0)
            {
                UpdateGallerySelection(imageCounter + 1);
            }
        }
        private static bool selected = false;

        private static Dictionary<GameFilter, Dictionary<TypeFilter, List<GameBananaCategory>>> cats = new();

        private static readonly List<GameBananaCategory> All = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "All",
                ID = null
            }
        }.ToList();
        private static readonly List<GameBananaCategory> None = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "- - -",
                ID = null
            }
        }.ToList();
        private async void InitializeBrowser()
        {
            using (var httpClient = new HttpClient())
            {
                ErrorPanel.Visibility = Visibility.Collapsed;
                // Initialize categories for Guilty Gear -Strive- (11534) exclusively
                var gameIDS = new string[] { "11534" };
                var types = new string[] { "Mod", "Wip", "Sound" };
                var gameCounter = 0;
                foreach (var gameID in gameIDS)
                {
                    var counter = 0;
                    double totalPages = 0;
                    foreach (var type in types)
                    {
                        var requestUrl = $"https://gamebanana.com/apiv4/{type}Category/ByGame?_aGameRowIds[]={gameID}&_sRecordSchema=Custom" +
                            "&_csvProperties=_idRow,_sName,_sProfileUrl,_sIconUrl,_idParentCategoryRow&_nPerpage=50";
                        string responseString = "";
                        try
                        {
                            var responseMessage = await httpClient.GetAsync(requestUrl);
                            responseString = await responseMessage.Content.ReadAsStringAsync();
                            responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                            var numRecords = responseMessage.GetHeader("X-GbApi-Metadata_nRecordCount");
                            if (numRecords != -1)
                            {
                                totalPages = Math.Ceiling(numRecords / 50);
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            LoadingBar.Visibility = Visibility.Collapsed;
                            ErrorPanel.Visibility = Visibility.Visible;
                            BrowserRefreshButton.Visibility = Visibility.Visible;
                            switch (Regex.Match(ex.Message, @"\d+").Value)
                            {
                                case "443":
                                    BrowserMessage.Text = "Your internet connection is down.";
                                    break;
                                case "500":
                                case "503":
                                case "504":
                                    BrowserMessage.Text = "GameBanana's servers are down.";
                                    break;
                                default:
                                    BrowserMessage.Text = ex.Message;
                                    break;
                            }
                            return;
                        }
                        catch (Exception ex)
                        {
                            LoadingBar.Visibility = Visibility.Collapsed;
                            ErrorPanel.Visibility = Visibility.Visible;
                            BrowserRefreshButton.Visibility = Visibility.Visible;
                            BrowserMessage.Text = ex.Message;
                            return;
                        }
                        List<GameBananaCategory> response = new();
                        try
                        {
                            response = JsonSerializer.Deserialize<List<GameBananaCategory>>(responseString);
                        }
                        catch (Exception)
                        {
                            LoadingBar.Visibility = Visibility.Collapsed;
                            ErrorPanel.Visibility = Visibility.Visible;
                            BrowserRefreshButton.Visibility = Visibility.Visible;
                            BrowserMessage.Text = "Uh oh! Something went wrong while deserializing the categories...";
                            return;
                        }
                        if (!cats.ContainsKey(0))
                            cats.Add(0, new Dictionary<TypeFilter, List<GameBananaCategory>>());
                        if (!cats[0].ContainsKey((TypeFilter)counter))
                            cats[0].Add((TypeFilter)counter, response);

                        // Make more requests if needed
                        if (totalPages > 1)
                        {
                            for (double i = 2; i <= totalPages; i++)
                            {
                                var requestUrlPage = $"{requestUrl}&_nPage={i}";
                                try
                                {
                                    responseString = await httpClient.GetStringAsync(requestUrlPage);
                                    responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                                }
                                catch (HttpRequestException ex)
                                {
                                    LoadingBar.Visibility = Visibility.Collapsed;
                                    ErrorPanel.Visibility = Visibility.Visible;
                                    BrowserRefreshButton.Visibility = Visibility.Visible;
                                    switch (Regex.Match(ex.Message, @"\d+").Value)
                                    {
                                        case "443":
                                            BrowserMessage.Text = "Your internet connection is down.";
                                            break;
                                        case "500":
                                        case "503":
                                        case "504":
                                            BrowserMessage.Text = "GameBanana's servers are down.";
                                            break;
                                        default:
                                            BrowserMessage.Text = ex.Message;
                                            break;
                                    }
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    LoadingBar.Visibility = Visibility.Collapsed;
                                    ErrorPanel.Visibility = Visibility.Visible;
                                    BrowserRefreshButton.Visibility = Visibility.Visible;
                                    BrowserMessage.Text = ex.Message;
                                    return;
                                }
                                try
                                {
                                    response = JsonSerializer.Deserialize<List<GameBananaCategory>>(responseString);
                                }
                                catch (Exception)
                                {
                                    LoadingBar.Visibility = Visibility.Collapsed;
                                    ErrorPanel.Visibility = Visibility.Visible;
                                    BrowserRefreshButton.Visibility = Visibility.Visible;
                                    BrowserMessage.Text = "Uh oh! Something went wrong while deserializing the categories...";
                                    return;
                                }
                                cats[0][(TypeFilter)counter] = cats[0][(TypeFilter)counter].Concat(response).ToList();
                            }
                        }
                        counter++;
                    }
                    gameCounter++;
                }
            }
            filterSelect = true;
            GameFilterBox.SelectedIndex = GameBox.SelectedIndex;
            FilterBox.ItemsSource = FilterBoxList;
            CatBox.ItemsSource = All.Concat(cats[0][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
            SubCatBox.ItemsSource = None;
            CatBox.SelectedIndex = 0;
            SubCatBox.SelectedIndex = 0;
            FilterBox.SelectedIndex = 1;
            filterSelect = false;
            InitBrowseSectionBar();
            RefreshFilter();
            selected = true;
        }
        private void OnBrowserTabSelected(object sender, RoutedEventArgs e)
        {
            managerSelected = false;
            if (GameFilterBox.SelectedIndex != 7)
                DiscordButton.Visibility = Visibility.Visible;
            else
                DiscordButton.Visibility = Visibility.Collapsed;
            if (GameFilterBox.SelectedIndex == 1)
                SZFilters.Visibility = Visibility.Visible;
            else
                SZFilters.Visibility = Visibility.Collapsed;
            if (!selected)
            {
                InitializeBrowser();
                if (GameBox.SelectedIndex != 7)
                    DiscordButton.Visibility = Visibility.Visible;
                else
                    DiscordButton.Visibility = Visibility.Collapsed;
                if (GameBox.SelectedIndex == 1)
                    SZFilters.Visibility = Visibility.Visible;
                else
                    SZFilters.Visibility = Visibility.Collapsed;
            }
            else
            {
                UpdateBrowseSectionCounts();
                UpdateBrowseCategoryPillCounts();
            }
        }
        bool managerSelected = true;
        private void OnManagerTabSelected(object sender, RoutedEventArgs e)
        {
            managerSelected = true;
            if (GameBox.SelectedIndex != 7)
                DiscordButton.Visibility = Visibility.Visible;
            else
                DiscordButton.Visibility = Visibility.Collapsed;
            if (GameFilterBox.SelectedIndex == 1)
                SZFilters.Visibility = Visibility.Visible;
            else
                SZFilters.Visibility = Visibility.Collapsed;
        }

        private static int page = 1;
        private void DecrementPage(object sender, RoutedEventArgs e)
        {
            --page;
            RefreshFilter();
        }
        private void IncrementPage(object sender, RoutedEventArgs e)
        {
            ++page;
            RefreshFilter();
        }
        private void BrowserRefresh(object sender, RoutedEventArgs e)
        {
            if (!selected)
                InitializeBrowser();
            else
                RefreshFilter();
        }
        private static bool filterSelect;
        private static bool searched = false;
        private async void RefreshFilter()
        {
            NSFWCheckbox.IsEnabled = false;
            ZsJsonCheckbox.IsEnabled = false;
            ColorZCheckbox.IsEnabled = false;
            SearchBar.IsEnabled = false;
            SearchButton.IsEnabled = false;
            GameFilterBox.IsEnabled = false;
            FilterBox.IsEnabled = false;
            TypeBox.IsEnabled = false;
            CatBox.IsEnabled = false;
            SubCatBox.IsEnabled = false;
            PageLeft.IsEnabled = false;
            PageRight.IsEnabled = false;
            PageBox.IsEnabled = false;
            PerPageBox.IsEnabled = false;
            ClearCacheButton.IsEnabled = false;
            ErrorPanel.Visibility = Visibility.Collapsed;
            filterSelect = true;
            PageBox.SelectedValue = page;
            filterSelect = false;
            Page.Text = $"Page {page}";
            LoadingBar.Visibility = Visibility.Visible;
            FeedBox.Visibility = Visibility.Collapsed;
            PageLeft.IsEnabled = false;
            var search = searched ? SearchBar.Text : null;
            await FeedGenerator.GetFeed(page, 0, (TypeFilter)TypeBox.SelectedIndex, (FeedFilter)FilterBox.SelectedIndex, (GameBananaCategory)CatBox.SelectedItem,
                (GameBananaCategory)SubCatBox.SelectedItem, (PerPageBox.SelectedIndex + 1) * 10, (bool)NSFWCheckbox.IsChecked, search, (bool)ZsJsonCheckbox.IsChecked, (bool)ColorZCheckbox.IsChecked);
            if (FeedGenerator.CurrentFeed?.Records != null)
            {
                if (NSFWCheckbox.IsChecked != true)
                    FeedBox.ItemsSource = new ObservableCollection<GameBananaRecord>(FeedGenerator.CurrentFeed.Records.Where(r => !r.IsNsfw));
                else
                    FeedBox.ItemsSource = FeedGenerator.CurrentFeed.Records;
            }
            else
            {
                FeedBox.ItemsSource = null;
            }
            if (FeedGenerator.error)
            {
                LoadingBar.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Visible;
                if (FeedGenerator.exception.Message.Contains("JSON tokens"))
                {
                    BrowserMessage.Text = "Uh oh! Striverum failed to deserialize the GameBanana feed.";
                    return;
                }
                switch (Regex.Match(FeedGenerator.exception.Message, @"\d+").Value)
                {
                    case "443":
                        BrowserMessage.Text = "Your internet connection is down.";
                        break;
                    case "500":
                    case "503":
                    case "504":
                        BrowserMessage.Text = "GameBanana's servers are down.";
                        break;
                    default:
                        BrowserMessage.Text = FeedGenerator.exception.Message;
                        break;
                }
                return;
            }
            if (page < FeedGenerator.CurrentFeed.TotalPages)
                PageRight.IsEnabled = true;
            if (page != 1)
                PageLeft.IsEnabled = true;
            if (FeedBox.Items.Count > 0)
            {
                FeedBox.ScrollIntoView(FeedBox.Items[0]);
                FeedBox.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Collapsed;
                BrowserMessage.Visibility = Visibility.Visible;
                BrowserMessage.Text = "Striverum couldn't find any mods.";
            }
            PageBox.ItemsSource = Enumerable.Range(1, (int)(FeedGenerator.CurrentFeed.TotalPages));

            LoadingBar.Visibility = Visibility.Collapsed;
            CatBox.IsEnabled = true;
            SubCatBox.IsEnabled = true;
            TypeBox.IsEnabled = true;
            FilterBox.IsEnabled = true;
            PageBox.IsEnabled = true;
            PerPageBox.IsEnabled = true;
            GameFilterBox.IsEnabled = true;
            SearchBar.IsEnabled = true;
            SearchButton.IsEnabled = true;
            NSFWCheckbox.IsEnabled = true;
            ZsJsonCheckbox.IsEnabled = true;
            ColorZCheckbox.IsEnabled = true;
            ClearCacheButton.IsEnabled = true;
        }

        private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                if (!searched || FilterBox.SelectedIndex != 3)
                {
                    filterSelect = true;
                    var temp = FilterBox.SelectedIndex;
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = temp;
                    filterSelect = false;
                }
                SearchBar.Clear();
                searched = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void PerPageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                page = 1;
                RefreshFilter();
            }
        }
        private void GameFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                SearchBar.Clear();
                searched = false;
                if (GameFilterBox.SelectedIndex != 7)
                    DiscordButton.Visibility = Visibility.Visible;
                else
                    DiscordButton.Visibility = Visibility.Collapsed;
                if (GameFilterBox.SelectedIndex == 1)
                    SZFilters.Visibility = Visibility.Visible;
                else
                    SZFilters.Visibility = Visibility.Collapsed;
                filterSelect = true;
                if (!searched)
                {
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                }
                // Set categories
                if (cats[0][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[0][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[0][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[0][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void TypeFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                SearchBar.Clear();
                searched = false;
                filterSelect = true;
                if (!searched)
                {
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                }
                // Set categories
                if (cats[0][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[0][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[0][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[0][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void MainFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                SearchBar.Clear();
                searched = false;
                filterSelect = true;
                if (!searched)
                {
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                }
                // Set Categories
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[0][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[0][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void SubFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                SearchBar.Clear();
                searched = false;
                page = 1;
                RefreshFilter();
            }
        }

        private void InitBrowseSectionBar()
        {
            if (BrowseSections.Count > 0)
            {
                UpdateBrowseSectionCounts();
                return;
            }
            BrowseSections.Add(new BrowseSectionItem { Name = "All", FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_LayerGroup, IsSelected = true });
            BrowseSections.Add(new BrowseSectionItem { Name = "Skins", IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/60ce8d5f438ee.png"), FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_UserAlt });
            BrowseSections.Add(new BrowseSectionItem { Name = "Sounds", IconPath = GetCategoryIconPath("pack://application:,,,/Assets/Icons/sounds.png"), FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_VolumeUp });
            BrowseSections.Add(new BrowseSectionItem { Name = "WiPs", IconPath = GetCategoryIconPath("pack://application:,,,/Assets/Icons/wips.png"), FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Wrench });
            BrowseSections.Add(new BrowseSectionItem { Name = "Other/Misc", IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/62829c5f9e5f8.png"), FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_QuestionCircle });
            BrowseSections.Add(new BrowseSectionItem { Name = "GUIs", IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/6101d57ac2be9.png"), FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Palette });
            BrowseSections.Add(new BrowseSectionItem { Name = "Gameplay", IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/616169f346a22.png"), FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Cog });
            BrowseSections.Add(new BrowseSectionItem { Name = "Stages", IconPath = GetCategoryIconPath("https://images.gamebanana.com/img/ico/ModCategory/6168e12ead8c9.png"), FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Tree });
            UpdateBrowseSectionCounts();
        }

        private static Dictionary<string, int> _gbCategoryCounts = new();

        private static void LoadGbCategoryCounts()
        {
            if (_gbCategoryCounts != null && _gbCategoryCounts.Count > 0) return;
            string[] possiblePaths = new[]
            {
                $@"{Global.assemblyLocation}{Global.s}Cache{Global.s}GbCategoryCounts_GGS.json",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "GbCategoryCounts_GGS.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Cache", "GbCategoryCounts_GGS.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Cache", "GbCategoryCounts_GGS.json")
            };

            foreach (var p in possiblePaths)
            {
                try
                {
                    if (File.Exists(p))
                    {
                        string json = File.ReadAllText(p);
                        var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                        if (dict != null && dict.Count > 0)
                        {
                            _gbCategoryCounts = dict;
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        private static int GetGbCountForSection(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return 0;
            LoadGbCategoryCounts();
            if (_gbCategoryCounts.TryGetValue($"Section_{sectionName}", out int count))
                return count;
            if (_gbCategoryCounts.TryGetValue(sectionName, out count))
                return count;
            return 0;
        }

        private static int GetGbCountForCategory(string catName, int? catId = null)
        {
            LoadGbCategoryCounts();
            if (catId.HasValue && _gbCategoryCounts.TryGetValue($"ID_{catId.Value}", out int idCount))
                return idCount;
            if (!string.IsNullOrEmpty(catName))
            {
                if (_gbCategoryCounts.TryGetValue($"Mod_{catName}", out int modCount))
                    return modCount;
                if (_gbCategoryCounts.TryGetValue(catName, out int count))
                    return count;
                var match = _gbCategoryCounts.FirstOrDefault(kvp => CategoryMatches(kvp.Key, catName) || CategoryMatches(kvp.Key, $"Mod_{catName}"));
                if (match.Value > 0) return match.Value;
            }
            return 0;
        }

        public void UpdateBrowseSectionCounts()
        {
            if (BrowseSections == null) return;
            LoadGbCategoryCounts();

            foreach (var bSec in BrowseSections)
            {
                bSec.ModCount = GetGbCountForSection(bSec.Name);
            }
        }

        public void UpdateBrowseCategoryPillCounts()
        {
            if (BrowseCategoryPills == null || BrowseCategoryPills.Count == 0) return;
            LoadGbCategoryCounts();

            foreach (var pill in BrowseCategoryPills)
            {
                pill.ModCount = GetGbCountForCategory(pill.Name, pill.GbCategory?.ID);
            }
        }

        private int GetModCountForName(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;

            var matchingSecCat = _sections?.SelectMany(s => s.Categories)?.FirstOrDefault(c => CategoryMatches(c.Name, name));
            var matchingSubCat = _sections?.SelectMany(s => s.Categories)?.SelectMany(c => c.Subcategories)?.FirstOrDefault(sub => CategoryMatches(sub.Name, name));

            if (matchingSubCat != null) return matchingSubCat.ModCount;
            if (matchingSecCat != null) return matchingSecCat.ModCount;

            if (Global.ModList != null)
            {
                return Global.ModList.Count(m =>
                    (!string.IsNullOrEmpty(m.subcategory) && CategoryMatches(m.subcategory, name)) ||
                    (m.tags != null && m.tags.Any(t => CategoryMatches(t, name)))
                );
            }

            return 0;
        }

        private void AnimateBrowseToCategories(string sectionName, string iconPath, FontAwesome5.EFontAwesomeIcon faIcon, int modCount)
        {
            BrowseCurrentSectionTitle.Text = sectionName;
            BrowseCurrentSectionCount.Text = modCount.ToString();
            if (!string.IsNullOrEmpty(iconPath))
            {
                try
                {
                    BrowseCurrentSectionImage.Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute));
                    BrowseCurrentSectionImage.Visibility = Visibility.Visible;
                    BrowseCurrentSectionIcon.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    BrowseCurrentSectionIcon.Icon = faIcon;
                    BrowseCurrentSectionIcon.Visibility = Visibility.Visible;
                    BrowseCurrentSectionImage.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                BrowseCurrentSectionIcon.Icon = faIcon;
                BrowseCurrentSectionIcon.Visibility = Visibility.Visible;
                BrowseCurrentSectionImage.Visibility = Visibility.Collapsed;
            }

            BrowseCategoryPanel.Visibility = Visibility.Visible;
            var duration = TimeSpan.FromMilliseconds(200);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            var secOpacity = new DoubleAnimation(1, 0, duration) { EasingFunction = ease };
            var secTranslate = new DoubleAnimation(0, -30, duration) { EasingFunction = ease };
            secOpacity.Completed += (s, e) => BrowseSectionPanel.Visibility = Visibility.Collapsed;
            BrowseSectionPanel.BeginAnimation(UIElement.OpacityProperty, secOpacity);
            BrowseSectionTranslate.BeginAnimation(TranslateTransform.XProperty, secTranslate);

            BrowseCategoryPanel.Opacity = 0;
            BrowseCategoryTranslate.X = 30;
            var catOpacity = new DoubleAnimation(0, 1, duration) { EasingFunction = ease };
            var catTranslate = new DoubleAnimation(30, 0, duration) { EasingFunction = ease };
            BrowseCategoryPanel.BeginAnimation(UIElement.OpacityProperty, catOpacity);
            BrowseCategoryTranslate.BeginAnimation(TranslateTransform.XProperty, catTranslate);

            BrowseFilterScroll?.ScrollToHorizontalOffset(0);
        }

        private void AnimateBrowseToSections()
        {
            BrowseSectionPanel.Visibility = Visibility.Visible;
            var duration = TimeSpan.FromMilliseconds(200);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            var catOpacity = new DoubleAnimation(1, 0, duration) { EasingFunction = ease };
            var catTranslate = new DoubleAnimation(0, 30, duration) { EasingFunction = ease };
            catOpacity.Completed += (s, e) => BrowseCategoryPanel.Visibility = Visibility.Collapsed;
            BrowseCategoryPanel.BeginAnimation(UIElement.OpacityProperty, catOpacity);
            BrowseCategoryTranslate.BeginAnimation(TranslateTransform.XProperty, catTranslate);

            BrowseSectionPanel.Opacity = 0;
            BrowseSectionTranslate.X = -30;
            var secOpacity = new DoubleAnimation(0, 1, duration) { EasingFunction = ease };
            var secTranslate = new DoubleAnimation(-30, 0, duration) { EasingFunction = ease };
            BrowseSectionPanel.BeginAnimation(UIElement.OpacityProperty, secOpacity);
            BrowseSectionTranslate.BeginAnimation(TranslateTransform.XProperty, secTranslate);

            BrowseFilterScroll?.ScrollToHorizontalOffset(0);
        }

        private void BrowseBackButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in BrowseCategoryPills) p.IsSelected = false;
            filterSelect = true;
            if (TypeBox.SelectedIndex == 2 || TypeBox.SelectedIndex == 1) // Sounds or WiPs
            {
                CatBox.SelectedIndex = 0;
            }
            else
            {
                SubCatBox.SelectedIndex = 0;
            }
            filterSelect = false;
            page = 1;
            RefreshFilter();
            AnimateBrowseToSections();
        }

        private void BrowseSectionBadge_Click(object sender, MouseButtonEventArgs e)
        {
            foreach (var p in BrowseCategoryPills) p.IsSelected = false;
            filterSelect = true;
            if (TypeBox.SelectedIndex == 2 || TypeBox.SelectedIndex == 1) // Sounds or WiPs
            {
                CatBox.SelectedIndex = 0;
            }
            else
            {
                SubCatBox.SelectedIndex = 0;
            }
            filterSelect = false;
            page = 1;
            RefreshFilter();
        }

        private void BrowseSectionButton_Click(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as BrowseSectionItem;
            if (item == null) return;

            foreach (var s in BrowseSections) s.IsSelected = (s == item);
            SelectBrowseSection(item.Name);

            if (BrowseCategoryPills.Count > 0)
            {
                AnimateBrowseToCategories(item.Name, item.IconPath, item.FaIcon, item.ModCount);
            }
            else
            {
                AnimateBrowseToSections();
            }
        }

        private void SelectBrowseSection(string sectionName)
        {
            if (!selected || cats == null || !cats.ContainsKey(0))
            {
                InitializeBrowser();
                return;
            }

            SearchBar.Clear();
            searched = false;
            filterSelect = true;

            BrowseCategoryPills.Clear();

            if (sectionName == "All")
            {
                TypeBox.SelectedIndex = 0; // Mods
                if (cats[0].ContainsKey(TypeFilter.Mods) && cats[0][TypeFilter.Mods].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[0][TypeFilter.Mods].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                AnimateBrowseToSections();
            }
            else if (sectionName == "Sounds")
            {
                TypeBox.SelectedIndex = 2; // Sounds
                if (cats[0].ContainsKey(TypeFilter.Sounds))
                {
                    var soundList = cats[0][TypeFilter.Sounds].Where(x => x.RootID == 0).OrderBy(y => y.ID).ToList();
                    CatBox.ItemsSource = All.Concat(soundList);
                    CatBox.SelectedIndex = 0;
                    SubCatBox.ItemsSource = None;
                    SubCatBox.SelectedIndex = 0;

                    foreach (var sc in soundList)
                    {
                        var matchingSecCat = _sections.SelectMany(s => s.Categories).FirstOrDefault(c => CategoryMatches(c.Name, sc.Name));
                        string icon = matchingSecCat?.IconPath ?? GetCategoryIconPath(sc.Icon?.OriginalString);
                        BrowseCategoryPills.Add(new BrowseCategoryItem
                        {
                            Name = sc.Name,
                            IconPath = icon,
                            FaIcon = string.IsNullOrEmpty(icon) ? FontAwesome5.EFontAwesomeIcon.Solid_Cog : FontAwesome5.EFontAwesomeIcon.Solid_Tag,
                            GbCategory = sc,
                            ModCount = GetGbCountForCategory(sc.Name, sc.ID),
                            IsSelected = false
                        });
                    }
                }
            }
            else if (sectionName == "WiPs")
            {
                TypeBox.SelectedIndex = 1; // WiPs
                if (cats[0].ContainsKey(TypeFilter.WiPs))
                {
                    var wipList = cats[0][TypeFilter.WiPs].Where(x => x.RootID == 0).OrderBy(y => y.ID).ToList();
                    CatBox.ItemsSource = All.Concat(wipList);
                    CatBox.SelectedIndex = 0;
                    SubCatBox.ItemsSource = None;
                    SubCatBox.SelectedIndex = 0;

                    foreach (var wc in wipList)
                    {
                        var matchingSecCat = _sections.SelectMany(s => s.Categories).FirstOrDefault(c => CategoryMatches(c.Name, wc.Name));
                        string icon = matchingSecCat?.IconPath ?? GetCategoryIconPath(wc.Icon?.OriginalString);
                        BrowseCategoryPills.Add(new BrowseCategoryItem
                        {
                            Name = wc.Name,
                            IconPath = icon,
                            FaIcon = string.IsNullOrEmpty(icon) ? FontAwesome5.EFontAwesomeIcon.Solid_Wrench : FontAwesome5.EFontAwesomeIcon.Solid_Tag,
                            GbCategory = wc,
                            ModCount = GetGbCountForCategory(wc.Name, wc.ID),
                            IsSelected = false
                        });
                    }
                }
            }
            else
            {
                // Mods sections: Skins, Other/Misc, GUIs, Gameplay, Stages
                TypeBox.SelectedIndex = 0; // Mods
                if (cats[0].ContainsKey(TypeFilter.Mods))
                {
                    var modList = cats[0][TypeFilter.Mods].Where(x => x.RootID == 0).OrderBy(y => y.ID).ToList();
                    CatBox.ItemsSource = All.Concat(modList);

                    var matchedCat = modList.FirstOrDefault(c => CategoryMatches(c.Name, sectionName));
                    if (matchedCat != null)
                    {
                        CatBox.SelectedItem = matchedCat;

                        // Check for subcategories (e.g. Skins -> characters)
                        var subCats = cats[0][TypeFilter.Mods].Where(x => x.RootID == matchedCat.ID).OrderBy(y => y.ID).ToList();
                        if (subCats.Count > 0)
                        {
                            SubCatBox.ItemsSource = All.Concat(subCats);
                            SubCatBox.SelectedIndex = 0;

                            foreach (var sc in subCats)
                            {
                                var matchingSecCat = _sections.SelectMany(s => s.Categories).FirstOrDefault(c => CategoryMatches(c.Name, sc.Name));
                                string icon = matchingSecCat?.IconPath ?? GetCategoryIconPath(sc.Icon?.OriginalString);
                                BrowseCategoryPills.Add(new BrowseCategoryItem
                                {
                                    Name = sc.Name,
                                    IconPath = icon,
                                    FaIcon = string.IsNullOrEmpty(icon) ? FontAwesome5.EFontAwesomeIcon.Solid_User : FontAwesome5.EFontAwesomeIcon.Solid_Tag,
                                    GbCategory = sc,
                                    ModCount = GetGbCountForCategory(sc.Name, sc.ID),
                                    IsSelected = false
                                });
                            }
                        }
                        else
                        {
                            SubCatBox.ItemsSource = None;
                            SubCatBox.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        CatBox.SelectedIndex = 0;
                        SubCatBox.ItemsSource = None;
                        SubCatBox.SelectedIndex = 0;
                    }
                }
            }

            filterSelect = false;
            page = 1;
            RefreshFilter();
        }

        private void BrowseCategoryPill_Click(object sender, MouseButtonEventArgs e)
        {
            var pill = (sender as FrameworkElement)?.DataContext as BrowseCategoryItem;
            if (pill == null) return;

            if (pill.IsSelected)
            {
                // Toggle off
                pill.IsSelected = false;
                filterSelect = true;
                if (TypeBox.SelectedIndex == 2 || TypeBox.SelectedIndex == 1) // Sounds or WiPs
                {
                    CatBox.SelectedIndex = 0; // All
                }
                else
                {
                    SubCatBox.SelectedIndex = 0; // All
                }
                filterSelect = false;
                page = 1;
                RefreshFilter();
                return;
            }

            foreach (var p in BrowseCategoryPills)
            {
                p.IsSelected = (p == pill);
            }

            filterSelect = true;
            if (TypeBox.SelectedIndex == 2 || TypeBox.SelectedIndex == 1) // Sounds or WiPs
            {
                foreach (var item in CatBox.Items)
                {
                    if (item is GameBananaCategory gbCat && (gbCat.ID == pill.GbCategory?.ID || CategoryMatches(gbCat.Name, pill.Name)))
                    {
                        CatBox.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                foreach (var item in SubCatBox.Items)
                {
                    if (item is GameBananaCategory gbCat && (gbCat.ID == pill.GbCategory?.ID || CategoryMatches(gbCat.Name, pill.Name)))
                    {
                        SubCatBox.SelectedItem = item;
                        break;
                    }
                }
            }
            filterSelect = false;
            page = 1;
            RefreshFilter();
        }

        private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as UniformGrid;
            grid.Columns = (int)grid.ActualWidth / 400 + 1;
        }
        private void OnResize(object sender, RoutedEventArgs e)
        {
            BigScreenshot.MaxHeight = ActualHeight - 240;
        }

        private void PageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                page = (int)PageBox.SelectedValue;
                RefreshFilter();
            }
        }
        private void NSFWCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                if (searched)
                {
                    filterSelect = true;
                    FilterBox.ItemsSource = FilterBoxList;
                    FilterBox.SelectedIndex = 1;
                    filterSelect = false;
                }
                SearchBar.Clear();
                searched = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void ClearCache(object sender, RoutedEventArgs e)
        {
            FeedGenerator.ClearCache();
            RefreshFilter();
        }

        private void OnFirstOpen()
        {
            if (!Global.config.CurrentGame.Equals("Dragon Ball FighterZ", StringComparison.InvariantCultureIgnoreCase) && !Global.config.Configs[Global.config.CurrentGame].FirstOpen)
            {
                ChoiceWindow choice;
                var store = Global.config.CurrentGame.Equals("Kingdom Hearts III", StringComparison.InvariantCultureIgnoreCase) ? "Epic Games" : "Steam";
                var choices = new List<Choice>();
                if (Global.config.CurrentGame.Equals("Shin Megami Tensei V", StringComparison.InvariantCultureIgnoreCase))
                {
                    choices.Add(new Choice()
                    {
                        OptionText = "Launch through Emulator",
                        OptionSubText = "Launches the game through Yuzu or Ryujinx emulator",
                        Index = 0
                    });
                    choices.Add(new Choice()
                    {
                        OptionText = "Build for Hardware",
                        OptionSubText = "Builds mod output without launching the game",
                        Index = 1
                    });
                }
                else if (Global.config.CurrentGame.Equals("Granblue Fantasy Versus Rising", StringComparison.InvariantCultureIgnoreCase))
                {
                    choices.Add(new Choice()
                    {
                        OptionText = "Launch through Executable",
                        OptionSubText = "Launches the executable directly with -fileopenlog argument",
                        Index = 0
                    });
                    choices.Add(new Choice()
                    {
                        OptionText = $"Launch through {store}",
                        OptionSubText = $"Uses the {store} shortcut to launch. Need to manually add -fileopenlog to\nManage > Properties... > General > Launch Options on Steam for mods to work",
                        Index = 1
                    });
                }
                else
                {
                    choices.Add(new Choice()
                    {
                        OptionText = "Launch through Executable",
                        OptionSubText = "Launches the executable directly",
                        Index = 0,
                        FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Play
                    });
                    choices.Add(new Choice()
                    {
                        OptionText = $"Launch through {store}",
                        OptionSubText = $"Uses the {store} shortcut to launch",
                        Index = 1,
                        FaIcon = store == "Steam" ? FontAwesome5.EFontAwesomeIcon.Brands_Steam : FontAwesome5.EFontAwesomeIcon.Solid_Gamepad
                    });
                }
                if (Global.config.CurrentGame.Equals("The King of Fighters XV", StringComparison.InvariantCultureIgnoreCase)
                    || Global.config.CurrentGame.Equals("MultiVersus", StringComparison.InvariantCultureIgnoreCase))
                {
                    choices.Add(new Choice()
                    {
                        OptionText = $"Launch through Epic Games",
                        OptionSubText = $"Uses the Epic Games shortcut to launch",
                        Index = 2
                    });
                }
                choice = new ChoiceWindow(choices, $"Launcher Options for {Global.config.CurrentGame}");
                choice.ShowDialog();
                if (choice.choice != null)
                {
                    Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex = (int)choice.choice;
                    LauncherOptionsBox.SelectedIndex = (int)choice.choice;
                }
                else if (!Global.config.CurrentGame.Equals("Shin Megami Tensei V", StringComparison.InvariantCultureIgnoreCase))
                {
                    Global.logger.WriteLine($"No launch option chosen, defaulting to {store} shortcut", LoggerType.Warning);
                    Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex = 1;
                    LauncherOptionsBox.SelectedIndex = 1;
                }
                else
                {
                    Global.logger.WriteLine($"No launch option chosen, defaulting to emulator setup", LoggerType.Warning);
                    Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex = 0;
                    LauncherOptionsBox.SelectedIndex = 0;
                }
                Global.config.Configs[Global.config.CurrentGame].FirstOpen = true;
                Global.UpdateConfig();
                Global.logger.WriteLine($"If you want to switch the Launch Method, use the dropdown box to the right of the Launch Button", LoggerType.Info);
            }
        }
        private bool handle;
        private void GameBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || Global.config == null)
                return;
            handle = true;

        }
        private void LauncherOptionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LauncherOptionsBox.SelectedIndex == 1
                && Global.config.CurrentGame.Equals("Shin Megami Tensei V", StringComparison.InvariantCultureIgnoreCase))
                LaunchButton.Content = "Build";
            else
                LaunchButton.Content = "Launch";
            if (!handle)
            {
                Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex = LauncherOptionsBox.SelectedIndex;
                Global.UpdateConfig();
            }
        }
        private void LoadoutsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            // Change the loadout
            else if (LoadoutsBox.SelectedItem != null)
            {
                Global.config.Configs[Global.config.CurrentGame].CurrentLoadout = LoadoutsBox.SelectedItem.ToString();

                // Create loadout if it doesn't exist
                if (!Global.config.Configs[Global.config.CurrentGame].Loadouts.ContainsKey(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout))
                    Global.config.Configs[Global.config.CurrentGame].Loadouts.Add(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout, new());
                else if (Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout] == null)
                    Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout] = new();

                Global.ModList = Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout];
                Refresh();
                Global.logger.WriteLine($"Loadout changed to {LoadoutsBox.SelectedItem}", LoggerType.Info);
            }
        }
        private void EditLoadouts_Click(object sender, RoutedEventArgs e)
        {
            var choices = new List<Choice>();
            choices.Add(new Choice()
            {
                OptionText = "Add New Loadout - Enabled",
                OptionSubText = "Adds a new loadout starting with all mods enabled in alphanumeric order",
                Index = 0,
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_PlusCircle
            });
            choices.Add(new Choice()
            {
                OptionText = "Add New Loadout - Disabled",
                OptionSubText = "Adds a new loadout starting with all mods disabled in alphanumeric order",
                Index = 1,
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_MinusCircle
            });
            choices.Add(new Choice()
            {
                OptionText = "Copy Loadout",
                OptionSubText = "Creates a copy of current loadout",
                Index = 2,
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Copy
            });
            choices.Add(new Choice()
            {
                OptionText = $"Rename Current Loadout",
                OptionSubText = $"Changes the name of the current loadout",
                Index = 3,
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_Edit
            });
            choices.Add(new Choice()
            {
                OptionText = $"Delete Current Loadout",
                OptionSubText = $"Deletes current loadout and switches to first available one",
                Index = 4,
                FaIcon = FontAwesome5.EFontAwesomeIcon.Solid_TrashAlt
            });
            Dispatcher.Invoke(() =>
            {
                var choice = new ChoiceWindow(choices, $"Loadout Options for {Global.config.CurrentGame}");
                choice.ShowDialog();
                if (choice.choice != null)
                {
                    switch ((int)choice.choice)
                    {
                        // Add new loadout
                        case 0:
                            var newLoadoutWindow = new EditWindow(null, false);
                            newLoadoutWindow.ShowDialog();
                            if (!String.IsNullOrEmpty(newLoadoutWindow.loadout))
                            {
                                Global.LoadoutItems.Add(newLoadoutWindow.loadout);
                                LoadoutsBox.SelectedItem = newLoadoutWindow.loadout;
                            }
                            ShowMetadata(null);
                            break;
                        // Add new Blank loadout
                        case 1:
                            var blankLoadoutWindow = new EditWindow(null, false);
                            blankLoadoutWindow.ShowDialog();
                            if (!String.IsNullOrEmpty(blankLoadoutWindow.loadout))
                            {
                                Global.LoadoutItems.Add(blankLoadoutWindow.loadout);
                                LoadoutsBox.SelectedItem = blankLoadoutWindow.loadout;
                                Global.config.Configs[Global.config.CurrentGame].DisableAllMods(blankLoadoutWindow.loadout);
                            }
                            ShowMetadata(null);
                            break;
                        // Copy current loadout
                        case 2:
                            if (true)
                            {
                                // Insert new name at index of original loadout
                                Global.LoadoutItems.Insert(Global.LoadoutItems.IndexOf(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout) + 1, Global.config.Configs[Global.config.CurrentGame].CurrentLoadout + " - Copy");
                                // Copy over current loadout
                                Global.config.Configs[Global.config.CurrentGame].Loadouts.Add(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout + " - Copy", Global.ModList);
                                // Delete current loadout
                                // Trigger selection changed event
                                LoadoutsBox.SelectedItem = Global.config.Configs[Global.config.CurrentGame].CurrentLoadout + " - Copy";
                            }
                            break;
                        // Rename current loadout
                        case 3:
                            var renameLoadoutWindow = new EditWindow(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout, false);
                            renameLoadoutWindow.ShowDialog();
                            if (!String.IsNullOrEmpty(renameLoadoutWindow.loadout))
                            {
                                // Insert new name at index of original loadout
                                Global.LoadoutItems.Insert(Global.LoadoutItems.IndexOf(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout), renameLoadoutWindow.loadout);
                                // Copy over current loadout
                                Global.config.Configs[Global.config.CurrentGame].Loadouts.Add(renameLoadoutWindow.loadout, Global.ModList);
                                // Delete current loadout
                                Global.LoadoutItems.Remove(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout);
                                Global.config.Configs[Global.config.CurrentGame].Loadouts.Remove(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout);
                                // Trigger selection changed event
                                LoadoutsBox.SelectedItem = renameLoadoutWindow.loadout;
                            }
                            break;
                        // Delete current loadout
                        case 4:
                            if (Global.config.Configs[Global.config.CurrentGame].Loadouts.Count == 1)
                            {
                                Global.logger.WriteLine("Unable to delete current loadout since there is only one", LoggerType.Error);
                                return;
                            }
                            else
                            {
                                Global.LoadoutItems.Remove(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout);
                                Global.config.Configs[Global.config.CurrentGame].Loadouts.Remove(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout);
                                // Triggers selection changed event
                                LoadoutsBox.SelectedIndex = 0;
                            }
                            ShowMetadata(null);
                            break;
                    }
                }
            });
        }
        private void GameBox_DropDownClosed(object sender, EventArgs e)
        {
            if (handle)
            {
                if (GameBox.SelectedIndex == 7)
                    DiscordButton.Visibility = Visibility.Collapsed;
                else
                    DiscordButton.Visibility = Visibility.Visible;
                if (GameFilterBox.SelectedIndex == 1)
                    SZFilters.Visibility = Visibility.Visible;
                else
                    SZFilters.Visibility = Visibility.Collapsed;
                Global.config.CurrentGame = (((GameBox.SelectedValue as ComboBoxItem).Content as StackPanel).Children[1] as TextBlock).Text.Trim().Replace(":", String.Empty);

                if (!Global.config.Configs.ContainsKey(Global.config.CurrentGame))
                {
                    Global.ModList = new();
                    Global.config.Configs.Add(Global.config.CurrentGame, new());
                    Global.config.Configs[Global.config.CurrentGame].CurrentLoadout = "Default";
                    Global.config.Configs[Global.config.CurrentGame].Loadouts = new();
                    Global.config.Configs[Global.config.CurrentGame].Loadouts.Add(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout, new());
                }
                else
                {
                    if (String.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout))
                        Global.config.Configs[Global.config.CurrentGame].CurrentLoadout = "Default";
                    if (Global.config.Configs[Global.config.CurrentGame].Loadouts == null)
                        Global.config.Configs[Global.config.CurrentGame].Loadouts = new();
                    if (!Global.config.Configs[Global.config.CurrentGame].Loadouts.ContainsKey(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout))
                        if (Global.config.Configs[Global.config.CurrentGame].ModList != null && Global.config.Configs[Global.config.CurrentGame].CurrentLoadout == "Default")
                        {
                            Global.config.Configs[Global.config.CurrentGame].Loadouts.Add(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout, Global.config.Configs[Global.config.CurrentGame].ModList);
                            Global.config.Configs[Global.config.CurrentGame].ModList = null;
                        }
                        else
                            Global.config.Configs[Global.config.CurrentGame].Loadouts.Add(Global.config.Configs[Global.config.CurrentGame].CurrentLoadout, new());
                    else if (Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout] == null)
                        if (Global.config.Configs[Global.config.CurrentGame].ModList != null && Global.config.Configs[Global.config.CurrentGame].CurrentLoadout == "Default")
                        {
                            Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout] = Global.config.Configs[Global.config.CurrentGame].ModList;
                            Global.config.Configs[Global.config.CurrentGame].ModList = null;
                        }
                        else
                            Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout] = new();
                    Global.ModList = Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout];
                }
                Global.LoadoutItems = new ObservableCollection<String>(Global.config.Configs[Global.config.CurrentGame].Loadouts.Keys);
                LoadoutsBox.ItemsSource = Global.LoadoutItems;
                LoadoutsBox.SelectedItem = Global.config.Configs[Global.config.CurrentGame].CurrentLoadout;
                var currentModDirectory = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}";
                Directory.CreateDirectory(currentModDirectory);
                ModsWatcher.Path = currentModDirectory;
                Global.logger.WriteLine($"Game switched to {Global.config.CurrentGame}", LoggerType.Info);
                RefreshAll();
                Refresh();
                Global.UpdateConfig();
                if (String.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].ModsFolder)
                    || String.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].Launcher) || !File.Exists(Global.config.Configs[Global.config.CurrentGame].Launcher))
                {
                    LaunchButton.IsEnabled = false;
                    Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
                }
                else
                {
                    LaunchButton.IsEnabled = true;
                }
                if (Global.config.CurrentGame.Equals("Shin Megami Tensei V", StringComparison.InvariantCultureIgnoreCase))
                {
                    LauncherOptions[0] = "Emulator";
                    LauncherOptions[1] = "Hardware";
                    if (LauncherOptions.Count > 2)
                        LauncherOptions.RemoveAt(2);
                }
                else if (Global.config.CurrentGame.Equals("Kingdom Hearts III", StringComparison.InvariantCultureIgnoreCase))
                {
                    LauncherOptions[0] = "Executable";
                    LauncherOptions[1] = "Epic Games";
                    if (LauncherOptions.Count > 2)
                        LauncherOptions.RemoveAt(2);
                }
                else if (Global.config.CurrentGame.Equals("The King of Fighters XV", StringComparison.InvariantCultureIgnoreCase))
                {
                    LauncherOptions[0] = "Executable";
                    LauncherOptions[1] = "Steam";
                    LauncherOptions.Add("Epic Games");
                }
                else
                {
                    LauncherOptions[0] = "Executable";
                    LauncherOptions[1] = "Steam";
                    if (LauncherOptions.Count > 2)
                        LauncherOptions.RemoveAt(2);
                }

                OnFirstOpen();

                if (Global.config.CurrentGame.Equals("Dragon Ball FighterZ", StringComparison.InvariantCultureIgnoreCase))
                    LauncherOptionsBox.IsEnabled = false;
                else
                    LauncherOptionsBox.IsEnabled = true;
                LauncherOptionsBox.ItemsSource = LauncherOptions;
                LauncherOptionsBox.SelectedIndex = Global.config.Configs[Global.config.CurrentGame].LauncherOptionIndex;

                DescriptionWindow.Document = defaultFlow;
                var bitmap = new BitmapImage(new Uri("pack://application:,,,/Striverum;component/Assets/Striverumpreview.png"));
                Preview.Source = bitmap;
                PreviewBG.Source = null;

                Global.logger.WriteLine("Checking for updates...", LoggerType.Info);
                GameBox.IsEnabled = false;
                ModListView.IsEnabled = false;
                ConfigButton.IsEnabled = false;
                LaunchButton.IsEnabled = false;
                OpenModsButton.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                EditLoadoutsButton.IsEnabled = false;
                LoadoutsBox.IsEnabled = false;
                LauncherOptionsBox.IsEnabled = false;
                App.Current.Dispatcher.Invoke(() =>
                {
                    ModUpdater.CheckForUpdates($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}", this);
                });
                handle = false;
            }
        }

        private void Search()
        {
            if (!filterSelect && IsLoaded && !String.IsNullOrWhiteSpace(SearchBar.Text))
            {
                filterSelect = true;
                FilterBox.ItemsSource = FilterBoxListWhenSearched;
                FilterBox.SelectedIndex = 3;
                NSFWCheckbox.IsChecked = true;
                // Set categories
                if (cats[0][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[0][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[0][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[0][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                searched = true;
                page = 1;
                RefreshFilter();
            }
        }
        private void SearchBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search();
        }
        private static readonly List<string> FilterBoxList = new string[] { "Featured", "Recent", "Popular" }.ToList();
        private static readonly List<string> FilterBoxListWhenSearched = new string[] { "Featured", "Recent", "Popular", "- - -" }.ToList();

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

                private void ModListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                foreach (var item in ModListView.SelectedItems)
                {
                    if (item is Mod mod)
                    {
                        mod.enabled = !mod.enabled;
                    }
                }
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (MediaPanel != null && MediaPanel.Visibility == Visibility.Visible)
                {
                    MediaPanel.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    return;
                }
                if (DescPanel != null && DescPanel.Visibility == Visibility.Visible)
                {
                    DescPanel.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    return;
                }
            }
        }

        private void ModListViewSearch()
        {
            RefreshModList();
        }

        private async void SortAlphabeticallyAndGroupEnabled_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                if (btn.Name == "SortAlphabetically")
                {
                    var choice = MessageBox.Show($"Confirm sorting all mods alphanumerically?", "Striverum", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (choice == MessageBoxResult.No)
                        return;
                    // Sort alphanumerically
                    Global.ModList = new ObservableCollection<Mod>(Global.ModList.ToList().OrderBy(x => x.name, new NaturalSort()).ToList());
                    Global.logger.WriteLine("Sorted alphanumerically!", LoggerType.Info);
                }
                else if (btn.Name == "GroupEnabled")
                {
                    var choice = MessageBox.Show($"Confirm moving all enabled mods to the top?", "Striverum", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (choice == MessageBoxResult.No)
                        return;
                    // Move all enabled mods to top
                    Global.ModList = new ObservableCollection<Mod>(Global.ModList.ToList().OrderByDescending(x => x.enabled).ToList());
                    Global.logger.WriteLine("Moved all enabled mods to the top!", LoggerType.Info);
                }
                else if (btn.Name == "SortCategories")
                {
                    Global.ModList = new ObservableCollection<Mod>(Global.ModList.ToList().OrderBy(x => x.cat).ThenBy(x => x.subcategory).ToList());
                    Global.logger.WriteLine("Sorted by Categories!", LoggerType.Info);
                }
                await Task.Run(() =>
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ModListView.ItemsSource = Global.ModList;
                        RefreshModList();
                    });
                });
                Global.config.Configs[Global.config.CurrentGame].ModList = Global.ModList;
            }
            if (e != null) e.Handled = true;
        }

        private void ZsJsonCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
                RefreshFilter();
        }

        private void ColorZCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
                RefreshFilter();
        }
    }
}

