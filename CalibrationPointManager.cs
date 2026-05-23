using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TarkovMapTool
{
    public class CalibrationPoint
    {
        public string MapName { get; set; }
        public int ImageX { get; set; }   // 图片像素坐标 X
        public int ImageY { get; set; }   // 图片像素坐标 Y
        public float GameX { get; set; }  // 对应游戏坐标 X
        public float GameZ { get; set; }  // 对应游戏坐标 Z
    }

    public static class CalibrationPointManager
    {
        private static readonly string PointsFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "calibration_points.json");

        private static List<CalibrationPoint> _allPoints;
        public static List<CalibrationPoint> AllPoints => _allPoints;

        static CalibrationPointManager()
        {
            Load();
        }

        private static void Load()
        {
            if (File.Exists(PointsFilePath))
            {
                string json = File.ReadAllText(PointsFilePath);
                _allPoints = JsonConvert.DeserializeObject<List<CalibrationPoint>>(json) ?? new List<CalibrationPoint>();
            }
            else
            {
                _allPoints = new List<CalibrationPoint>();
            }
        }

        public static void Save()
        {
            string json = JsonConvert.SerializeObject(_allPoints, Formatting.Indented);
            File.WriteAllText(PointsFilePath, json);
        }

        // 添加一个校准点
        public static void AddPoint(string mapName, Point imgPixel, float gameX, float gameZ)
        {
            _allPoints.Add(new CalibrationPoint
            {
                MapName = mapName,
                ImageX = imgPixel.X,
                ImageY = imgPixel.Y,
                GameX = gameX,
                GameZ = gameZ
            });
            Save();
        }

        // 获取某地图所有校准点
        public static List<CalibrationPoint> GetPoints(string mapName)
        {
            return _allPoints.Where(p => p.MapName == mapName).ToList();
        }

        // 清除某地图所有校准点
        public static void ClearPoints(string mapName)
        {
            _allPoints.RemoveAll(p => p.MapName == mapName);
            Save();
        }
    }
}