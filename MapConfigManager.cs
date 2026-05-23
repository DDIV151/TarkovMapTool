using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TarkovMapTool
{
    public class MapConfigItem
    {
        public string MapName { get; set; }
        public string FileName { get; set; }
        // 仿射矩阵参数：gx = A*px + B*py + C, gz = D*px + E*py + F
        public double AffineA { get; set; }
        public double AffineB { get; set; }
        public double AffineC { get; set; }
        public double AffineD { get; set; }
        public double AffineE { get; set; }
        public double AffineF { get; set; }

        public float ScaleX { get; set; } = 0;
        public float ScaleZ { get; set; } = 0;

        [JsonIgnore]
        public bool HasAffine => Math.Abs(AffineA) > 1e-9 || Math.Abs(AffineB) > 1e-9 ||
                                 Math.Abs(AffineD) > 1e-9 || Math.Abs(AffineE) > 1e-9;
    }

    public static class MapConfigManager
    {
        public static List<MapConfigItem> Configs { get; private set; }
        public static string ConfigFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mapconfig.json");
        public static event Action ConfigChanged;

        static MapConfigManager()
        {
            LoadConfig();
        }

        private static void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                Configs = JsonConvert.DeserializeObject<List<MapConfigItem>>(json) ?? GetDefaultConfigs();
            }
            else
            {
                Configs = GetDefaultConfigs();
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(Configs, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
            ConfigChanged?.Invoke();
        }

        public static MapConfigItem GetConfig(string mapName)
        {
            return Configs.FirstOrDefault(c => c.MapName == mapName);
        }

        public static void UpdateAffine(string mapName, double affA, double affB, double affC,
                                        double affD, double affE, double affF,
                                        float scaleX, float scaleZ)
        {
            var item = Configs.FirstOrDefault(x => x.MapName == mapName); // 改名避免冲突
            if (item != null)
            {
                item.AffineA = affA; item.AffineB = affB; item.AffineC = affC;
                item.AffineD = affD; item.AffineE = affE; item.AffineF = affF;
                item.ScaleX = scaleX;
                item.ScaleZ = scaleZ;
                SaveConfig();
            }
        }

        private static List<MapConfigItem> GetDefaultConfigs()
        {
            return new List<MapConfigItem>
            {
                new MapConfigItem { MapName = "中心区", FileName = "Center.png" },
                new MapConfigItem { MapName = "塔科夫街区", FileName = "Streets.png" },
                new MapConfigItem { MapName = "海岸线", FileName = "Shoreline.png" },
                new MapConfigItem { MapName = "立交桥", FileName = "Interchange.png" },
                new MapConfigItem { MapName = "森林", FileName = "Woods.png" },
                new MapConfigItem { MapName = "灯塔", FileName = "Lighthouse.png" },
                new MapConfigItem { MapName = "储备站", FileName = "Reserve.png" },
                new MapConfigItem { MapName = "海关", FileName = "Customs.png" },
                new MapConfigItem { MapName = "工厂", FileName = "Factory.png" },
                new MapConfigItem { MapName = "实验室", FileName = "Labs.png" },
                new MapConfigItem { MapName = "迷宫", FileName = "Labyrinth.png" },
                new MapConfigItem { MapName = "码头", FileName = "Docks.png" }
            };
        }
    }
}